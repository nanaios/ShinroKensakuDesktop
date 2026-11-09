using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.Views
{
	/* The meaning colours shared by cards, lists, tables and charts. App.xaml holds the light values;
	   a theme change replaces the brushes so every DynamicResource follows. */
	public static class ThemeColors
	{
		public const string StudyBrush = "StudyBrush";
		public const string WorkBrush = "WorkBrush";
		public const string NeutralBarBrush = "NeutralBarBrush";

		/* Validated categorical palette (light / dark steps of the same eight hues), assigned by slot, never cycled.
		   Slot 0 is 進学 and slot 1 is 就職 everywhere in the app. */
		private static readonly string [ ] LightPalette =
			[ "#2A78D6", "#EB6834", "#1BAF7A", "#EDA100", "#E87BA4", "#008300", "#4A3AA7", "#E34948" ];

		private static readonly string [ ] DarkPalette =
			[ "#3987E5", "#D95926", "#199E70", "#C98500", "#D55181", "#008300", "#9085E9", "#E66767" ];

		/* Neutral bars and その他 keep 3:1 against both card surfaces. */
		private const string NeutralLight = "#8A8A8A";
		private const string NeutralDark = "#8F8F8F";

		private static bool attached;

		public static string SeriesColor ( int slot, bool dark ) => slot < 0
			? dark ? NeutralDark : NeutralLight
			: ( dark ? DarkPalette : LightPalette ) [ Math.Clamp ( slot, 0, LightPalette.Length - 1 ) ];

		public static void Attach ( )
		{
			if ( attached )
			{
				return;
			}

			attached = true;
			ApplicationThemeManager.Changed += ( theme, _ ) => Apply ( IsDark ( theme ) );
		}

		public static void Apply ( bool dark )
		{
			if ( Application.Current?.Resources is not { } resources )
			{
				return;
			}

			resources [ StudyBrush ] = Freeze ( SeriesColor ( 0, dark ) );
			resources [ WorkBrush ] = Freeze ( SeriesColor ( 1, dark ) );
			resources [ NeutralBarBrush ] = Freeze ( SeriesColor ( -1, dark ) );
		}

		/* High contrast follows its text colour; the named themes are known directly. */
		private static bool IsDark ( ApplicationTheme theme ) => theme switch
		{
			ApplicationTheme.Dark => true,
			ApplicationTheme.Light => false,
			_ => Application.Current?.TryFindResource ( "TextFillColorPrimaryBrush" ) is SolidColorBrush text &&
			     ( ( 0.2126 * text.Color.R ) + ( 0.7152 * text.Color.G ) + ( 0.0722 * text.Color.B ) ) / 255 > 0.5
		};

		private static SolidColorBrush Freeze ( string color )
		{
			SolidColorBrush brush = new( ( Color ) ColorConverter.ConvertFromString ( color ) );
			brush.Freeze ( );
			return brush;
		}
	}
}
