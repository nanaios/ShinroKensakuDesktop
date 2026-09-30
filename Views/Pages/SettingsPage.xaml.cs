using ShinroKensakuDesktop.ViewModels.Pages;
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
	}
}