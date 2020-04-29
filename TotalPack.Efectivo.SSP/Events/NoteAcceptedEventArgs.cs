using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Events
{
    /// <summary>
    /// This class contains the event data of a note accepted.
    /// </summary>
    public class NoteAcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the value of the note accepted.
        /// </summary>
        public int Value { get; }
        public bool PosicionBillete { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteAcceptedEventArgs"/> class.
        /// </summary>
        /// <param name="value">The value of the note.</param>
        public NoteAcceptedEventArgs(int value, bool posicionBillete)
        {
            Value = value;
            PosicionBillete = posicionBillete;
        }
    }
}
