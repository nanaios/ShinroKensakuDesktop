using ShinroKensakuDesktop.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShinroKensakuDesktop.Views.Converters
{
	/* Visible for true or a positive count (reversed with "Invert"). "Hide" keeps the space, so a progress bar
	   does not shift the page. */
	public sealed class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert ( object? value, Type targetType, object? parameter, CultureInfo culture )
		{
			string options = parameter as string ?? "";
			bool flag = value switch { bool b => b, int count => count > 0, _ => false };
			bool shown = flag != options.Contains ( "Invert" );
			return shown ? Visibility.Visible : options.Contains ( "Hide" ) ? Visibility.Hidden : Visibility.Collapsed;
		}

		public object ConvertBack ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			Binding.DoNothing;
	}

	public sealed class InverseBoolConverter : IValueConverter
	{
		public object Convert ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			value is not true;

		public object ConvertBack ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			value is not true;
	}

	/* Collapses optional text so an empty line does not take space. */
	public sealed class TextToVisibilityConverter : IValueConverter
	{
		public object Convert ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			string.IsNullOrEmpty ( value as string ) ? Visibility.Collapsed : Visibility.Visible;

		public object ConvertBack ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			Binding.DoNothing;
	}

	/* A 0–1 ratio as a star column width; ConverterParameter=Rest gives the remainder of the row. */
	public sealed class RatioToStarConverter : IValueConverter
	{
		public object Convert ( object? value, Type targetType, object? parameter, CultureInfo culture )
		{
			double ratio = Math.Clamp ( value is double number ? number : 0, 0, 1 );
			return new GridLength ( parameter as string == "Rest" ? 1 - ratio : ratio, GridUnitType.Star );
		}

		public object ConvertBack ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			Binding.DoNothing;
	}

	/* A 0–1 ratio scaled to the length in ConverterParameter, with a 2px floor so non-zero values stay visible. */
	public sealed class RatioToLengthConverter : IValueConverter
	{
		public object Convert ( object? value, Type targetType, object? parameter, CultureInfo culture )
		{
			double ratio = Math.Clamp ( value is double number ? number : 0, 0, 1 );
			double length = double.TryParse ( parameter as string, NumberStyles.Float, CultureInfo.InvariantCulture,
				out double max )
				? max
				: 100;
			return ratio <= 0 ? 0 : Math.Max ( 2, ratio * length );
		}

		public object ConvertBack ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			Binding.DoNothing;
	}

	/* A flag becomes the severity in ConverterParameter (default Warning), or no notice. */
	public sealed class BoolToSeverityConverter : IValueConverter
	{
		public object Convert ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			value is true
				? Enum.TryParse ( parameter as string, out NoticeSeverity severity ) ? severity : NoticeSeverity.Warning
				: NoticeSeverity.None;

		public object ConvertBack ( object? value, Type targetType, object? parameter, CultureInfo culture ) =>
			Binding.DoNothing;
	}
}
