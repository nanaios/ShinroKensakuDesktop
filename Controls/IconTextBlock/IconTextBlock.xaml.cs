using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Controls.IconTextBlock;

public partial class IconTextBlock : UserControl
{
	public static readonly DependencyProperty TextProperty = DependencyProperty.Register (
		nameof ( Text ),
		typeof ( string ),
		typeof ( IconTextBlock ),
		new PropertyMetadata(""));

	public static readonly DependencyProperty IconPathProperty = DependencyProperty.Register (
		nameof ( IconPath ),
		typeof ( PathGeometry ),
		typeof ( IconTextBlock ),
		new PropertyMetadata(null));

	public string Text
	{
		get => ( string ) GetValue ( TextProperty );
		set => SetValue ( TextProperty, value );
	}

	public PathGeometry IconPath
	{
		get => ( PathGeometry ) GetValue ( IconPathProperty );
		set => SetValue ( IconPathProperty, value );
	}

	public IconTextBlock ( )
	{
		InitializeComponent ( );
	}
}
