using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Controls.SearchButton;

public partial class SearchButton : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register (
		nameof ( OnClick ),
		typeof ( ICommand ),
		typeof ( SearchButton ),
		new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public ICommand OnClick
	{
		get => ( ICommand ) GetValue ( IsOpenProperty );
		set => SetValue ( IsOpenProperty, value );
	}

	public SearchButton ( )
	{
		InitializeComponent ( );
	}
}