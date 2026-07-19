using ShinroKensakuDesktop.Utils;
using System.Windows.Input;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public class ViewModel : ViewModelBase
{
	public PropertyBridge<bool> IsOpenBridge = new();

	public bool IsOpen
	{
		get; set
		{
			field = value;
			IsOpenBridge.SyncToOther ( value );
			IconGeometry = IsOpen ? CloseIconGeometry : OpenIconGeometry;
		}
	} = false;

	public required PathGeometry OpenIconGeometry
	{
		get; set
		{
			field = value;
			// IconGeometryの初期値をOpenIconGeometryに設定する
			// ただし、IconGeometryがすでに設定されている場合は上書きしない
			IconGeometry ??= value;
		}
	}
	public required PathGeometry CloseIconGeometry { get; set; }
	public PathGeometry IconGeometry { get; set => SetProperty ( ref field, value ); }


	public ICommand ButtonClickCommand { get; }

	public ViewModel ( )
	{
		ButtonClickCommand = new RelayCommand ( _ => IsOpen = !IsOpen );
		IsOpenBridge.OnValueChanged = value => IsOpen = value;
	}
}
