using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TotalPack.Efectivo.SSP;
using TotalPack.Efectivo.TPpagoL2.Models;
using TotalPack.Efectivo.TPpagoL2.Other;
using TotalPack.Efectivo.TPpagoL2.Properties;
using TPpagoL2.MessageProtocol;

namespace TotalPack.Efectivo.TPpagoL2.Services
{
    class DBConnectionService : BaseRepository
    {
        private static readonly Session session = new Session();
        private string connString;
        private string resp;
        private string Port;
        private string Host;
        private string Username;
        private string Password;
        private string Database;
        private DataSet ds;
        private DataTable dt;
        List<string> dataResp;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DBConnectionService()
        {
            Port = Settings.Default.DBPort;
            Host = Settings.Default.DBHost;
            Username = Settings.Default.DBUser;
            Password = Settings.Default.DBPass;
            Database = Settings.Default.DBNombre;

            connString = String.Format("Port = {0}; Host= {1}; Username = {2}; Password = {3}; Database = {4}",
                Port, Host, Username, Password, Database);

            ds = new DataSet();
            dt = new DataTable();
        }

        public string inCargaDinero(List<string> msg)//listo
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {

                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {

                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);
            var accion = "GAVIN";
            var idacc = "307";
            var snd = "";
            var tip = "";
            var sng = msg[1];//necesario lo rescato de la data de msg, es el numero serie de la gaveta

            msg.RemoveRange(0, 2);

            var data = formatDenData(msg);//necesario, denominaciones montos a ingresar en la gaveta

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            // data adapter making request from our connection
            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            // i always reset DataSet before i do
            ds.Reset();
            // filling DataSet with result from NpgsqlDataAdapter
            da.Fill(ds);
            // since it C# DataSet can handle multiple tables, we will select first
            dt = ds.Tables[0];
            // since we only showing the result we don't need connection anymore
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());
        }

        public string inRetiroDinero(List<string> msg)//listo
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVOUT";
            var idacc = "308";
            var snd = "";
            var tip = "";
            var sng = msg[0]; //necesario

            msg.RemoveAt(0);

            var data = formatDenData(msg); //necesario denominaciones y cantidades a agregar

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());
        }

        public string inDiscrepancia(List<string> msg)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);
            var accion = "DISCRE";
            var idacc = "309";
            var snd = "";
            var tip = "";
            var sng = msg[0];//necesario lo rescato de la data de msg, es el numero serie de la gaveta

            msg.RemoveAt(0);

            var data = formatDenData(msg);//necesario, denominaciones montos a ingresar en la gaveta

            conn.Open();

            string sql = String.Format("select * from sp_app_gavope('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            // data adapter making request from our connection
            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            // i always reset DataSet before i do
            ds.Reset();
            // filling DataSet with result from NpgsqlDataAdapter
            da.Fill(ds);
            // since it C# DataSet can handle multiple tables, we will select first
            dt = ds.Tables[0];
            // since we only showing the result we don't need connection anymore
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_app_gavope"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string procResumenSaldo()//listo, misma accion que gavget
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVGET";
            var idacc = "310";
            var snd = "";
            var tip = "";
            var sng = "";
            var data = "";

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string getGavetas()//listo
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVGET";
            var idacc = "311";
            var snd = "";
            var tip = "";
            var sng = "";
            var data = "";

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string inSetDispositivo(List<string> msg)//seteo manual dispositivo
        {
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DSPSET";
            var idacc = "312";
            var snd = "";//necesario
            var tip = msg[0];//necesario
            var sng = "";
            var data = "";

            if (tip == "M")
            {
                //test
                //snd = "44555";
                /////
                snd = Estate.numeroSerieHopper;
            }

            if (tip == "B")
            {
                //test
                //snd = "55544";
                /////
                snd = Estate.numeroSeriePayout;
            }

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string retiroDispositivo(List<string> msg)
        {
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DSPOFF";
            var idacc = "313";
            var snd = "";//necesario
            var tip = msg[0];
            string inTip = "";
            var sng = "";
            var data = "";

            if (tip == "M")
            {
                //test
                //snd = "44555";
                snd = Estate.numeroSerieHopper;
            }

            if (tip == "B")
            {
                //test
                //snd = "55544";
                /////
                snd = Estate.numeroSeriePayout;
                //snd = 4797475.ToString();
            }

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, inTip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());
        }

        public string inSetGaveta(List<string> msg)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVSET";
            var idacc = "314";
            var tip = msg[0];
            string inTip = "";
            var sng = msg[1];
            var data = "";
            string snd = "";

            if (tip == "M" || tip == "MB")
            {
                //test
                //snd = "44555";
                /////
                snd = Estate.numeroSerieHopper;
            }

            if (tip == "B")
            {
                //test
                //snd = "55544";
                /////
                snd = Estate.numeroSeriePayout;
            }

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, inTip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string vaciarGavetaB(List<string> msg, List<ChannelData> PayOutList, List<string> gavdata)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVVAC";
            var idacc = "322";
            var tip = "";
            var sng = msg[0];
            var data = "";
            string snd = msg[1];

            /////
            //snd = Estate.numeroSeriePayout;//para este metodo no necesito el snd de la maquina

            //foreach (ChannelData d in PayOutList)
            //{
            //    var den = d.Value / 100;
            //    var q = d.Level;
            //    var inData = den + "," + q;
            //    dataQuery.Add(inData);
            //}

            data = formatDenData(gavdata);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            log.Info("vaciado gaveta B sql: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            if (!resp.Equals("0~OK"))
            {
                RestartPaymentSerie();
                log.Info("Respuesta Negativa: " + resp);

            }
            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: vaciado gaveta B sql: " + resp);

            return resp;

        }
        public string AutoDisM(List<string> msg, List<ChannelData> PayOutList)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DISCRE";
            var idacc = "397";
            var tip = "";
            var sng = msg[0];
            var data = "";
            string snd = msg[1];

            /////
            //snd = Estate.numeroSeriePayout;//para este metodo no necesito el snd de la maquina

            foreach (ChannelData d in PayOutList)
            {
                var den = d.Value;
                var q = d.Level;
                var inData = den + "," + q;
                dataQuery.Add(inData);
            }

            data = formatDenData(dataQuery);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            log.Info("AutoDiscrepancia Mondas sql: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            if (!resp.Equals("0~OK"))
            {
                RestartPaymentSerie();
                log.Info("Respuesta Negativa: " + resp);

            }
            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: vaciado gaveta B sql: " + resp);

            return resp;

        }

        public string AutoDisB(List<string> msg, List<ChannelData> PayOutList)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DISCRE";
            var idacc = "396";
            var tip = "";
            var sng = msg[0];//origen
            var data = "";
            string snd = msg[1];//destino

            /////
            //snd = Estate.numeroSeriePayout;//para este metodo no necesito el snd de la maquina

            foreach (ChannelData d in PayOutList)
            {
                var den = d.Value / 100;
                var q = d.Level;
                var inData = den + "," + q;
                dataQuery.Add(inData);
            }

            data = formatDenData(dataQuery);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            log.Info("AutoDiscrepancia Billetes sql: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            if (!resp.Equals("0~OK"))
            {
                RestartPaymentSerie();
                log.Info("Respuesta Negativa: " + resp);

            }
            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: vaciado gaveta B sql: " + resp);

            return resp;

        }
        public string ConsultarGavetaDiscreB(List<string> msg, List<ChannelData> PayOutList)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVMONCHK";
            var idacc = "399";
            var tip = "";
            var sng = msg[0];//origen
            var data = "";
            string snd = msg[1];//destino

            foreach (ChannelData d in PayOutList)
            {
                var den = d.Value / 100;
                var q = d.Level;
                var inData = den + "," + q;
                dataQuery.Add(inData);
            }

            //data = formatDenData(gavdata);
            data = formatDenData(dataQuery);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            log.Info("Discrepancia gaveta B sql: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            if (!resp.Equals("0~OK"))
            {
                RestartPaymentSerie();
                log.Info("Respuesta Negativa: " + resp);

            }
            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: vaciado gaveta B sql: " + resp);

            return resp;

        }

        private void RestartPaymentSerie()
        {
            session.NotesPaid = 0;
            session.CoinsPaid = 0;
            session.BillsDispensedBeforeTimeout = 0;
            session.CoinsDispensedBeforeTimeout = 0;
            session.PayoutAmount = 0;

            session.CoinDispenseFinished = false;
            session.BillDispenseFinished = false;
            session.CoinsUsedInPayout = false;
            session.BillsUsedInPayout = false;
            session.IncompleteCoinPayoutDetected = false;
            session.CoinPayoutTimeOut = false;
            session.BillPayoutTimeOut = false;
            session.CreditsPaid = null;

        }

        public string vaciarGavetaM(List<string> msg, List<ChannelData> HopperList, List<string> datahoppe)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVVAC";
            var idacc = "321";
            var tip = "";
            var sng = msg[0];
            var data = "";
            string snd = msg[1];

            /////
            //snd = Estate.numeroSeriePayout;//para este metodo no necesito el snd de la maquina

            //foreach (ChannelData d in HopperList)
            //{
            //    var den = d.Value;
            //    var q = d.Level;
            //    var inData = den + "," + q;
            //    dataQuery.Add(inData);
            //}

            data = formatDenData(datahoppe);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            log.Info("vaciado gaveta M sql: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: vaciado gaveta M sql: " + resp);

            return resp;

        }

        public string ConsultarGavetaDiscreM(List<string> msg, List<ChannelData> PayOutList)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            //var accion = "DISCRE";
            var accion = "GAVMONCHK";
            var idacc = "398";
            var tip = "";
            var sng = msg[0];//origen
            var data = "";
            string snd = msg[1];//destino

            foreach (ChannelData d in PayOutList)
            {
                var den = d.Value;
                var q = d.Level;
                var inData = den + "," + q;
                dataQuery.Add(inData);
            }

            data = formatDenData(dataQuery);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            log.Info("Discrepancia gaveta B sql: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            if (!resp.Equals("0~OK"))
            {
                RestartPaymentSerie();
                log.Info("Respuesta Negativa: " + resp);

            }
            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: vaciado gaveta B sql: " + resp);

            return resp;

        }


        public string retiroGavetaB(List<string> msg, List<ChannelData> PayOutList)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVOFF";
            var idacc = "315";
            var tip = "";
            var sng = msg[0];
            var data = "";
            string snd = "";

            //test
            //snd = "55544";
            /////
            snd = Estate.numeroSeriePayout;

            foreach (ChannelData d in PayOutList)
            {
                var den = d.Value / 100;
                var q = d.Level;
                var inData = den + "," + q;
                dataQuery.Add(inData);
            }

            data = formatDenData(dataQuery);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string retiroGavetaM(List<string> msg, List<ChannelData> HopperList)
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            List<string> dataQuery = new List<string>();
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "GAVOFF";
            var idacc = "320";
            var tip = "";
            var sng = msg[0];
            var data = "";
            string snd = "";

            //test
            //snd = "44555";
            /////
            snd = Estate.numeroSerieHopper;

            foreach (ChannelData d in HopperList)
            {
                var den = d.Value / 100;
                var q = d.Level;
                var inData = den + "," + q;
                dataQuery.Add(inData);
            }

            data = formatDenData(dataQuery);

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public string getDenominaciones() //listo
        {
            if (Estate.estadoSerieDispM[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispM.ToArray());
            }

            if (Estate.estadoSerieDispB[0] != "0~OK")
            {
                return resp = String.Join("|", Estate.estadoSerieDispB.ToArray());
            }

            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DENGET";
            var idacc = "316";
            var snd = "";
            var tip = "";
            var sng = "";
            var data = "";

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_GavOpe"].ToString();
                dataResp.Add(rowData);
            }

            return resp = String.Join("|", dataResp.ToArray());

        }

        public void checkSerieDispositivoB()
        {
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DSPCHK";
            var idacc = "317";
            var snd = Estate.numeroSeriePayout;//Estate.numeroSeriePayout //test: 55544;
            var tip = "B";
            var sng = "";
            var data = "";

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);
            try
            {
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
                ds.Reset();
                da.Fill(ds);
                dt = ds.Tables[0];
                conn.Close();

                foreach (DataRow row in (dt ?? new DataTable()).Rows)
                {
                    var rowData = row["sp_App_GavOpe"].ToString();
                    dataResp.Add(rowData);
                }

                if (dataResp[0] != "0~OK")
                {
                    setEvento(CommandEventos.DifSerialDispB);
                }

                Estate.estadoSerieDispB = dataResp;
            }
            catch (Exception e)
            {
                List<string> exList = new List<string>();
                exList.Add("-1~NOK");

                Estate.estadoSerieDispB = exList;
            }
        }

        public void checkSerieDispositivoM()
        {
            dataResp = new List<string>();

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "DSPCHK";
            var idacc = "319";
            var snd = Estate.numeroSerieHopper;//Estate.numeroSerieHopper //Test: 44555
            var tip = "M";
            var sng = "";
            var data = "";

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            try
            {
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
                ds.Reset();
                da.Fill(ds);
                dt = ds.Tables[0];
                conn.Close();

                foreach (DataRow row in (dt ?? new DataTable()).Rows)
                {
                    var rowData = row["sp_App_GavOpe"].ToString();
                    dataResp.Add(rowData);
                }

                if (dataResp[0] != "0~OK")
                {
                    setEvento(CommandEventos.DifSerialDispM);
                }

                Estate.estadoSerieDispM = dataResp;
            }
            catch (Exception e)
            {
                List<string> exList = new List<string>();
                exList.Add("-1~NOK");

                Estate.estadoSerieDispM = exList;
            }

        }

        public void setEvento(CommandEventos idEvento)
        {

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "EVESET";//FALTA
            var idacc = "318";
            var snd = "";
            var tip = "";
            var sng = "";
            var data = "";
            string idEve = "";
            string tipo = "";

            switch (idEvento)
            {
                case CommandEventos.DifSerialDispB:
                    idEve = "501";
                    tipo = "E";
                    data = idEve + "," + tipo + "," + Estate.numeroSerieHopper;
                    break;
                case CommandEventos.DifSerialDispM:
                    idEve = "500";
                    tipo = "E";
                    data = idEve + "," + tipo + "," + Estate.numeroSeriePayout;
                    break;
                case CommandEventos.GavetaBVaciada:
                    idEve = "502";
                    tipo = "I";
                    data = idEve + "," + tipo + "," + "Gaveta B vaciada";
                    break;
                case CommandEventos.GavetaMVaciada:
                    idEve = "503";
                    tipo = "I";
                    data = idEve + "," + tipo + "," + "Gaveta M vaciada";
                    break;
                case CommandEventos.SspCmdReset:
                    idEve = "0X01";
                    tipo = "E";
                    data = "SSP_CMD_RESET";
                    break;
                case CommandEventos.SspCmdChannelInhibits:
                    idEve = "0X02";
                    tipo = "E";
                    data = "SSP_CMD_SET_CHANNEL_INHIBITS";
                    break;
                case CommandEventos.SspCmdDisplayOn:
                    idEve = "0X03";
                    tipo = "E";
                    data = "SSP_CMD_DISPLAY_ON";
                    break;
                case CommandEventos.SspCmdDisplayOff:
                    idEve = "0X04";
                    tipo = "E";
                    data = "SSP_CMD_DISPLAY_OFF";
                    break;
                case CommandEventos.SspCmdSetupRequest:
                    idEve = "0X05";
                    tipo = "E";
                    data = "SSP_CMD_SETUP_REQUEST";
                    break;
                case CommandEventos.SspCmdHostProtocolVersion:
                    idEve = "0X06";
                    tipo = "E";
                    data = "SSP_CMD_HOST_PROTOCOL_VERSION";
                    break;
                case CommandEventos.SspCmdPoll:
                    idEve = "0X07";
                    tipo = "E";
                    data = "SSP_CMD_POLL";
                    break;
                case CommandEventos.SspCmdRejectBanknote:
                    idEve = "0X08";
                    tipo = "E";
                    data = "SSP_CMD_REJECT_BANKNOTE";
                    break;
                case CommandEventos.SspCmdDisable:
                    idEve = "0X09";
                    tipo = "E";
                    data = "SSP_CMD_DISABLE";
                    break;
                case CommandEventos.SspCmdEnable:
                    idEve = "0X0A";
                    tipo = "E";
                    data = "SSP_CMD_ENABLE";
                    break;
                case CommandEventos.SspCmdGetSerialNumber:
                    idEve = "0X0C";
                    tipo = "E";
                    data = "SSP_CMD_GET_SERIAL_NUMBER";
                    break;
                case CommandEventos.SspCmdUnitData:
                    idEve = "0X0D";
                    tipo = "E";
                    data = "SSP_CMD_UNIT_DATA";
                    break;
                case CommandEventos.SspCmdChannelValueRequest:
                    idEve = "0X0E";
                    tipo = "E";
                    data = "SSP_CMD_CHANNEL_VALUE_REQUEST";
                    break;
                case CommandEventos.SspCmdChannelSecurityData:
                    idEve = "0X0F";
                    tipo = "E";
                    data = "SSP_CMD_CHANNEL_SECURITY_DATA";
                    break;
                case CommandEventos.SspCmdChannelReTeachData:
                    idEve = "0X10";
                    tipo = "E";
                    data = "SSP_CMD_CHANNEL_RE_TEACH_DATA";
                    break;
                case CommandEventos.SspCmdSync:
                    idEve = "0X11";
                    tipo = "E";
                    data = "SSP_CMD_SYNC";
                    break;
                case CommandEventos.SspCmdLastRejectCode:
                    idEve = "0X17";
                    tipo = "E";
                    data = "SSP_CMD_LAST_REJECT_CODE";
                    break;
                case CommandEventos.SspCmdHold:
                    idEve = "0X18";
                    tipo = "E";
                    data = "SSP_CMD_HOLD";
                    break;
                case CommandEventos.SspCmdGetFirmwareVersion:
                    idEve = "0X20";
                    tipo = "E";
                    data = "SSP_CMD_GET_FIRMWARE_VERSION";
                    break;
                case CommandEventos.SspCmdGetDatasetVersion:
                    idEve = "0X21";
                    tipo = "E";
                    data = "SSP_CMD_GET_DATASET_VERSION";
                    break;
                case CommandEventos.SspCmdGetAllLevels:
                    idEve = "0X22";
                    tipo = "E";
                    data = "SSP_CMD_GET_ALL_LEVELS";
                    break;
                case CommandEventos.SspCmdGetBarCodeReaderConfiguration:
                    idEve = "0X23";
                    tipo = "E";
                    data = "SSP_CMD_GET_BAR_CODE_READER_CONFIGURATION";
                    break;
                case CommandEventos.SspcmdSetBarCodeConfiguration:
                    idEve = "0X24";
                    tipo = "E";
                    data = "SSP_CMD_SET_BAR_CODE_CONFIGURATION";
                    break;
                case CommandEventos.SspCmdGetBarCodeInhibitStatus:
                    idEve = "0X25";
                    tipo = "E";
                    data = "SSP_CMD_GET_BAR_CODE_INHIBIT_STATUS";
                    break;
                case CommandEventos.SspCmdSetBarCodeInhbitStatus:
                    idEve = "0X26";
                    tipo = "E";
                    data = "SSP_CMD_SET_BAR_CODE_INHIBIT_STATUS";
                    break;
                case CommandEventos.SspCmdGetBarCodeData:
                    idEve = "0X27";
                    tipo = "E";
                    data = "SSP_CMD_GET_BAR_CODE_DATA";
                    break;
                case CommandEventos.SspCmdSetRefillMode:
                    idEve = "0X30";
                    tipo = "E";
                    data = "SSP_CMD_SET_REFILL_MODE";
                    break;
                case CommandEventos.SspCmdPayoutAmount:
                    idEve = "0X33";
                    tipo = "E";
                    data = "SSP_CMD_PAYOUT_AMOUNT";
                    break;
                case CommandEventos.SspCmdSetDenominationLevel:
                    idEve = "0X34";
                    tipo = "E";
                    data = "SSP_CMD_SET_DENOMINATION_LEVEL";
                    break;
                case CommandEventos.SspCmdGetDenominationLevel:
                    idEve = "0X35";
                    tipo = "E";
                    data = "SSP_CMD_GET_DENOMINATION_LEVEL";
                    break;
                case CommandEventos.SspCmdCommuncationPassThrough:
                    idEve = "0X37";
                    tipo = "E";
                    data = "SSP_CMD_COMMUNICATION_PASS_THROUGH";
                    break;
                case CommandEventos.SspCmdHaltPayout:
                    idEve = "0X38";
                    tipo = "E";
                    data = "SSP_CMD_HALT_PAYOUT";
                    break;
                case CommandEventos.SspCmdSetDenominationRoute:
                    idEve = "0X3B";
                    tipo = "E";
                    data = "SSP_CMD_SET_DENOMINATION_ROUTE";
                    break;
                case CommandEventos.SspCmdGetDenominationRoute:
                    idEve = "0X3C";
                    tipo = "E";
                    data = "SSP_CMD_GET_DENOMINATION_ROUTE";
                    break;
                case CommandEventos.SspCmdFloatAmount:
                    idEve = "0X3D";
                    tipo = "E";
                    data = "SSP_CMD_FLOAT_AMOUNT";
                    break;
                case CommandEventos.SspCmdGetMinimunPauout:
                    idEve = "0X3E";
                    tipo = "E";
                    data = "SSP_CMD_FLOAT_AMOUNT";
                    break;
                case CommandEventos.SspCmdEmptyAll:
                    idEve = "0X3F";
                    tipo = "E";
                    data = "SSP_CMD_FLOAT_AMOUNT";
                    break;
                case CommandEventos.SspCmdSetCoinMechInhibits:
                    idEve = "0X40";
                    tipo = "E";
                    data = "SSP_CMD_SET_COIN_MECH_INHIBITS";
                    break;
                case CommandEventos.SspCmdGetNotePositions:
                    idEve = "0X41";
                    tipo = "E";
                    data = "SSP_CMD_GET_NOTE_POSITIONS";
                    break;
                case CommandEventos.SspCmdPayoutNote:
                    idEve = "0X42";
                    tipo = "E";
                    data = "SSP_CMD_PAYOUT_NOTE";
                    break;
                case CommandEventos.SspCmdStackNote:
                    idEve = "0X43";
                    tipo = "E";
                    data = "SSP_CMD_STACK_NOTE";
                    break;
                case CommandEventos.SspCmdFloatByDenomination:
                    idEve = "0X44";
                    tipo = "E";
                    data = "SSP_CMD_FLOAT_BY_DENOMINATION";
                    break;
                case CommandEventos.SspCmdSetValueReportingType:
                    idEve = "0X45";
                    tipo = "E";
                    data = "SSP_CMD_SET_VALUE_REPORTING_TYPE";
                    break;
                case CommandEventos.SspCmdPayoutByDenomination:
                    idEve = "0X46";
                    tipo = "E";
                    data = "SSP_CMD_PAYOUT_BY_DENOMINATION";
                    break;
                case CommandEventos.SspCmdSetCoinMechGlobalInhibit:
                    idEve = "0X49";
                    tipo = "E";
                    data = "SSP_CMD_SET_COIN_MECH_GLOBAL_INHIBIT";
                    break;
                case CommandEventos.SspCmdSetGenerator:
                    idEve = "0X4A";
                    tipo = "E";
                    data = "SSP_CMD_SET_GENERATOR";
                    break;
                case CommandEventos.SspCmdSetModulus:
                    idEve = "0X4B";
                    tipo = "E";
                    data = "SSP_CMD_SET_MODULUS";
                    break;
                case CommandEventos.SspCmdRequestKeyExchange:
                    idEve = "0X4C";
                    tipo = "E";
                    data = "SSP_CMD_REQUEST_KEY_EXCHANGE";
                    break;
                case CommandEventos.SspCmdSetBaudRate:
                    idEve = "0X4D";
                    tipo = "E";
                    data = "SSP_CMD_SET_BAUD_RATE";
                    break;
                case CommandEventos.sspCmdGetBuildrevision:
                    idEve = "0X4F";
                    tipo = "E";
                    data = "SSP_CMD_GET_BUILD_REVISION";
                    break;
                case CommandEventos.SspCmdSetHopperOptions:
                    idEve = "0X50";
                    tipo = "E";
                    data = "SSP_CMD_SET_HOPPER_OPTIONS";
                    break;
                case CommandEventos.SspCmdGetHopperOption:
                    idEve = "0X51";
                    tipo = "E";
                    data = "SSP_CMD_GET_HOPPER_OPTIONS";
                    break;
                case CommandEventos.SspCmdSmartEmpty:
                    idEve = "0X52";
                    tipo = "E";
                    data = "SSP_CMD_SMART_EMPTY";
                    break;
                case CommandEventos.SspCmdCashBoxPayoutOperationData:
                    idEve = "0X53";
                    tipo = "E";
                    data = "SSP_CMD_CASHBOX_PAYOUT_OPERATION_DATA";
                    break;
                case CommandEventos.SspCmdConfigureBezel:
                    idEve = "0X54";
                    tipo = "E";
                    data = "SSP_CMD_CONFIGURE_BEZEL";
                    break;
                case CommandEventos.SspCmdPollWithAck:
                    idEve = "0X56";
                    tipo = "E";
                    data = "SSP_CMD_POLL_WITH_ACK";
                    break;
                case CommandEventos.SspCmdEventAck:
                    idEve = "0X57";
                    tipo = "E";
                    data = "SSP_CMD_EVENT_ACK";
                    break;
                case CommandEventos.SspCmdGetCounters:
                    idEve = "0X58";
                    tipo = "E";
                    data = "SSP_CMD_GET_COUNTERS";
                    break;
                case CommandEventos.SspCmdResetCounters:
                    idEve = "0X59";
                    tipo = "E";
                    data = "SSP_CMD_RESET_COUNTERS";
                    break;
                case CommandEventos.SspCmdCoinMechOptions:
                    idEve = "0X5A";
                    tipo = "E";
                    data = "SSP_CMD_COIN_MECH_OPTIONS";
                    break;
                case CommandEventos.SspCmdDisablePayoutDevice:
                    idEve = "0X5B";
                    tipo = "E";
                    data = "SSP_CMD_DISABLE_PAYOUT_DEVICE";
                    break;
                case CommandEventos.sspCmdEnablePayoutDevice:
                    idEve = "0X5C";
                    tipo = "E";
                    data = "SSP_CMD_ENABLE_PAYOUT_DEVICE";
                    break;
                case CommandEventos.SspCmdSetFixedEncryptionKey:
                    idEve = "0X60";
                    tipo = "E";
                    data = "SSP_CMD_SET_FIXED_ENCRYPTION_KEY";
                    break;
                case CommandEventos.SspCmdResetFixedEncryptionKey:
                    idEve = "0X61";
                    tipo = "E";
                    data = "SSP_CMD_RESET_FIXED_ENCRYPTION_KEY";
                    break;
                case CommandEventos.SspCmdRequestTebsBarcode:
                    idEve = "0X65";
                    tipo = "E";
                    data = "SSP_CMD_REQUEST_TEBS_BARCODE";
                    break;
                case CommandEventos.SspCmdRequestTebsLog:
                    idEve = "0X66";
                    tipo = "E";
                    data = "SSP_CMD_REQUEST_TEBS_LOG";
                    break;
                case CommandEventos.SspCmdTebsUnlockEnable:
                    idEve = "0X67";
                    tipo = "E";
                    data = "SSP_CMD_TEBS_UNLOCK_ENABLE";
                    break;
                case CommandEventos.SspCmdTebsUnlockDiable:
                    idEve = "0X68";
                    tipo = "E";
                    data = "SSP_CMD_TEBS_UNLOCK_DISABLE";
                    break;
                case CommandEventos.SspPollTebsCashboxOutOfService:
                    idEve = "0X90";
                    tipo = "E";
                    data = "SSP_POLL_TEBS_CASHBOX_OUT_OF_SERVICE";
                    break;
                case CommandEventos.SspPollTebsCashboxTamper:
                    idEve = "0X91";
                    tipo = "E";
                    data = "SSP_POLL_TEBS_CASHBOX_TAMPER";
                    break;
                case CommandEventos.sspPollTebsCashboxInService:
                    idEve = "0X92";
                    tipo = "E";
                    data = "SSP_POLL_TEBS_CASHBOX_IN_SERVICE";
                    break;
                case CommandEventos.SspPollTebsChashboxUnlockEnabled:
                    idEve = "0X93";
                    tipo = "E";
                    data = "SSP_POLL_TEBS_CASHBOX_UNLOCK_ENABLED";
                    break;
                case CommandEventos.SspPollJamRecovery:
                    idEve = "0XB0";
                    tipo = "E";
                    data = "SSP_POLL_JAM_RECOVERY";
                    break;
                case CommandEventos.SspPollErrorDuringPayout:
                    idEve = "0XB1";
                    tipo = "E";
                    data = "SSP_POLL_ERROR_DURING_PAYOUT";
                    break;
                case CommandEventos.SspPollSmartEmptying:
                    idEve = "0XB2";
                    tipo = "E";
                    data = "SSP_POLL_SMART_EMPTYING";
                    break;
                case CommandEventos.SspPollSmartEmptied:
                    idEve = "0XB3";
                    tipo = "E";
                    data = "SSP_POLL_SMART_EMPTIED";
                    break;
                case CommandEventos.SspPollChannelDisable:
                    idEve = "0XB4";
                    tipo = "E";
                    data = "SSP_POLL_CHANNEL_DISABLE";
                    break;
                case CommandEventos.SspPollInitialising:
                    idEve = "0XB5";
                    tipo = "E";
                    data = "SSP_POLL_INITIALISING";
                    break;
                case CommandEventos.SspPollCoinMechError:
                    idEve = "0XB6";
                    tipo = "E";
                    data = "SSP_POLL_COIN_MECH_ERROR";
                    break;
                case CommandEventos.SspPollEmptying:
                    idEve = "0XB7";
                    tipo = "E";
                    data = "SSP_POLL_EMPTYING";
                    break;
                case CommandEventos.SspPollEmptied:
                    idEve = "0XC2";
                    tipo = "E";
                    data = "SSP_POLL_EMPTIED";
                    break;
                case CommandEventos.SspPollCoinMechJammed:
                    idEve = "0XC4";
                    tipo = "E";
                    data = "SSP_POLL_COIN_MECH_JAMMED";
                    break;
                case CommandEventos.SspPollCoinMechReturnPressed:
                    idEve = "0XC5";
                    tipo = "E";
                    data = "SSP_POLL_COIN_MECH_RETURN_PRESSED";
                    break;
                case CommandEventos.SspPollPayoutOutofService:
                    idEve = "0XC6";
                    tipo = "E";
                    data = "SSP_POLL_PAYOUT_OUT_OF_SERVICE";
                    break;
                case CommandEventos.SspPollNoteFloatRemoved:
                    idEve = "0XC7";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_FLOAT_REMOVED";
                    break;
                case CommandEventos.SspPPollNoteFloatAttached:
                    idEve = "0XC8";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_FLOAT_ATTACHED";
                    break;
                case CommandEventos.SspPollNoteTransferedToStacker:
                    idEve = "0XC9";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_TRANSFERED_TO_STACKER";
                    break;
                case CommandEventos.SspPollNotePaidIntoStackerAtPowerUp:
                    idEve = "0XCA";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_PAID_INTO_STACKER_AT_POWER_UP";
                    break;
                case CommandEventos.SspPollNotePaidIntoStoreAtPowerUp:
                    idEve = "0XCB";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_PAID_INTO_STORE_AT_POWER_UP";
                    break;
                case CommandEventos.SspPollNoteStacking:
                    idEve = "0XCC";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_STACKING";
                    break;
                case CommandEventos.SspPollNoteDispenseatPowerUp:
                    idEve = "0XCD";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_DISPENSED_AT_POWER_UP";
                    break;
                case CommandEventos.SspPollNoteHeldInBezel:
                    idEve = "0XCE";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_HELD_IN_BEZEL";
                    break;
                case CommandEventos.sspPollBarCodeTicketAcknwledge:
                    idEve = "0XD1";
                    tipo = "E";
                    data = "SSP_POLL_BAR_CODE_TICKET_ACKNOWLEDGE";
                    break;
                case CommandEventos.SspPollDispensed:
                    idEve = "0XD2";
                    tipo = "E";
                    data = "SSP_POLL_DISPENSED";
                    break;
                case CommandEventos.SspPollJammed:
                    idEve = "0XD5";
                    tipo = "E";
                    data = "SSP_POLL_JAMMED";
                    break;
                case CommandEventos.SspPollHaled:
                    idEve = "0xD6";
                    tipo = "E";
                    data = "SSP_POLL_HALTED";
                    break;
                case CommandEventos.SspPollFloating:
                    idEve = "0XD7";
                    tipo = "E";
                    data = "SSP_POLL_BAR_CODE_TICKET_ACKNOWLEDGE";
                    break;
                case CommandEventos.SspPollFloated:
                    idEve = "0XD8";
                    tipo = "E";
                    data = "SSP_POLL_FLOATED";
                    break;
                case CommandEventos.SspPollTimeOut:
                    idEve = "0XD9";
                    tipo = "E";
                    data = "SSP_POLL_TIME_OUT";
                    break;
                case CommandEventos.SspPollDispensing:
                    idEve = "0XDA";
                    tipo = "E";
                    data = "SSP_POLL_DISPENSING";
                    break;
                case CommandEventos.SppPollNoteStoredInPayout:
                    idEve = "0XDB";
                    tipo = "E";
                    data = "SSP_POLL_DISPENSING";
                    break;
                case CommandEventos.SspPollIncompletePayout:
                    idEve = "0XDC";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_STORED_IN_PAYOUT";
                    break;
                case CommandEventos.SspPollIncompleteFloat:
                    idEve = "0XDD";
                    tipo = "E";
                    data = "SSP_POLL_INCOMPLETE_FLOAT";
                    break;
                case CommandEventos.SspPollCashBoxPaid:
                    idEve = "0XDE";
                    tipo = "E";
                    data = "SSP_POLL_CASHBOX_PAID";
                    break;
                case CommandEventos.SspPollCoinCredit:
                    idEve = "0XDF";
                    tipo = "E";
                    data = "SSP_POLL_COIN_CREDIT";
                    break;
                case CommandEventos.SspPollNotePathOpen:
                    idEve = "0XE0";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_PATH_OPEN";
                    break;
                case CommandEventos.SspPollNoteClearedFromFront:
                    idEve = "0XE1";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_CLEARED_FROM_FRONT";
                    break;
                /* case CommandEventos.sspPollNoteClearedCashBox:
                     idEve = "0XE2";
                     tipo = "E";
                     data = "SSP_POLL_NOTE_CLEARED_TO_CASHBOX";
                     break;*/
                case CommandEventos.SspPollCashBoxRemoved:
                    idEve = "0XE3";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_CLEARED_TO_CASHBOX";
                    break;
                case CommandEventos.SspPollCashBoxReplaced:
                    idEve = "0XE4";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_CLEARED_TO_CASHBOX";
                    break;
                case CommandEventos.SspPollBarCodeTicketValidated:
                    idEve = "0XE5";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_CLEARED_TO_CASHBOX";
                    break;
                case CommandEventos.SspPollfraudAttempt:
                    idEve = "0XE6";
                    tipo = "E";
                    data = "SSP_POLL_FRAUD_ATTEMPT";
                    break;
                case CommandEventos.SspPollStackerFull:
                    idEve = "0XE7";
                    tipo = "E";
                    data = "SSP_POLL_STACKER_FULL";
                    break;
                case CommandEventos.SspPollDisabled:
                    idEve = "0XE8";
                    tipo = "E";
                    data = "SSP_POLL_DISABLED";
                    break;
                case CommandEventos.SspPollUnsafeNoteJam:
                    idEve = "0XEA";
                    tipo = "E";
                    data = "SSP_POLL_UNSAFE_NOTE_JAM";
                    break;
                case CommandEventos.SspPollNoteStacked:
                    idEve = "0XEB";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_STACKED";
                    break;
                case CommandEventos.SspPollNoteRejected:
                    idEve = "0XEC";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_REJECTED";
                    break;
                case CommandEventos.SspPollNoteRejecting:
                    idEve = "0XED";
                    tipo = "E";
                    data = "SSP_POLL_NOTE_REJECTING";
                    break;
                case CommandEventos.SspPollCreditNote:
                    idEve = "0XEE";
                    tipo = "E";
                    data = "SSP_POLL_CREDIT_NOTE";
                    break;
                case CommandEventos.SspPollReadNote:
                    idEve = "0XEF";
                    tipo = "E";
                    data = "SSP_POLL_READ_NOTE";
                    break;
                case CommandEventos.SsPollSlaveReset:
                    idEve = "0XF1";
                    tipo = "E";
                    data = "SSP_POLL_SLAVE_RESET";
                    break;
                case CommandEventos.SspResponseCommandNotKnown:
                    idEve = "0XF2";
                    tipo = "E";
                    data = "SSP_RESPONSE_COMMAND_NOT_KNOWN";
                    break;
            }

            conn.Open();

            string sql = String.Format("select * from sp_App_GavOpe('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}');",
                                        accion, idacc, IdUsr, snd, tip, sng, data);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

        }

        public string formatDenData(List<string> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = data[i].Replace("~", ",");
            }

            var fData = String.Join(";", data.ToArray());

            return fData;
        }

        public string GetNumeroSerieGaveta(TipoGaveta tipoGaveta)
        {
            var response = getGavetas();
            var responseArray = response.Split('|');

            if (responseArray[0] == "0~OK")
            {
                var gaveta = Gaveta.GetGavetaByEnum(tipoGaveta);
                foreach (var item in responseArray)
                {
                    var aux = item.Split('~');
                    if (aux[1] == gaveta)
                    {
                        return aux[0];
                    }
                }
            }

            throw new Exception($"No se encontró el número de serie para la gaveta {tipoGaveta}");
        }

        public void RegistrarIngresoMoneda(int moneda, int cantidad)
        {
            var parameters = new Dictionary<string, object>();
            var data = string.Format("{0},{1}", moneda, cantidad);

            parameters.Add("@accion", "GAVIN");
            parameters.Add("@idacc", Command.CargarDineroGaveta);
            parameters.Add("@idusr", IdUsr);
            parameters.Add("@snd", "");
            parameters.Add("@tip", "");
            parameters.Add("@sng", GetNumeroSerieGaveta(TipoGaveta.MoneyRecycler));
            parameters.Add("@data", data);

            using (var dataSet = ExecuteStoredProcedure("sp_App_GavOpe", parameters))
            {
                var responseMessage = DatabaseResponseMessage.Parse(dataSet);
                responseMessage.EnsureSuccessStatusCode();
            }
        }

        public void RegistrarIngresoBillete(int billete, int cantidad, bool posicionBillete)
        {
            var parameters = new Dictionary<string, object>();
            var data = string.Format("{0},{1}", billete, cantidad);

            parameters.Add("@accion", "GAVIN");
            parameters.Add("@idacc", Command.CargarDineroGaveta);
            parameters.Add("@idusr", IdUsr);
            parameters.Add("@snd", "");
            parameters.Add("@tip", "");
            if (!posicionBillete)
            {
                //falso abajo
                parameters.Add("@sng", GetNumeroSerieGaveta(TipoGaveta.BillRecycler));
            }
            else
            {
                //verdadero arriba
                parameters.Add("@sng", GetNumeroSerieGaveta(TipoGaveta.BillAcceptor));
            }
            parameters.Add("@data", data);

            using (var dataSet = ExecuteStoredProcedure("sp_App_GavOpe", parameters))
            {
                var responseMessage = DatabaseResponseMessage.Parse(dataSet);
                responseMessage.EnsureSuccessStatusCode();
            }
        }

        public void RegistrarRetiroMonedas(List<ChannelData> monedasRetiradas)
        {
            var parameters = new Dictionary<string, object>();
            var data = default(string);

            foreach (var monedaRetirada in monedasRetiradas)
            {
                data += string.Format("{0},{1};", monedaRetirada.Value, monedaRetirada.Level);
            }

            // Removes the last comma
            data = data.Remove(data.Length - 1);

            parameters.Add("@accion", "GAVOUT");
            parameters.Add("@idacc", Command.RetiroDineroGaveta);
            parameters.Add("@idusr", IdUsr);
            parameters.Add("@snd", "");
            parameters.Add("@tip", "");
            parameters.Add("@sng", GetNumeroSerieGaveta(TipoGaveta.MoneyRecycler));
            parameters.Add("@data", data);

            using (var dataSet = ExecuteStoredProcedure("sp_App_GavOpe", parameters))
            {
                var responseMessage = DatabaseResponseMessage.Parse(dataSet);
                responseMessage.EnsureSuccessStatusCode();
            }
        }
        //registro de mensajes en la BD en la tabla CommandEvent

        public void RegistrarRetiroBilletes(List<ChannelData> billetesRetirados)
        {
            var parameters = new Dictionary<string, object>();
            var data = default(string);

            foreach (var billeteRetirado in billetesRetirados)
            {
                data += string.Format("{0},{1};", billeteRetirado.Value, billeteRetirado.Level);
            }
            if (data == null)
            {
                return;
            }
            // Removes the last comma
            data = data.Remove(data.Length - 1);

            parameters.Add("@accion", "GAVOUT");
            parameters.Add("@idacc", Command.RetiroDineroGaveta);
            parameters.Add("@idusr", IdUsr);
            parameters.Add("@snd", "");
            parameters.Add("@tip", "");
            parameters.Add("@sng", GetNumeroSerieGaveta(TipoGaveta.BillAcceptor));
            parameters.Add("@data", data);

            using (var dataSet = ExecuteStoredProcedure("sp_App_GavOpe", parameters))
            {
                var responseMessage = DatabaseResponseMessage.Parse(dataSet);
                responseMessage.EnsureSuccessStatusCode();
            }
        }

        public void RegistrarRetiroBilletesF(List<ChannelData> billetesRetirados)
        {
            var parameters = new Dictionary<string, object>();
            var data = default(string);

            foreach (var billeteRetirado in billetesRetirados)
            {
                data += string.Format("{0},{1};", billeteRetirado.Value, billeteRetirado.Level);
            }
            if (data == null)
            {
                return;
            }
            // Removes the last comma
            data = data.Remove(data.Length - 1);

            parameters.Add("@accion", "GAVIN");
            parameters.Add("@idacc", Command.RetiroDineroGaveta);
            parameters.Add("@idusr", IdUsr);
            parameters.Add("@snd", "");
            parameters.Add("@tip", "");
            parameters.Add("@sng", GetNumeroSerieGaveta(TipoGaveta.BillRecycler));
            parameters.Add("@data", data);

            using (var dataSet = ExecuteStoredProcedure("sp_App_GavOpe", parameters))
            {
                var responseMessage = DatabaseResponseMessage.Parse(dataSet);
                responseMessage.EnsureSuccessStatusCode();
            }
        }

        public void ValidateGaveta(TipoGaveta tipoGaveta)
        {
            var response = getGavetas();
            var gavetaFound = false;
            var nombreGaveta = Gaveta.GetGavetaByEnum(tipoGaveta);
            var responseMessage = DatabaseResponseMessage.Parse(response);

            responseMessage.EnsureSuccessStatusCode();

            foreach (var row in responseMessage.Data)
            {
                var aux = row.Split('~');
                if (aux[1] == nombreGaveta)
                {
                    gavetaFound = true;
                    if (string.IsNullOrEmpty(aux[5]))
                    {
                        throw new Exception($"No se encontraron denominaciones configuradas para la gaveta {tipoGaveta}. Revisar la tabla OL_Valor.");
                    }

                    break;
                }
            }

            if (!gavetaFound)
            {
                throw new Exception($"No se encontró la gaveta {tipoGaveta}. Revisar las tablas Gavetas y DispGaveta.");
            }

            // TODO: se coloca un Sleep para prevenir una key duplicada en la base de datos
            // producto de que las transacciones se invocan de forma muy rápida, generando valores
            // duplicados en el campo "fh_acc" de la tabla "usrtrx". Se debe dar una solución elegante.
            System.Threading.Thread.Sleep(100);
        }

        public void CierreZ()
        {

            NpgsqlConnection conn = new NpgsqlConnection(connString);

            var accion = "ZET";
            var idacc = "126";
            var usuario = "0";
            var kiosco = "0";
            conn.Open();

            string sql = String.Format("select * from sp_App_TesOpe('{0}', {1}, {2}, {3});",
                                        accion, idacc, usuario, kiosco);

            log.Info("CierreZ: " + sql);

            NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
            ds.Reset();
            da.Fill(ds);
            dt = ds.Tables[0];
            conn.Close();

            foreach (DataRow row in (dt ?? new DataTable()).Rows)
            {
                var rowData = row["sp_App_TesOpe"].ToString();
                dataResp.Add(rowData);
            }

            resp = String.Join("|", dataResp.ToArray());
            log.Info("R: CierreZ: " + resp);
        }
    }
}
