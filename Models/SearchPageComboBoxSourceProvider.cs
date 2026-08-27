using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageComboBoxSourceProvider
	{
		public static async Task<List<ExamMethodData>> GetExamMethodComboBoxSource ( )
		{
			var query = """select * from jyukenhouhouTbl""";
			var table = await MySQLCommand.Query(query);
			var datas = new List<ExamMethodData>();
			foreach ( DataRow row in table.Rows )
			{
				var data = new ExamMethodData()
				{
					Id = ( string ) row["jyukenhouhou_code"],
					Name = ( string )  row["jyukenhouhou_name"]
				};
				datas.Add ( data );
			}

			return datas;
		}

		public static async Task<List<DepartmentData>> GetDepartmentComboBoxSource ( )
		{
			var query = """select * from clsTbl""";
			var table = await MySQLCommand.Query(query);
			var datas = new List<DepartmentData>();
			foreach ( DataRow row in table.Rows )
			{
				var data = new DepartmentData()
				{
					Id = ( sbyte ) row["cls_code"],
					Name = ( string )  row["cls_name"],
					LongName = ( string )  row["cls_lname"]
				};
				datas.Add ( data );
			}

			return datas;
		}
	}
}
