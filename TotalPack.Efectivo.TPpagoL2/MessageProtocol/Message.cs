using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.MessageProtocol
{
    class Message
    {
        private const string header = "TP10PP";
        private const string fieldSeparator = "|";
        private const char footer = '@';

        /// <summary>
        /// Gets the full content of the message.
        /// </summary>
        public string Content { get; private set; }
        /// <summary>
        /// Gets or sets the data field of the message.
        /// </summary>
        public List<string> Data { get; set; }
        /// <summary>
        /// Gets or sets the command of the message.
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        public Message()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class with a given command.
        /// </summary>
        /// <param name="command"></param>
        public Message(Command command)
        {
            Command = command;
        }

        /// <summary>
        /// Converts the string representation of a message to its object equivalent.
        /// </summary>
        /// <param name="s">A string containing a message to convert.</param>
        /// <returns>An object equivalent to the message contained in <see cref="s"/>.</returns>
        public static Message Parse(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            if (s.Equals(string.Empty))
                throw new InvalidCastException("The input string is empty.");

            if (s.Trim().Equals(string.Empty))
                throw new InvalidCastException("The input string contains only spaces.");

            int messageLength, command;
            Message message = new Message();

            message.Content = s;

            if (s.Length < StructureLength.CMD_POS || s.Substring(0, StructureLength.HEAD_LEN) != header)
            {
                throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGHDR), FormatErrorType.MSGHDR);
            }

            if (!int.TryParse(s.Substring(StructureLength.LEN_POS, StructureLength.LEN_LEN), out messageLength))
            {
                throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGDAT), FormatErrorType.MSGDAT);
            }

            if (messageLength < StructureLength.MIN_MSG_LEN || messageLength > StructureLength.MAX_MSG_LEN)
            {
                FormatErrorType error = (messageLength < StructureLength.MIN_MSG_LEN ? FormatErrorType.MSGMIN : FormatErrorType.MSGMAX);
                throw new CommunicationException(GetErrorMessage(error), error);
            }

            if (s.Length < messageLength)
            {
                throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGLEN), FormatErrorType.MSGLEN);
            }

            if (s.Last() != footer)
            {
                throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGFMT), FormatErrorType.MSGFMT);
            }

            message.Data = s.Substring(StructureLength.DAT_POS, messageLength - StructureLength.FIX_LEN).Split(fieldSeparator[0]).ToList();

            if (!int.TryParse(s.Substring(StructureLength.CMD_POS, StructureLength.CMD_LEN), out command))
            {
                throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGCMD), FormatErrorType.MSGCMD);
            }

            if (Enum.IsDefined(typeof(Command), command) && command != (int)Command.Error)
            {
                message.Command = (Command)Enum.Parse(typeof(Command), command.ToString(), true);
                if (IsChecksumValid(s))
                    return message;
                else
                    throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGCKS), FormatErrorType.MSGCKS);
            }
            else if (command == (int)Command.Error)
            {
                int internalErrorCode = Convert.ToInt32(message.Data[0]);
                string internalErrorMessage;

                if (message.Data.Count > 1)
                {
                    internalErrorMessage = message.Data[1];
                }
                else
                {
                    internalErrorMessage = GetInternalErrorMessage(internalErrorCode);
                }

                InternalError internalError = new InternalError()
                {
                    ErrorCode = internalErrorCode,
                    Message = internalErrorMessage
                };

                throw new CommunicationException(GetErrorMessage(FormatErrorType.GENSCS), FormatErrorType.GENSCS, internalError);
            }
            else
            {
                throw new CommunicationException(GetErrorMessage(FormatErrorType.MSGCMD), FormatErrorType.MSGCMD);
            }
        }

        /// <summary>
        /// Creates a error message with a given error code.
        /// </summary>
        /// <param name="error">The error code of the message.</param>
        /// <returns>The message.</returns>
        public static Message CreateErrorMessage(FormatErrorType error)
        {
            var message = new Message();
            message.Command = Command.Error;
            message.Data = new List<string>() { ((int)error).ToString() };
            return message;
        }

        /// <summary>
        /// Creates an error message with a given error code and message.
        /// </summary>
        /// <param name="error">The error code of the message.</param>
        /// <param name="description">The description of the error.</param>
        /// <returns>The message.</returns>
        public static Message CreateErrorMessage(FormatErrorType error, string description)
        {
            var message = new Message();
            message.Command = Command.Error;
            message.Data = new List<string>() { ((int)error).ToString(), description };
            return message;
        }

        /// <summary>
        /// Creates an error message with a given error code and multiple values.
        /// </summary>
        /// <param name="error">The error code of the message.</param>
        /// <param name="values">The values that describes the error.</param>
        /// <returns>The message.</returns>
        public static Message CreateErrorMessage(FormatErrorType error, params object[] values)
        {
            var message = new Message();
            message.Command = Command.Error;
            message.Data = new List<string>() { ((int)error).ToString(), string.Join(fieldSeparator, values) };
            return message;
        }

        /// <summary>
        /// Appends a value to the data field.
        /// </summary>
        /// <param name="value">The value to append.</param>
        public void AppendData(object value)
        {
            if (Data == null)
            {
                Data = new List<string>();
            }

            Data.Add(value.ToString());
        }

        /// <summary>
        /// Converts the message to its byte array equivalent.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            var messageString = ToString();
            var bytes = Encoding.GetEncoding("Windows-1252").GetBytes(messageString);
            return bytes;
        }

        /// <summary>
        /// Converts the message to its string equivalent.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString()
        {
            var dataString = (Data == null ? "" : string.Join(fieldSeparator, Data));
            var cmd = ((int)Command).ToString().PadLeft(StructureLength.CMD_LEN, '0');
            int length = header.Length + StructureLength.LEN_LEN + cmd.Length + dataString.Length + StructureLength.FOOT_LEN;

            return
                header +
                length.ToString().PadLeft(StructureLength.LEN_LEN, '0') +
                cmd +
                dataString +
                BuildChecksum(header + length.ToString().PadLeft(StructureLength.LEN_LEN, '0') + cmd + dataString) +
                footer;
        }

        /// <summary>
        /// Returns a value that indicates if the message has data.
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            return Data != null && Data.Count > 0;
        }

        /// <summary>
        /// Calculates the checksum of a string.
        /// </summary>
        /// <param name="dataToCalculate">A string that contains the data to calculate.</param>
        /// <returns>A string with the checksum.</returns>
        private static string BuildChecksum(string dataToCalculate)
        {
            long cks = 0;
            for (int i = 1; i <= dataToCalculate.Length; i++)
            {
                cks -= Strings.Asc(Strings.Mid(dataToCalculate, i, 1));
            }
            return Strings.Right(Conversion.Hex(cks), 4).PadLeft(4, '0');
        }

        /// <summary>
        /// Returns a value that indicates whether the checksum of the message is valid.
        /// </summary>
        /// <param name="message">A string containing the message to validate the checksum.</param>
        /// <returns><c>true</c> if <see cref="message"/> has a valid checksum; otherwise, <c>false</c>.</returns>
        private static bool IsChecksumValid(string message)
        {
            return !(Strings.Mid(message, Convert.ToInt32(Strings.Mid(message, 7, 5)) - 5 + 1, 4) != BuildChecksum(Strings.Mid(message, 1, message.Length - 5)));
        }

        /// <summary>
        /// Returns an error message for a given <see cref="FormatErrorType"/> value.
        /// </summary>
        /// <param name="formatErrorType">One of the <see cref="FormatErrorType"/> values.</param>
        /// <returns>A string that contains the error message.</returns>
        private static string GetErrorMessage(FormatErrorType formatErrorType)
        {
            switch (formatErrorType)
            {
                case FormatErrorType.MSGMIN:
                    return "Largo de mensaje < permitido";
                case FormatErrorType.MSGMAX:
                    return "Largo de mensaje > permitido";
                case FormatErrorType.MSGFMT:
                    return "Error de formato en mensaje";
                case FormatErrorType.MSGLEN:
                    return "Error de tamaño de mensaje";
                case FormatErrorType.MSGCMD:
                    return "Comando no válido";
                case FormatErrorType.MSGCKS:
                    return "Checksum no válido";
                case FormatErrorType.MSGDAT:
                    return "Parámetros no válidos";
                case FormatErrorType.GENSCS:
                    return "Error general";
                case FormatErrorType.MSGHDR:
                    return "Header no válido";
                default:
                    return "Error General";
            }
        }

        /// <summary>
        /// Returns an error message for a given error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>A string that contains the error message.</returns>
        private static string GetInternalErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 14001: return "Oficina no reconocida";
                case 14002: return "Oficina sin comunicación";
                case 12001: return "Largo de mensaje menor al mínimo permitido";
                case 12002: return "Largo de mensaje mayor al máximo permitido";
                case 12003: return "Formato erróneo";
                case 12004: return "Tamaño de mensaje erróneo";
                case 12005: return "Comando asociado en mensaje no válido";
                case 12006: return "Checksum no válido";
                case 12096: return "Error de sincronización";
                case 12097: return "Transacción en progreso";
                case 12098: return "Timeout en recibir respuesta";
                case 12099: return "Error general";
                case 13001: return "Largo de mensaje menor al mínimo permitido";
                case 13002: return "Largo de mensaje mayor al máximo permitido";
                case 13003: return "Formato erróneo";
                case 13004: return "Tamaño de mensaje erróneo";
                case 13005: return "Comando asociado en mensaje no válido";
                case 13006: return "Checksum no válido";
                case 13007: return "Número de parámetros no válidos";
                case 13020: return "Otro Ejecutivo conectado en mismo Escritorio";
                case 13021: return "Mismo Ejecutivo conectado en otro Escritorio";
                case 13022: return "Error en Login";
                case 13023: return "Escritorio no definido";
                case 13025: return "Atención no válida";
                case 13026: return "Series incorrectas";
                case 13027: return "Serie incorrecta";
                case 13028: return "Turno no ha sido emitido";
                case 13029: return "Turno previamente atendido";
                case 13099: return "Error General";
                default: return "Error Desconocido";
            }
        }
    }
}
