using CommunityToolkit.Mvvm.ComponentModel;
using ShinroKensakuDesktop.Controls;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : FluentWindow
{
	public MainWindow ( )
	{
		// システムのテーマの変更を監視する
		// SystemThemeWatcher.Watch ( this );

		InitializeComponent ( );
	}
}

public partial class MainWindowViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Title { get; set; } = "Test App";

	[ObservableProperty]
	public partial object ViewContent { get; set; } = new TopPageContent ( );

	[ObservableProperty]
	public partial double SideMenuItemSize { get; set; } = 24;

}