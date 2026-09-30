using ShinroKensakuDesktop.Models.Data;
using System.Globalization;
using System.Text;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchResultCsv
	{
		public static string Create ( IEnumerable<SearchResultData> rows )
		{
			StringBuilder csv = new("卒業年度,受験日,進路先,学部,学科,コース,就職先内区分,受験方法,氏名,性別,クラス,合否\r\n");
			foreach ( SearchResultData row in rows )
			{
				string? [ ] cells =
				[
					row.Year.ToString ( CultureInfo.InvariantCulture ),
					row.Jyukenbi.ToString ( "yyyy/MM/dd", CultureInfo.InvariantCulture ),
					row.Shinro_name, row.Gakubu, row.Gakka, row.Course, row.Syuusyokusakinai_kubun,
					row.Jyukenhouhou_name, row.G_name, row.Sei, row.Cls_name, row.Result
				];
				csv.AppendJoin ( ',', cells.Select ( Escape ) ).Append ( "\r\n" );
			}

			return csv.ToString ( );
		}

		private static string Escape ( string? value )
		{
			value ??= "";
			// Keep spreadsheet applications from executing text as a formula.
			string trimmed = value.TrimStart ( );
			if ( ( trimmed.Length > 0 && "=+-@".Contains ( trimmed [ 0 ] ) ) || value.StartsWith ( '\t' ) ||
			     value.StartsWith ( '\r' ) || value.StartsWith ( '\n' ) )
			{
				value = "'" + value;
			}

			return "\"" + value.Replace ( "\"", "\"\"" ) + "\"";
		}
	}
}