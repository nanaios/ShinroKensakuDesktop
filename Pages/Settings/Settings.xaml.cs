using System.Windows.Controls;

namespace ShinroKensakuDesktop.Pages.Settings;

/// <summary>
/// Settings.xaml の相互作用ロジック
/// </summary>
public partial class Settings : Page
{
	public static Settings? Instance { get; private set; }

	public Settings ( )
	{
		InitializeComponent ( );
		Instance = this;
	}
}
