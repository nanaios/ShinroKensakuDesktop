using ShinroKensakuDesktop.Pages.TopPage;
using ShinroKensakuDesktop.Utils;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Windows.MainWindow;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : FluentWindow
{
	private readonly INavigationService navigationService = new NavigationService(new SimplePageProvider());

	public MainWindow ( )
	{
		InitializeComponent ( );

		// ナビゲーションサービスを設定
		navigationService.SetNavigationControl ( MainView );
		Loaded += ( _, _ ) => navigationService.Navigate ( typeof ( TopPage ) );

		// アプリケーションのアクセントカラーとテーマを設定。総合科学の校章の色に合わせる
		ApplicationAccentColorManager.Apply (
			Color.FromArgb ( 0xFF, 0x12, 0x2b, 0x89 ),
			ApplicationTheme.Light
		);
	}
}