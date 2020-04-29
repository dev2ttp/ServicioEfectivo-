using System;

namespace TotalPack.Efectivo.SSP
{
    /// <summary>
    /// Represent errors that occur during the execution of the library.
    /// </summary>
    public class SspException : Exception
    {
        /// <summary>
        /// Gets the error code that is associated with this exception.
        /// </summary>
        public byte ErrorCode { get; }
        /// <summary>
        /// Gets the sub error code that is associated with this exception.
        /// </summary>
        public byte SubErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SspException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SspException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SspException"/> class with a specified error message and error code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code that indicates the error that occurred.</param>
        public SspException(string message, byte errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SspException"/> class with a specified error message, error code and sub error code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code that indicates the error that occurred.</param>
        /// <param name="subErrorCode">The sub error code that indicates the error that occured.</param>
        public SspException(string message, byte errorCode, byte subErrorCode) : base(message)
        {
            ErrorCode = errorCode;
            SubErrorCode = subErrorCode;
        }
    }
}
