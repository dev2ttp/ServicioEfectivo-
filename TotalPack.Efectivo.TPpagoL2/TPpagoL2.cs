using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TotalPack.Efectivo.TPpagoL2.Properties;
using TPpagoL2.Helpers;

namespace TPpagoL2
{
    public partial class TPpagoL2 : ServiceBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private System.Threading.Timer timerStartService;
        private Timer timerAlive;

        public TPpagoL2()
        {
            InitializeComponent();

            timerAlive = new Timer();
            timerAlive.Interval = 60 * 1000;
            timerAlive.Elapsed += new ElapsedEventHandler(timerAlive_Elapsed);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                MyConsole.Clear();
                MyConsole.WriteLine();
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var buildDate = ApplicationInformation.CompileDate;
                MyConsole.WriteLine("Servicio TPpagoL2 v{0}", version);
                MyConsole.WriteLine("Generado el {0}", buildDate.ToString("dd-MM-yyyy HH:mm:ss"));
                log.InfoFormat("Servicio TPpagoL2 v{0} Generado el {1}", version, buildDate.ToString("dd-MM-yyyy HH:mm:ss"));
                MyConsole.WriteLine(new string('-', 50));
                MyConsole.WriteLine("PARÁMETROS");
                MyConsole.WriteLine(" Nombre Pipe         : " + Settings.Default.NombrePipe);
                MyConsole.WriteLine(" Puerto COM Máquinas : " + Settings.Default.PuertoMaquinasEfectivo);
                MyConsole.WriteLine(" Puerto COM Arduino  : " + Settings.Default.PuertoArduino);
                MyConsole.WriteLine(new string('-', 50));

                timerStartService = new System.Threading.Timer(StartService, null, 0, System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                MyConsole.WriteLine("*** ERROR: {0}", ex.Message);
                EventLog.Source = "TPpagoL2";
                EventLog.WriteEntry("Service TPpagoL2 shutdown unexpectedly --> " + ex.ToString(), EventLogEntryType.Error);
                OnStop();
            }
        }

        protected override void OnStop()
        {
            timerAlive.Enabled = false;
            log.Info("Servicio detenido");
            MyConsole.WriteLine("Servicio detenido");
        }

        private void StartService(object obj)
        {
            try
            {
                var pipeServer = new PipeServer(Settings.Default.NombrePipe, Settings.Default.PuertoMaquinasEfectivo, 9600, 1000, 3);
                pipeServer.StartListening();
                timerAlive.Enabled = true;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MyConsole.WriteLine("*** ERROR: {0}", ex.Message);
                EventLog.Source = "TPpagoL2";
                EventLog.WriteEntry("Service TPpagoL2 shutdown unexpectedly --> " + ex.ToString(), EventLogEntryType.Error);
                OnStop();
            }
        }

        private void timerAlive_Elapsed(object sender, ElapsedEventArgs e)
        {
            MyConsole.WriteLine("...");
        }

        internal void StartOnConsoleMode(string[] args)
        {
            OnStart(args);
            Console.ReadLine();
        }
    }
}
