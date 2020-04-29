using ITLlib;
using System;
using System.Collections.Generic;
using System.Linq;
using TotalPack.Efectivo.SSP.Events;
using TotalPack.Efectivo.SSP.Exceptions;
using TotalPack.Efectivo.SSP.Helpers;
using TotalPack.Efectivo.SSP.Properties;

namespace TotalPack.Efectivo.SSP
{
    /// <summary>
    /// Represents a SMART Hopper device.
    /// </summary>
    public class SmartHopper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private SSP_COMMAND commandH;
        private SSP_KEYS keys;
        private SSP_FULL_KEY sspKey;
        private SSP_COMMAND_INFO info;
        CCommsWindow m_Comms;

        public string Currency = Settings.Default.moneda;
        public delegate void DispenseOperationFinishedEventHandler(object sender, DispenseOperationFinishedEventArgs e);
        public delegate void NotePollEventHandler(object sender, EventArgs e);
        public delegate void IncompletePayoutDetectedEventHandler(object sender, IncompletePayoutDetectedEventArgs e);
        public delegate void DispenseOperationTimedOutEventHandler(object sender, DispenseOperationTimedOutEventArgs e);

        public int dispensingCoins { get; set; }

        /// <summary>
        /// Occurs when all the coins has been dispensed.
        /// </summary>
        public event DispenseOperationFinishedEventHandler DispenseOperationFinished;
        /// <summary>
        /// Occurs when the Hopper device has finished emptying.
        /// </summary>
        public event NotePollEventHandler NotePollEmptied;
        /// <summary>
        /// Occurs when the payout operation could not be completed.
        /// </summary>
        public event IncompletePayoutDetectedEventHandler IncompletePayoutDetected;
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
        /// Gets a value that indicates if the coin mech is enabled.
        /// </summary>
        public bool IsCoinMechEnabled { get; private set; }
        /// <summary>
        /// Gets the type of this unit.
        /// </summary>
        public char UnitType { get; private set; }
        /// <summary>
        /// Gets the information of channel number, value, currency, level and whether it is being recycled.
        /// </summary>
        public List<ChannelData> UnitDataList { get; private set; }
        /// <summary>
        /// Gets the firmware version.
        /// </summary>
        public string Firmware { get; private set; }
        /// <summary>
        /// Gets or sets the information of the channels, before a payout operation.
        /// </summary>
        public List<ChannelData> InitialUnitDataList { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartHopper"/> class.
        /// </summary>
        /// <param name="portName">The name of the COM port the device is connected to.</param>
        /// <param name="baudRate">The baud rate of the port.</param>
        /// <param name="sspAddress">The SSP address of the device.</param>
        /// <param name="timeout">The maximum time the library will wait for a response from the device before retrying the command.</param>
        /// <param name="retryLevel">The number of times the library will retry sending a packet to the device if no response is received.</param>
        public SmartHopper(string portName, int baudRate, byte sspAddress, uint timeout, byte retryLevel)
        {
            commandH = new SSP_COMMAND();
            keys = new SSP_KEYS();
            sspKey = new SSP_FULL_KEY();
            info = new SSP_COMMAND_INFO();
            m_Comms = new CCommsWindow("SMARTHopper");

            commandH.ComPort = portName;
            commandH.BaudRate = baudRate;
            commandH.SSPAddress = sspAddress;
            commandH.Timeout = timeout;
            commandH.RetryLevel = retryLevel;

            NumberOfChannels = 0;
            ProtocolVersion = 0;
            IsCoinMechEnabled = false;
            UnitDataList = new List<ChannelData>();
        }

        /// <summary>
        /// Gets the value of a channel.
        /// </summary>
        /// <param name="channelNumber">The number of the channel to get the value from.</param>
        /// <returns>The value of the channel.</returns>
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
            commandH.CommandData[0] = CCommands.SSP_CMD_SYNC;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            return true;
        }

        /// <summary>
        /// Enables the validator, allowing it to receive and act on commands.
        /// </summary>
        public bool EnableValidator()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_ENABLE;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("SMART Hopper enabled\r\n");
            return true;
        }

        /// <summary>
        /// Enables or disables the encryption.
        /// </summary>
        /// <param name="enableEncryption">True to enable encryption; otherwise, false.</param>
        public void SetEncryption(bool enableEncryption)
        {
            commandH.EncryptionStatus = enableEncryption;
        }

        /// <summary>
        /// Moves all the coins in the device to the cashbox. Then sets the channel levels to zero.
        /// </summary>
        public bool EmptyDevice()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_EMPTY_ALL;
            commandH.CommandDataLength = 1;

            if (!SendCommand() || !CheckGenericResponses())
                return false;

            foreach (ChannelData d in UnitDataList)
            {
                log.Info("Emptying all stored coins to cashbox...\r\n");
                d.Level = 0;
            }
            return true;
        }

        /// <summary>
        /// Sets a channel to route to cashbox.
        /// </summary>
        /// <param name="channelNumber"></param>
        public bool RouteChannelToCashbox(int channelNumber)
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_ROUTE;
            commandH.CommandData[1] = 0x01; // Cashbox

            // Get value of coin (4 byte protocol 6)
            byte[] b = CHelpers.ConvertInt32ToBytes(GetChannelValue(channelNumber));
            commandH.CommandData[2] = b[0];
            commandH.CommandData[3] = b[1];
            commandH.CommandData[4] = b[2];
            commandH.CommandData[5] = b[3];

            // Add country code
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    commandH.CommandData[6] = (byte)d.Currency[0];
                    commandH.CommandData[7] = (byte)d.Currency[1];
                    commandH.CommandData[8] = (byte)d.Currency[2];
                    break;
                }
            }

            commandH.CommandDataLength = 9;
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
            log.Info("Successfully routed coin on channel " + channelNumber.ToString() + " to cashbox\r\n");
            return true;
        }

        /// <summary>
        /// Sets a channel to route to storage.
        /// </summary>
        /// <param name="channelNumber"></param>
        public bool RouteChannelToStorage(int channelNumber)
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_ROUTE;
            commandH.CommandData[1] = 0x01; // Storage

            // Get value of coin (4 byte protocol 6)
            byte[] b = CHelpers.ConvertInt32ToBytes(GetChannelValue(channelNumber));
            commandH.CommandData[2] = b[0];
            commandH.CommandData[3] = b[1];
            commandH.CommandData[4] = b[2];
            commandH.CommandData[5] = b[3];

            // Add country code
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    commandH.CommandData[6] = (byte)d.Currency[0];
                    commandH.CommandData[7] = (byte)d.Currency[1];
                    commandH.CommandData[8] = (byte)d.Currency[2];
                    break;
                }
            }

            commandH.CommandDataLength = 9;
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
            log.Info("Successfully routed coin on channel " + channelNumber.ToString() + " to storage\r\n");
            return false;
        }

        /// <summary>
        /// Disables the validator, disallowing it to receive and act on commands.
        /// </summary>
        public bool DisableValidator()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_DISABLE;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("SMART Hopper disabled\r\n");
            return true;
        }

        /// <summary>
        /// Resets the validator (same as switching on and off).
        /// </summary>
        public bool Reset()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_RESET;
            commandH.CommandDataLength = 1;
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
            commandH.CommandData[0] = CCommands.SSP_CMD_PAYOUT_AMOUNT;

            // Value to payout
            byte[] b = CHelpers.ConvertInt32ToBytes(amount);
            commandH.CommandData[1] = b[0];
            commandH.CommandData[2] = b[1];
            commandH.CommandData[3] = b[2];
            commandH.CommandData[4] = b[3];

            // Country code
            commandH.CommandData[5] = (byte)Currency[0];
            commandH.CommandData[6] = (byte)Currency[1];
            commandH.CommandData[7] = (byte)Currency[2];

            // Payout option (0x19 for test, 0x58 for real)
            commandH.CommandData[8] = (byte)(checkIfPayoutIsPossible ? 0x19 : 0x58);
            commandH.CommandDataLength = 9;
           
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
                }
            }
            catch (SspException ex)
            {
                if (checkIfPayoutIsPossible && ex.ErrorCode == CCommands.SSP_RESPONSE_COMMAND_CANNOT_BE_PROCESSED)
                {
                    throw new UnableToPayAmountException("Smart Hopper unable to pay requested amount");
                }
                //throw;
            }
            return true;
        }

        /// <summary>
        /// Payout a specified amount of certain coins.
        /// </summary>
        /// <param name="numberOfDenominations">The number of denominations to payout.</param>
        /// <param name="data">The data to payout.</param>
        /// <param name="dataLength">The length of the data array.</param>
        public bool PayoutByDenomination(byte numberOfDenominations, byte[] data, byte dataLength)
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_PAYOUT_BY_DENOMINATION;
            commandH.CommandData[1] = numberOfDenominations;

            // Copy over data byte array parameter into command structure
            var currentIndex = 2;
            for (var i = 0; i < dataLength; i++)
            {
                commandH.CommandData[currentIndex++] = data[i];
            }

            // Perform a real payout (0x19 for test)
            commandH.CommandData[currentIndex++] = 0x58;

            // Length of command data (add 3 to cover the command byte, num of denoms and real/test byte)
            dataLength += 3;
            commandH.CommandDataLength = dataLength;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Paying out by denomination...\r\n");
            return false;
        }

        /// <summary>
        /// Disables the coin mech.
        /// </summary>
        public bool DisableCoinMech()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_COIN_MECH_GLOBAL_INHIBIT;
            commandH.CommandData[1] = 0x00; // 0x00 = Disable
            commandH.CommandDataLength = 2;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Disabled coin mech\r\n");
            IsCoinMechEnabled = false;
            return true;
        }

        /// <summary>
        /// Enables the coin mech.
        /// </summary>
        public bool EnableCoinMech()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_COIN_MECH_GLOBAL_INHIBIT;
            commandH.CommandData[1] = 0x01; // 0x01 = Enable
            commandH.CommandDataLength = 2;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Enabled coin mech\r\n");
            IsCoinMechEnabled = true;
            return true;
        }

        /// <summary>
        /// Empties all the coins to the cashbox and keeps a count of what was put in.
        /// </summary>
        public bool SmartEmpty()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_SMART_EMPTY;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("SMART empyting...\r\n");
            return true;
        }

        /// <summary>
        /// Sets the level of the coins, passing over the channel and the amount to increment by.
        /// </summary>
        /// <param name="channel">The channel of the coin.</param>
        /// <param name="amount">The amount to increment by.</param>
        public bool SetCoinLevelsByChannel(int channel, short amount)
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_LEVEL;

            // Level to increase
            var b = CHelpers.ConvertInt16ToBytes(amount);
            commandH.CommandData[1] = b[0];
            commandH.CommandData[2] = b[1];

            // Coin (channel) to set
            b = CHelpers.ConvertInt32ToBytes(GetChannelValue(channel));
            commandH.CommandData[3] = b[0];
            commandH.CommandData[4] = b[1];
            commandH.CommandData[5] = b[2];
            commandH.CommandData[6] = b[3];

            // Add country code, locate from dataset
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channel)
                {
                    commandH.CommandData[7] = (byte)d.Currency[0];
                    commandH.CommandData[8] = (byte)d.Currency[1];
                    commandH.CommandData[9] = (byte)d.Currency[2];
                    break;
                }
            }

            commandH.CommandDataLength = 10;

            if (!SendCommand() || !CheckGenericResponses())
                return false;

            // Update the level
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channel)
                {
                    d.Level += amount;
                    break;
                }
            }
            log.Info("Changed coin value " + CHelpers.FormatToCurrency(GetChannelValue(channel)).ToString() +
                    "'s level to " + amount.ToString() + "\r\n");
            return true;
        }

        /// <summary>
        /// Sets the level of the coins, passing over the coin value and the amount to increment by.
        /// </summary>
        /// <param name="coin">The coin value.</param>
        /// <param name="amount">The amount to increment by.</param>
        public bool SetCoinLevelsByCoin(int coin, short amount)
        {
            byte[] b = CHelpers.ConvertInt16ToBytes(amount);

            commandH.CommandData[0] = CCommands.SSP_CMD_SET_DENOMINATION_LEVEL;
            commandH.CommandData[1] = b[0];
            commandH.CommandData[2] = b[1];

            b = CHelpers.ConvertInt32ToBytes(coin);
            commandH.CommandData[3] = b[0];
            commandH.CommandData[4] = b[1];
            commandH.CommandData[5] = b[2];
            commandH.CommandData[6] = b[3];
            commandH.CommandData[7] = (byte)Currency[0];
            commandH.CommandData[8] = (byte)Currency[1];
            commandH.CommandData[9] = (byte)Currency[2];
            commandH.CommandDataLength = 10;

            if (!SendCommand() || !CheckGenericResponses())
                return false;

            // Update the level
            foreach (ChannelData d in UnitDataList)
            {
                if (d.Value == coin)
                {
                    d.Level += amount;
                    break;
                }
            }
            log.Info("Increased coin value " + CHelpers.FormatToCurrency(coin).ToString() + "'s level by " + amount.ToString() + "\r\n");
            return true;
        }

        /// <summary>
        /// Gets the level of a coin.
        /// </summary>
        /// <param name="coinValue">The coin to check.</param>
        /// <param name="currency">The currency of the coin.</param>
        /// <returns>The level of the coin.</returns>
        public short GetCoinLevel(int coinValue, char[] currency)
        {
            byte[] b = CHelpers.ConvertInt32ToBytes(coinValue);

            commandH.CommandData[0] = CCommands.SSP_CMD_GET_DENOMINATION_LEVEL;
            commandH.CommandData[1] = b[0];
            commandH.CommandData[2] = b[1];
            commandH.CommandData[3] = b[2];
            commandH.CommandData[4] = b[3];
            commandH.CommandData[5] = (byte)currency[0];
            commandH.CommandData[6] = (byte)currency[1];
            commandH.CommandData[7] = (byte)currency[2];
            commandH.CommandDataLength = 8;

            if (!SendCommand() || !CheckGenericResponses())
                return 0;

            var coinLevel = CHelpers.ConvertBytesToInt16(commandH.ResponseData, 1);
            return coinLevel;
        }

        /// <summary>
        /// Updates all the coins levels in the list.
        /// </summary>
        public void UpdateData()
        {
            foreach (ChannelData d in UnitDataList)
            {
                d.Level = GetCoinLevel(d.Value, d.Currency);
               d.IsRecycling = IsCoinRecycling(d.Value, d.Currency);
            }
        }

        /// <summary>
        /// Gets a value that determines if a coin is being recycled.
        /// </summary>
        /// <param name="coinValue">The coin to check.</param>
        /// <param name="currency">The currency of the coin.</param>
        /// <returns>True if the coin is being recycled; otherwise, false.</returns>
        public bool IsCoinRecycling(int coinValue, char[] currency)
        {
            byte[] b = CHelpers.ConvertInt32ToBytes(coinValue);

            // First determine if the coin is currently being recycled
            commandH.CommandData[0] = CCommands.SSP_CMD_GET_DENOMINATION_ROUTE;
            commandH.CommandData[1] = b[0];
            commandH.CommandData[2] = b[1];
            commandH.CommandData[3] = b[2];
            commandH.CommandData[4] = b[3];

            // Add currency
            commandH.CommandData[5] = (byte)currency[0];
            commandH.CommandData[6] = (byte)currency[1];
            commandH.CommandData[7] = (byte)currency[2];
            commandH.CommandDataLength = 8;

            if (!SendCommand() || !CheckGenericResponses())
                return false;

            var isRecycling = (commandH.ResponseData[1] == 0x00);
            log.Info(CHelpers.FormatToCurrency(coinValue) + " is recycling\r\n");
            return isRecycling;
        }

        /// <summary>
        /// Gets a value that determines if a channel is recycling.
        /// </summary>
        /// <param name="channelNumber">The channel to check.</param>
        /// <returns>True if the channel is recycling; otherwise, false.</returns>
        public bool IsChannelRecycling(int channelNumber)
        {
            if (channelNumber <= 0 || channelNumber > NumberOfChannels)
            {
                throw new ArgumentException("The value must be greater than zero and less than the number of channels.", nameof(channelNumber));
            }

            foreach (ChannelData d in UnitDataList)
            {
                if (d.Channel == channelNumber)
                {
                    //log.Info("Channel " + channelNumber + " recycling status: " + d.Recycling.ToString() + "\r\n");
                    return d.IsRecycling;
                }
            }

            throw new SspException("Could not find the channel.");
        }

        /// <summary>
        /// Sets the protocol version in the validator to the version passed across.
        /// </summary>
        /// <param name="protocolVersion">The protocol version to set.</param>
        public bool SetProtocolVersion(byte protocolVersion)
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_HOST_PROTOCOL_VERSION;
            commandH.CommandData[1] = protocolVersion;
            commandH.CommandDataLength = 2;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Setting protocol version " + protocolVersion.ToString() + "\r\n");
            return true;
        }

        /// <summary>
        /// Setups the encryption between the host and the validator.
        /// </summary>
        public bool NegotiateKeys()
        {
            // Make sure encryption is off
            commandH.EncryptionStatus = false;
            // Send sync
            log.Info("Syncing... ");
            commandH.CommandData[0] = CCommands.SSP_CMD_SYNC;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success");

            LibraryHandler.InitiateKeys(ref keys, ref commandH);

            // Send generator
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_GENERATOR;
            commandH.CommandDataLength = 9;
            log.Info("Setting generator... ");
            // Convert generator to bytes and add to command data
            BitConverter.GetBytes(keys.Generator).CopyTo(commandH.CommandData, 1);
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success\r\n");

            // Send modulus
            commandH.CommandData[0] = CCommands.SSP_CMD_SET_MODULUS;
            commandH.CommandDataLength = 9;
            log.Info("Sending modulus... ");
            // Convert modulus to bytes and add to command data
            BitConverter.GetBytes(keys.Modulus).CopyTo(commandH.CommandData, 1);
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success\r\n");
            // Send key exchange
            commandH.CommandData[0] = CCommands.SSP_CMD_REQUEST_KEY_EXCHANGE;
            commandH.CommandDataLength = 9;
            log.Info("Exchanging keys... ");
            // Convert host intermediate key to bytes and add to command data
            BitConverter.GetBytes(keys.HostInter).CopyTo(commandH.CommandData, 1);
            if (!SendCommand() || !CheckGenericResponses())
                return false;
            log.Info("Success\r\n");
            // Read slave intermediate key
            keys.SlaveInterKey = BitConverter.ToUInt64(commandH.ResponseData, 1);

            LibraryHandler.CreateFullKey(ref keys);

            // Get full encryption key
            commandH.Key.FixedKey = 0x0123456701234567;
            commandH.Key.VariableKey = keys.KeyHost;
            log.Info("Keys successfully negotiated\r\n");
            
            return true;
        }

        /// <summary>
        /// Request all the information about the validator.
        /// </summary>
        public bool RequestValidatorInformation()
        {
            // Send setup request
            commandH.CommandData[0] = CCommands.SSP_CMD_SETUP_REQUEST;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;

            var index = 1;

            var firmware = "";

            UnitType = (char)commandH.ResponseData[index++];

            while (index <= 5)
            {
                firmware += (char)commandH.ResponseData[index++];
                if (index == 4)
                {
                    firmware += ".";
                }
            }

            Firmware = firmware;

            // Country code. Legacy code so skip it.
            index += 3;

            // Protocol version
            ProtocolVersion = commandH.ResponseData[index++];

            // number of coin values
            NumberOfChannels = commandH.ResponseData[index++];

            // Channel denominations
            UnitDataList.Clear();

            for (byte i = 0; i < NumberOfChannels; i++)
            {
                ChannelData loopChannelData = new ChannelData();

                loopChannelData.Channel = (byte)(i + 1);
                loopChannelData.Value = BitConverter.ToInt16(commandH.ResponseData, index + (i * 2));
                loopChannelData.Currency[0] = (char)commandH.ResponseData[index + (2 * (NumberOfChannels) + (i * 3))];
                loopChannelData.Currency[1] = (char)commandH.ResponseData[(index + 1) + (2 * (NumberOfChannels) + (i * 3))];
                loopChannelData.Currency[2] = (char)commandH.ResponseData[(index + 2) + (2 * (NumberOfChannels) + (i * 3))];
                loopChannelData.Level = GetCoinLevel(loopChannelData.Value, loopChannelData.Currency);
                loopChannelData.IsRecycling = IsCoinRecycling(loopChannelData.Value, loopChannelData.Currency);

                UnitDataList.Add(loopChannelData);
            }

            // Sort the list by Value
            UnitDataList.Sort((d1, d2) => d1.Value.CompareTo(d2.Value));
            return true;
        }

        /// <summary>
        /// Sets which coins are accepted on the hopper.
        /// The response to this command if there is no coin mech attached will be WRONG PARAMETERS.
        /// </summary>
        public bool SetInhibits()
        {
            // Set inhibits on each coin
            for (int i = 1; i <= NumberOfChannels; i++)
            {
                commandH.CommandData[0] = CCommands.SSP_CMD_SET_COIN_MECH_INHIBITS;
                commandH.CommandData[1] = 0x01; // Coin accepted

                // Convert values to byte array and set command data
                byte[] b = BitConverter.GetBytes(GetChannelValue(i));
                commandH.CommandData[2] = b[0];
                commandH.CommandData[3] = b[1];

                // Currency
                foreach (ChannelData d in UnitDataList)
                {
                    if (d.Channel == i)
                    {
                        commandH.CommandData[4] = (byte)d.Currency[0];
                        commandH.CommandData[5] = (byte)d.Currency[1];
                        commandH.CommandData[6] = (byte)d.Currency[2];
                        break;
                    }
                }

                commandH.CommandDataLength = 7;

                try
                {
                    if (!SendCommand() || !CheckGenericResponses())
                        return false;
                }
                catch (SspException ex)
                {
                    if (ex.ErrorCode == CCommands.SSP_RESPONSE_WRONG_NO_PARAMETERS)
                    {
                        log.DebugFormat("The channel {0} has no coin mech attached", i);
                        continue;
                    }

                    log.Error(ex);
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the serial number of the hopper.
        /// </summary>
        /// <returns></returns>
        public uint GetSerialNumber()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_GET_SERIAL_NUMBER;
            commandH.CommandDataLength = 1;

            if (!SendCommand() || !CheckGenericResponses())
                return 0;

            // Response data is big endian, so reverse bytes 1 to 4
            Array.Reverse(commandH.ResponseData, 1, 4);
            return BitConverter.ToUInt32(commandH.ResponseData, 1);
        }

        public string GetAllLevels()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_GET_ALL_LEVELS;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return "false";

            var n = commandH.ResponseData[1];
            var displayString = "";
            var i = 0;
            for (i = 2; i < (9 * n); i += 9)
            {
                displayString += ";" + CHelpers.FormatToCurrency(CHelpers.ConvertBytesToInt32(commandH.ResponseData, i + 2))
                + "," + CHelpers.ConvertBytesToInt16(commandH.ResponseData, i);
            }
            return displayString;
        }
        public string GetChannelLevelInfo()
        {
            string s = "";
            foreach (ChannelData d in UnitDataList)
            {
                s +=";"+(d.Value).ToString()+","+d.Level;
            }
            return s;
        }

        /// <summary>
        /// Polls the validator for information.
        /// </summary>
        public bool DoPolling()
        {
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_monedaAtascada;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.men_busquedaFallida;
            ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_intentoFraudeM;

            // Send poll
            commandH.CommandData[0] = CCommands.SSP_CMD_POLL;
            commandH.CommandDataLength = 1;
            if (!SendCommand() || !CheckGenericResponses())
                return false;

            if (commandH.ResponseData[0] == 0xFA)
                return false;

            // Isolate poll response
            var response = new byte[255];
            var responseLength = commandH.ResponseDataLength;
            var coin = 0;
            var currency = "";

            commandH.ResponseData.CopyTo(response, 0);

           

            for (var i = 1; i < responseLength; i++)
            {
                switch (response[i])
                {
                    // This response indicates that the unit was reset and this is the first time a poll
                    // has been called since the reset.
                    case CCommands.SSP_POLL_SLAVE_RESET:
                        log.Debug("SmartHopper reset");
                        UpdateData();
                        break;
                    // This response is given when the unit is disabled.
                    case CCommands.SSP_POLL_DISABLED:
                        //log.Debug("SmartHopper disabled");
                        break;
                    // The unit is in the process of paying out a coin or series of coins, this will continue to poll
                    // until the coins have been fully dispensed
                    case CCommands.SSP_POLL_DISPENSING:
                        // Now the index needs to be moved on to skip over the data provided by this response so it
                        // is not parsed as a normal poll response.
                        // In this response, the data includes the number of countries being dispensed (1 byte), then a 4 byte value
                        // and 3 byte currency code for each country.
                        log.Debug("Dispensing coins");                      
                        for (int j = 0; j < response[i + 1] * 7; j += 7)
                        {
                            coin = CHelpers.ConvertBytesToInt32(commandH.ResponseData, i + j + 2); // get coin data from response
                            // get currency from response
                            currency = "";
                            currency += (char)response[i + j + 6];
                            currency += (char)response[i + j + 7];
                            currency += (char)response[i + j + 8];
                            log.Info(CHelpers.FormatToCurrency(coin) + " " + currency + " coin(s) dispensed\r\n");
                        }
                        dispensingCoins = coin;
                        UpdateData();
                        EnableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // This is polled when a unit has finished a dispense operation. The following 4 bytes give the 
                    // value of the coin(s) dispensed.
                    case CCommands.SSP_POLL_DISPENSED:
                        log.Debug("Dispense operation finished");
                        UpdateData();

                        var dispensedCoins = new List<ChannelData>();
                        for (var j = 0; j < UnitDataList.Count; j++)
                        {
                            if (UnitDataList[j].Level < InitialUnitDataList[j].Level)
                            {
                                var channelData = new ChannelData();
                                channelData.Value = UnitDataList[j].Value;
                                channelData.Level = InitialUnitDataList[j].Level - UnitDataList[j].Level;
                                channelData.Currency = Currency.ToArray();
                                dispensedCoins.Add(channelData);
                            }
                        }

                        DispenseOperationFinished?.Invoke(this, new DispenseOperationFinishedEventArgs(dispensedCoins));
                        EnableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The mechanism inside the unit is jammed.
                    case CCommands.SSP_POLL_JAMMED:
                        log.Debug("Jammed");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_monedaAtascada;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // A dispense, SMART empty or float operation has been halted.
                    case CCommands.SSP_POLL_HALTED:
                        log.Debug("Halted");
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The device is 'floating' a specified amount of coins. It will transfer some to the cashbox and
                    // leave the specified amount in the device. This can be parsed in the same way as the 
                    case CCommands.SSP_POLL_FLOATING:
                        log.Debug("Floating");
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The float operation has completed.
                    case CCommands.SSP_POLL_FLOATED:
                        log.Debug("Float operation completed");
                        UpdateData();
                        EnableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // This poll appears when the SMART Hopper has been searching for a coin but cannot find it within
                    // the timeout period.
                    case CCommands.SSP_POLL_TIME_OUT:
                        log.Debug("Search for suitable coins failed");
                        var valueDispensed = CHelpers.ConvertBytesToInt32(response, i + 11);
                        DispenseOperationTimedOut?.Invoke(this, new DispenseOperationTimedOutEventArgs(valueDispensed));
                        ComandosSalud.b_estadoSalud |= ComandosSalud.men_busquedaFallida;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // A payout was interrupted in some way. The amount paid out does not match what was requested. The value
                    // of the dispensed and requested amount is contained in the response.
                    case CCommands.SSP_POLL_INCOMPLETE_PAYOUT:
                        log.Debug("Incomplete payout detected");

                        /*for (var j = 0; j < response[i + 1] * 11; j += 11)
                        {
                            var dispensedValue = CHelpers.ConvertBytesToInt32(command.ResponseData, i + j + 2);
                            var requestedValue = CHelpers.ConvertBytesToInt32(command.ResponseData, i + j + 6);

                            currency = "";
                            currency += (char)response[i + j + 10];
                            currency += (char)response[i + j + 11];
                            currency += (char)response[i + j + 12];

                            IncompletePayoutDetected?.Invoke(this, new IncompletePayoutDetectedEventArgs(dispensedValue, requestedValue));
                        }*/

                        i += (byte)((response[i + 1] * 11) + 1);
                        break;
                    // A float was interrupted in some way. The amount floated does not match what was requested. The value
                    // of the dispensed and requested amount is contained in the response.
                    case CCommands.SSP_POLL_INCOMPLETE_FLOAT:
                        log.Debug("Incomplete float detected");
                        i += (byte)((response[i + 1] * 11) + 1);
                        break;
                    // This poll appears when coins have been dropped to the cashbox whilst making a payout. The value of
                    // coins and the currency is reported in the response.
                    case CCommands.SSP_POLL_CASHBOX_PAID:
                        log.Debug("Cashbox paid");
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // A credit event has been detected, this is when the coin mech has accepted a coin as legal currency.
                    case CCommands.SSP_POLL_COIN_CREDIT:
                        coin = CHelpers.ConvertBytesToInt16(commandH.ResponseData, i + 1);

                        currency = "";
                        currency += (char)response[i + 5];
                        currency += (char)response[i + 6];
                        currency += (char)response[i + 7];

                        log.DebugFormat("{0} {1} coin credited", coin, currency);
                        UpdateData();
                        i += 7;
                        break;
                    // The coin mech has become jammed.
                    case CCommands.SSP_POLL_COIN_MECH_JAMMED:
                        log.Debug("Coin mech jammed");
                        break;
                    // The return button on the coin mech has been pressed.
                    case CCommands.SSP_POLL_COIN_MECH_RETURN_PRESSED:
                        log.Debug("Return button pressed");
                        break;
                    // The unit is in the process of dumping all the coins stored inside it into the cashbox.
                    case CCommands.SSP_POLL_EMPTYING:
                        log.Debug("Emptying");
                        break;
                    // The unit has finished dumping coins to the cashbox.
                    case CCommands.SSP_POLL_EMPTIED:
                        log.Debug("Emptied");
                        NotePollEmptied?.Invoke(this, EventArgs.Empty);
                        UpdateData();
                        EnableValidator();
                        break;
                    // A fraud attempt has been detected.
                    case CCommands.SSP_POLL_FRAUD_ATTEMPT:
                        log.Debug("Fraud attempted");
                        ComandosSalud.b_estadoSalud |= ComandosSalud.exc_intentoFraudeM;
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The unit is in the process of dumping all the coins stored inside it into the cashbox.
                    // This poll means that the unit is keeping track of what it empties.
                    case CCommands.SSP_POLL_SMART_EMPTYING:
                        log.Debug("SMART emptying");
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    // The unit has finished SMART emptying. The info on what has been dumped can be obtained
                    // by sending the CASHBOX PAYOUT OPERATION DATA command.
                    case CCommands.SSP_POLL_SMART_EMPTIED:
                        log.Debug("SMART emptied");
                        //GetCashboxPayoutOpData(log);
                        UpdateData();
                        EnableValidator();
                        i += (byte)((response[i + 1] * 7) + 1);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unsupported poll response received: {0:X2}", commandH.ResponseData[i]));
                }
            }
            return true;
        }
        /// <summary>
        /// Opens a new connection to the device.
        /// </summary>
        public void OpenPort()
        {
            LibraryHandler.OpenPort(ref commandH);
        }

        /// <summary>
        /// Sends a command via SSP to the validator.
        /// </summary>
        private bool SendCommand()
        {
            var periferico = "Hopper";
            if (!LibraryHandler.SendCommand(ref commandH, ref info, periferico))
            {
                m_Comms.UpdateLog(info, true);
                return false;
            }
            m_Comms.UpdateLog(info);
            return true;
        }
      
        public void GetHopperOptions()
        {
            commandH.CommandData[0] = CCommands.SSP_CMD_GET_HOPPER_OPTIONS;
            commandH.CommandDataLength = 1;

            // Options carried as single bits, so decode.
            log.Info("Hopper Options\r\n");
            if ((commandH.ResponseData[1] & 0x01) == 0x01)
            {
                log.Info("Pay Mode: Free Pay\r\n");
            }
            else
            {
                log.Info("Pay Mode: Highest value\r\n");
            }
            if ((commandH.ResponseData[1] & 0x02) == 0x02)
            {
                log.Info("Level Check: Enabled\r\n");
            }
            else
            {
                log.Info("Level Check: Disabled\r\n");
            }
            if ((commandH.ResponseData[1] & 0x04) == 0x04)
            {
                log.Info("Motor Speed: High\r\n");
            }
            else
            {
                log.Info("Motor Speed: Low\r\n");
            }
            if ((commandH.ResponseData[1] & 0x08) == 0x08)
            {
                log.Info("Cashbox Pay: Active\r\n");
            }
            else
            {
                log.Info("Cashbox Pay: Inactive\r\n");
            }
        }

        public bool FloatByDenominationCLP()
        {
            
            var denomination = GetAllLevels();

            string[] denomination1 = denomination.Split(';');

            string[] n10Pesos  = denomination1[1].Split(',');
            string[] n50Pesos  = denomination1[2].Split(',');
            string[] n100Pesos = denomination1[3].Split(',');
            string[] n500Pesos = denomination1[4].Split(',');
            var flouting = 0;

            if (Int32.Parse(n10Pesos[2])  > Settings.Default.diezPesosCLP) flouting += 1;
            if (Int32.Parse(n50Pesos[2])  > Settings.Default.cincuentaPesosCLP) flouting += 1;
            if (Int32.Parse(n100Pesos[2]) > Settings.Default.cienPesosCLP) flouting += 1;
            if (Int32.Parse(n500Pesos[2]) > Settings.Default.quinientosPesosCLP) flouting += 1;
            if (flouting == 0) return false;

            EnableValidator();
            int numDiezPesos = Settings.Default.diezPesosCLP;
            int diezPesos = 10;

            if ((Int32.Parse(n10Pesos[2])) < numDiezPesos) numDiezPesos = (Int32.Parse(n10Pesos[2]));

            commandH.CommandData[0] = CCommands.SSP_CMD_FLOAT_BY_DENOMINATION;
            commandH.CommandData[1] = 4;

            // Min payout
            byte[] b = CHelpers.ConvertInt32ToBytes(numDiezPesos); 
            commandH.CommandData[2] = b[0];
            commandH.CommandData[3] = b[1];

            // Amount to payout
            b = CHelpers.ConvertInt32ToBytes(diezPesos);
            commandH.CommandData[4] = b[0];
            commandH.CommandData[5] = b[1];
            commandH.CommandData[6] = b[2];
            commandH.CommandData[7] = b[3];

            // Country code
            commandH.CommandData[8] = (byte)Currency[0];
            commandH.CommandData[9] = (byte)Currency[1];
            commandH.CommandData[10] = (byte)Currency[2];

            int numCincuentaPesos = Settings.Default.cincuentaPesosCLP;
            int cincuentaPesos = 50;
            if ((Int32.Parse(n50Pesos[2])) < numCincuentaPesos) numCincuentaPesos = (Int32.Parse(n50Pesos[2]));

            byte[] bCincuentaPesos = CHelpers.ConvertInt32ToBytes(numCincuentaPesos);
            commandH.CommandData[11] = bCincuentaPesos[0];
            commandH.CommandData[12] = bCincuentaPesos[1];

            bCincuentaPesos = CHelpers.ConvertInt32ToBytes(cincuentaPesos);
            commandH.CommandData[13] = bCincuentaPesos[0];
            commandH.CommandData[14] = bCincuentaPesos[1];
            commandH.CommandData[15] = bCincuentaPesos[2];
            commandH.CommandData[16] = bCincuentaPesos[3];

            commandH.CommandData[17] = (byte)Currency[0];
            commandH.CommandData[18] = (byte)Currency[1];
            commandH.CommandData[19] = (byte)Currency[2];

            int numCienPesos = Settings.Default.cienPesosCLP;
            int cienPesos = 100;
            if ((Int32.Parse(n100Pesos[2])) < numCienPesos) numCienPesos = (Int32.Parse(n100Pesos[2]));

            byte[] bCienPesos = CHelpers.ConvertInt32ToBytes(numCienPesos);
            commandH.CommandData[20] = bCienPesos[0];
            commandH.CommandData[21] = bCienPesos[1];

            bCienPesos = CHelpers.ConvertInt32ToBytes(cienPesos);
            commandH.CommandData[22] = bCienPesos[0];
            commandH.CommandData[23] = bCienPesos[1];
            commandH.CommandData[24] = bCienPesos[2];
            commandH.CommandData[25] = bCienPesos[3];

            commandH.CommandData[26] = (byte)Currency[0];
            commandH.CommandData[27] = (byte)Currency[1];
            commandH.CommandData[28] = (byte)Currency[2];

            int numQuinientosPesos = Settings.Default.quinientosPesosCLP;
            int quinientosPesos = 500;
            if ((Int32.Parse(n500Pesos[2])) < numQuinientosPesos) numQuinientosPesos = (Int32.Parse(n500Pesos[2]));

            byte[] bQuinientosPesos = CHelpers.ConvertInt32ToBytes(numQuinientosPesos);
            commandH.CommandData[29] = bQuinientosPesos[0];
            commandH.CommandData[30] = bQuinientosPesos[1];

            bQuinientosPesos = CHelpers.ConvertInt32ToBytes(quinientosPesos);
            commandH.CommandData[31] = bQuinientosPesos[0];
            commandH.CommandData[32] = bQuinientosPesos[1];
            commandH.CommandData[33] = bQuinientosPesos[2];
            commandH.CommandData[34] = bQuinientosPesos[3];

            commandH.CommandData[35] = (byte)Currency[0];
            commandH.CommandData[36] = (byte)Currency[1];
            commandH.CommandData[37] = (byte)Currency[2];

            commandH.CommandData[38] = 0x58; // real float

            commandH.CommandDataLength = 39;

            if (!SendCommand() || !CheckGenericResponses())
                return false;
            return true;
        }
        // This uses the GET COIN AMOUNT command to query the validator on a specified coin it has stored, it returns
        // the level as an int.
        public short CheckCoinLevel()
        {
            int coinValue = 100;
            commandH.CommandData[0] = CCommands.SSP_CMD_GET_DENOMINATION_LEVEL;
            byte[] b = CHelpers.ConvertInt32ToBytes(coinValue);
            commandH.CommandData[1] = b[0];
            commandH.CommandData[2] = b[1];
            commandH.CommandData[3] = b[2];
            commandH.CommandData[4] = b[3];
            commandH.CommandData[5] = (byte)Currency[0];
            commandH.CommandData[6] = (byte)Currency[1];
            commandH.CommandData[7] = (byte)Currency[2];
            commandH.CommandDataLength = 8;

            if (!SendCommand() || !CheckGenericResponses())
                return 0;
            short ret 
                = CHelpers.ConvertBytesToInt16(commandH.ResponseData, 1);
            return ret;
        }
        private bool CheckGenericResponses()
        {
            if (commandH.ResponseData[0] == CCommands.SSP_RESPONSE_OK)
                return true;
            else
            {
                switch (commandH.ResponseData[0])
                {
                    case CCommands.SSP_RESPONSE_COMMAND_CANNOT_BE_PROCESSED:
                        if (commandH.ResponseData[1] == 0x03)
                        {
                            log.Info("(Hopper) Unit responded with a \"Busy\" response, command cannot be " +
                                "processed at this time\r\n");
                        }
                        else
                        {
                            log.Info("(Hopper)Command response is CANNOT PROCESS COMMAND, error code - 0x"
                            + BitConverter.ToString(commandH.ResponseData, 1, 1) + "\r\n");
                        }
                        return false;
                    case CCommands.SSP_RESPONSE_FAIL:
                        log.Info("(Hopper) Command response is FAIL\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_KEY_NOT_SET:
                        log.Info("(Hopper) Command response is KEY NOT SET, renegotiate keys\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_PARAMETER_OUT_OF_RANGE:
                        log.Info("(Hopper) Command response is PARAM OUT OF RANGE\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_SOFTWARE_ERROR:

                        log.Info("(Hopper) Command response is SOFTWARE ERROR\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_COMMAND_NOT_KNOWN:
                        log.Info("(Hopper) Command response is UNKNOWN\r\n");
                        return false;
                    case CCommands.SSP_RESPONSE_WRONG_NO_PARAMETERS:
                        log.Info("(Hopper) Command response is WRONG PARAMETERS\r\n");
                        return false;
                    default:
                        return false;
                }
            }
        }
    }
}
