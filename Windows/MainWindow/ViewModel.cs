using ShinroKensakuDesktop.Utils;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public class ViewModel : ViewModelBase
{
	public bool IsOpen { get; set => SetProperty ( ref field, value ); }
}
