using ITLlib;
using System;
using System.Collections.Generic;
using TotalPack.Efectivo.SSP.Events;
using TotalPack.Efectivo.SSP.Exceptions;
using TotalPack.Efectivo.SSP.Helpers;
using TotalPack.Efectivo.SSP.Properties;

namespace TotalPack.Efectivo.SSP
{
    /// <summary>
    /// Represents a SMART Payout device.
    /// </summary>
    public class SmartPayout
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private SSP_COMMAND command;
        private SSP_KEYS keys;
        private SSP_FULL_KEY sspKey;
        private SSP_COMMAND_INFO info;
        CCommsWindow m_Comms;
       

        public string Currency = Settings.Default.moneda;
        public delegate void NoteAcceptedEventHandler(object sender, NoteAcceptedEventArgs e);
        public delegate void DispenseOperationFinishedEventHandler(object sender, DispenseOperationFinishedEventArgs e);
        public delegate void NotePollEventHandler(object sender, EventArgs e);
        public delegate void NoteDispensedEventHandler(object sender, NoteDispensedEventArgs e);
        public delegate void IncompletePayoutDetectedEventHandler(object sender, IncompletePayoutDetectedEventArgs e);
        public delegate void DispenseOperationTimedOutEventHandler(object sender, DispenseOperationTimedOutEventArgs e);
        public delegate void NoteFloatEventHandler(object sender, NoteFloatEventArgs e);
        public static int canalData;
        public static bool posicion;
         
        public int dispensingNotes { get; set; }
        public int NotasDetenidas { get; set; }


        /// <summary>
        /// Occurs when a note has been accepted as legaly currency.
        /// </summary>
        public event NoteAcceptedEventHandler NoteAccepted;
        /// <summary>
        /// Occurs when the payout device has finished emptying.
        /// </summary>
        public event NotePollEventHandler NotePollEmptied;
        /// <summary>
        /// Occurs when the payout operation could not be completed.
        /// </summary>
        public event IncompletePayoutDetectedEventHandler IncompletePayoutDetected;
        /// <summary>
        /// Occurs when all the notes has been dispensed.
        /// </summary>
        public event DispenseOperationFinishedEventHandler DispenseOperationFinished;

        public event NoteFloatEventHandler NoteFloat;

        /// <summary>
        /// Occurs when the dispense operation times out.
        /// </summary>
        public event DispenseOperationTimedOutEventHandler DispenseOperationTimedOut;

        /// <summary>
        /// Gets the protocol version of this validator.
        /// </summary>
        public int ProtocolVersion { get; private set; }
        /// <summary>
        /// Gets the number of channels used in this validator.
        /// </summary>
        public int NumberOfChannels { get; private set; }
        /// <summary>
        /// Gets the multiplier by which the channel values are multiplied to get their true penny value.
        /// </summary>
        public int ValueMultiplier { get; private set; }
        /// <summary>
        /// Gets the total number of hold messages to be issued before releasing note from escrow.
        /// </summary>
        public int HoldNumber { get; set; }
        /// <summary>
        /// Gets the number of hold messages still to be issued.
        /// </summary>
        public int HoldCount { get; private set; }
        /// <summary>
        /// Gets a value that indicates if a note is being held in escrow.
        /// </summary>
        public bool NoteHeld { get; private set; }
        /// <summary>
        /// Gets the type of this unit.
        /// </summary>
        public char UnitType { get; private set; }
        /// <summary>
        /// Gets the information of channel number, value, currency, level and whether it is being recycled.
        /// </summary>
        public List<ChannelData> UnitDataList { get; private set; }
        /// <summary>
        /// Gets the last rejection reason.
        /// </summary>
        public RejectionReason LastRejectionReason { get; private set; }
        /// <summary>
        /// Gets the firmware version.
        /// </summary>
        public string Firmware { get; private set; }
        /// <summary>
        /// Gets or sets the information of the channels, before a payout operation.
        /// </summary>
        public List<ChannelData> InitialUnitDataList { get; private set; }
        //DM
        public List<ChannelData> InitialUnitDataListFloat { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartPayout"/> class.
        /// </summary>
        /// <param name="portName">The name of the COM port the device is connected to.</param>
        /// <param name="baudRate">The baud rate of the port.</param>
        /// <param name="sspAddress">The SSP address of the device.</param>
        /// <param name="timeout">The maximum time the library will wait for a response from the device before retrying the command.</param>
        /// <param name="retryLevel">The number of times the library will retry sending a packet to the device if no response is received.</param>
        public SmartPayout(string portName, int baudRate, byte sspAddress, uint timeout, byte retryLevel)
        {
            command = new SSP_COMMAND();
            keys = new SSP_KEYS();
            sspKey = new SSP_FULL_KEY();
            info = new SSP_COMMAND_INFO();

            m_Comms = new CCommsWindow("SMARTPayout");
            
            command.ComPort = portName;
            command.BaudRate = baudRate;
            command.SSPAddress = sspAddress;
            command.Timeout = timeout;
            command.RetryLevel = retryLevel;

            NumberOfChannels = 0;
            ValueMultiplier = 1;
            UnitDataList = new List<ChannelData>();
            HoldCount = 0;
            HoldNumber = 0;
        }

        /// <summary>
        /// Enables the validator, allowing it to receive and act on commands.
        /// </summary>
        public bool EnableValidator()
        {
            command.CommandData[0] = CCommands.SSP_CMD_ENABLE;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Payout enabled\r\n");
            return true;
        }

        /// <summary>
        /// Disables the validator, disallowing it to receive and act on commands.
        /// </summary>
        public bool DisableValidator()
        {
            command.CommandData[0] = CCommands.SSP_CMD_DISABLE;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Payout disabled\r\n");
            return true;
        }

        public bool Detener()
        {
            command.CommandData[0] = CCommands.SSP_CMD_HALT_PAYOUT;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Stop\r\n");
            return true;
        }

        public string GetAllLevels()
        {
            command.CommandData[0] = CCommands.SSP_CMD_GET_ALL_LEVELS;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return "false";

            var n = command.ResponseData[1];
            var displayString = "";
            var v = "";
            var i = 0;
            for (i = 2; i < (9 * n); i += 9)
            {
                displayString = ";" + CHelpers.FormatToCurrency(CHelpers.ConvertBytesToInt32(command.ResponseData, i + 2));
                v += displayString.Remove(displayString.Length-3) + "," + CHelpers.ConvertBytesToInt16(command.ResponseData, i);
            }
            return v;
        }
        public string GetChannelLevelInfo()
        {
            string s = "";
            foreach (ChannelData d in UnitDataList)
            {
                s += ";" + (d.Value / 100f).ToString() + "," + d.Level;
            }
            return s;
        }

        /// <summary>
        /// Returns the note held in escrow to bezel.
        /// </summary>
        public bool ReturnNote()
        {
            command.CommandData[0] = CCommands.SSP_CMD_REJECT_BANKNOTE;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            HoldCount = 0;
            return true;
        }

        /// <summary>
        /// Allows the validator to payout and store notes.
        /// </summary>
        public bool EnablePayout()
        {
            command.CommandData[0] = CCommands.SSP_CMD_ENABLE_PAYOUT_DEVICE;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Payout enabled\r\n");
            return true;
        }

        /// <summary>
        /// Stops the validator from being able to payout and store notes.
        /// </summary>
        public bool DisablePayout()
        {
            command.CommandData[0] = CCommands.SSP_CMD_DISABLE_PAYOUT_DEVICE;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Payout disabled\r\n");
            return true;
        }

        /// <summary>
        /// Takes all the notes stored and moves them to the cashbox.
        /// </summary>
        public bool EmptyPayoutDevice()
        {
            command.CommandData[0] = CCommands.SSP_CMD_EMPTY_ALL;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Emptying payout device\r\n");
            return true;
        }

        /// <summary>
        /// Returns the number of notes stored in the device for a specific denomination.
        /// </summary>
        /// <param name="note">The note to check for.</param>
        /// <returns>The number of notes stored.</returns>
        public short CheckNoteLevel(int note, char[] currency)
        {
            byte[] b = CHelpers.ConvertInt32ToBytes(note);

            command.CommandData[0] = CCommands.SSP_CMD_GET_DENOMINATION_LEVEL;
            command.CommandData[1] = b[0];
            command.CommandData[2] = b[1];
            command.CommandData[3] = b[2];
            command.CommandData[4] = b[3];

            command.CommandData[5] = (byte)currency[0];
            command.CommandData[6] = (byte)currency[1];
            command.CommandData[7] = (byte)currency[2];
            command.CommandDataLength = 8;

            if (!SendCommand() || !CheckGenericResponses())
                return 0;

            var noteLevel = CHelpers.ConvertBytesToInt16(command.ResponseData, 1);
            return noteLevel;
        }

        public int GetChannelValue(int channelNumber)
        {
            if (channelNumber <= 0 || channelNumber > NumberOfChannels)
            {
                throw new ArgumentException("The value must be greater than zero and less than the number of channels.", nameof(channelNumber));
            }

            foreach (var channelData in UnitDataList)
            {
                if (channelData.Channel == channelNumber)
                {
                    return channelData.Value;
                }
            }

            throw new SspException("Could not find the channel.");
        }

        /// <summary>
        /// Sends a sync command to the validator.
        /// </summary>
        public bool SendSync()
        {
            command.CommandData[0] = CCommands.SSP_CMD_SYNC;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Sent sync\r\n");
            return true;
        }

        /// <summary>
        /// Sets a channel to route to storage.
        /// </summary>
        /// <param name="channelNumber"></param>
        public bool RouteChannelToStorage(int channelNumber)
        {
            command.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_ROUTE;
            command.CommandData[1] = 0x01; // Storage

            // Get value of coin (4 byte protocol 6)
            byte[] b = CHelpers.ConvertInt32ToBytes(GetChannelValue(channelNumber));
            command.CommandData[2] = b[0];
            command.CommandData[3] = b[1];
            command.CommandData[4] = b[2];
            command.CommandData[5] = b[3];

            // Add country code
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    command.CommandData[6] = (byte)d.Currency[0];
                    command.CommandData[7] = (byte)d.Currency[1];
                    command.CommandData[8] = (byte)d.Currency[2];
                    break;
                }
            }

            command.CommandDataLength = 9;
            if (!SendCommand() || !CheckGenericResponses())
                return false;

            // Update list
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    d.IsRecycling = false;
                    break;
                }
            }
            return true;
        }

        /// <summary>
        /// Sets a channel to route to Recycle.
        /// </summary>
        /// <param name="channelNumber"></param>
        public bool RouteChannelToRecycle(int channelNumber)
        {
            command.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_ROUTE;
            command.CommandData[1] = 0x00; // Storage

            // Get value of coin (4 byte protocol 6)
            byte[] b = CHelpers.ConvertInt32ToBytes(GetChannelValue(channelNumber));
            command.CommandData[2] = b[0];
            command.CommandData[3] = b[1];
            command.CommandData[4] = b[2];
            command.CommandData[5] = b[3];

            // Add country code
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    command.CommandData[6] = (byte)d.Currency[0];
                    command.CommandData[7] = (byte)d.Currency[1];
                    command.CommandData[8] = (byte)d.Currency[2];
                    break;
                }
            }

            command.CommandDataLength = 9;
            if (!SendCommand() || !CheckGenericResponses())
                return false;

            // Update list
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    d.IsRecycling = true;
                    break;
                }
            }
            log.Info("Note routing successful");
            return false;
        }

        /// <summary>
        /// Sets the protocol version in the validator to the version passed across.
        /// </summary>
        /// <param name="protocolVersion">The protocol version to set.</param>
        public bool SetProtocolVersion(byte protocolVersion)
        {
            command.CommandData[0] = CCommands.SSP_CMD_HOST_PROTOCOL_VERSION;
            command.CommandData[1] = protocolVersion;
            command.CommandDataLength = 2;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            return true;
        }

        /// <summary>
        /// Payout a specified value.
        /// </summary>
        /// <param name="amount">The amount to payout.</param>
        /// <param name="checkIfPayoutIsPossible">If true, checks if it's possible to payout the amount required.</param>
        public bool Payout(int amount, bool checkIfPayoutIsPossible = false)
        {
            command.CommandData[0] = CCommands.SSP_CMD_PAYOUT_AMOUNT;

            // Value to payout
            byte[] b = CHelpers.ConvertInt32ToBytes(amount);
            command.CommandData[1] = b[0];
            command.CommandData[2] = b[1];
            command.CommandData[3] = b[2];
            command.CommandData[4] = b[3];

            // Country code
            command.CommandData[5] = (byte)Currency[0];
            command.CommandData[6] = (byte)Currency[1];
            command.CommandData[7] = (byte)Currency[2];

            // Payout option (0x19 for test, 0x58 for real)
            command.CommandData[8] = (byte)(checkIfPayoutIsPossible ? 0x19 : 0x58);
            command.CommandDataLength = 9;

            try
            {
                if (!SendCommand() || !CheckGenericResponses())
                    return false;
                if (!checkIfPayoutIsPossible)
                {
                    // It's a real payout operation, so we set the inital values to the current values.
                    // This way when the payout operation is finished, we compare the initial values to
                    // the final values, to get the notes dispensed.
                    InitialUnitDataList = UnitDataList.DeepClone();
                    // ommand.ResponseData[0].ToString("X2"));
                }
            }
            catch (SspException ex)
            {
                if (checkIfPayoutIsPossible && ex.ErrorCode == CCommands.SSP_RESPONSE_COMMAND_CANNOT_BE_PROCESSED)
                {
                    throw new UnableToPayAmountException("Smart Payout unable to pay requested amount");
                }
                //throw;
            }
            log.Info("Paying out " + CHelpers.FormatToCurrency(amount)+"\r\n");
            return true;
        }
        public bool FloatByDenominationCLP()
        {
            var denomination = GetAllLevels();

            string[] denomination1 = denomination.Split(';');

            string[] n1Mil  = denomination1[1].Split(',');
            string[] n2Mil  = denomination1[2].Split(',');
            string[] n5Mil  = denomination1[3].Split(',');
            string[] n10Mil = denomination1[4].Split(',');
            string[] n20Mil = denomination1[5].Split(',');

            var flouting = 0;

            if (Int32.Parse(n1Mil[1])  > Settings.Default.milCLP) flouting += 1;
            if (Int32.Parse(n2Mil[1])  > Settings.Default.dosMilCLP) flouting += 1;
            if (Int32.Parse(n5Mil[1])  > Settings.Default.cincoMilCLP) flouting += 1;
            if (Int32.Parse(n10Mil[1]) > Settings.Default.diezMilCLP) flouting += 1;
            if (Int32.Parse(n20Mil[1]) > Settings.Default.veinteMilCLP) flouting += 1;
            if (flouting == 0) return false;

            int numMil = Settings.Default.milCLP;
            int mil = 100000;
            if((Int32.Parse(n1Mil[1])) < numMil) numMil = (Int32.Parse(n1Mil[1]));

            command.CommandData[0] = CCommands.SSP_CMD_FLOAT_BY_DENOMINATION;

            command.CommandData[1] = 5;

            byte[] bMil = CHelpers.ConvertInt32ToBytes(numMil);
            command.CommandData[2] = bMil[0];
            command.CommandData[3] = bMil[1];

            bMil = CHelpers.ConvertInt32ToBytes(mil);
            command.CommandData[4] = bMil[0];
            command.CommandData[5] = bMil[1];
            command.CommandData[6] = bMil[2];
            command.CommandData[7] = bMil[3];

            command.CommandData[8] = (byte)Currency[0];
            command.CommandData[9] = (byte)Currency[1];
            command.CommandData[10] = (byte)Currency[2];

            int numDosMil = Settings.Default.dosMilCLP;
            int dosMil = 200000;
            if ((Int32.Parse(n2Mil[1])) < numDosMil) numDosMil = (Int32.Parse(n2Mil[1]));

            byte[] bDosMil = CHelpers.ConvertInt32ToBytes(numDosMil);
            command.CommandData[11] = bDosMil[0];
            command.CommandData[12] = bDosMil[1];
             
            bDosMil = CHelpers.ConvertInt32ToBytes(dosMil);
            command.CommandData[13] = bDosMil[0];
            command.CommandData[14] = bDosMil[1];
            command.CommandData[15] = bDosMil[2];
            command.CommandData[16] = bDosMil[3];

            command.CommandData[17] = (byte)Currency[0];
            command.CommandData[18] = (byte)Currency[1];
            command.CommandData[19] = (byte)Currency[2];

            int numCincoMil = Settings.Default.cincoMilCLP;
            int cincoMil = 500000;
            if ((Int32.Parse(n5Mil[1])) < numCincoMil) numCincoMil = (Int32.Parse(n5Mil[1]));

            byte[] bCincoMil = CHelpers.ConvertInt32ToBytes(numCincoMil);
            command.CommandData[20] = bCincoMil[0];
            command.CommandData[21] = bCincoMil[1];

            bCincoMil = CHelpers.ConvertInt32ToBytes(cincoMil);
            command.CommandData[22] = bCincoMil[0];
            command.CommandData[23] = bCincoMil[1];
            command.CommandData[24] = bCincoMil[2];
            command.CommandData[25] = bCincoMil[3];

            command.CommandData[26] = (byte)Currency[0];
            command.CommandData[27] = (byte)Currency[1];
            command.CommandData[28] = (byte)Currency[2];

            int numDiezMil = Settings.Default.diezMilCLP;
            int diezMil = 1000000;
            if ((Int32.Parse(n10Mil[1])) < numDiezMil) numDiezMil = (Int32.Parse(n10Mil[1]));

            byte[] bDiezMil = CHelpers.ConvertInt32ToBytes(numDiezMil);
            command.CommandData[29] = bDiezMil[0];
            command.CommandData[30] = bDiezMil[1];

            bDiezMil = CHelpers.ConvertInt32ToBytes(diezMil);
            command.CommandData[31] = bDiezMil[0];
            command.CommandData[32] = bDiezMil[1];
            command.CommandData[33] = bDiezMil[2];
            command.CommandData[34] = bDiezMil[3];

            command.CommandData[35] = (byte)Currency[0];
            command.CommandData[36] = (byte)Currency[1];
            command.CommandData[37] = (byte)Currency[2];

            int numVeinteMil = Settings.Default.veinteMilCLP;
            int veinteMil = 2000000;
            if ((Int32.Parse(n20Mil[1])) < numVeinteMil) numVeinteMil = (Int32.Parse(n20Mil[1]));

            byte[] bVeinteMil = CHelpers.ConvertInt32ToBytes(numVeinteMil);
            command.CommandData[38] = bVeinteMil[0];
            command.CommandData[39] = bVeinteMil[1];

            bVeinteMil = CHelpers.ConvertInt32ToBytes(veinteMil);
            command.CommandData[40] = bVeinteMil[0];
            command.CommandData[41] = bVeinteMil[1];
            command.CommandData[42] = bVeinteMil[2];
            command.CommandData[43] = bVeinteMil[3];

            command.CommandData[44] = (byte)Currency[0];
            command.CommandData[45] = (byte)Currency[1];
            command.CommandData[46] = (byte)Currency[2];

            command.CommandData[47] = 0x58;

            command.CommandDataLength = 48;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Paying out by denomination...\r\n");
            return true;
        }
        // Payout by denomination. This function allows a developer to payout specified amounts of certain
        // notes. Due to the variable length of the data that could be passed to the function, the user 
        // passes an array containing the data to payout and the length of that array along with the number
        // of denominations they are paying out.
        //DM
        public bool PayoutByDenomination(byte numDenoms, byte[] data, byte dataLength)
        {
            // First is the command byte
            command.CommandData[0] = CCommands.SSP_CMD_PAYOUT_BY_DENOMINATION;

            // Next is the number of denominations to be paid out
            command.CommandData[1] = numDenoms;

            // Copy over data byte array parameter into command structure
            int currentIndex = 2;
            for (int i = 0; i < dataLength; i++)
                command.CommandData[currentIndex++] = data[i];

            // Perform a real payout (0x19 for test)
            command.CommandData[currentIndex++] = 0x58;

            // Length of command data (add 3 to cover the command byte, num of denoms and real/test byte)
            dataLength += 3;
            command.CommandDataLength = dataLength;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            return true;
        }
        // This function changes the colour of a supported bezel.  As command data byte 4 is set to 0x00, the change will not
        // be stored in EEPROM.
        //dm
        public bool ConfigureBezel(byte red, byte green, byte blue)
        {
            command.CommandData[0] = CCommands.SSP_CMD_CONFIGURE_BEZEL;
            command.CommandData[1] = red;
            command.CommandData[2] = green;
            command.CommandData[3] = blue;
            command.CommandData[4] = 0x00;
            command.CommandDataLength = 5;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            return true;
        }
        /// <summary>
        /// Setups the encryption between the host and the validator.
        /// </summary>
        public bool NegotiateKeys()
        {
            // Make sure encryption is off
            command.EncryptionStatus = false;
            log.Info("Syncing... ");
            // Send sync command
            command.CommandData[0] = CCommands.SSP_CMD_SYNC;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success");
            LibraryHandler.InitiateKeys(ref keys, ref command);

            // Send generator
            command.CommandData[0] = CCommands.SSP_CMD_SET_GENERATOR;
            command.CommandDataLength = 9;
            log.Info("Setting generator... ");
            // Convert generator to bytes and add to command data
            BitConverter.GetBytes(keys.Generator).CopyTo(command.CommandData, 1);
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success\r\n");
            // Send modulus
            command.CommandData[0] = CCommands.SSP_CMD_SET_MODULUS;
            command.CommandDataLength = 9;
            log.Info("Sending modulus... ");
            // Convert modulus to bytes and add to command data.
            BitConverter.GetBytes(keys.Modulus).CopyTo(command.CommandData, 1);
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success\r\n");
            // Send key exchange
            command.CommandData[0] = CCommands.SSP_CMD_REQUEST_KEY_EXCHANGE;
            command.CommandDataLength = 9;
            log.Info("Exchanging keys... ");
            // Convert host intermediate key to bytes and add to command data.
            BitConverter.GetBytes(keys.HostInter).CopyTo(command.CommandData, 1);
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success\r\n");
            // Read slave intermediate key.
            keys.SlaveInterKey = BitConverter.ToUInt64(command.ResponseData, 1);

            LibraryHandler.CreateFullKey(ref keys);

            // Get full encryption key
            command.Key.FixedKey = 0x0123456701234567;
            command.Key.VariableKey = keys.KeyHost;
            log.Info("Keys successfully negotiated\r\n");
            return true;
        }

        /// <summary>
        /// Request all the information about the validator.
        /// </summary>
        public bool RequestValidatorInformation()
        {
            command.CommandData[0] = CCommands.SSP_CMD_SETUP_REQUEST;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;

            var index = 1;
            UnitType = (char)command.ResponseData[index++];

            var firmware = "";
            while (index <= 5)
            {
                firmware += (char)command.ResponseData[index++];
                if (index == 4)
                {
                    firmware += ".";
                }
            }

            Firmware = firmware;

            // Country code. Legacy code so skip it.
            index += 3;

            // Value multiplier. Legacy code so skip it.
            index += 3;

            NumberOfChannels = command.ResponseData[index++];

            // Channel values. Legacy code so skip it.
            index += NumberOfChannels; // Skip channel values

            // Channel security. Legacy code so skip it.
            index += NumberOfChannels;

            // Real value multiplier (big endian)
            ValueMultiplier = command.ResponseData[index + 2];
            ValueMultiplier += command.ResponseData[index + 1] << 8;
            ValueMultiplier += command.ResponseData[index] << 16;
            index += 3;

            // Protocol version
            ProtocolVersion = command.ResponseData[index++];

            // Add channel data to list then display. Clear list
            UnitDataList.Clear();

            // Loop through all channels
            for (byte i = 0; i < NumberOfChannels; i++)
            {
                ChannelData loopChannelData = new ChannelData();

                loopChannelData.Channel = (byte)(i + 1);
                loopChannelData.Value = BitConverter.ToInt32(command.ResponseData, index + (NumberOfChannels * 3) + (i * 4)) * ValueMultiplier;
                loopChannelData.Currency[0] = (char)command.ResponseData[index + (i * 3)];
                loopChannelData.Currency[1] = (char)command.ResponseData[(index + 1) + (i * 3)];
                loopChannelData.Currency[2] = (char)command.ResponseData[(index + 2) + (i * 3)];
                loopChannelData.Level = CheckNoteLevel(loopChannelData.Value, loopChannelData.Currency);
                loopChannelData.IsRecycling = IsNoteRecycling(loopChannelData.Value, loopChannelData.Currency);

                UnitDataList.Add(loopChannelData);
            }

            // Sort the list by Value
            UnitDataList.Sort((d1, d2) => d1.Value.CompareTo(d2.Value));
            return true;
        }

        /// <summary>
        /// Sets the inhibits on the validator.
        /// </summary>
        public bool SetInhibits()
        {
            command.CommandData[0] = CCommands.SSP_CMD_SET_CHANNEL_INHIBITS;
            command.CommandData[1] = 0xFF;
            command.CommandData[2] = 0xFF;
            command.CommandDataLength = 3;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Inhibits set\r\n");
            return true;
        }

        /// <summary>
        /// Returns a value that indicates if a note is currently being recyled.
        /// </summary>
        /// <param name="noteValue">The note to check for.</param>
        /// <param name="currency">The currency of the note.</param>
        /// <returns>True if a note is recycling; otherwise, false.</returns>
        public bool IsNoteRecycling(int noteValue, char[] currency)
        {
            byte[] b = CHelpers.ConvertInt32ToBytes(noteValue);

            command.CommandData[0] = CCommands.SSP_CMD_GET_DENOMINATION_ROUTE;
            command.CommandData[1] = b[0];
            command.CommandData[2] = b[1];
            command.CommandData[3] = b[2];
            command.CommandData[4] = b[3];

            command.CommandData[5] = (byte)currency[0];
            command.CommandData[6] = (byte)currency[1];
            command.CommandData[7] = (byte)currency[2];
            command.CommandDataLength = 8;

            if (!SendCommand() || !CheckGenericResponses())
                return false;

            var isNoteRecycling = (command.ResponseData[1] == 0x00);

            return isNoteRecycling;
        }

        /// <summary>
        /// Updates all the notes information in the list.
        /// </summary>
        public void UpdateData()
        {
            foreach (ChannelData d in UnitDataList)
            {
                d.Level = CheckNoteLevel(d.Value, d.Currency);
                d.IsRecycling = IsNoteRecycling(d.Value, d.Currency);
            }
        }

        /// <summary>
        /// Returns a channel by a given channel number.
        /// </summary>
        /// <param name="channelNumber">The channel number to search for.</param>
        /// <returns>A <see cref="ChannelData"/>.</returns>
        public ChannelData GetChannelDataByChannelNumber(int channelNumber)
        {
            foreach (ChannelData item in UnitDataList)
            {
                if (item.Channel == channelNumber)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a report of what was moved to the cashbox, after a SMART empty.
        /// </summary>
        /// <returns>A string that contains the report.</returns>
        public string GetCashboxPayoutOpData()
        {
            command.CommandData[0] = CCommands.SSP_CMD_CASHBOX_PAYOUT_OPERATION_DATA;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return "false";

            // now deal with the response data
            // number of different notes
            var n = command.ResponseData[1];
            var displayString = "Number of Total Notes: " + n.ToString() + "\r\n\r\n";
            var i = 0;
            for (i = 2; i < (9 * n); i += 9)
            {
                displayString += "Moved " + CHelpers.ConvertBytesToInt16(command.ResponseData, i) +
                    " x " + CHelpers.FormatToCurrency(CHelpers.ConvertBytesToInt32(command.ResponseData, i + 2)) +
                    " " + (char)command.ResponseData[i + 6] + (char)command.ResponseData[i + 7] + (char)command.ResponseData[i + 8] + " to cashbox\r\n";
            }

            displayString += CHelpers.ConvertBytesToInt32(command.ResponseData, i) + " notes not recognised\r\n";
            return displayString;
        }

        /// <summary>
        /// Returns information about why a note has been rejected.
        /// </summary>
        /// <returns>The reason of the rejection.</returns>
        public RejectionReason GetRejectionReason()
        {
            command.CommandData[0] = CCommands.SSP_CMD_LAST_REJECT_CODE;
            command.CommandDataLength = 1;
            SendCommand();
            

            switch (command.ResponseData[1])
            {
                case 0x00: return RejectionReason.NoteAccepted;
                case 0x01: return RejectionReason.NoteLengthIncorrect;
                case 0x02: return RejectionReason.InvalidNote;
                case 0x03: return RejectionReason.InvalidNote;
                case 0x04: return RejectionReason.InvalidNote;
                case 0x05: return RejectionReason.InvalidNote;
                case 0x06: return RejectionReason.ChannelInhabited;
                case 0x07: return RejectionReason.SecondNoteInsertedDuringRead;
                case 0x08: return RejectionReason.HostRejectedNote;
                case 0x09: return RejectionReason.InvalidNote;
                case 0x0A: return RejectionReason.InvalidNoteRead;
                case 0x0B: return RejectionReason.NoteTooLong;
                case 0x0C: return RejectionReason.ValidatorDisabled;
                case 0x0D: return RejectionReason.MechanismSlowOrStalled;
                case 0x0E: return RejectionReason.StrimmingAttempt;
                case 0x0F: return RejectionReason.FraudChannelReject;
                case 0x10: return RejectionReason.NoNotesInserted;
                case 0x11: return RejectionReason.InvalidNoteRead;
                case 0x12: return RejectionReason.TwistedNoteDetected;
                case 0x13: return RejectionReason.EscrowTimeout;
                case 0x14: return RejectionReason.BarcodeScanFail;
                case 0x15: return RejectionReason.InvalidNoteRead;
                case 0x16: return RejectionReason.InvalidNoteRead;
                case 0x17: return RejectionReason.InvalidNoteRead;
                case 0x18: return RejectionReason.InvalidNoteRead;
                case 0x19: return RejectionReason.IncorrectNoteWidth;
                case 0x1A: return RejectionReason.NoteTooShort;
                default: throw new InvalidOperationException(string.Format("The rejection code ({0:X2}) has an unknown value.", command.ResponseData[1]));
            }
        }

        /// <summary>
        /// Enables or disables the encryption.
        /// </summary>
        /// <param name="enableEncryption">True to enable encryption; otherwise, false.</param>
        public void SetEncryption(bool enableEncryption)
        {
            command.EncryptionStatus = enableEncryption;
        }

        /// <summary>
        /// Gets the serial number of the device.
        /// </summary>
        /// <returns>The serial number.</returns>
        public uint GetSerialNumber()
        {
            command.CommandData[0] = CCommands.SSP_CMD_GET_SERIAL_NUMBER;
            command.CommandDataLength = 1;

            if (!SendCommand() || !CheckGenericResponses())
                return 0;

            // Response data is big endian, so reverse bytes 1 to 4.
            Array.Reverse(command.ResponseData, 1, 4);
            return BitConverter.ToUInt32(command.ResponseData, 1);
        }

        /// <summary>
        /// Polls the validator for information.
        /// </summary>
        public bool DoPolling()
        {
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_atascoSeguro;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_atascoInSeguro;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_intentoFraudeB;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_cajaFull;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_unidadAtascada;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_flotacion;

            // If a note is held in escrow, send hold command, as poll releases note.
            if (HoldCount > 0)
            {
                NoteHeld = true;
                HoldCount--;
                command.CommandData[0] = CCommands.SSP_CMD_HOLD;
                command.CommandDataLength = 1;
                log.Debug("Note held in scrow: " + HoldCount);
                if (!SendCommand() || !CheckGenericResponses()) 
                    return false;
                return true;
            }

            // Send poll
            command.CommandData[0] = CCommands.SSP_CMD_POLL;
            command.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;

            if (command.ResponseData[0] == 0xFA)
                return false;

            // Store response locally so data can't get corrupted by other use of the command variable
            var response = new byte[255];
            command.ResponseData.CopyTo(response, 0);
            var responseLength = command.ResponseDataLength;

            // Parse poll response
            ChannelData channelData = new ChannelData();

           
            for (byte i = 1; i < responseLength; ++i)
            {
                switch (response[i])
                {
                    // This response indicates that the unit was reset and this is the first time a poll has been called since the reset.
                    case CCommands.SSP_POLL_SLAVE_RESET:
                        log.Debug("SmartPayout reset");
                        UpdateData();
                        break;
                    // This response is given when the unit is disabled.
                    case CCommands.SSP_POLL_DISABLED:
                        //log.Debug("SmartPayout disabled");
                        break;
                    // A note is currently being read by the validator sensors. The second byte of this response
                    // is zero until the note's type has been determined, it then changes to the channel of the
                    // scanned note.
                    case CCommands.SSP_POLL_READ_NOTE:
                        if (command.ResponseData[i + 1] > 0)
                        {
                            channelData = GetChannelDataByChannelNumber(response[i + 1]);
                            log.DebugFormat("Note read: {0}", channelData.Value);
                            HoldCount = HoldNumber;
                        }
                        else
                        {
                            log.Debug("Reading note");
                        }
                        i++;
                        break;
                    // A credit event has been detected, this is when the validator has accepted a note as legal currency.
                    case CCommands.SSP_POLL_CREDIT_NOTE:
                        channelData = GetChannelDataByChannelNumber(response[i + 1]);
                        canalData = channelData.Value;
                        posicion = channelData.IsRecycling;
                        log.DebugFormat("Note accepted: {0}", channelData.Value);
                        //NoteAccepted?.Invoke(this, new NoteAcceptedEventArgs(channelData.Value, channelData.IsRecycling));
                        //UpdateData(); // Commented because this command randomly responds "Busy"
                        channelData.Level++;
                        i++;
                        break;
                    // A note is being rejected from the validator. This will carry on polling while the note is in transit.
                    case CCommands.SSP_POLL_NOTE_REJECTING:
                        log.Debug("Rejecting note");
                        break;
                    // A note has been rejected from the validator, the note will be resting in the bezel. This response only
                    // appears once.
                    case CCommands.SSP_POLL_NOTE_REJECTED:
                        log.Debug("Note rejected");
                        LastRejectionReason = GetRejectionReason();
                        break;
                    // A note is in transit to the cashbox.
                    case CCommands.SSP_POLL_NOTE_STACKING:
                        log.Debug("Note stacking");
                        break;
                    // The payout device is 'floating' a specified amount of notes. It will transfer some to the cashbox and
                    // leave the specified amount in the payout device.
                    case CCommands.SSP_POLL_FLOATING:
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_flotacion;
                        // Now the index needs to be moved on to skip over the data provided by this response so it
                        // is not parsed as a normal poll response.
                        // In this response, the data includes the number of countries being floated (1 byte), then a 4 byte value
                        // and 3 byte currency code for each country. 
                        log.Debug("Floating");
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // A note has reached the cashbox.
                    case CCommands.SSP_POLL_NOTE_STACKED:
                        //abajo
                        log.Debug("Note stacked");
                        NoteAccepted?.Invoke(this, new NoteAcceptedEventArgs(canalData, false));
                        //RequestValidatorInformation();
                        //UpdateData();
                        break;
                    // The float operation has been completed.
                    case CCommands.SSP_POLL_FLOATED:
                        ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_flotacion;
                        log.Debug("Completed floating");
                        InitialUnitDataListFloat = UnitDataList.DeepClone();
                        UpdateData();
                        var dispensedNotesF = new List<ChannelData>();
                        for (var j = 0; j < UnitDataList.Count; j++)
                        {
                            if (UnitDataList[j].Level < InitialUnitDataListFloat[j].Level)
                            {
                                var channelF = new ChannelData();
                                channelF.Value = UnitDataList[j].Value / 100;
                                channelF.Level = InitialUnitDataListFloat[j].Level - UnitDataList[j].Level;
                                dispensedNotesF.Add(channelF);
                            }
                        }
                        NoteFloat?.Invoke(this, new NoteFloatEventArgs(dispensedNotesF));
                        DisableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // A note has been stored in the payout device to be paid out instead of going into the cashbox.The float operation has been completed.
                    case CCommands.SSP_POLL_NOTE_STORED_IN_PAYOUT:
                        //arriba
                        log.Debug("Note stored in the payout device");
                        NoteAccepted?.Invoke(this, new NoteAcceptedEventArgs(canalData, true));
                        //UpdateData();
                        break;
                    // A safe jam has been detected. This is where the user has inserted a note and the note
                    // is jammed somewhere that the user cannot reach.
                    case CCommands.SSP_POLL_SAFE_NOTE_JAM:
                    //atasco seguro
                    //usuario no puede alcanzar billete
                        log.Debug("Safe note jam");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_atascoSeguro;
                        break;
                    // An unsafe jam has been detected. This is where a user has inserted a note and the note
                    // is jammed somewhere that the user can potentially recover the note from.
                    case CCommands.SSP_POLL_UNSAFE_NOTE_JAM:
                        //atasco inseguro
                        //usuario si puede alcanzar billete
                        log.Debug("Unsafe note jam");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_atascoInSeguro;
                        break;
                    // An error has been detected by the payout unit.
                    case CCommands.SSP_POLL_ERROR_DURING_PAYOUT: // Note: Will be reported only when Protocol version >= 7
                        log.Debug("Detected error with payout device");
                        i += (byte)((response[i + 1] * 7) + 2);
                        break;
                    // A fraud attempt has been detected. 
                    case CCommands.SSP_POLL_FRAUD_ATTEMPT:
                        log.Debug("Fraud attempt");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_intentoFraudeB;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The stacker (cashbox) is full.
                    case CCommands.SSP_POLL_STACKER_FULL:
                        log.Debug("Stacker full");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_cajaFull;
                        break;
                    // A note was detected somewhere inside the validator on startup and was rejected from the front of the
                    // unit.
                    case CCommands.SSP_POLL_NOTE_CLEARED_FROM_FRONT:
                        log.Debug("Note cleared from front of validator");
                        i++;
                        break;
                    // A note was detected somewhere inside the validator on startup and was cleared into the cashbox.
                    case CCommands.SSP_POLL_NOTE_CLEARED_TO_CASHBOX:
                        log.Debug("Note cleared to cashbox");
                        i++;
                        break;
                    // A note has been detected in the validator on startup and moved to the payout device 
                    case CCommands.SSP_POLL_NOTE_PAID_INTO_STORE_AT_POWER_UP:
                        log.Debug("Note paid into payout device on startup");
                        i += 7;
                        break;
                    // A note has been detected in the validator on startup and moved to the cashbox
                    case CCommands.SSP_POLL_NOTE_PAID_INTO_STACKER_AT_POWER_UP:
                        log.Debug("Note paid into cashbox on startup");
                        i += 7;
                        break;
                    // The cashbox has been removed from the unit. This will continue to poll until the cashbox is replaced.
                    case CCommands.SSP_POLL_CASHBOX_REMOVED:
                        log.Debug("Cashbox removed");
                        break;
                    // The cashbox has been replaced, this will only display on a poll once.
                    case CCommands.SSP_POLL_CASHBOX_REPLACED:
                        log.Debug("Cashbox replaced");
                        break;
                    // The validator is in the process of paying out a note, this will continue to poll until the note has 
                    // been fully dispensed and removed from the front of the validator by the user.
                    case CCommands.SSP_POLL_DISPENSING:
                        log.Debug("Dispensing note");
                        int iVal = 0, iV;
                        int auxpos = i;
                        for (int j=0; j< response[i + 1];j++)
                        {
                            iV =  response[auxpos + 1 + 4] << 32;
                            iV += (response[auxpos + 1 + 3] << 16);
                            iV += (response[auxpos + 1 + 2] << 8);
                            iV += (response[auxpos + 1 + 1]);

                            iVal += iV;
                            auxpos += 7; 
                        }
                        log.Info("Valor descifrado del dispensin " + iVal);
                        dispensingNotes = iVal;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The note(s) has been dispensed and removed from the bezel by the user.
                    case CCommands.SSP_POLL_DISPENSED:
                        log.Debug("Dispensed note(s)");
                        UpdateData();
                        var dispensedNotes = new List<ChannelData>();
                        for (var j = 0; j < UnitDataList.Count; j++)
                        {
                            if (UnitDataList[j].Level < InitialUnitDataList[j].Level)
                            {
                                var channel = new ChannelData();
                                channel.Value = UnitDataList[j].Value / 100;
                                channel.Level = InitialUnitDataList[j].Level - UnitDataList[j].Level;
                                dispensedNotes.Add(channel);
                            }
                        }

                        DispenseOperationFinished?.Invoke(this, new DispenseOperationFinishedEventArgs(dispensedNotes));
                        EnableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The payout device is in the process of emptying all its stored notes to the cashbox. This
                    // will continue to poll until the device is empty.
                    case CCommands.SSP_POLL_EMPTYING:
                        log.Debug("Emptying");
                        break;
                    // This single poll response indicates that the payout device has finished emptying.
                    case CCommands.SSP_POLL_EMPTIED:
                        log.Debug("Emptied");
                        NotePollEmptied?.Invoke(this, EventArgs.Empty);
                        UpdateData();
                        EnableValidator();
                        break;
                    // The payout device is in the process of SMART emptying all its stored notes to the cashbox, keeping
                    // a count of the notes emptied. This will continue to poll until the device is empty.
                    case CCommands.SSP_POLL_SMART_EMPTYING:
                        log.Debug("SMART Emptying");
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The payout device has finished SMART emptying, the information of what was emptied can now be displayed
                    // using the CASHBOX PAYOUT OPERATION DATA command.
                    case CCommands.SSP_POLL_SMART_EMPTIED:
                        log.Debug("SMART Emptied");
                        UpdateData();
                        GetCashboxPayoutOpData();
                        EnableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The payout device has encountered a jam. This will not clear until the jam has been removed and the unit
                    // reset.
                    case CCommands.SSP_POLL_JAMMED:
                        log.Debug("Unit jammed");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_unidadAtascada;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // This is reported when the payout has been halted by a host command. This will report the value of
                    // currency dispensed upto the point it was halted. 
                    case CCommands.SSP_POLL_HALTED:
                        log.Debug("Halted");
                        int iVal1 = 0, iV1;
                        int auxpos1 = i;
                        for (int j = 0; j < response[i + 1]; j++)
                        {
                            iV1 = response[auxpos1 + 1 + 4] << 32;
                            iV1 += (response[auxpos1 + 1 + 3] << 16);
                            iV1 += (response[auxpos1 + 1 + 2] << 8);
                            iV1 += (response[auxpos1 + 1 + 1]);

                            iVal1 += iV1;
                            auxpos1 += 7;
                        }
                        log.Info("Valor notas stop " + iVal1);
                        NotasDetenidas = iVal1;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // This is reported when the payout was powered down during a payout operation. It reports the original amount
                    // requested and the amount paid out up to this point for each currency.
                    case CCommands.SSP_POLL_INCOMPLETE_PAYOUT:
                        log.Debug("Incomplete payout");

                        /*for (var j = 0; j < response[i + 1] * 11; j += 11)
                        {
                            var dispensedValue = CHelpers.ConvertBytesToInt32(command.ResponseData, i + j + 2);
                            var requestedValue = CHelpers.ConvertBytesToInt32(command.ResponseData, i + j + 6);
                            var currency = "";

                            currency = "";
                            currency += (char)response[i + j + 10];
                            currency += (char)response[i + j + 11];
                            currency += (char)response[i + j + 12];

                            log.DebugFormat("DispensedValue: {0}, RequestedValue: {1}", dispensedValue, requestedValue);

                            IncompletePayoutDetected?.Invoke(this, new IncompletePayoutDetectedEventArgs(dispensedValue, requestedValue));
                        }*/

                        i += (byte)((response[i + 1] * 11) + 1);
                        break;
                    // This is reported when the payout was powered down during a float operation. It reports the original amount
                    // requested and the amount paid out up to this point for each currency.
                    case CCommands.SSP_POLL_INCOMPLETE_FLOAT:
                        log.Debug("Incomplete float");
                        i += (byte)((response[i + 1] * 11) + 1);
                        break;
                    // A note has been transferred from the payout unit to the stacker.
                    case CCommands.SSP_POLL_NOTE_TRANSFERED_TO_STACKER:
                        log.Debug("Note transferred to stacker");
                        i += 7;
                        break;
                    // A note is resting in the bezel waiting to be removed by the user.
                    case CCommands.SSP_POLL_NOTE_HELD_IN_BEZEL:
                        log.Debug("Note in bezel");                        
                        i += 7;
                        break;
                    // The payout has gone out of service, the host can attempt to re-enable the payout by sending the enable payout
                    // command.
                    case CCommands.SSP_POLL_PAYOUT_OUT_OF_SERVICE:
                        log.Debug("Payout out of service");
                        break;
                    // The unit has timed out while searching for a note to payout. It reports the value dispensed before the timeout
                    // event.
                    case CCommands.SSP_POLL_TIME_OUT:
                        log.Debug("Timed out searching for a note");
                        var valueDispensed = CHelpers.ConvertBytesToInt32(response, i + 11);
                        DispenseOperationTimedOut?.Invoke(this, new DispenseOperationTimedOutEventArgs(valueDispensed));
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unsupported poll response received: {0:X2}", command.ResponseData[i]));
                }
            }
            return true;
        }

        /// <summary>
        /// Opens a new connection to the device.
        /// </summary>
      
        public void OpenPort()
        {
            LibraryHandler.OpenPort(ref command);
        }

        /// <summary>
        /// Sends a command via SSP to the validator.
        /// </summary>
        private bool SendCommand()
        {
            var periferico = "Payout";
            if(!LibraryHandler.SendCommand(ref command, ref info, periferico))
            {
                m_Comms.UpdateLog(info, true);
                return false;
            }
            m_Comms.UpdateLog(info);
            return true;
        }
               
        /// <summary>
        /// Gets a friendly error message.
        /// </summary>
        /// <param name="command">The command containing the error.</param>
        /// <returns>A string that contains the message.</returns>
        private bool CheckGenericResponses()
        {
            if (command.ResponseData[0] == CCommands.SSP_RESPONSE_OK)
                return true;
            else
            {
                switch (command.ResponseData[0])
                {
                    case CCommands.SSP_RESPONSE_COMMAND_CANNOT_BE_PROCESSED:
                        if (command.ResponseData[1] == 0x03)
                        {
                            log.Info("(Payout) Unit responded with a \"Busy\" response, command cannot be " +
                                "processed at this time\r\n");
                        }
                        else
                        {
                            log.Info("(Payout) Command response is CANNOT PROCESS COMMAND, error code - 0x"
                            + BitConverter.ToString(command.ResponseData, 1, 1) + "\r\n");
                        }
                        return false;
                    case CCommands.SSP_RESPONSE_FAIL:
                        log.Info("(Payout) Command response is FAIL\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_KEY_NOT_SET:
                        log.Info("(Payout) Command response is KEY NOT SET, renegotiate keys\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_PARAMETER_OUT_OF_RANGE:
                        log.Info("(Payout) Command response is PARAM OUT OF RANGE\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_SOFTWARE_ERROR:

                        log.Info("(Payout) Command response is SOFTWARE ERROR\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_COMMAND_NOT_KNOWN:
                        log.Info("(Payout) Command response is UNKNOWN\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_WRONG_NO_PARAMETERS:
                        log.Info("(Payout) Command response is WRONG PARAMETERS\r\n");
                        return false;
                    default:
                        return false;
                }
            }
        }
    }
}
