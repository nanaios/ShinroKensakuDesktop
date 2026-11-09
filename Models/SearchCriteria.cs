using System.Globalization;

namespace ShinroKensakuDesktop.Models
{
	/* Department is a class code (clsTbl). Gakka is a department name derived from class names; its classes are
	   kept as a sorted, comma-joined string so two criteria compare by value. */
	public sealed record SearchCriteria (
		string? Target,
		int? Year,
		string? ExamMethod,
		sbyte? Department,
		string? Gakka = null,
		string? GakkaClasses = null )
	{
		public IReadOnlyList<sbyte> GakkaClassCodes => string.IsNullOrEmpty ( GakkaClasses )
			? [ ]
			: GakkaClasses.Split ( ',' ).Select ( x => sbyte.Parse ( x, CultureInfo.InvariantCulture ) ).ToList ( );

		public static SearchCriteria Create ( string? target, string? year, string? method, sbyte? department,
			string? gakka = null, IEnumerable<sbyte>? gakkaClasses = null )
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

			string? gakkaName = Normalize ( gakka );
			string? classes = gakkaName == null
				? null
				: string.Join ( ",", ( gakkaClasses ?? [ ] ).Distinct ( ).Order ( )
					.Select ( x => x.ToString ( CultureInfo.InvariantCulture ) ) );
			return new SearchCriteria ( Normalize ( target ), parsed, Normalize ( method ), department, gakkaName, classes );
		}

		private static string? Normalize ( string? value ) =>
			string.IsNullOrWhiteSpace ( value ) ? null : value.Trim ( );

		// A fixed escape character keeps literal matching independent of SQL modes.
		public static string EscapeLike ( string value ) =>
			value.Replace ( "!", "!!" ).Replace ( "%", "!%" ).Replace ( "_", "!_" );
	}
}
