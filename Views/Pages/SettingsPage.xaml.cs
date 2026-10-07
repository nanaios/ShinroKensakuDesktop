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
				DatabasePassword.Clear ( );
			}
		}
	}
}