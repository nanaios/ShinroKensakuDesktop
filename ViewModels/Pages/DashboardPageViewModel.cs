using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Views.Pages;
using Wpf.Ui;
using ShinroKensakuDesktop.Models;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class DashboardPageViewModel ( INavigationService navigation, SearchPageViewModel search,
		Func<Task<List<DashboardGroup>>>? load = null )
		: AnalyticsPageViewModel ( navigation, search, load )
	{
		protected override bool PreserveYear => false;

		[ RelayCommand ]
		private void OpenAnalytics ( ) => Navigation.Navigate ( typeof(AnalyticsPage) );

		[ RelayCommand ]
		private void OpenComparison ( ) => Navigation.Navigate ( typeof(ComparisonPage) );
	}
}
