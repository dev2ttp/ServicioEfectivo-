using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.TPpagoL2.Services
{
    public class DatabaseResponseMessage
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public List<string> Data { get; set; }

        public static DatabaseResponseMessage Parse(DataSet dataSet)
        {
            var row = dataSet.Tables[0].Rows[0];
            var fields = row[0].ToString().Split('~');
            var statusCode = int.Parse(fields[0]);
            var statusMessage = fields[1];
            var databaseResponseMessage = new DatabaseResponseMessage();

            databaseResponseMessage.StatusCode = statusCode;
            databaseResponseMessage.StatusMessage = statusMessage;
            databaseResponseMessage.IsSuccessStatusCode = (statusCode == 0);
            return databaseResponseMessage;
        }

        public static DatabaseResponseMessage Parse(string value)
        {
            var rows = value.Split('|');
            var fields = rows[0].Split('~');
            var statusCode = int.Parse(fields[0]);
            var statusMessage = fields[1];
            var databaseResponseMessage = new DatabaseResponseMessage();

            databaseResponseMessage.StatusCode = statusCode;
            databaseResponseMessage.StatusMessage = statusMessage;
            databaseResponseMessage.IsSuccessStatusCode = (statusCode == 0);
            databaseResponseMessage.Data = rows.Skip(1).ToList();

            return databaseResponseMessage;
        }

        public void EnsureSuccessStatusCode()
        {
            if (!IsSuccessStatusCode)
            {
                throw new Exception(StatusMessage);
            }
        }
    }
}
