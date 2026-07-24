using CommunityToolkit.Mvvm.ComponentModel;

namespace ShinroKensakuDesktop.Windows.MainWindow;

public partial class ViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Title { get; set; } = "総合科学進路検索システム";
}