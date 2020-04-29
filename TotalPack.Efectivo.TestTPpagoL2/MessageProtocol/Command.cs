using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.TestTPpagoL2.MessageProtocol
{
    enum Command
    {
        HabilitarValidadores = 160,
        GetDineroIngresado = 161,        
        Error = 999
    }
}
