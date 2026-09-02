using ShinroKensakuDesktop.ViewModels.Pages;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Views.Pages;

public partial class SearchPage : Page
{
	public SearchPage ( SearchPageViewModel viewModel )
	{
		DataContext = viewModel;
		InitializeComponent ( );
		Loaded += async ( object sender, RoutedEventArgs e ) =>
		{
			await viewModel.LoadExamMethodComboBoxItems ( );
			await viewModel.LoadDepartmentComboBoxItems ( );
		};
		Loaded += async ( object sender, RoutedEventArgs e ) =>
		{
			await DebugSize ( );
		};
	}

	private async Task DebugSize ( )
	{
		using var cts = new CancellationTokenSource();
		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
		while ( await timer.WaitForNextTickAsync ( cts.Token ) )
		{
			// 定期実行したい処理
			Console.WriteLine ( "処理を実行中..." );

			Debug.WriteLine ( $"RootGrid:" );
			Debug.WriteLine ( $"  Desired = {ResultGrid.DesiredSize}" );
			Debug.WriteLine ( $"  Render  = {ResultGrid.RenderSize}" );
			Debug.WriteLine ( $"  Actual  = {ResultGrid.ActualWidth} x {ResultGrid.ActualHeight}" );

			Debug.WriteLine ( $"DataGrid:" );
			Debug.WriteLine ( $"  Desired = {ResultGrid.DesiredSize}" );
			Debug.WriteLine ( $"  Render  = {ResultGrid.RenderSize}" );
			Debug.WriteLine ( $"  Actual  = {ResultGrid.ActualWidth} x {ResultGrid.ActualHeight}" );

			var element = ResultGrid as FrameworkElement;
			while ( element != null )
			{
				Debug.WriteLine (
					$"{element.GetType ( ).FullName}\n" +
					$"  Desired = {element.DesiredSize}\n" +
					$"  Render  = {element.RenderSize}\n" +
					$"  Actual  = {element.ActualWidth} x {element.ActualHeight}" );

				element = VisualTreeHelper.GetParent ( element ) as FrameworkElement;
			}
		}
	}
}