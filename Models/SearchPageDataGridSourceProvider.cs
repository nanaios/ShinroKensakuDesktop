using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageDataGridSourceProvider
	{
		public static async Task<List<SearchResultData>> GetDataGridSource ( string? target, string? year, string? examMethod, sbyte? department )
		{
			string query = """
				select year,jyukenbi,gakubu,gakka,course,jyukenhouhou_name,g_name,sei, cls_name, IF(gouhi = 1, '合格', '不合格') AS result
				from shingakukekkaTbl
				inner join jyukenhouhouTbl
				on shingakukekkaTbl.jyukenhouhou_code = jyukenhouhouTbl.jyukenhouhou_code
				inner join shingakusakiTbl
				on shingakukekkaTbl.shingakusaki_code = shingakusakiTbl.shingakusaki_code
				inner join seitoTbl
				on shingakukekkaTbl.g_code = seitoTbl.g_code
				inner join clsTbl
				on seitoTbl.cls_code = clsTbl.cls_code
				limit 100
				""";
			var table = await MySQLCommand.Query(query);
			var datas = new List<SearchResultData>();
			foreach ( DataRow row in table.Rows )
			{
				var data = new SearchResultData(
					(short)row["year"],
					(DateTime)row["jyukenbi"],
					row["gakubu"] as string,
					row["gakka"] as string,
					row["course"] as string,
					row["jyukenhouhou_name"] as string,
					row["g_name"] as string,
					row["sei"] as string,
					row["cls_name"] as string,
					row["result"] as string
				);
				datas.Add ( data );
			}

			return datas;
		}
	}
}
