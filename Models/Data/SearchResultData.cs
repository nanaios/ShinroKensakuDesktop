namespace ShinroKensakuDesktop.Models.Data
{
	public record SearchResultData (
		short Year,
		string Shingakusaki_code,
		string G_code,
		string ExamCode,
		bool Gouhi,
		DateTime Jyukenbi
	)
	{
	}
}
