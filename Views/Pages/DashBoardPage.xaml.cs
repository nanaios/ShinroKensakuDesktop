using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Pages
{
	public partial class DashBoardPage : Page
	{
		public DashBoardPage ( DashboardPageViewModel viewModel )
		{
			InitializeComponent ( );
			DataContext = viewModel;
			Loaded += async ( _, _ ) => await viewModel.RefreshCommand.ExecuteAsync ( null );
		}

		/* The link is registered in the settings; with none yet, the button takes the user there. */
		private void OpenReports_Click ( object sender, RoutedEventArgs e )
		{
			Uri? link = UserPreferences.ParseReportLink ( UserPreferences.LoadReportLink ( ) );
			if ( link != null )
			{
				ExternalLink.Open ( link );
			}
			else if ( DataContext is DashboardPageViewModel viewModel )
			{
				viewModel.OpenSettingsCommand.Execute ( null );
			}
		}

		private void ReportButton_ToolTipOpening ( object sender, ToolTipEventArgs e ) =>
			ReportButton.ToolTip = UserPreferences.ParseReportLink ( UserPreferences.LoadReportLink ( ) ) == null
				? "リンクが未登録です。押すと設定を開きます（詳細設定をオンにして登録）"
				: "受験報告書をブラウザで開く";
	}
}
