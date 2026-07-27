using Wpf.Ui.Abstractions;

namespace ShinroKensakuDesktop.Utils;

internal class SimplePageProvider : INavigationViewPageProvider
{
	public object? GetPage ( Type pageType )
	{
		if ( pageType is ISingletonic < typeof ( pageType ) > singletonic )
		{
			return singletonic.Instance;
		}
		return Activator.CreateInstance ( pageType );
	}
}