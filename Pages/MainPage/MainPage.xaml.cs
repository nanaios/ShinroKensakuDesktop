using ShinroKensakuDesktop.Utils;
using System.Diagnostics;
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
	public ICommand OnSearchButtonClickCommand { get; private set; }
	public string SearchText { get; set; } = "";

	public ViewModel ( )
	{
		OnSearchButtonClickCommand = new RelayCommand ( OnSearchButtonClick );
	}

	private void OnSearchButtonClick ( object? obj )
	{
		Debug.WriteLine ( $"Search button clicked with text: {SearchText}" );
	}
}