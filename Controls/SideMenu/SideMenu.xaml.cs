using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty =
		DependencyProperty.Register(
			nameof(IsOpen),
			typeof(bool),
			typeof(SideMenu),
			new FrameworkPropertyMetadata(
				false,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public SideMenu()
	{
		InitializeComponent();
	}
}
