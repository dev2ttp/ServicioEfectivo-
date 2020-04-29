using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Events
{
    public class NoteFloatEventArgs : EventArgs
    {
     
        public List<ChannelData> NoteFloat { get; }

        public NoteFloatEventArgs(List<ChannelData> noteFloat)
        {
            NoteFloat = noteFloat;
        }
 
    }
}
