using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Pages;

public partial class HomePage : Page
{
	public HomePage ( )
	{
		InitializeComponent ( );
	}
}


public partial class HomePageViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsTypingSearchTextBox { get; set; } = false;
}