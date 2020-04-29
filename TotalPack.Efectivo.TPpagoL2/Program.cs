using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace TPpagoL2
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main(string[] args)
        {
            if (Debugger.IsAttached || args.Contains("-console"))
            {
                TPpagoL2 service1 = new TPpagoL2();
                service1.StartOnConsoleMode(args);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new TPpagoL2()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
