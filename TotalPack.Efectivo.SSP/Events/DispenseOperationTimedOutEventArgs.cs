using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Events
{
    /// <summary>
    /// This class contains the event data of a dispense operation timed out.
    /// </summary>
    public class DispenseOperationTimedOutEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the value dispensed before the timeout event.
        /// </summary>
        public int ValueDispensed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispenseOperationTimedOutEventArgs"/> class.
        /// </summary>
        /// <param name="valueDispensed">The value dispensed before the timeout event.</param>
        public DispenseOperationTimedOutEventArgs(int valueDispensed)
        {
            ValueDispensed = valueDispensed;
        }
    }
}
