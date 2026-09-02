namespace ShinroKensakuDesktop.Models.Data
{
	public record SearchResultData (
		short Year,
		DateTime Jyukenbi,
		string? Gakubu,
		string? Gakka,
		string? Course,
		string? Jyukenhouhou_name,
		string? G_name,
		string? Sei,
		string? Cls_name,
		string? Result
	)
	{
	}
}
