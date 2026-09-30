using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace ShinroKensakuDesktop.Models;

public static class MySQLCommand
{
    public static Task<DataTable> Query(string sql, params MySqlParameter[] parameters) =>
        Query(sql, CancellationToken.None, parameters);

    // Keep materialization and synchronous driver work off the WPF dispatcher.
    public static Task<DataTable> Query(string sql, CancellationToken cancellationToken,
        params MySqlParameter[] parameters) => Task.Run(async () =>
    {
        using MySqlConnection connection = new(DatabaseSettings.LoadConnectionString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using MySqlCommand command = new(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddRange(parameters);
        using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        DataTable table = new();
        for (int i = 0; i < reader.FieldCount; i++)
            table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            object[] values = new object[reader.FieldCount];
            reader.GetValues(values);
            table.Rows.Add(values);
        }
        return table;
    }, cancellationToken);

    public static Task TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default) =>
        Task.Run(async () =>
        {
            using MySqlConnection connection = new(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using MySqlCommand command = new("SELECT 1", connection) { CommandTimeout = 15 };
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
}
