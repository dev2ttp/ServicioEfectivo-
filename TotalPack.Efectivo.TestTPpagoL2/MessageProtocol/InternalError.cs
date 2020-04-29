using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.TestTPpagoL2.MessageProtocol
{
    public class InternalError
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }
}
