using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageDataGridSourceProvider
	{
		public static async Task<List<SearchResultData>> GetDataGridSource ( string? target, string? year, string? examMethod, sbyte? department )
		{
			string query = """
				select year,jyukenbi,gakkou_name,gakubu,gakka,course,jyukenhouhou_name,g_name,sei, cls_name, IF(gouhi = 1, '合格', '不合格') AS result
				from shingakukekkaTbl
				inner join jyukenhouhouTbl
				on shingakukekkaTbl.jyukenhouhou_code = jyukenhouhouTbl.jyukenhouhou_code
				inner join shingakusakiTbl
				on shingakukekkaTbl.shingakusaki_code = shingakusakiTbl.shingakusaki_code
				inner join seitoTbl
				on shingakukekkaTbl.g_code = seitoTbl.g_code
				inner join clsTbl
				on seitoTbl.cls_code = clsTbl.cls_code
				inner join gakkoumeiTbl
				on  shingakusakiTbl.gakkou_code = gakkoumeiTbl.gakkou_code
				""";

			List<string> conditions = [];
			if ( string.IsNullOrEmpty ( target ) == false )
			{
				conditions.Add ( $"gakkou_name like '%{target}%'" );
			}
			if ( string.IsNullOrEmpty ( year ) == false )
			{
				conditions.Add ( $"year = '{year}'" );
			}
			if ( string.IsNullOrEmpty ( examMethod ) == false )
			{
				conditions.Add ( $"shingakukekkaTbl.jyukenhouhou_code = '{examMethod}'" );
			}
			if ( department.HasValue )
			{
				conditions.Add ( $"seitoTbl.cls_code = '{department}'" );
			}

			if ( conditions.Count != 0 )
			{
				query += $"\nwhere {string.Join ( " and ", conditions )}";
			}

			var table = await MySQLCommand.Query(query);
			var datas = new List<SearchResultData>();
			foreach ( DataRow row in table.Rows )
			{
				var data = new SearchResultData
				{
					Year = (short)row["year"],
					Jyukenbi = (DateTime)row["jyukenbi"],
					Gakkou_name = row["gakkou_name"] as string,
					Gakubu = row["gakubu"] as string,
					Gakka = row["gakka"] as string,
					Course = row["course"] as string,
					Jyukenhouhou_name = row["jyukenhouhou_name"] as string,
					G_name = row["g_name"] as string,
					Sei = row["sei"] as string,
					Cls_name = row["cls_name"] as string,
					Result = row["result"] as string
				};
				datas.Add ( data );
			}

			return datas;
		}
	}
}
