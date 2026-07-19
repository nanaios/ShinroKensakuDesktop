using ShinroKensakuDesktop.Utils;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = DependencyPropertyFactory.Create(nameof(IsOpen), true);

	public PropertyBridge<bool> IsOpenBridge = new();
	public bool IsOpen
	{
		get => ( bool ) GetValue ( IsOpenProperty );
		set
		{
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
}
