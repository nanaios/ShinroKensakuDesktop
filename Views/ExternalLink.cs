using System.Diagnostics;

namespace ShinroKensakuDesktop.Views
{
	/* Opens a validated https address in the default browser. */
	public static class ExternalLink
	{
		public static bool Open ( Uri? uri )
		{
			if ( uri == null || uri.Scheme != Uri.UriSchemeHttps )
			{
				return false;
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
	}
}
