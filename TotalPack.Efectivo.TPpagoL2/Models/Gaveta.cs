using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.TPpagoL2.Models
{
    class Gaveta
    {
        public const string BillAcceptor = "BA";
        public const string BillRecycler = "BR";
        public const string MoneyAcceptor = "MA";
        public const string MoneyBank = "MB";
        public const string MoneyRecycler = "MR";

        public static string GetGavetaByEnum(TipoGaveta tipoGaveta)
        {
            switch (tipoGaveta)
            {
                case TipoGaveta.BillAcceptor:
                    return BillAcceptor;
                case TipoGaveta.BillRecycler:
                    return BillRecycler;
                case TipoGaveta.MoneyAcceptor:
                    return MoneyAcceptor;
                case TipoGaveta.MoneyBank:
                    return MoneyBank;
                case TipoGaveta.MoneyRecycler:
                    return MoneyRecycler;
                default:
                    throw new InvalidOperationException("The value of the enum is unknown.");
            }
        }
    }
}
