using ShinroKensakuDesktop.Utils;
using System.Windows.Input;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Controls.SideMenu;

public class ViewModel : ViewModelBase
{
	public bool IsOpen
	{
		get; set => SetProperty ( ref field, value );
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
		ButtonClickCommand = new RelayCommand ( _ => OnButtonClick ( ) );
	}

	private void OnButtonClick ( )
	{
		IsOpen = !IsOpen;
		IconGeometry = IsOpen ? CloseIconGeometry : OpenIconGeometry;
	}
}
