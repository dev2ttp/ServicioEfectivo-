using ITLlib;
using System;

namespace TotalPack.Efectivo.SSP
{
    /// <summary>
    /// Manages the access to the ITLlib library.
    /// </summary>
    static class LibraryHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static SSPComms libHandle = new SSPComms();
        static Exception m_LastEx;
        static object thisLock = new object();
        static string portName;
        static bool isPortOpen;

        /// <summary>
        /// Opens a new port connection.
        /// </summary>
        /// <param name="command">The command that contains the port name to open.</param>
        public static void OpenPort(ref SSP_COMMAND command)
        {
            lock (thisLock)
            {
                if (command.ComPort == portName && isPortOpen)
                {
                    return;
                }
                
                if (libHandle.OpenSSPComPort(command))
                {
                    portName = command.ComPort;
                    isPortOpen = true;
                }
                else
                {
                    throw new LibraryException($"Could not open the port {command.ComPort}.");
                }
            }
        }

        /// <summary>
        /// Closes the port connection.
        /// </summary>
        public static void ClosePort()
        {
            lock (thisLock)
            {
                try
                {
                    var result = libHandle.CloseComPort();
                    if (!result)
                    {
                        throw new LibraryException("Could not close the port");
                    }
                }
                finally
                {
                    portName = null;
                    isPortOpen = false;
                }
            }
        }

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        /// <param name="cmd">The command to send.</param>
        /// <param name="inf">When this method returns, contains the information about the command being sent.</param>
        /// 
        public static bool SendCommand(ref SSP_COMMAND cmd, ref SSP_COMMAND_INFO inf, string periferico)
        {
            try
            {
                // Lock critical section to prevent multiple commands being sent simultaneously
                lock (thisLock)
                {
                    return libHandle.SSPSendCommand(cmd, inf);
                }
            }
            catch (Exception ex)
            {
                m_LastEx = ex;
                return false;
            }
        }      

        /// <summary>
        /// Initiates the key negotiation to establish a secure channel.
        /// </summary>
        /// <param name="keys">When this method returns, contains the information about the keys.</param>
        /// <param name="cmd">The command to send.</param>
        public static void InitiateKeys(ref SSP_KEYS keys, ref SSP_COMMAND cmd)
        {
            lock (thisLock)
            {
                var result = libHandle.InitiateSSPHostKeys(keys, cmd);
                if (!result)
                {
                    throw new LibraryException("Could not initiate keys", cmd);
                }
            }
        }

        /// <summary>
        /// Generates the encryption key.
        /// </summary>
        /// <param name="keys">The key to send.</param>
        public static void CreateFullKey(ref SSP_KEYS keys)
        {
            lock (thisLock)
            {
                var result = libHandle.CreateSSPHostEncryptionKey(keys);
                if (!result)
                {
                    throw new LibraryException("Could not create full key");
                }
            }
        }
    }
}
