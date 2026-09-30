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
		private List<DashboardGroup> groups = [ ];
		protected INavigationService Navigation => navigation;
		protected virtual bool PreserveYear => true;
		[ ObservableProperty ] public partial List<ChartBar> KindBars { get; set; } = [ ];
		[ ObservableProperty ] public partial List<ChartBar> MethodBars { get; set; } = [ ];
		[ ObservableProperty ] public partial List<ChartBar> TrendBars { get; set; } = [ ];
		[ ObservableProperty ] public partial int? BaselineYear { get; set; }
		[ ObservableProperty ] public partial List<YearComparison> Comparisons { get; set; } = [ ];
		public string YearCaption => SelectedYear.HasValue ? $"{SelectedYear}年度" : "対象年度なし";
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
		partial void OnIsBusyChanged ( bool value ) => OpenSearchCommand.NotifyCanExecuteChanged ( );

		[ RelayCommand ]
		public async Task RefreshAsync ( )
		{
			IsBusy = true;
			StatusText = "データを読み込んでいます…";
			try
			{
				List<DashboardGroup> loaded = await ( load ?? DashboardDataProvider.LoadAsync ) ( );
				int? previous = SelectedYear;
				groups = loaded;
				Years = groups.Select ( x => x.Year ).Distinct ( ).OrderDescending ( ).ToList ( );
				SelectedYear = PreserveYear && previous.HasValue && Years.Contains ( previous.Value )
					? previous
					: Years.Cast<int?> ( ).FirstOrDefault ( );
				BaselineYear = BaselineYear.HasValue && Years.Contains ( BaselineYear.Value )
					? BaselineYear
					: Years.Skip ( 1 ).Cast<int?> ( ).FirstOrDefault ( ) ?? SelectedYear;
				UpdateSummary ( );
				Trends = groups.GroupBy ( x => x.Year ).OrderByDescending ( x => x.Key )
					.Select ( x => new DashboardSummary ( $"{x.Key}年度", x.Sum ( y => y.Count ) ) ).ToList ( );
				TrendBars = ChartData.Bars ( Trends.AsEnumerable ( ).Reverse ( ) );
				StatusText = groups.Count == 0 ? "登録されている受験記録はありません。" : $"最終更新 {DateTime.Now:yyyy/MM/dd HH:mm}";
			}
			catch ( Exception )
			{
				groups = [ ];
				KindBars = MethodBars = TrendBars = [ ];
				BaselineYear = null;
				Comparisons = [ ];
				Years = [ ];
				SelectedYear = null;
				Methods = [ ];
				Trends = [ ];
				TotalText = StudyText = WorkText = "—";
				StatusText = "データを取得できませんでした。データベースの接続を確認し、再読み込みしてください。";
			}
			finally { IsBusy = false; }
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
			KindBars = SelectedYear.HasValue
				? ChartData.Bars ( new [ ]
				{
					new DashboardSummary ( "進学", rows.Where ( x => x.Kind == "進学" ).Sum ( x => x.Count ) ),
					new DashboardSummary ( "就職", rows.Where ( x => x.Kind == "就職" ).Sum ( x => x.Count ) )
				} )
				: [ ];
			MethodBars = ChartData.Bars ( Methods );
			Comparisons = ChartData.Compare ( groups, BaselineYear, SelectedYear );
			OnPropertyChanged ( nameof(YearCaption) );
			OpenSearchCommand.NotifyCanExecuteChanged ( );
		}

		private bool CanOpenSearch ( ) => SelectedYear.HasValue && !IsBusy;

		[ RelayCommand ( CanExecute = nameof(CanOpenSearch) ) ]
		private async Task OpenSearchAsync ( )
		{
			search.SearchTargetName = null;
			search.SelectedYear = SelectedYear?.ToString ( );
			search.ExamMethodComboBoxSelectedItem = null;
			search.DepartmentComboBoxSelectedItem = null;
			if ( navigation.Navigate ( typeof(SearchPage) ) )
			{
				await search.ExecuteSearchCommand.ExecuteAsync ( null );
			}
		}
	}
}