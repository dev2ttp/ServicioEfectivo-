using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP
{
    [Serializable]
    public class ChannelData
    {
        public int Value { get; set; }
        public byte Channel { get; set; }
        public char[] Currency { get; set; }
        public int Level { get; set; }
        public bool IsRecycling { get; set; }

        public ChannelData()
        {
            Value = 0;
            Channel = 0;
            Currency = new char[3];
            Level = 0;
            IsRecycling = false;
        }

        public ChannelData(int value, int level)
        {
            Value = value;
            Level = level;
        }
    }
}
