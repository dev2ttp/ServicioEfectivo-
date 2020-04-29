using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace TotalPack.Efectivo.TPpagoL2.HostPipe
{
    public class ServicioPago
    {
        public const string Header = "TP10PP";
        private bool serviceExists;
        private ServiceController service;

        public enum Comandos
        {
            CierreZ = 126, 
            OPGaveta = 127
        }
        public ServicioPago()
        {
            serviceExists = (ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == "TPV10pagoL") != null);
            if (serviceExists) service = new ServiceController("TPV10pagoL");
        }
    }  
}
