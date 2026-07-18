using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public class BooleanToMenuButtonPathGeometryConverter : IValueConverter
{
	public required PathGeometry OpenIconGeometry { get; set; }
	public required PathGeometry CloseIconGeometry { get; set; }

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return (bool)value ? OpenIconGeometry : CloseIconGeometry;
	}
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
