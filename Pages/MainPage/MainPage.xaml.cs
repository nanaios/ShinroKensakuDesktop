using ShinroKensakuDesktop.Utils;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Pages.MainPage;

public partial class MainPage : Page
{
	public MainPage ( )
	{
		InitializeComponent ( );
	}
}

public class ViewModel
{
	public ICommand? OnSearchButtonClickCommand { get; private set; }

	public ViewModel ( )
	{
		OnSearchButtonClickCommand = new RelayCommand ( OnSearchButtonClick );
	}

	private void OnSearchButtonClick ( object? obj )
	{
	}
}