using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Pages
{
	public partial class ComparisonPage : Page
	{
		public ComparisonPage ( AnalyticsPageViewModel viewModel )
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