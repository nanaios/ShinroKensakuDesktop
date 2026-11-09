using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.ViewModels.Pages;
using ShinroKensakuDesktop.ViewModels.Windows;
using ShinroKensakuDesktop.Views.Pages;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Windows
{
	/// <summary>
	///     MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : FluentWindow
	{
		public MainWindow (
			MainWindowViewModel viewModel,
			INavigationService navigationService,
			IServiceProvider serviceProvider,
			SettingsPageViewModel settings )
		{
			ViewModel = viewModel;
			DataContext = viewModel;
			InitializeComponent ( );
			Width = Math.Min ( Width, SystemParameters.WorkArea.Width );
			Height = Math.Min ( Height, SystemParameters.WorkArea.Height );
			settings.AttachWindow ( this );

			INavigationViewPageProvider pageProvider =
				serviceProvider.GetRequiredService<INavigationViewPageProvider> ( );
			MainView.SetPageProviderService ( pageProvider );
			navigationService.SetNavigationControl ( MainView );

			Loaded += ( _, _ ) =>
			{
				NameTemplateButtons ( MainView );
				navigationService.Navigate ( typeof(DashBoardPage) );
			};
		}

		public MainWindowViewModel ViewModel { get; }

		/* The navigation template's own buttons have no accessible names. */
		private static void NameTemplateButtons ( DependencyObject parent )
		{
			for ( int i = 0 ; i < VisualTreeHelper.GetChildrenCount ( parent ) ; i++ )
			{
				DependencyObject child = VisualTreeHelper.GetChild ( parent, i );
				if ( child is FrameworkElement element )
				{
					string? name = element.Name switch
					{
						"NavigationBackButton" => "戻る",
						"NavigationToggleButton" => "メニューを開閉",
						_ => null
					};
					if ( name != null )
					{
						AutomationProperties.SetName ( element, name );
						element.ToolTip ??= name;
					}
				}

				NameTemplateButtons ( child );
			}
		}
	}
}