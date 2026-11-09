using System.IO;
using System.Text.Json;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.Models
{
	/* Per-user settings kept on this PC. The exam report link lives here rather than in the source, because the
	   repository is shared and the reports hold personal data. */
	public static class UserPreferences
	{
		public static string SettingsPath => Path.Combine (
			Environment.GetFolderPath ( Environment.SpecialFolder.LocalApplicationData ), "KST-DeCS",
			"preferences.json" );

		public static ApplicationTheme LoadTheme ( string? path = null ) =>
			Load ( path ) is { } value && Enum.IsDefined ( value.Theme ) ? value.Theme : ApplicationTheme.Unknown;

		public static string? LoadReportLink ( string? path = null ) => Load ( path )?.ReportLink;

		public static void SaveTheme ( ApplicationTheme theme, string? path = null ) =>
			Save ( ( Load ( path ) ?? new Preference ( ApplicationTheme.Unknown ) ) with { Theme = theme }, path );

		public static void SaveReportLink ( string? link, string? path = null ) =>
			Save ( ( Load ( path ) ?? new Preference ( ApplicationTheme.Unknown ) ) with { ReportLink = link }, path );

		/* Only an absolute https address is opened, so a stored file path or script can never be launched. */
		public static Uri? ParseReportLink ( string? text ) =>
			!string.IsNullOrWhiteSpace ( text ) && Uri.TryCreate ( text.Trim ( ), UriKind.Absolute, out Uri? uri ) &&
			uri.Scheme == Uri.UriSchemeHttps && uri.Host.Length > 0
				? uri
				: null;

		private static Preference? Load ( string? path )
		{
			try { return JsonSerializer.Deserialize<Preference> ( File.ReadAllText ( path ?? SettingsPath ) ); }
			catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException or JsonException )
			{
				return null;
			}
		}

		private static void Save ( Preference preference, string? path )
		{
			string target = path ?? SettingsPath;
			Directory.CreateDirectory ( Path.GetDirectoryName ( Path.GetFullPath ( target ) )! );
			string temporary = target + "." + Guid.NewGuid ( ).ToString ( "N" ) + ".tmp";
			try
			{
				File.WriteAllText ( temporary, JsonSerializer.Serialize ( preference ) );
				File.Move ( temporary, target, true );
			}
			finally
			{
				if ( File.Exists ( temporary ) )
				{
					File.Delete ( temporary );
				}
			}
		}

		private sealed record Preference ( ApplicationTheme Theme, string? ReportLink = null );
	}
}
