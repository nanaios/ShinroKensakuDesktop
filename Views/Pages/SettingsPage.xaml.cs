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
			/* Leaving the page locks the settings again, and locking drops a typed password. */
			Unloaded += ( _, _ ) => viewModel.IsAdvancedMode = false;
			viewModel.PropertyChanged += ( _, e ) =>
			{
				if ( e.PropertyName == nameof(SettingsPageViewModel.IsAdvancedMode) && !viewModel.IsAdvancedMode )
				{
					DatabasePassword.Password = string.Empty;
				}
			};
		}

		private async void SaveDatabase_Click ( object sender, RoutedEventArgs e )
		{
			if ( DataContext is SettingsPageViewModel viewModel &&
			     await viewModel.SaveDatabaseAsync ( DatabasePassword.Password ) )
			{
				DatabasePassword.Password = string.Empty;
			}
		}

		private void OpenReport_Click ( object sender, RoutedEventArgs e )
		{
			if ( DataContext is SettingsPageViewModel viewModel )
			{
				ExternalLink.Open ( viewModel.ReportUri );
			}
		}
	}
}