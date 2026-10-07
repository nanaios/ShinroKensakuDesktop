using System.Globalization;

namespace ShinroKensakuDesktop.Models
{
	public sealed record SearchCriteria ( string? Target, int? Year, string? ExamMethod, sbyte? Department )
	{
		public static SearchCriteria Create ( string? target, string? year, string? method, sbyte? department )
		{
			string? yearText = Normalize ( year );
			int? parsed = null;
			if ( yearText != null )
			{
				if ( yearText.Any ( c => c is < '0' or > '9' ) ||
				     !int.TryParse ( yearText, NumberStyles.None, CultureInfo.InvariantCulture, out int value ) ||
				     value is < 1901 or > 2155 )
				{
					throw new ArgumentException ( "年度は1901～2155の数字で入力してください。" );
				}

				parsed = value;
			}

			return new SearchCriteria ( Normalize ( target ), parsed, Normalize ( method ), department );
		}

		private static string? Normalize ( string? value ) =>
			string.IsNullOrWhiteSpace ( value ) ? null : value.Trim ( );

		// A fixed escape character keeps literal matching independent of SQL modes.
		public static string EscapeLike ( string value ) =>
			value.Replace ( "!", "!!" ).Replace ( "%", "!%" ).Replace ( "_", "!_" );
	}
}