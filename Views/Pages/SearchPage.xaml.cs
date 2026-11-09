using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MenuItem = System.Windows.Controls.MenuItem;

namespace ShinroKensakuDesktop.Views.Pages
{
	public partial class SearchPage : Page
	{
		/* 卒業年度・区分・進路先 stay frozen and always shown; the rest can be hidden. */
		private const int FixedColumns = 3;
		private readonly SnackbarService snackbar = new( );

		public SearchPage ( SearchPageViewModel viewModel )
		{
			DataContext = viewModel;
			InitializeComponent ( );
			snackbar.SetSnackbarPresenter ( Snackbar );
			viewModel.Exported += ( _, message ) => snackbar.Show ( "CSVに保存しました", message,
				ControlAppearance.Success, new SymbolIcon ( SymbolRegular.CheckmarkCircle24 ), TimeSpan.FromSeconds ( 5 ) );
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

		/* A checkable list of the optional columns, opened under the button. */
		private void ColumnsButton_Click ( object sender, RoutedEventArgs e )
		{
			ContextMenu menu = new( ) { PlacementTarget = ColumnsButton, Placement = PlacementMode.Bottom };
			foreach ( DataGridColumn column in ResultGrid.Columns.Skip ( FixedColumns ) )
			{
				MenuItem item = new( )
				{
					Header = column.Header,
					IsCheckable = true,
					IsChecked = column.Visibility == Visibility.Visible,
					StaysOpenOnClick = true
				};
				item.Click += ( _, _ ) =>
					column.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
				menu.Items.Add ( item );
			}

			menu.IsOpen = true;
		}

		/* The link is registered in the settings; with none yet, the button takes the user there. */
		private void OpenReports_Click ( object sender, RoutedEventArgs e )
		{
			Uri? link = UserPreferences.ParseReportLink ( UserPreferences.LoadReportLink ( ) );
			if ( link == null )
			{
				OpenSettings_Click ( sender, e );
				return;
			}

			ExternalLink.Open ( link );
		}

		private void ReportButton_ToolTipOpening ( object sender, ToolTipEventArgs e ) =>
			ReportButton.ToolTip = UserPreferences.ParseReportLink ( UserPreferences.LoadReportLink ( ) ) == null
				? "リンクが未登録です。押すと設定を開きます（詳細設定をオンにして登録）"
				: "受験報告書をブラウザで開く";

		private void OpenSettings_Click ( object sender, RoutedEventArgs e )
		{
			for ( DependencyObject? parent = this ; parent != null ; parent = VisualTreeHelper.GetParent ( parent ) )
			{
				if ( parent is NavigationView navigation )
				{
					navigation.Navigate ( typeof(SettingsPage) );
					return;
				}
			}
		}
	}
}
