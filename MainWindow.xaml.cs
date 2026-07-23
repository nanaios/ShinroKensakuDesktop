using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : FluentWindow
{
	public MainWindow (
		MainWindowViewModel viewModel,
		INavigationService navigationService,
		IServiceProvider serviceProvider )
	{
		InitializeComponent ( );
		DataContext = this;
		ViewModel = viewModel;

		var pageProvider = serviceProvider.GetRequiredService<INavigationViewPageProvider>();
		MainView.SetPageProviderService ( pageProvider );
		navigationService.SetNavigationControl ( MainView );

		Loaded += ( _, _ ) => navigationService.Navigate ( typeof ( HomePage ) );
	}

	public MainWindowViewModel ViewModel { get; }
}

public partial class MainWindowViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Title { get; set; } = "Test App";

	[ObservableProperty]
	public partial double SideMenuItemSize { get; set; } = 24;

}