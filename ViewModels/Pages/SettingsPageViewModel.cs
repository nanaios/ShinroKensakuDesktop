using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class SettingsPageViewModel : ObservableObject
	{
		[ ObservableProperty ]
		public partial ApplicationTheme CurrentApplicationTheme { get; set; } = ApplicationThemeManager.GetAppTheme ( );

		[ ObservableProperty ]
		public partial string ApplicationVersion { get; set; } =
			Assembly.GetExecutingAssembly ( ).GetName ( ).Version?.ToString ( ) ?? "Unknown";

		partial void OnCurrentApplicationThemeChanged ( ApplicationTheme oldValue, ApplicationTheme newValue )
		{
			if ( newValue == ApplicationTheme.Unknown )
			{
				ApplicationThemeManager.ApplySystemTheme ( false );
				ApplicationThemeManager.Changed += ApplySystemTheme;
			}
			else
			{
				ApplicationThemeManager.Changed -= ApplySystemTheme;
				ApplicationThemeManager.Apply ( newValue );
			}
		}

		private void ApplySystemTheme ( ApplicationTheme currentApplicationTheme, Color systemAccent ) =>
			ApplicationThemeManager.ApplySystemTheme ( false );
	}
}