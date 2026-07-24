using Wpf.Ui.Abstractions;

namespace ShinroKensakuDesktop.Utils;

internal class SimplePageProvider : INavigationViewPageProvider
{
	public object? GetPage ( Type pageType ) => Activator.CreateInstance ( pageType );
}