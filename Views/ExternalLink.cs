using System.Diagnostics;
using System.Windows;

namespace ShinroKensakuDesktop.Views
{
	/* Opens a validated https address in the default browser.
	   Windows opens it through its launcher, so the browser is not started as a child of this app: on this PC a
	   browser started directly by the app crashed at once, while the same link opened from the shell worked. */
	public static class ExternalLink
	{
		public static async Task<bool> OpenAsync ( Uri? uri )
		{
			if ( uri == null || uri.Scheme != Uri.UriSchemeHttps )
			{
				return false;
			}

			try
			{
				if ( await global::Windows.System.Launcher.LaunchUriAsync ( uri ) )
				{
					return true;
				}
			}
			catch ( Exception )
			{
				/* Fall back to the shell below. */
			}

			try
			{
				Process.Start ( new ProcessStartInfo ( uri.AbsoluteUri ) { UseShellExecute = true } );
				return true;
			}
			catch ( Exception ex ) when ( ex is System.ComponentModel.Win32Exception or InvalidOperationException )
			{
				return false;
			}
		}

		/* When nothing could open it, the link goes to the clipboard so it can be pasted into a browser. */
		public static bool CopyToClipboard ( Uri uri )
		{
			try
			{
				Clipboard.SetText ( uri.AbsoluteUri );
				return true;
			}
			catch ( System.Runtime.InteropServices.ExternalException )
			{
				return false;
			}
		}
	}
}
