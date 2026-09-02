using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageDataGridSourceProvider
	{
		public static async Task<List<SearchResultData>> GetDataGridSource ( string? target, string? year, string? examMethod, sbyte? department )
		{
			string query = "select * from shingakukekkaTbl limit 100";
			var table = await MySQLCommand.Query(query);
			var datas = new List<SearchResultData>();
			foreach ( DataRow row in table.Rows )
			{
				var data = new SearchResultData(
					( short ) row["year"],
					( string ) row["shingakusaki_code"],
					( string ) row["g_code"],
					( string ) row["jyukenhouhou_code"],
					( bool ) row["gouhi"],
					( DateTime ) row["jyukenbi"]
				);
				datas.Add ( data );
			}

			return datas;
		}
	}
}
