using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageComboBoxSourceProvider
	{
		public static async Task<List<ExamMethodData>> GetExamMethodComboBoxSource ( )
		{
			string query =
				"""select jyukenhouhou_code, jyukenhouhou_name from jyukenhouhouTbl order by jyukenhouhou_code""";
			DataTable table = await MySQLCommand.Query ( query ).ConfigureAwait ( false );
			List<ExamMethodData> datas = new( );
			foreach ( DataRow row in table.Rows )
			{
				ExamMethodData data = new(
					( string ) row [ "jyukenhouhou_code" ],
					( string ) row [ "jyukenhouhou_name" ]
				);
				datas.Add ( data );
			}

			return datas;
		}

		public static async Task<List<DepartmentData>> GetDepartmentComboBoxSource ( )
		{
			string query = """select cls_code, cls_name, cls_lname from clsTbl order by cls_code""";
			DataTable table = await MySQLCommand.Query ( query ).ConfigureAwait ( false );
			List<DepartmentData> datas = new( );
			foreach ( DataRow row in table.Rows )
			{
				DepartmentData data = new(
					( sbyte ) row [ "cls_code" ],
					( string ) row [ "cls_name" ],
					( string ) row [ "cls_lname" ]
				);
				datas.Add ( data );
			}

			return datas;
		}
	}
}