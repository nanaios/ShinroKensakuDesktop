using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = Create(nameof(IsOpen), false);

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public SideMenu()
	{
		InitializeComponent();
	}

	private static DependencyProperty Create<T>(string propertyName, T defaultValue)
	{
		// 現在のメソッドの1つ前のスタックフレームを取得
		var frame = new StackFrame(1);

		var method = frame.GetMethod() ?? throw new Exception();
		var type = method.DeclaringType ?? throw new Exception();

		Debug.WriteLine(type.FullName);

		return DependencyProperty.Register(
			propertyName,
			typeof(T),
			type,
			new FrameworkPropertyMetadata(
				defaultValue,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
	}
}
