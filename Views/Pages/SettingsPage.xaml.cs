using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Pages
{
	/// <summary>
	///     Settings.xaml の相互作用ロジック
	/// </summary>
	public partial class SettingsPage : Page
	{
		public SettingsPage ( SettingsPageViewModel viewModel )
		{
			DataContext = viewModel;
			InitializeComponent ( );
		}

		private async void SaveDatabase_Click ( object sender, RoutedEventArgs e )
		{
			if ( DataContext is SettingsPageViewModel viewModel &&
			     await viewModel.SaveDatabaseAsync ( DatabasePassword.Password ) )
			{
				DatabasePassword.Password = string.Empty;
			}
		}

		private async void OpenReport_Click ( object sender, RoutedEventArgs e )
		{
			if ( DataContext is SettingsPageViewModel { ReportUri: { } link } && !await ExternalLink.OpenAsync ( link ) )
			{
				MessageBox.Show ( ExternalLink.CopyToClipboard ( link )
						? "ブラウザを開けませんでした。リンクをコピーしたので、ブラウザに貼り付けてください。"
						: "ブラウザを開けませんでした。",
					"受験報告書", MessageBoxButton.OK, MessageBoxImage.Warning );
			}
		}
	}
}