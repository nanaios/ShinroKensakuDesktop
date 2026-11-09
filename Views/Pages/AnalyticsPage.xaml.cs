using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Pages
{
	public partial class AnalyticsPage : Page
	{
		/* Below this width the two method cards are stacked; above it they share a row. */
		public const double SideBySideWidth = 1100;

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

		private void MethodSection_SizeChanged ( object sender, SizeChangedEventArgs e )
		{
			bool wide = e.NewSize.Width >= SideBySideWidth;
			Grid.SetColumnSpan ( MethodTrendCard, wide ? 1 : 3 );
			Grid.SetRow ( MethodBreakdownCard, wide ? 0 : 1 );
			Grid.SetColumn ( MethodBreakdownCard, wide ? 2 : 0 );
			Grid.SetColumnSpan ( MethodBreakdownCard, wide ? 1 : 3 );
		}
	}
}
