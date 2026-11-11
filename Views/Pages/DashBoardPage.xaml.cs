using ShinroKensakuDesktop.ViewModels.Pages;
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
	}
}