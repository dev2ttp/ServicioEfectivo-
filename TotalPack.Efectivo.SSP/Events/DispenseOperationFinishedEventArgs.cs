using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Events
{
    /// <summary>
    /// This class contains the event data of a dispense operation.
    /// </summary>
    public class DispenseOperationFinishedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the values dispensed.
        /// </summary>
        public List<ChannelData> ValuesDispensed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispenseOperationFinishedEventArgs"/> class.
        /// </summary>
        /// <param name="valuesDispensed">The values dispensed.</param>
        public DispenseOperationFinishedEventArgs(List<ChannelData> valuesDispensed)
        {
            
            ValuesDispensed = valuesDispensed;
        }
    }
}
