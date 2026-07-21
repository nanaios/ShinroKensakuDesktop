using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Controls.InputField;

public partial class InputField : UserControl
{
	public static readonly DependencyProperty FieldTextProperty = DependencyProperty.Register (
		nameof ( FieldText ),
		typeof ( string ),
		typeof ( InputField ),
		new PropertyMetadata(""));
	public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register (
		nameof ( PlaceholderText ),
		typeof ( string ),
		typeof ( InputField ),
		new PropertyMetadata(""));

	public string FieldText
	{
		get => ( string ) GetValue ( FieldTextProperty );
		set => SetValue ( FieldTextProperty, value );
	}

	public string PlaceholderText
	{
		get => ( string ) GetValue ( PlaceholderTextProperty );
		set => SetValue ( PlaceholderTextProperty, value );
	}

	public InputField ( )
	{
		InitializeComponent ( );
	}

	private void InputFieldBox_KeyDown ( object sender, KeyEventArgs e )
	{
		if ( e.Key == Key.Return )
		{
			FieldText = this.InputFieldBox.Text;
			Keyboard.ClearFocus ( );
			e.Handled = true;
		}
	}

	private void InputFieldBox_LostKeyboardFocus ( object sender, KeyboardFocusChangedEventArgs e )
	{
		if ( string.IsNullOrEmpty ( this.InputFieldBox.Text ) )
		{
			this.Placeholder.Visibility = Visibility.Visible;
		}
	}

	private void InputFieldBox_GotKeyboardFocus ( object sender, KeyboardFocusChangedEventArgs e )
	{
		this.Placeholder.Visibility = Visibility.Collapsed;
	}
}
