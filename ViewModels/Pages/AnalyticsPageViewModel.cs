using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Views.Pages;
using Wpf.Ui;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class AnalyticsPageViewModel (
		INavigationService navigation,
		SearchPageViewModel search,
		Func<Task<List<DashboardGroup>>>? load = null ) : ObservableObject
	{
		public const int RecentYearCount = 5;
		private List<DashboardGroup> groups = [ ];
		private bool settingRange;
		protected INavigationService Navigation => navigation;
		protected virtual bool PreserveYear => true;
		[ ObservableProperty ] public partial SeriesChart KindTrendChart { get; set; } = SeriesChart.Empty;
		[ ObservableProperty ] public partial SeriesChart MethodTrendChart { get; set; } = SeriesChart.Empty;
		[ ObservableProperty ] public partial SeriesChart MethodBreakdownChart { get; set; } = SeriesChart.Empty;
		[ ObservableProperty ] public partial int? RangeStartYear { get; set; }
		[ ObservableProperty ] public partial int? RangeEndYear { get; set; }
		[ ObservableProperty ] public partial List<int> RangeYears { get; set; } = [ ];
		[ ObservableProperty ] public partial string RangeTotalText { get; set; } = "—";
		[ ObservableProperty ] public partial string RangeStudyText { get; set; } = "—";
		[ ObservableProperty ] public partial string RangeWorkText { get; set; } = "—";
		[ ObservableProperty ] public partial int? BaselineYear { get; set; }
		[ ObservableProperty ] public partial List<YearComparison> Comparisons { get; set; } = [ ];
		public string YearCaption => SelectedYear.HasValue ? $"{SelectedYear}年度" : "対象年度なし";

		public string RangeCaption => ( RangeYears.Count == 0
			? "対象年度なし"
			: RangeYears.Count == 1
				? $"{RangeYears [ 0 ]}年度"
				: $"{RangeYears [ 0 ]}～{RangeYears [ ^1 ]}年度（{RangeYears.Count}年度分）" ) + "・" +
		                              ( DepartmentFilter ?? AllDepartments );

		public const string AllDepartments = "すべての学科";
		[ ObservableProperty ] public partial List<string> Departments { get; set; } = [ AllDepartments ];

		/* Null comes from the selector while its items are replaced, and means every department. */
		[ ObservableProperty ] public partial string? SelectedDepartment { get; set; } = AllDepartments;

		/* Search filters by class, not department, so a department choice cannot be carried over. */
		public string PointHint => DepartmentFilter == null
			? "点をクリックすると、その年度の検索・明細を開きます。"
			: "点をクリックすると、その年度の検索・明細を開きます（学科の絞り込みは検索に引き継がれません）。";

		private string? DepartmentFilter =>
			SelectedDepartment is null or AllDepartments ? null : SelectedDepartment;

		partial void OnSelectedDepartmentChanged ( string? value )
		{
			if ( !settingRange )
			{
				UpdateRange ( );
			}
		}

		[ ObservableProperty ] public partial List<int> Years { get; set; } = [ ];
		[ ObservableProperty ] public partial int? SelectedYear { get; set; }
		[ ObservableProperty ] public partial string TotalText { get; set; } = "—";
		[ ObservableProperty ] public partial string StudyText { get; set; } = "—";
		[ ObservableProperty ] public partial string WorkText { get; set; } = "—";
		[ ObservableProperty ] public partial string StatusText { get; set; } = "データを読み込んでいます…";
		[ ObservableProperty ] public partial bool IsBusy { get; set; }
		[ ObservableProperty ] public partial List<DashboardSummary> Methods { get; set; } = [ ];
		[ ObservableProperty ] public partial List<DashboardSummary> Trends { get; set; } = [ ];
		partial void OnBaselineYearChanged ( int? value ) => UpdateSummary ( );

		partial void OnSelectedYearChanged ( int? value ) => UpdateSummary ( );

		partial void OnIsBusyChanged ( bool value )
		{
			OpenSearchCommand.NotifyCanExecuteChanged ( );
			OpenYearSearchCommand.NotifyCanExecuteChanged ( );
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
			StatusText = "データを読み込んでいます…";
			try
			{
				List<DashboardGroup> loaded = await ( load ?? DashboardDataProvider.LoadAsync ) ( );
				groups = loaded;
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
				Trends = groups.GroupBy ( x => x.Year ).OrderByDescending ( x => x.Key )
					.Select ( x => new DashboardSummary ( $"{x.Key}年度", x.Sum ( y => y.Count ) ) ).ToList ( );
				StatusText = groups.Count == 0 ? "登録されている受験記録はありません。" : $"最終更新 {DateTime.Now:yyyy/MM/dd HH:mm}";
			}
			catch ( Exception )
			{
				groups = [ ];
				BaselineYear = null;
				Comparisons = [ ];
				Years = [ ];
				SelectedYear = null;
				SetDepartments ( [ ], null );
				SetRange ( null, null );
				Methods = [ ];
				Trends = [ ];
				TotalText = StudyText = WorkText = "—";
				StatusText = "データを取得できませんでした。データベースの接続を確認し、再読み込みしてください。";
			}
			finally { IsBusy = false; }
		}

		[ RelayCommand ]
		private void ShowAllYears ( ) => SetRange ( Years.Cast<int?> ( ).LastOrDefault ( ), Years.Cast<int?> ( ).FirstOrDefault ( ) );

		[ RelayCommand ]
		private void ShowRecentYears ( ) =>
			SetRange ( Years.Take ( RecentYearCount ).Cast<int?> ( ).LastOrDefault ( ), Years.Cast<int?> ( ).FirstOrDefault ( ) );

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
				Departments = [ AllDepartments, .. departments ];
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
			RangeTotalText = empty ? "—" : rows.Sum ( x => x.Count ).ToString ( "N0" );
			RangeStudyText = empty ? "—" : rows.Where ( x => x.Kind == "進学" ).Sum ( x => x.Count ).ToString ( "N0" );
			RangeWorkText = empty ? "—" : rows.Where ( x => x.Kind == "就職" ).Sum ( x => x.Count ).ToString ( "N0" );
			KindTrendChart = empty ? SeriesChart.Empty : ChartData.KindTrend ( groups, RangeYears, department );
			MethodTrendChart = empty ? SeriesChart.Empty : ChartData.MethodTrend ( groups, RangeYears, department );
			MethodBreakdownChart = empty ? SeriesChart.Empty : ChartData.MethodBreakdown ( groups, RangeYears, department );
			OnPropertyChanged ( nameof(RangeCaption) );
			OnPropertyChanged ( nameof(PointHint) );
		}

		private void UpdateSummary ( )
		{
			List<DashboardGroup> rows = groups.Where ( x => x.Year == SelectedYear ).ToList ( );
			TotalText = rows.Sum ( x => x.Count ).ToString ( "N0" );
			StudyText = rows.Where ( x => x.Kind == "進学" ).Sum ( x => x.Count ).ToString ( "N0" );
			WorkText = rows.Where ( x => x.Kind == "就職" ).Sum ( x => x.Count ).ToString ( "N0" );
			Methods = rows.GroupBy ( x => x.Method )
				.Select ( x => new DashboardSummary ( x.Key, x.Sum ( y => y.Count ) ) )
				.OrderByDescending ( x => x.Count ).ThenBy ( x => x.Label ).ToList ( );
			Comparisons = ChartData.Compare ( groups, BaselineYear, SelectedYear );
			OnPropertyChanged ( nameof(YearCaption) );
			OpenSearchCommand.NotifyCanExecuteChanged ( );
		}

		private bool CanOpenSearch ( ) => SelectedYear.HasValue && CanOpenYearSearch ( SelectedYear );

		[ RelayCommand ( CanExecute = nameof(CanOpenSearch) ) ]
		private Task OpenSearchAsync ( ) => OpenYearSearchAsync ( SelectedYear );

		private bool CanOpenYearSearch ( int? year ) => year.HasValue && !IsBusy && search.CanEditConditions;

		/* Clicking a point on a year chart opens the search for that year. */
		[ RelayCommand ( CanExecute = nameof(CanOpenYearSearch) ) ]
		private async Task OpenYearSearchAsync ( int? year )
		{
			if ( !CanOpenYearSearch ( year ) )
			{
				return;
			}

			if ( navigation.Navigate ( typeof(SearchPage) ) )
			{
				search.ClearAllConditions ( );
				search.SelectedYear = year?.ToString ( );
				await search.ExecuteSearchCommand.ExecuteAsync ( null );
			}
		}
	}
}