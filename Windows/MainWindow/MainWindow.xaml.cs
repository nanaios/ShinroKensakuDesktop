using ShinroKensakuDesktop.Pages.DashBoard;
using ShinroKensakuDesktop.Utils;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Windows.MainWindow;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : FluentWindow
{
	private readonly NavigationService navigationService = new(new SimplePageProvider());

	public MainWindow ( )
	{
		InitializeComponent ( );

		// ナビゲーションサービスを設定
		navigationService.SetNavigationControl ( MainView );
		Loaded += ( _, _ ) => navigationService.Navigate ( typeof ( DashBoard ) );
	}
}