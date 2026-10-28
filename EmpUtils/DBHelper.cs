// ""

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace EmpUtils
{
    public static class DbHelper
    {
        private static string sqlConnStr = "sql_connection_string";
        private static string pgConnStr = "pg_connection_string";
        
        public static async Task<DataTable> ExecuteSQLSPAsync(string spName)
        {
            return await Task.Run(() =>
            {
                using (SqlConnection conn = new SqlConnection(sqlConnStr))
                using (SqlCommand cmd = new SqlCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            });
        }

        public static async Task<DataTable> ExecuteNpgSQLSPAsync(string spName)
        {
            //convert the below method to work with PostgreSQL using Npgsql library instead of SqlClient

            return await Task.Run(() =>
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(pgConnStr))
                using (NpgsqlCommand cmd = new NpgsqlCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            });
        }
    }
}
