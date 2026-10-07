using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.ViewModels.Pages;
using ShinroKensakuDesktop.ViewModels.Windows;
using ShinroKensakuDesktop.Views.Pages;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Windows
{
	/// <summary>
	///     MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : FluentWindow
	{
		public MainWindow (
			MainWindowViewModel viewModel,
			INavigationService navigationService,
			IServiceProvider serviceProvider,
			SettingsPageViewModel settings )
		{
			ViewModel = viewModel;
			DataContext = viewModel;
			InitializeComponent ( );
			Width = Math.Min ( Width, SystemParameters.WorkArea.Width );
			Height = Math.Min ( Height, SystemParameters.WorkArea.Height );
			settings.AttachWindow ( this );

			INavigationViewPageProvider pageProvider =
				serviceProvider.GetRequiredService<INavigationViewPageProvider> ( );
			MainView.SetPageProviderService ( pageProvider );
			navigationService.SetNavigationControl ( MainView );

			Loaded += ( _, _ ) => navigationService.Navigate ( typeof(DashBoardPage) );
		}

		public MainWindowViewModel ViewModel { get; }
	}
}