namespace ShinroKensakuDesktop.Utils
{
	internal interface ISingletonic<T>
	{
		static T? Instance { get; }
	}
}