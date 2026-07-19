namespace ShinroKensakuDesktop.Utils;

public class PropertyBridge<T>
{
	public PropertyBridge<T>? Bind { get; set; }
	private bool isSyncing = false;

	public Action<T>? OnValueChanged { get; set; }

	public void SyncToOther ( T value )
	{
		isSyncing = true;
		Bind?.SyncFromOther ( value );
		isSyncing = false;
	}

	public void SyncFromOther ( T value )
	{
		if ( !isSyncing )
		{
			OnValueChanged?.Invoke ( value );
		}
	}
}
