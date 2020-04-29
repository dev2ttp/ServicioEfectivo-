using ITLlib;
using System;

namespace TotalPack.Efectivo.SSP
{
    class LibraryException : Exception
    {
        public SSP_COMMAND Command { get; set; }
        public SSP_COMMAND_INFO CommandInfo { get; set; }
        
        public LibraryException(string message) : base(message)
        {
        }

        public LibraryException(string message, SSP_COMMAND command) : base(message)
        {
            Command = command;
        }

        public LibraryException(string message, SSP_COMMAND command, SSP_COMMAND_INFO commandInfo) : base(message)
        {
            Command = command;
            CommandInfo = commandInfo;
        }
    }
}
