using System.ComponentModel;

namespace ShinroKensakuDesktop.Models.Data
{
	public record ExamResult
	{
		[DisplayName ( "年度" )]
		public required DateTime Year { get; set; }
		[DisplayName ( "進学先コード" )]
		public required string ShingakusakiCode { get; set; }
		[DisplayName ( "学籍番号" )]
		public required string GCode { get; set; }
		[DisplayName ( "受験方法" )]
		public required string JyukenhouhouCode { get; set; }
		[DisplayName ( "合否" )]
		public required bool Gouhi { get; set; }
		[DisplayName ( "受験日" )]
		public required DateTime Jyukenbi { get; set; }
	}
}
