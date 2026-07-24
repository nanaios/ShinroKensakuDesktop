using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Pages.TopPage;

public partial class ViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial Visibility IsPlaceHolderVisible { get; set; } = Visibility.Visible;

	[RelayCommand]
	private void OnTextChanged ( TextChangedEventArgs e )
	{
		string currentText = ( e.Source as TextBox )?.Text ?? string.Empty;
		IsPlaceHolderVisible = string.IsNullOrEmpty ( currentText ) ? Visibility.Visible : Visibility.Collapsed;
	}
}