using MySql.Data.MySqlClient;
using ShinroKensakuDesktop.Models.Data;
using System.Data;
using System.Diagnostics;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageDataGridSourceProvider
	{
		public static string FormatResult(object value) => value == DBNull.Value ? "未確定" : value.ToString() switch
        {
            "True" or "1" => "合格",
            "False" or "0" => "不合格",
            var text => text ?? "未確定"
        };

        public static async Task<List<SearchResultData>> GetDataGridSource ( string? target, string? year, string? examMethod, sbyte? department )
		{
			string queryShingaku = """
				select year,jyukenbi,gakkou_name AS shinro_name,gakubu,gakka,course,NULL AS syuusyokusakinai_kubun,jyukenhouhou_name,g_name,sei,cls_name,gouhi
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
				on shingakusakiTbl.gakkou_code = gakkoumeiTbl.gakkou_code
				""";
			string querySyuusyoku = """
			                        select year,jyukenbi,houjin_name,NULL,NULL,NULL,syuusyokusakinai_kubun,jyukenhouhou_name,g_name,sei,cls_name,gouhi
			                        from syuusyokukekkaTbl
			                        inner join jyukenhouhouTbl
			                        on syuusyokukekkaTbl.jyukenhouhou_code = jyukenhouhouTbl.jyukenhouhou_code
			                        inner join syuusyokusakiTbl
			                        on syuusyokukekkaTbl.syuusyokusaki_code = syuusyokusakiTbl.syuusyokusaki_code
			                        inner join seitoTbl
			                        on syuusyokukekkaTbl.g_code = seitoTbl.g_code
			                        inner join clsTbl
			                        on seitoTbl.cls_code = clsTbl.cls_code
			                        inner join houjinmeiTbl
			                        on syuusyokusakiTbl.houjin_code = houjinmeiTbl.houjin_code
			                        """;
			string query = string.Empty;

			List<MySqlParameter> parameters = [];
            List<string> conditionsShingaku = [];
			List<string> conditionsSyuusyoku = [];
			
			if ( string.IsNullOrEmpty ( target ) == false )
			{
				parameters.Add(new MySqlParameter("@target", $"%{target}%"));
                conditionsShingaku.Add ( "gakkou_name like @target" );
				conditionsSyuusyoku.Add ( "houjin_name like @target" );
			}
			if ( string.IsNullOrEmpty ( year ) == false )
			{
				parameters.Add(new MySqlParameter("@year", year));
                conditionsShingaku.Add ( "year = @year" );
				conditionsSyuusyoku.Add ( "year = @year" );
			}
			if ( string.IsNullOrEmpty ( examMethod ) == false )
			{
				parameters.Add(new MySqlParameter("@method", examMethod));
                conditionsShingaku.Add ( "shingakukekkaTbl.jyukenhouhou_code = @method" );
				conditionsSyuusyoku.Add ( "syuusyokukekkaTbl.jyukenhouhou_code = @method" );
				
			}
			if ( department.HasValue )
			{
				parameters.Add(new MySqlParameter("@department", department.Value));
                conditionsShingaku.Add ( "seitoTbl.cls_code = @department" );
				conditionsSyuusyoku.Add ( "seitoTbl.cls_code = @department" );
				
			}

			if ( conditionsShingaku.Count != 0 )
			{
				querySyuusyoku += $"\nwhere {string.Join ( " and ", conditionsSyuusyoku)}";
				queryShingaku += $"\nwhere {string.Join ( " and ", conditionsShingaku )}";
			}
			
			query += queryShingaku;
			query += "\nunion all\n";
			query += querySyuusyoku;
			
			query += "\nORDER BY year DESC, jyukenbi DESC, shinro_name, g_name, jyukenhouhou_name, gakubu, gakka, course, syuusyokusakinai_kubun, cls_name, gouhi";

			var table = await MySQLCommand.Query(query, parameters.ToArray());
			var datas = new List<SearchResultData>();
			foreach ( DataRow row in table.Rows )
			{
				var data = new SearchResultData
				{
					Year = (short)row["year"],
					Jyukenbi = (DateTime)row["jyukenbi"],
					Shinro_name = row["shinro_name"] as string,
					Gakubu = row["gakubu"] as string,
					Gakka = row["gakka"] as string,
					Course = row["course"] as string,
					Syuusyokusakinai_kubun = row["syuusyokusakinai_kubun"] as string,
					Jyukenhouhou_name = row["jyukenhouhou_name"] as string,
					G_name = row["g_name"] as string,
					Sei = row["sei"] as string,
					Cls_name = row["cls_name"] as string,
					Result = FormatResult(row["gouhi"])
				};
				datas.Add ( data );
			}

			return datas;
		}
	}
}
