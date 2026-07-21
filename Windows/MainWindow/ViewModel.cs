using ShinroKensakuDesktop.Controls.SideMenu;
using ShinroKensakuDesktop.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public class ViewModel : ViewModelBase
{
	public ICommand OnSideMenuContentClickCommand { get; private set; }
	public bool IsOpen { get; set => SetProperty ( ref field, value ); }

	public double SideMenuWidth { get => field; set => SetProperty ( ref field, value ); } = 400;

	public ViewModel ( )
	{
		OnSideMenuContentClickCommand = new RelayCommand ( OnSideMenuContentClick );
	}

	private void OnSideMenuContentClick ( object? obj )
	{
		SideMenuContentType contentType = ( SideMenuContentType ) obj!;
		Debug.WriteLine ( $"SideMenuContentType: {contentType}" );
	}
}
