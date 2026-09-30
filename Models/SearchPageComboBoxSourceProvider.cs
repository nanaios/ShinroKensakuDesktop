using ShinroKensakuDesktop.Models.Data;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class SearchPageComboBoxSourceProvider
	{
		public static async Task<List<ExamMethodData>> GetExamMethodComboBoxSource ( )
		{
			string query = """select * from jyukenhouhouTbl""";
			DataTable table = await MySQLCommand.Query ( query );
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
			string query = """select * from clsTbl""";
			DataTable table = await MySQLCommand.Query ( query );
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