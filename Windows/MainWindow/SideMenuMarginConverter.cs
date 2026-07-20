using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public class SideMenuMarginConverter : IMultiValueConverter
{
	public object Convert ( object [ ] values, Type targetType, object parameter, CultureInfo culture )
	{
		double width = ( double ) values [ 0 ];
		bool isOpen = ( bool ) values [ 1 ];

		return new Thickness ( isOpen ? 0 : -width + 80, 0, 0, 0 );

	}

	public object [ ] ConvertBack ( object value, Type [ ] targetTypes, object parameter, CultureInfo culture ) => throw new NotImplementedException ( );
}
