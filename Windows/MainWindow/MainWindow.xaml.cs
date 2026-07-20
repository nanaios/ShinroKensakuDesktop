using System.Windows;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public partial class MainWindow : Window
{
	public static MainWindow? Instance { get; private set; }
	public MainWindow ( )
	{
		InitializeComponent ( );
	}
}