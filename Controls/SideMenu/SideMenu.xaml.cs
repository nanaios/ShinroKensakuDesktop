using ShinroKensakuDesktop.Utils;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = DependencyPropertyFactory.Create(nameof(IsOpen), false);

	private ViewModel vm = new();

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public SideMenu()
	{
		InitializeComponent();
		this.MainGrid.DataContext = this.vm;
	}
}
