using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.Pages;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
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

		Loaded += ( _, _ ) =>
		{
			navigationService.Navigate ( typeof ( HomePage ) );
		};

		ApplicationAccentColorManager.Apply (
			Color.FromArgb ( 0xFF, 0x12, 0x2b, 0x89 ),
			ApplicationTheme.Light
		);
	}

	public MainWindowViewModel ViewModel { get; }
}

public partial class MainWindowViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Title { get; set; } = "総合科学進路検索システム";

	[ObservableProperty]
	public partial double SideMenuItemIconSize { get; set; } = 32;

	[ObservableProperty]
	public partial double SideMenuItemFontSize { get; set; } = 24;

}