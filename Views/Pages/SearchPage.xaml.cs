using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;

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
	}
}