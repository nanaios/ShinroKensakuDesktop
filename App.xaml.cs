using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.ViewModels.Pages;
using ShinroKensakuDesktop.ViewModels.Windows;
using ShinroKensakuDesktop.Views.Pages;
using ShinroKensakuDesktop.Views.Windows;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace ShinroKensakuDesktop
{
	public partial class App : Application
	{
		private ServiceProvider? _serviceProvider;

		protected override void OnStartup ( StartupEventArgs e )
		{
			ServiceCollection services = new( );

			services.AddNavigationViewPageProvider ( );
			services.AddSingleton<INavigationService, NavigationService> ( );
			services.AddSingleton<MainWindow> ( );
			services.AddSingleton<MainWindowViewModel> ( );

			services.AddSingleton<DashBoardPage> ( );
			services.AddSingleton<DashboardPageViewModel> ( );
			services.AddSingleton<AnalyticsPageViewModel> ( );
			services.AddSingleton<AnalyticsPage> ( );
			services.AddSingleton<ComparisonPage> ( );

			services.AddSingleton<SettingsPage> ( );
			services.AddSingleton<SettingsPageViewModel> ( );

			services.AddSingleton<SearchPage> ( );
			services.AddSingleton<SearchPageViewModel> ( );

			_serviceProvider = services.BuildServiceProvider ( );

			MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow> ( );
			mainWindow.Show ( );

			base.OnStartup ( e );
		}

		protected override void OnExit ( ExitEventArgs e )
		{
			_serviceProvider?.Dispose ( );
			base.OnExit ( e );
		}
	}
}
