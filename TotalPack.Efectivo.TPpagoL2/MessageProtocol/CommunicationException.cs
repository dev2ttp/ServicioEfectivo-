using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.MessageProtocol
{
    public class CommunicationException : Exception
    {
        public FormatErrorType FormatError { get; private set; }
        public InternalError InternalError { get; private set; }

        public CommunicationException()
        { }

        public CommunicationException(string message)
            : base(message)
        { }

        public CommunicationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public CommunicationException(string message, FormatErrorType formatError)
            : base(message)
        {
            FormatError = formatError;
        }

        public CommunicationException(string message, FormatErrorType formatError, InternalError internalError)
            : base(message)
        {
            FormatError = formatError;
            InternalError = internalError;
        }
    }
}
