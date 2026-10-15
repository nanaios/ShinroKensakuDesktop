using System.Data;
using System.Text.RegularExpressions;

namespace ShinroKensakuDesktop.Models
{
	/* DepartmentOrder is the smallest class code of the department, so selectors follow the clsTbl order. */
	public record DashboardGroup (
		int Year,
		string Kind,
		string Method,
		int Count,
		string Department = DashboardDataProvider.Unassigned,
		int DepartmentOrder = int.MaxValue );

	public record DashboardSummary ( string Label, int Count );

	public static partial class DashboardDataProvider
	{
		public const string Unassigned = "未設定";

		public static async Task<List<DashboardGroup>> LoadAsync ( )
		{
			// UNION ALL preserves separate exam records, including repeat applications.
			DataTable table = await MySQLCommand.Query ( """
			                                             SELECT r.year, r.kind, COALESCE(m.jyukenhouhou_name, '未設定') AS method,
			                                                    c.cls_code, c.cls_lname, COUNT(*) AS count
			                                             FROM (
			                                                 SELECT year, '進学' AS kind, jyukenhouhou_code, g_code FROM shingakukekkaTbl
			                                                 UNION ALL
			                                                 SELECT year, '就職' AS kind, jyukenhouhou_code, g_code FROM syuusyokukekkaTbl
			                                             ) r
			                                             LEFT JOIN jyukenhouhouTbl m ON r.jyukenhouhou_code = m.jyukenhouhou_code
			                                             LEFT JOIN seitoTbl s ON r.g_code = s.g_code
			                                             LEFT JOIN clsTbl c ON s.cls_code = c.cls_code
			                                             GROUP BY r.year, r.kind, m.jyukenhouhou_name, c.cls_code, c.cls_lname
			                                             ORDER BY r.year DESC, r.kind, method
			                                             """ );
			return table.Rows.Cast<DataRow> ( ).Select ( row => new DashboardGroup (
				Convert.ToInt32 ( row [ "year" ] ), ( string ) row [ "kind" ], ( string ) row [ "method" ],
				Convert.ToInt32 ( row [ "count" ] ), DepartmentOf ( row [ "cls_lname" ] as string ),
				row.IsNull ( "cls_code" ) ? int.MaxValue : Convert.ToInt32 ( row [ "cls_code" ] ) ) ).ToList ( );
		}

		/* Classes are named per grade (情報工学科3年), so the department is the name without its grade. */
		public static string DepartmentOf ( string? classLongName )
		{
			string name = GradeSuffix ( ).Replace ( classLongName?.Trim ( ) ?? "", "" );
			return name.Length == 0 ? Unassigned : name;
		}

		[ GeneratedRegex ( @"\s*[0-9０-９]+年$" ) ]
		private static partial Regex GradeSuffix ( );
	}
}