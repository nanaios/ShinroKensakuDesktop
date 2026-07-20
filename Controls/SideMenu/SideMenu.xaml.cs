using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register (
		nameof ( IsOpen ),
		typeof ( bool ),
		typeof ( SideMenu ),
		new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public bool IsOpen
	{
		get => ( bool ) GetValue ( IsOpenProperty );
		set => SetValue ( IsOpenProperty, value );
	}

	public SideMenu ( )
	{
		InitializeComponent ( );
	}

	private void Button_Click ( object sender, RoutedEventArgs e )
	{
		IsOpen = !IsOpen;
	}
}

public class PathGeometryConverter : IValueConverter
{
	public required PathGeometry OpenIconGeometry { get; set; }
	public required PathGeometry CloseIconGeometry { get; set; }

	public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
	{
		return ( bool ) value ? CloseIconGeometry : OpenIconGeometry;
	}
	public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture ) => throw new NotImplementedException ( );
}