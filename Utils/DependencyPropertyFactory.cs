using System.Diagnostics;
using System.Windows;

namespace ShinroKensakuDesktop.Utils;

public static class DependencyPropertyFactory
{
	public static DependencyProperty Create<T>(string propertyName, T defaultValue)
	{
		// 現在のメソッドの1つ前のスタックフレームを取得
		var frame = new StackFrame(1);

		// 呼び出し元のクラスの型を取得
		var method = frame.GetMethod() ?? throw new Exception();
		var type = method.DeclaringType ?? throw new Exception();

		return DependencyProperty.Register(
			propertyName,
			typeof(T),
			type,
			new FrameworkPropertyMetadata(
				defaultValue,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
	}

	public static DependencyProperty Create<T>(string propertyName, T defaultValue, PropertyChangedCallback propertyChangedCallback)
	{
		// 現在のメソッドの1つ前のスタックフレームを取得
		var frame = new StackFrame(1);

		// 呼び出し元のクラスの型を取得
		var method = frame.GetMethod() ?? throw new Exception();
		var type = method.DeclaringType ?? throw new Exception();

		return DependencyProperty.Register(
			propertyName,
			typeof(T),
			type,
			new FrameworkPropertyMetadata(
				defaultValue,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				propertyChangedCallback));
	}
}
