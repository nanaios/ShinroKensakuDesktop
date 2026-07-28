using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.Pages.DashBoard;
using ShinroKensakuDesktop.Pages.Settings;
using ShinroKensakuDesktop.Windows.MainWindow;
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
		services.AddSingleton<Windows.MainWindow.ViewModel> ( );

		services.AddSingleton<DashBoard> ( );
		services.AddSingleton<Settings> ( );

		_serviceProvider = services.BuildServiceProvider ( );

		var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
		mainWindow.Show ( );

		base.OnStartup ( e );
	}
}