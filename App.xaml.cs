using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.ViewModels.Pages;
using ShinroKensakuDesktop.ViewModels.Windows;
using ShinroKensakuDesktop.Views.Pages;
using ShinroKensakuDesktop.Views.Windows;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace ShinroKensakuDesktop;

public partial class App : Application
{
	private IServiceProvider? _serviceProvider;

	protected override void OnStartup ( StartupEventArgs e )
	{
		var services = new ServiceCollection();

		services.AddNavigationViewPageProvider ( );
		services.AddSingleton<INavigationService, NavigationService> ( );
		services.AddSingleton<MainWindow> ( );
		services.AddSingleton<MainWindowViewModel> ( );

		services.AddSingleton<DashBoardPage> ( );

		services.AddSingleton<SettingsPage> ( );
		services.AddSingleton<SettingsPageViewModel> ( );

		services.AddTransient<SearchPage> ( );

		_serviceProvider = services.BuildServiceProvider ( );

		var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
		mainWindow.Show ( );

		base.OnStartup ( e );
	}
}