using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Pages
{
	public partial class SearchPage : Page
	{
		public SearchPage ( SearchPageViewModel viewModel )
		{
			DataContext = viewModel;
			InitializeComponent ( );
			Loaded += async ( _, _ ) =>
			{
				if ( viewModel.LoadConditionsCommand.CanExecute ( null ) )
				{
					await viewModel.LoadConditionsCommand.ExecuteAsync ( null );
				}
			};
		}

		private async void SearchNameBox_QuerySubmitted ( AutoSuggestBox sender,
			AutoSuggestBoxQuerySubmittedEventArgs e )
		{
			if ( DataContext is SearchPageViewModel viewModel && viewModel.ExecuteSearchCommand.CanExecute ( null ) )
			{
				await viewModel.ExecuteSearchCommand.ExecuteAsync ( null );
			}
		}
	}
}