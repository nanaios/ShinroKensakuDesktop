using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.Utils;
using ShinroKensakuDesktop.ViewModels.Windows;
using ShinroKensakuDesktop.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Windows;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : FluentWindow
{
	private readonly NavigationService navigationService = new(new SimplePageProvider());

	public MainWindow (
		MainWindowViewModel viewModel,
		INavigationService navigationService,
		IServiceProvider serviceProvider )
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		InitializeComponent ( );

		var pageProvider = serviceProvider.GetRequiredService<INavigationViewPageProvider>();
		MainView.SetPageProviderService ( pageProvider );
		navigationService.SetNavigationControl ( MainView );

		Loaded += ( _, _ ) => navigationService.Navigate ( typeof ( DashBoardPage ) );
	}

	public MainWindowViewModel ViewModel { get; }
}