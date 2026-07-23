using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ShinroKensakuDesktop.Pages;

public partial class HomePage : Page
{
	public HomePage ( )
	{
		InitializeComponent ( );
	}
}

public class HomePagePlaceHolderVisibleConverter : IValueConverter
{
	public object Convert ( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
	{
		if ( value is string text )
		{
			return ( string.IsNullOrEmpty ( text ) ) ? Visibility.Visible : Visibility.Collapsed;
		}
		return Visibility.Visible;
	}
	public object ConvertBack ( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
	{
		throw new NotImplementedException ( );
	}
}


public partial class HomePageViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;
}