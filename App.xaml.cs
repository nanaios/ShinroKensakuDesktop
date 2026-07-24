using ShinroKensakuDesktop.Windows.MainWindow;
using System.Windows;

namespace ShinroKensakuDesktop;

public partial class App : Application
{
	protected override void OnStartup ( StartupEventArgs e )
	{
		var mainWindow = new MainWindow();
		mainWindow.Show ( );
		base.OnStartup ( e );
	}
}