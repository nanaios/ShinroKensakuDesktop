using ShinroKensakuDesktop.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public class ViewModel : ViewModelBase
{
	public bool IsOpen
	{
		get => field; set => SetProperty(ref field, value);
	}

	public ICommand ButtonClickCommand { get; }

	public ViewModel()
	{
		ButtonClickCommand = new RelayCommand(_ => OnButtonClick());
	}

	private void OnButtonClick()
	{
		IsOpen = !IsOpen;
		Debug.WriteLine($"Side menu is now {(IsOpen ? "open" : "closed")}");
	}
}
