using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Controls.SearchButton;

public partial class SearchButton : UserControl
{
	public static readonly DependencyProperty OnClickProperty = DependencyProperty.Register (
		nameof ( OnClick ),
		typeof ( ICommand ),
		typeof ( SearchButton ),
		new PropertyMetadata(null));

	public ICommand OnClick
	{
		get => ( ICommand ) GetValue ( OnClickProperty );
		set => SetValue ( OnClickProperty, value );
	}

	public SearchButton ( )
	{
		InitializeComponent ( );
	}

	private void Button_Click ( object sender, RoutedEventArgs e )
	{
		OnClick.Execute ( null );
	}
}