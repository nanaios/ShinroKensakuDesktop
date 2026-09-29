using MySql.Data.MySqlClient;
using System.Data;

namespace ShinroKensakuDesktop.Models
{
	public static class MySQLCommand
	{
		private static readonly string username = "f2024121";
		private static readonly string password = "#Oq8Fa7#";
		private static readonly string database = "sk2026";
		private static readonly string server = "localhost";
		private static readonly string port = "3306";

		private static string ConnectionString => $"server={server};port={port};database={database};uid={username};password={password};";

		public static async Task<DataTable> Query ( string sql, params MySqlParameter[] parameters )
		{
			using var connection = new MySqlConnection(ConnectionString);
			await connection.OpenAsync ( );

			using var command = new MySqlCommand(sql, connection);
			command.Parameters.AddRange(parameters);
            using var reader = await command.ExecuteReaderAsync();

			var table = new DataTable();
			table.Load ( reader );

			return table;
		}
	}
}
