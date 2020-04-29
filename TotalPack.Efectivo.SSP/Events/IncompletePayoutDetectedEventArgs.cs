using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Events
{
    /// <summary>
    /// This class contains the event data of an incomplete payout operation.
    /// </summary>
    public class IncompletePayoutDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the dispensed value.
        /// </summary>
        public int DispensedValue { get; }
        /// <summary>
        /// Gets the request value.
        /// </summary>
        public int RequestedValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncompletePayoutDetectedEventArgs"/> class.
        /// </summary>
        /// <param name="dispensedValue">The dispensed value.</param>
        /// <param name="requestedValue">The request value.</param>
        public IncompletePayoutDetectedEventArgs(int dispensedValue, int requestedValue)
        {
            DispensedValue = dispensedValue;
            RequestedValue = requestedValue;
        }
    }
}
