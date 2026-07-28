using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.Pages.DashBoard;
using ShinroKensakuDesktop.Utils;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Windows.MainWindow;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : FluentWindow
{
	private readonly NavigationService navigationService = new(new SimplePageProvider());

	public MainWindow (
		ViewModel viewModel,
		INavigationService navigationService,
		IServiceProvider serviceProvider )
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		InitializeComponent ( );

		var pageProvider = serviceProvider.GetRequiredService<INavigationViewPageProvider>();
		MainView.SetPageProviderService ( pageProvider );
		navigationService.SetNavigationControl ( MainView );

		Loaded += ( _, _ ) => navigationService.Navigate ( typeof ( DashBoard ) );
	}

	public ViewModel ViewModel { get; }
}