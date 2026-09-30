using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using ShinroKensakuDesktop.Views.Pages;
namespace ShinroKensakuDesktop.ViewModels.Pages;

public partial class DashboardPageViewModel(INavigationService navigation, SearchPageViewModel search)
    : AnalyticsPageViewModel(navigation, search)
{
    protected override bool PreserveYear => false;
    [RelayCommand] private void OpenAnalytics() => Navigation.Navigate(typeof(AnalyticsPage));
    [RelayCommand] private void OpenComparison() => Navigation.Navigate(typeof(ComparisonPage));
}
