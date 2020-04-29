using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Events
{
    /// <summary>
    /// This class contains the event data of a note dispensed.
    /// </summary>
    public class NoteDispensedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the note dispensed.
        /// </summary>
        public ChannelData NoteDispensed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteDispensedEventArgs"/> class.
        /// </summary>
        /// <param name="noteDispensed">The note dispensed.</param>
        public NoteDispensedEventArgs(ChannelData noteDispensed)
        {
            NoteDispensed = noteDispensed;
        }
    }
}
