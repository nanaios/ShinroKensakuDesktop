using ShinroKensakuDesktop.Models.Data;
using System.Globalization;
using System.Text;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchResultCsv
	{
		public static string Create ( IEnumerable<SearchResultData> rows )
		{
			/* The same 13 columns, in the same order, as the result table (hidden columns included). */
			StringBuilder csv = new("卒業年度,区分,進路先,受験方法,合否,氏名,クラス,受験日,学部,学科,コース,就職先内区分,性別\r\n");
			foreach ( SearchResultData row in rows )
			{
				string? [ ] cells =
				[
					row.Year?.ToString ( CultureInfo.InvariantCulture ), row.Kind, row.Shinro_name,
					row.Jyukenhouhou_name, row.Result, row.G_name, row.Cls_name,
					row.Jyukenbi?.ToString ( "yyyy/MM/dd", CultureInfo.InvariantCulture ),
					row.Gakubu, row.Gakka, row.Course, row.Syuusyokusakinai_kubun, row.Sei
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