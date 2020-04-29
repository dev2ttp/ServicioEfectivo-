using System;
using System.Linq;
using System.Diagnostics;
using TPpagoL2;

namespace TotalPack.Efectivo.TPpagoL2
{
    public class Power
    {
        EventLog myLog = new EventLog();
        DateTime FechaLectura = new DateTime();
        bool salir = false;
        bool primera = true;

        public void estadoPower()
        {
            myLog.Log = "Application";

            foreach (EventLogEntry entry in myLog.Entries.Cast<EventLogEntry>().Reverse())
            {
                if (!primera && entry.TimeGenerated > FechaLectura)
                {
                    if (entry.EventID == 61455)
                    {
                        // ON
                        PipeServer.EstadoCorriente = true;
                        salir = true;
                    }
                    if (entry.EventID == 204)
                    {
                        // ON
                        PipeServer.EstadoCorriente = true;
                        salir = true;
                    }
                    else if (entry.EventID == 174)
                    {
                        // OFF
                        PipeServer.EstadoCorriente = false;
                        salir = true;
                    }
                }
                else if (entry.EventID == 61455)
                {
                    // ON
                    PipeServer.EstadoCorriente  = true;
                    salir = true;
                }
                else if (entry.EventID == 204)
                {
                    // ON
                    PipeServer.EstadoCorriente = true;
                    salir = true;
                }
                else if (entry.Source == "APC Data Service" && entry.EventID == 0)
                {
                    // ON
                    PipeServer.EstadoCorriente = true;
                    salir = true;
                }
                else if (entry.EventID == 174)
                {
                    // OFF
                    PipeServer.EstadoCorriente = false;
                    salir = true;
                }
                if (salir) break;
            }
            primera = false;
            FechaLectura = DateTime.Now;
        }
    }
}
