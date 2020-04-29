using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TotalPack.Efectivo.TPpagoL2.Properties;

namespace TotalPack.Efectivo.TPpagoL2.Services
{
    class BaseRepository
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(BaseRepository));
        private static readonly object lockObject = new object();
        private const string Space = " ";
        protected static string connectionString = GetConnectionString();
        protected int IdUsr = int.Parse(Settings.Default.IdUsr);

        private static string GetConnectionString()
        {
            return string.Format(
                "Port = {0}; Host= {1}; Username = {2}; Password = {3}; Database = {4}",
                Settings.Default.DBPort, Settings.Default.DBHost, Settings.Default.DBUser, Settings.Default.DBPass, Settings.Default.DBNombre);
        }

        private void LogRequest(NpgsqlCommand cmd)
        {
            var sql = string.Format("select * from {0} ", cmd.CommandText);

            foreach (NpgsqlParameter kvp in cmd.Parameters)
            {
                if (kvp.Value is string)
                {
                    sql += string.Format("'{0}', ", kvp.Value);
                }
                else if (kvp.Value == null)
                {
                    sql += string.Format("null, ");
                }
                else
                {
                    sql += string.Format("{0}, ", kvp.Value);
                }
            }

            sql = sql.TrimEnd();
            sql = sql.Remove(sql.Length - 1); // Removes last comma
            log.Info(sql);
        }

        private void LogResponse()
        {
            log.Info("Stored procedure executed successfully");
        }

        protected DataSet ExecuteStoredProcedure(string procedureName, Dictionary<string, object> parameters)
        {
            lock (lockObject)
            {
                using (var sqlConnection = new NpgsqlConnection(connectionString))
                using (var cmd = new NpgsqlCommand(procedureName, sqlConnection))
                using (var adapter = new NpgsqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var kvp in parameters)
                        {
                            if (kvp.Value is Enum)
                            {
                                cmd.Parameters.AddWithValue(null, (int)kvp.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue(null, kvp.Value ?? DBNull.Value);
                            }
                        }
                    }

                    LogRequest(cmd);
                    sqlConnection.Open();

                    var ds = new DataSet();
                    adapter.Fill(ds);
                    LogResponse();

                    return ds;
                }
            }
        }
    }
}
