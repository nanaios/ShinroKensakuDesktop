using ShinroKensakuDesktop.Controls.SideMenu;
using ShinroKensakuDesktop.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public class ViewModel : ViewModelBase
{
	public ICommand OnSideMenuContentClickCommand { get; private set; }
	public bool IsOpen { get; set => SetProperty ( ref field, value ); } = false;
	public Uri PageSource { get; set => SetProperty ( ref field, value ); } = new Uri ( "/Pages/MainPage/MainPage.xaml", UriKind.Relative );
	public double SideMenuWidth { get; set => SetProperty ( ref field, value ); } = 400;

	private Dictionary<SideMenuContentType,Uri> PageSources =new()
	{
		{ SideMenuContentType.Home, new Uri ( "/Pages/MainPage/MainPage.xaml", UriKind.Relative ) },
		{ SideMenuContentType.Help, new Uri ( "/Pages/HelpPage/HelpPage.xaml", UriKind.Relative ) },
		{ SideMenuContentType.Details, new Uri ( "/Pages/DetailsPage/DetailsPage.xaml", UriKind.Relative ) }
	};

	public ViewModel ( )
	{
		OnSideMenuContentClickCommand = new RelayCommand ( OnSideMenuContentClick );
	}

	private void OnSideMenuContentClick ( object? obj )
	{
		SideMenuContentType contentType = ( SideMenuContentType ) obj!;
		Debug.WriteLine ( $"SideMenuContentType: {contentType}" );
		IsOpen = false;
		PageSource = PageSources [ contentType ];
	}
}
