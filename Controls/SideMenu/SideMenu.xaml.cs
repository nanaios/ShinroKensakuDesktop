using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public partial class SideMenu : UserControl
{
	public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register (
		nameof ( IsOpen ),
		typeof ( bool ),
		typeof ( SideMenu ),
		new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty MenuContentMarginWidthProperty = DependencyProperty.Register (
		nameof ( MenuContentMarginWidth ),
		typeof ( double ),
		typeof ( SideMenu ),
		new FrameworkPropertyMetadata(35.0,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty MenuContentWidthProperty = DependencyProperty.Register (
		nameof ( MenuContentWidth ),
		typeof ( double ),
		typeof ( SideMenu ),
		new FrameworkPropertyMetadata(60.0,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty OnListContentClickProperty = DependencyProperty.Register (
		nameof ( OnListContentClick ),
		typeof ( ICommand ),
		typeof ( SideMenu ),
		new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public ICommand OnListContentClick
	{
		get => ( ICommand ) GetValue ( OnListContentClickProperty );
		set => SetValue ( OnListContentClickProperty, value );
	}

	public bool IsOpen
	{
		get => ( bool ) GetValue ( IsOpenProperty );
		set => SetValue ( IsOpenProperty, value );
	}

	public double MenuContentMarginWidth
	{
		get => ( double ) GetValue ( MenuContentMarginWidthProperty );
		set => SetValue ( MenuContentMarginWidthProperty, value );
	}

	public double MenuContentWidth
	{
		get => ( double ) GetValue ( MenuContentWidthProperty );
		set => SetValue ( MenuContentWidthProperty, value );
	}

	public SideMenu ( )
	{
		InitializeComponent ( );
		this.ListGrid.AddHandler ( Button.ClickEvent, new RoutedEventHandler ( List_Content_Click ), true );
	}

	private void List_Content_Click ( object sender, RoutedEventArgs e )
	{
		Button button = (Button)e.OriginalSource;
		SideMenuContentType type = (SideMenuContentType)button.Tag;
		OnListContentClick?.Execute ( type );
	}

	private void Button_Click ( object sender, RoutedEventArgs e )
	{
		IsOpen = !IsOpen;
	}
}

public class PathGeometryConverter : IValueConverter
{
	public required PathGeometry OpenIconGeometry { get; set; }
	public required PathGeometry CloseIconGeometry { get; set; }

	public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
	{
		return ( bool ) value ? CloseIconGeometry : OpenIconGeometry;
	}
	public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture ) => throw new NotImplementedException ( );
}

public enum SideMenuContentType
{
	Home,
	Details,
	Help,
}