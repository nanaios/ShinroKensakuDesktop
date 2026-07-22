using System.Windows;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public partial class MainWindow : Window
{
	public MainWindow ( )
	{
		InitializeComponent ( );
	}
}

public class SideMenuAnimation
{
	public bool IsOpen { get; set; }
	public double SideMenuWidth { get; set; }
	public double CurrentTime { get; set; }
	public double AnimationDuration { get; set; }
}