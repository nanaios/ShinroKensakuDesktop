using MySql.Data.MySqlClient;
using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageDataGridSourceProvider
	{
		public static string FormatResult ( object? value ) => value is null or DBNull
			? "未確定"
			: value.ToString ( ) switch
			{
				"True" or "1" => "合格",
				"False" or "0" => "不合格",
				var text => text ?? "未確定"
			};

		public static async Task<List<SearchResultData>> GetDataGridSource ( string? target, string? year,
			string? examMethod, sbyte? department, CancellationToken cancellationToken = default ) =>
			await GetDataGridSource ( SearchCriteria.Create ( target, year, examMethod, department ), cancellationToken ).ConfigureAwait ( false );

		public static async Task<List<SearchResultData>> GetDataGridSource ( SearchCriteria criteria,
			CancellationToken cancellationToken = default )
		{
			string queryShingaku = """
			                       select year,jyukenbi,gakkou_name AS shinro_name,gakubu,gakka,course,NULL AS syuusyokusakinai_kubun,jyukenhouhou_name,g_name,sei,cls_name,gouhi
			                       from shingakukekkaTbl
			                       left join jyukenhouhouTbl
			                       on shingakukekkaTbl.jyukenhouhou_code = jyukenhouhouTbl.jyukenhouhou_code
			                       left join shingakusakiTbl
			                       on shingakukekkaTbl.shingakusaki_code = shingakusakiTbl.shingakusaki_code
			                       left join seitoTbl
			                       on shingakukekkaTbl.g_code = seitoTbl.g_code
			                       left join clsTbl
			                       on seitoTbl.cls_code = clsTbl.cls_code
			                       left join gakkoumeiTbl
			                       on shingakusakiTbl.gakkou_code = gakkoumeiTbl.gakkou_code
			                       """;
			string querySyuusyoku = """
			                        select year,jyukenbi,houjin_name,NULL,NULL,NULL,syuusyokusakinai_kubun,jyukenhouhou_name,g_name,sei,cls_name,gouhi
			                        from syuusyokukekkaTbl
			                        left join jyukenhouhouTbl
			                        on syuusyokukekkaTbl.jyukenhouhou_code = jyukenhouhouTbl.jyukenhouhou_code
			                        left join syuusyokusakiTbl
			                        on syuusyokukekkaTbl.syuusyokusaki_code = syuusyokusakiTbl.syuusyokusaki_code
			                        left join seitoTbl
			                        on syuusyokukekkaTbl.g_code = seitoTbl.g_code
			                        left join clsTbl
			                        on seitoTbl.cls_code = clsTbl.cls_code
			                        left join houjinmeiTbl
			                        on syuusyokusakiTbl.houjin_code = houjinmeiTbl.houjin_code
			                        """;
			string query = string.Empty;

			List<MySqlParameter> parameters = [ ];
			List<string> conditionsShingaku = [ ];
			List<string> conditionsSyuusyoku = [ ];

			if ( criteria.Target != null )
			{
				parameters.Add ( new MySqlParameter ( "@target", $"%{SearchCriteria.EscapeLike ( criteria.Target )}%" ) );
				conditionsShingaku.Add ( "gakkou_name like @target ESCAPE '!'" );
				conditionsSyuusyoku.Add ( "houjin_name like @target ESCAPE '!'" );
			}

			if ( criteria.Year.HasValue )
			{
				parameters.Add ( new MySqlParameter ( "@year", criteria.Year.Value ) );
				conditionsShingaku.Add ( "year = @year" );
				conditionsSyuusyoku.Add ( "year = @year" );
			}

			if ( criteria.ExamMethod != null )
			{
				parameters.Add ( new MySqlParameter ( "@method", criteria.ExamMethod ) );
				conditionsShingaku.Add ( "shingakukekkaTbl.jyukenhouhou_code = @method" );
				conditionsSyuusyoku.Add ( "syuusyokukekkaTbl.jyukenhouhou_code = @method" );
			}

			if ( criteria.Department.HasValue )
			{
				parameters.Add ( new MySqlParameter ( "@department", criteria.Department.Value ) );
				conditionsShingaku.Add ( "seitoTbl.cls_code = @department" );
				conditionsSyuusyoku.Add ( "seitoTbl.cls_code = @department" );
			}

			if ( conditionsShingaku.Count != 0 )
			{
				querySyuusyoku += $"\nwhere {string.Join ( " and ", conditionsSyuusyoku )}";
				queryShingaku += $"\nwhere {string.Join ( " and ", conditionsShingaku )}";
			}

			query += queryShingaku;
			query += "\nunion all\n";
			query += querySyuusyoku;

			query +=
				"\nORDER BY year DESC, jyukenbi DESC, shinro_name, g_name, jyukenhouhou_name, gakubu, gakka, course, syuusyokusakinai_kubun, cls_name, gouhi";

			DataTable table = await MySQLCommand.Query ( query, cancellationToken, parameters.ToArray ( ) ).ConfigureAwait ( false );
			List<SearchResultData> datas = new( );
			foreach ( DataRow row in table.Rows )
			{
				cancellationToken.ThrowIfCancellationRequested ( );
				SearchResultData data = MapRow ( row );
				datas.Add ( data );
			}

			return datas;
		}

		public static SearchResultData MapRow ( DataRow row ) => new()
				{
					Year = row.IsNull ( "year" ) ? null : Convert.ToInt16 ( row [ "year" ] ),
					Jyukenbi = row.IsNull ( "jyukenbi" ) ? null : Convert.ToDateTime ( row [ "jyukenbi" ] ),
					Shinro_name = row [ "shinro_name" ] as string,
					Gakubu = row [ "gakubu" ] as string,
					Gakka = row [ "gakka" ] as string,
					Course = row [ "course" ] as string,
					Syuusyokusakinai_kubun = row [ "syuusyokusakinai_kubun" ] as string,
					Jyukenhouhou_name = row [ "jyukenhouhou_name" ] as string,
					G_name = row [ "g_name" ] as string,
					Sei = row [ "sei" ] as string,
					Cls_name = row [ "cls_name" ] as string,
					Result = FormatResult ( row [ "gouhi" ] )
				};
	}
}
