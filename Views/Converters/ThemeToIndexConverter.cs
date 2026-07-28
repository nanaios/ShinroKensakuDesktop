using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.Views.Converters;

class ThemeToIndexConverter : IValueConverter
{
	public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
	{
		if ( value is ApplicationTheme theme )
		{
			return theme switch
			{
				ApplicationTheme.Light => 0,
				ApplicationTheme.Dark => 1,
				ApplicationTheme.HighContrast => 2,
				ApplicationTheme.Unknown => 3,
				_ => throw new NotImplementedException ( )
			};
		}
		return 0;
	}

	public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
	{
		if ( value is int index )
		{
			return index switch
			{
				0 => ApplicationTheme.Light,
				1 => ApplicationTheme.Dark,
				2 => ApplicationTheme.HighContrast,
				3 => ApplicationTheme.Unknown,
				_ => throw new NotImplementedException ( ),
			};
		}
		return ApplicationTheme.Light;
	}
}
