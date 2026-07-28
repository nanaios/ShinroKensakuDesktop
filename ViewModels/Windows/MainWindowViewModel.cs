using CommunityToolkit.Mvvm.ComponentModel;

namespace ShinroKensakuDesktop.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Title { get; set; } = "KST-DeCS 総合科学進路検索システム";
}