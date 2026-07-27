namespace ShinroKensakuDesktop.Utils;

interface ISingletonic<T>
{
	public static T? Instance { get; }
}
