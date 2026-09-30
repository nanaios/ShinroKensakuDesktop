using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Pages
{
	public partial class AnalyticsPage : Page
	{
		public AnalyticsPage ( AnalyticsPageViewModel viewModel )
		{
			InitializeComponent ( );
			DataContext = viewModel;
			Loaded += async ( _, _ ) =>
			{
				if ( !viewModel.IsBusy )
				{
					await viewModel.RefreshCommand.ExecuteAsync ( null );
				}
			};
		}
	}
}