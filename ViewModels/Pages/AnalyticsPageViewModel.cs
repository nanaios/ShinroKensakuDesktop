using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Views.Pages;
using Wpf.Ui;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	/* Shared by the graph page (year range and department) and the comparison page (two years).
	   The dashboard derives from it and always shows the latest year. */
	public partial class AnalyticsPageViewModel (
		INavigationService navigation,
		SearchPageViewModel search,
		Func<Task<List<DashboardGroup>>>? load = null ) : ObservableObject
	{
		public const int RecentYearCount = 5;
		public const string AllDepartments = "すべての学科";
		public const string TotalLabel = "延べ受験数";
		public const string LoadErrorText = "データベースの接続を確認して、再読み込みしてください。";
		private List<DashboardGroup> groups = [ ];
		private bool settingRange;

		/* True only while the first data is outstanding: cards show placeholders, never after a result or failure. */
		private bool awaitingData;
		protected INavigationService Navigation => navigation;
		protected virtual bool PreserveYear => true;

		/* ---- Status ---- */
		[ ObservableProperty ] public partial bool IsBusy { get; set; }
		[ ObservableProperty ] public partial bool HasData { get; set; }
		[ ObservableProperty ] public partial bool HasLoaded { get; set; }
		[ ObservableProperty ] public partial string UpdatedText { get; set; } = "";
		[ ObservableProperty ] public partial string ErrorText { get; set; } = "";
		public bool HasError => ErrorText.Length > 0;
		public bool IsEmpty => HasLoaded && !IsBusy && !HasError && !HasData;
		public bool IsFirstLoad => IsBusy && !HasData;

		/* ---- Year range and department (進路グラフ) ---- */
		[ ObservableProperty ] public partial SeriesChart KindTrendChart { get; set; } = SeriesChart.Empty;
		[ ObservableProperty ] public partial SeriesChart MethodTrendChart { get; set; } = SeriesChart.Empty;
		[ ObservableProperty ] public partial List<BreakdownGroup> MethodBreakdown { get; set; } = [ ];
		[ ObservableProperty ] public partial bool ShowMethodShare { get; set; }
		[ ObservableProperty ] public partial int? RangeStartYear { get; set; }
		[ ObservableProperty ] public partial int? RangeEndYear { get; set; }
		[ ObservableProperty ] public partial List<int> RangeYears { get; set; } = [ ];
		[ ObservableProperty ] public partial string RangeTotalText { get; set; } = "—";
		[ ObservableProperty ] public partial string RangeStudyText { get; set; } = "—";
		[ ObservableProperty ] public partial string RangeWorkText { get; set; } = "—";
		[ ObservableProperty ] public partial List<StatItem> RangeStats { get; set; } = Dashes ( );
		[ ObservableProperty ] public partial List<string> Departments { get; set; } = [ AllDepartments ];

		/* Null comes from the selector while its items are replaced, and means every department. */
		[ ObservableProperty ] public partial string? SelectedDepartment { get; set; } = AllDepartments;

		public bool IsRecentRange => Years.Count > 0 && RangeEndYear == Years [ 0 ] &&
		                             RangeStartYear == Years.Take ( RecentYearCount ).Last ( ) && !IsAllRange;

		public bool IsAllRange => Years.Count > 0 && RangeEndYear == Years [ 0 ] && RangeStartYear == Years [ ^1 ];

		private string? DepartmentFilter =>
			SelectedDepartment is null or AllDepartments ? null : SelectedDepartment;

		/* ---- One year and two years (ダッシュボード・年度比較) ---- */
		[ ObservableProperty ] public partial List<int> Years { get; set; } = [ ];
		[ ObservableProperty ] public partial int? SelectedYear { get; set; }
		[ ObservableProperty ] public partial int? BaselineYear { get; set; }
		[ ObservableProperty ] public partial string TotalText { get; set; } = "—";
		[ ObservableProperty ] public partial string StudyText { get; set; } = "—";
		[ ObservableProperty ] public partial string WorkText { get; set; } = "—";
		[ ObservableProperty ] public partial List<StatItem> SummaryStats { get; set; } = Dashes ( );
		[ ObservableProperty ] public partial List<BreakdownGroup> MethodGroups { get; set; } = [ ];
		[ ObservableProperty ] public partial List<YearBar> YearBars { get; set; } = [ ];
		[ ObservableProperty ] public partial List<YearComparison> Comparisons { get; set; } = [ ];
		[ ObservableProperty ] public partial List<StatItem> ComparisonStats { get; set; } = Dashes ( );
		[ ObservableProperty ] public partial List<MethodComparisonRow> MethodComparisons { get; set; } = [ ];
		public string YearTitle => SelectedYear.HasValue ? $"{SelectedYear}年度" : "";
		public bool IsLatestYear => SelectedYear.HasValue && Years.Count > 0 && SelectedYear == Years [ 0 ];
		public bool IsSameYear => SelectedYear.HasValue && SelectedYear == BaselineYear;
		public string SelectedSearchText => SelectedYear.HasValue ? $"{SelectedYear}年度の明細" : "明細";
		public string BaselineSearchText => BaselineYear.HasValue ? $"{BaselineYear}年度の明細" : "明細";

		/* The registered year before the selected one; years can be missing, so it is not always year − 1. */
		private int? PreviousYear => SelectedYear.HasValue
			? Years.Where ( y => y < SelectedYear ).Cast<int?> ( ).FirstOrDefault ( )
			: null;

		partial void OnErrorTextChanged ( string value )
		{
			OnPropertyChanged ( nameof(HasError) );
			OnPropertyChanged ( nameof(IsEmpty) );
		}

		partial void OnHasDataChanged ( bool value )
		{
			OnPropertyChanged ( nameof(IsEmpty) );
			OnPropertyChanged ( nameof(IsFirstLoad) );
		}

		partial void OnHasLoadedChanged ( bool value ) => OnPropertyChanged ( nameof(IsEmpty) );

		partial void OnSelectedDepartmentChanged ( string? value )
		{
			if ( !settingRange )
			{
				UpdateRange ( );
			}
		}

		partial void OnBaselineYearChanged ( int? value ) => UpdateSummary ( );

		partial void OnSelectedYearChanged ( int? value ) => UpdateSummary ( );

		partial void OnIsBusyChanged ( bool value )
		{
			OpenSearchCommand.NotifyCanExecuteChanged ( );
			OpenBaselineSearchCommand.NotifyCanExecuteChanged ( );
			OpenYearSearchCommand.NotifyCanExecuteChanged ( );
			OnPropertyChanged ( nameof(IsEmpty) );
			OnPropertyChanged ( nameof(IsFirstLoad) );
		}

		/* A reversed range is corrected by moving the other end, so the latest choice is always kept. */
		partial void OnRangeStartYearChanged ( int? value )
		{
			if ( value > RangeEndYear )
			{
				RangeEndYear = value;
			}

			if ( !settingRange )
			{
				UpdateRange ( );
			}
		}

		partial void OnRangeEndYearChanged ( int? value )
		{
			if ( value < RangeStartYear )
			{
				RangeStartYear = value;
			}

			if ( !settingRange )
			{
				UpdateRange ( );
			}
		}

		[ RelayCommand ]
		public async Task RefreshAsync ( )
		{
			if ( IsBusy )
			{
				return;
			}

			int? previous = SelectedYear;
			int? previousBaseline = BaselineYear;
			int? previousStart = RangeStartYear;
			int? previousEnd = RangeEndYear;
			string? previousDepartment = SelectedDepartment;
			IsBusy = true;
			awaitingData = !HasData;
			if ( awaitingData )
			{
				SummaryStats = Placeholders ( );
				RangeStats = Placeholders ( );
				ComparisonStats = Placeholders ( );
			}

			try
			{
				List<DashboardGroup> loaded;
				try { loaded = await ( load ?? DashboardDataProvider.LoadAsync ) ( ); }
				finally { awaitingData = false; }

				groups = loaded;
				HasData = groups.Count > 0;
				Years = groups.Select ( x => x.Year ).Distinct ( ).OrderDescending ( ).ToList ( );
				SelectedYear = PreserveYear && previous.HasValue && Years.Contains ( previous.Value )
					? previous
					: Years.Cast<int?> ( ).FirstOrDefault ( );
				BaselineYear = previousBaseline.HasValue && Years.Contains ( previousBaseline.Value )
					? previousBaseline
					: Years.Skip ( 1 ).Cast<int?> ( ).FirstOrDefault ( ) ?? SelectedYear;
				SetDepartments ( groups.GroupBy ( x => x.Department )
					.OrderBy ( x => x.Min ( y => y.DepartmentOrder ) ).ThenBy ( x => x.Key, StringComparer.Ordinal )
					.Select ( x => x.Key ), previousDepartment );
				bool keepRange = previousStart.HasValue && previousEnd.HasValue &&
				                 Years.Contains ( previousStart.Value ) && Years.Contains ( previousEnd.Value );
				SetRange ( keepRange ? previousStart : Years.Cast<int?> ( ).LastOrDefault ( ),
					keepRange ? previousEnd : Years.Cast<int?> ( ).FirstOrDefault ( ) );
				UpdateSummary ( );
				ErrorText = "";
				UpdatedText = $"{DateTime.Now:HH:mm} 更新";
			}
			catch ( Exception )
			{
				groups = [ ];
				HasData = false;
				BaselineYear = null;
				Years = [ ];
				SelectedYear = null;
				SetDepartments ( [ ], null );
				SetRange ( null, null );
				UpdateSummary ( );
				TotalText = StudyText = WorkText = "—";
				UpdatedText = "";
				ErrorText = LoadErrorText;
			}
			finally
			{
				HasLoaded = true;
				IsBusy = false;
			}
		}

		[ RelayCommand ]
		private void ShowAllYears ( ) => SetRange ( Years.Cast<int?> ( ).LastOrDefault ( ), Years.Cast<int?> ( ).FirstOrDefault ( ) );

		[ RelayCommand ]
		private void ShowRecentYears ( ) =>
			SetRange ( Years.Take ( RecentYearCount ).Cast<int?> ( ).LastOrDefault ( ), Years.Cast<int?> ( ).FirstOrDefault ( ) );

		[ RelayCommand ]
		private void SwapYears ( ) => ( BaselineYear, SelectedYear ) = ( SelectedYear, BaselineYear );

		[ RelayCommand ]
		private void OpenSettings ( ) => navigation.Navigate ( typeof(SettingsPage) );

		/* Clearing the end first keeps the reversed-range correction from moving either end; charts rebuild once. */
		private void SetRange ( int? start, int? end )
		{
			settingRange = true;
			try
			{
				RangeEndYear = null;
				RangeStartYear = start;
				RangeEndYear = end;
			}
			finally { settingRange = false; }

			UpdateRange ( );
		}

		/* The previous department is kept while it still has records; the refresh rebuilds the charts afterwards. */
		private void SetDepartments ( IEnumerable<string> departments, string? keep )
		{
			settingRange = true;
			try
			{
				Departments = [ AllDepartments, .. departments.OrderBy ( x => x == DashboardDataProvider.Unassigned ) ];
				SelectedDepartment = keep != null && Departments.Contains ( keep ) ? keep : AllDepartments;
			}
			finally { settingRange = false; }
		}

		private void UpdateRange ( )
		{
			RangeYears = RangeStartYear.HasValue && RangeEndYear.HasValue
				? Years.Where ( y => y >= RangeStartYear && y <= RangeEndYear ).Order ( ).ToList ( )
				: [ ];
			string? department = DepartmentFilter;
			List<DashboardGroup> rows = ChartData.Filter ( groups, RangeYears, department );
			bool empty = RangeYears.Count == 0;
			int total = rows.Sum ( x => x.Count );
			int study = rows.Where ( x => x.Kind == ChartData.Study ).Sum ( x => x.Count );
			int work = rows.Where ( x => x.Kind == ChartData.Work ).Sum ( x => x.Count );
			RangeTotalText = empty ? "—" : total.ToString ( "N0" );
			RangeStudyText = empty ? "—" : study.ToString ( "N0" );
			RangeWorkText = empty ? "—" : work.ToString ( "N0" );
			RangeStats = empty
				? awaitingData ? Placeholders ( ) : Dashes ( )
				:
				[
					new StatItem ( StatKind.Total, TotalLabel, RangeTotalText )
					{
						Note = RangeYears.Count > 1 ? $"年平均 {total / ( double ) RangeYears.Count:N0}件" : null
					},
					new StatItem ( StatKind.Study, ChartData.Study, RangeStudyText ) { Share = ChartData.Percent ( study, total ) },
					new StatItem ( StatKind.Work, ChartData.Work, RangeWorkText ) { Share = ChartData.Percent ( work, total ) }
				];
			KindTrendChart = empty ? SeriesChart.Empty : ChartData.KindTrend ( groups, RangeYears, department );
			MethodTrendChart = empty ? SeriesChart.Empty : ChartData.MethodTrend ( groups, RangeYears, department );
			MethodBreakdown = empty ? [ ] : ChartData.MethodBreakdown ( rows );
			OnPropertyChanged ( nameof(IsRecentRange) );
			OnPropertyChanged ( nameof(IsAllRange) );
		}

		private void UpdateSummary ( )
		{
			List<DashboardGroup> rows = groups.Where ( x => x.Year == SelectedYear ).ToList ( );
			TotalText = rows.Sum ( x => x.Count ).ToString ( "N0" );
			StudyText = rows.Where ( x => x.Kind == ChartData.Study ).Sum ( x => x.Count ).ToString ( "N0" );
			WorkText = rows.Where ( x => x.Kind == ChartData.Work ).Sum ( x => x.Count ).ToString ( "N0" );
			bool waiting = awaitingData;
			SummaryStats = SelectedYear.HasValue ? YearStats ( SelectedYear.Value, PreviousYear ) :
				waiting ? Placeholders ( ) : Dashes ( );
			MethodGroups = ChartData.MethodBreakdown ( rows );
			YearBars = ChartData.YearBars ( groups, SelectedYear );
			Comparisons = ChartData.Compare ( groups, BaselineYear, SelectedYear );
			ComparisonStats = Comparisons.Count == 0
				? waiting ? Placeholders ( ) : Dashes ( )
				: Comparisons.Select ( c => new StatItem ( ChartData.KindOf ( c.Label ),
					c.Label == "合計" ? TotalLabel : c.Label, c.Current.ToString ( "N0" ) )
				{
					Before = $"{c.Baseline:N0} →",
					Direction = StatItem.DirectionOf ( c.Difference ),
					Change = StatItem.ChangeOf ( c.Difference ),
					ChangeNote = $"（{c.RateText}）"
				} ).ToList ( );
			MethodComparisons = ChartData.CompareMethods ( groups, BaselineYear, SelectedYear );
			OnPropertyChanged ( nameof(YearTitle) );
			OnPropertyChanged ( nameof(IsLatestYear) );
			OnPropertyChanged ( nameof(IsSameYear) );
			OnPropertyChanged ( nameof(SelectedSearchText) );
			OnPropertyChanged ( nameof(BaselineSearchText) );
			OpenSearchCommand.NotifyCanExecuteChanged ( );
			OpenBaselineSearchCommand.NotifyCanExecuteChanged ( );
		}

		/* Totals, shares of the year and the change from the registered year before it. */
		private List<StatItem> YearStats ( int year, int? previous )
		{
			List<DashboardGroup> rows = groups.Where ( x => x.Year == year ).ToList ( );
			List<DashboardGroup>? before = previous.HasValue ? groups.Where ( x => x.Year == previous ).ToList ( ) : null;
			string note = previous == year - 1 ? "前年度" : $"{previous}年度";
			int total = rows.Sum ( x => x.Count );

			StatItem Stat ( StatKind kind, string label, Func<DashboardGroup, bool> match, bool share )
			{
				int value = rows.Where ( match ).Sum ( x => x.Count );
				int? reference = before?.Where ( match ).Sum ( x => x.Count );
				return new StatItem ( kind, label, value.ToString ( "N0" ) )
				{
					Share = share ? ChartData.Percent ( value, total ) : null,
					Direction = reference.HasValue ? StatItem.DirectionOf ( value - reference.Value ) : ChangeDirection.None,
					Change = reference.HasValue ? StatItem.ChangeOf ( value - reference.Value ) : null,
					ChangeNote = reference.HasValue ? $"{note} {reference:N0}" : null
				};
			}

			return
			[
				Stat ( StatKind.Total, TotalLabel, _ => true, false ),
				Stat ( StatKind.Study, ChartData.Study, x => x.Kind == ChartData.Study, true ),
				Stat ( StatKind.Work, ChartData.Work, x => x.Kind == ChartData.Work, true )
			];
		}

		private static List<StatItem> Dashes ( ) =>
		[
			new ( StatKind.Total, TotalLabel, "—" ), new ( StatKind.Study, ChartData.Study, "—" ),
			new ( StatKind.Work, ChartData.Work, "—" )
		];

		private static List<StatItem> Placeholders ( ) =>
		[
			StatItem.Placeholder ( StatKind.Total, TotalLabel ), StatItem.Placeholder ( StatKind.Study, ChartData.Study ),
			StatItem.Placeholder ( StatKind.Work, ChartData.Work )
		];

		private bool CanOpenSearch ( ) => CanOpenYearSearch ( SelectedYear );
		private bool CanOpenBaselineSearch ( ) => CanOpenYearSearch ( BaselineYear );

		/* The comparison and dashboard years are not tied to the graph's department. */
		[ RelayCommand ( CanExecute = nameof(CanOpenSearch) ) ]
		private Task OpenSearchAsync ( ) => OpenSearchForAsync ( SelectedYear, null );

		[ RelayCommand ( CanExecute = nameof(CanOpenBaselineSearch) ) ]
		private Task OpenBaselineSearchAsync ( ) => OpenSearchForAsync ( BaselineYear, null );

		private bool CanOpenYearSearch ( int? year ) => year.HasValue && !IsBusy && search.CanEditConditions;

		/* A point on a year chart (or a year bar) opens that year's records, keeping the graph's department. */
		[ RelayCommand ( CanExecute = nameof(CanOpenYearSearch) ) ]
		private Task OpenYearSearchAsync ( int? year ) => OpenSearchForAsync ( year, DepartmentFilter );

		private async Task OpenSearchForAsync ( int? year, string? department )
		{
			if ( !CanOpenYearSearch ( year ) || !navigation.Navigate ( typeof(SearchPage) ) )
			{
				return;
			}

			/* The department maps to classes, so the class list must be there before the search starts. */
			await search.LoadConditionsAsync ( );
			search.ClearAllConditions ( );
			search.SelectedYear = year?.ToString ( );
			search.SelectedDepartmentName = department;
			await search.ExecuteSearchCommand.ExecuteAsync ( null );
		}
	}
}
