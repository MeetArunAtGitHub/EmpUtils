using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace EmpUtils
{
    internal sealed class QuerySpec
    {
        internal string ConnectionName;
        internal string Sql;
        internal bool SqlServer;
        internal readonly Dictionary<string, object> Parameters = new Dictionary<string, object>();
        internal QuerySpec(string connectionName, string sql, bool sqlServer = false)
        { ConnectionName = connectionName; Sql = sql; SqlServer = sqlServer; }
        internal string Add(string name, object value) { Parameters.Add(name, value); return "@" + name; }
        internal void AddTime(string name, DateTime value)
        {
            Add(name, SqlServer ? (object)value : value.ToString("yyyy-MM-dd HH:mm:ss.fffffffK", CultureInfo.InvariantCulture));
        }
        internal DbCommand CreateCommand(DbConnection connection)
        {
            DbCommand command = SqlServer ? (DbCommand)new SqlCommand(Sql, (SqlConnection)connection)
                : new NpgsqlCommand(Sql, (NpgsqlConnection)connection);
            command.CommandTimeout = 60;
            foreach (var pair in Parameters)
            {
                if (!SqlServer && pair.Value is string)
                    command.Parameters.Add(new NpgsqlParameter(pair.Key, NpgsqlDbType.Unknown) { Value = pair.Value });
                else
                {
                    var parameter = command.CreateParameter(); parameter.ParameterName = pair.Key;
                    parameter.Value = pair.Value; command.Parameters.Add(parameter);
                }
            }
            return command;
        }
    }

    internal interface IQueryRunner
    {
        Task<DataTable> ExecuteAsync(QuerySpec query, CancellationToken token);
    }

    internal sealed class QueryRunner : IQueryRunner
    {
        public Task<DataTable> ExecuteAsync(QuerySpec query, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var settings = ConfigurationManager.ConnectionStrings[query.ConnectionName];
                if (settings == null) throw new InvalidOperationException("Connection not configured: " + query.ConnectionName);
                string connectionString = Utilities.Decode(settings.ConnectionString);
                using (DbConnection connection = query.SqlServer ? (DbConnection)new SqlConnection(connectionString)
                    : new NpgsqlConnection(connectionString))
                using (var command = query.CreateCommand(connection))
                {
                    await connection.OpenAsync(token).ConfigureAwait(false);
                    using (token.Register(() => { try { command.Cancel(); } catch (Exception) { } }))
                    using (var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        var table = new DataTable(); table.Load(reader); token.ThrowIfCancellationRequested(); return table;
                    }
                }
            }, token);
        }
    }

    internal static class QueryFilters
    {
        internal static int Rows(string value)
        {
            int count;
            if (!int.TryParse(value, out count) || count < 1 || count > 1000)
                throw new ArgumentException("Max rows must be between 1 and 1000.");
            return count;
        }
        internal static string Prefix(string text) => text.Trim().Replace("~", "~~").Replace("%", "~%")
            .Replace("_", "~_") + "%";
        internal static void DateRange(QuerySpec query, DateTime from, DateTime through, string column)
        {
            if (through < from) throw new ArgumentException("The end date must be on or after the start date.");
            query.Sql += " WHERE " + column + " >= @from AND " + column + " <= @through ";
            query.AddTime("from", from); query.AddTime("through", through);
        }
        internal static void Exact(QuerySpec query, string column, string name, string value, bool ignoreCase = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string parameter = query.Add(name, value.Trim());
            query.Sql += ignoreCase ? " AND lower(" + column + ") = lower(" + parameter + ") "
                : " AND " + column + " = " + parameter + " ";
        }
    }
}
