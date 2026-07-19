using ShinroKensakuDesktop.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = DependencyPropertyFactory.Create(nameof(IsOpen), true, OnIsOpenChanged);

	public PropertyBridge<bool> IsOpenBridge = new();
	public bool IsOpen
	{
		get => ( bool ) GetValue ( IsOpenProperty );
		set
		{
			Debug.WriteLine ( $"SideMenu.IsOpen set to {value}" );
			SetValue ( IsOpenProperty, value );
			IsOpenBridge.SyncToOther ( value );
		}
	}

	public SideMenu ( )
	{
		InitializeComponent ( );
		var data = (ViewModel)this.MainGrid.DataContext;

		IsOpenBridge.OnValueChanged = value => IsOpen = value;

		// 双方向バインディングを設定する
		data.IsOpenBridge.Bind = IsOpenBridge;
		IsOpenBridge.Bind = data.IsOpenBridge;
	}

	public static void OnIsOpenChanged ( DependencyObject d, DependencyPropertyChangedEventArgs e )
	{
		if ( d is SideMenu sideMenu )
		{
			sideMenu.IsOpen = ( bool ) e.NewValue;
		}
	}
}
