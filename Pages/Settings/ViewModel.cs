using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.Pages.Settings;

public partial class ViewModel : ObservableObject
{
	[ObservableProperty]
	public partial ApplicationTheme CurrentApplicationTheme { get; set; } = ApplicationTheme.Light;

	partial void OnCurrentApplicationThemeChanged ( ApplicationTheme oldValue, ApplicationTheme newValue )
	{
		if ( newValue == ApplicationTheme.Unknown )
		{
			ApplicationThemeManager.ApplySystemTheme ( false );
		}
		else
		{
			ApplicationThemeManager.Apply ( newValue );
		}
	}
}
