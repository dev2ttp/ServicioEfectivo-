using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Exceptions
{
    public class UnableToPayAmountException : Exception
    {
        public UnableToPayAmountException(string message) : base(message)
        {
        }
    }
}
