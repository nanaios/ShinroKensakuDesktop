using System.Data;

namespace ShinroKensakuDesktop.Models;

public record DashboardGroup(int Year, string Kind, string Method, int Count);
public record DashboardSummary(string Label, int Count);

public static class DashboardDataProvider
{
    public static async Task<List<DashboardGroup>> LoadAsync()
    {
        // UNION ALL preserves separate exam records, including repeat applications.
        var table = await MySQLCommand.Query("""
            SELECT r.year, r.kind, COALESCE(m.jyukenhouhou_name, '未設定') AS method, COUNT(*) AS count
            FROM (
                SELECT year, '進学' AS kind, jyukenhouhou_code FROM shingakukekkaTbl
                UNION ALL
                SELECT year, '就職' AS kind, jyukenhouhou_code FROM syuusyokukekkaTbl
            ) r
            LEFT JOIN jyukenhouhouTbl m ON r.jyukenhouhou_code = m.jyukenhouhou_code
            GROUP BY r.year, r.kind, m.jyukenhouhou_name
            ORDER BY r.year DESC, r.kind, method
            """);
        return table.Rows.Cast<DataRow>().Select(row => new DashboardGroup(
            Convert.ToInt32(row["year"]), (string)row["kind"], (string)row["method"],
            Convert.ToInt32(row["count"]))).ToList();
    }
}
