using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TotalPack.Efectivo.SSP;
using TotalPack.Locker.ControlBoard;

namespace TotalPack.Efectivo.TPpagoL2.Models
{
    public class Session
    {
        public int NotesPaid { get; set; }
        public int CoinsPaid { get; set; }
        public int PayoutAmount { get; set; }
        public int BillsDispensedBeforeTimeout { get; set; }
        public int CoinsDispensedBeforeTimeout { get; set; }        
        public bool BillsUsedInPayout { get; set; }
        public bool CoinsUsedInPayout { get; set; }
        public bool BillDispenseFinished { get; set; }
        public bool CoinDispenseFinished { get; set; }
        public bool IncompleteCoinPayoutDetected { get; set; }
        public bool CoinPayoutTimeOut { get; set; }
        public bool BillPayoutTimeOut { get; set; }
        public List<ChannelData> PayoutChannels { get; set; }
        public List<ChannelData> HopperChannels { get; set; }
        public List<CreditEvent> CreditsPaid { get; set; }
    }
}
