using ShinroKensakuDesktop.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public class ViewModel : ViewModelBase
{
	public bool IsOpen
	{
		get; set => SetProperty(ref field, value);
	}

	public ICommand ButtonClickCommand { get; }

	public ViewModel()
	{
		ButtonClickCommand = new RelayCommand(_ => OnButtonClick());
	}

	private void OnButtonClick()
	{
		Debug.WriteLine("Button clicked!");
	}
}
