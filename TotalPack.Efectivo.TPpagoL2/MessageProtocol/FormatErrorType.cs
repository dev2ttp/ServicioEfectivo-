﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.MessageProtocol
{
    public enum FormatErrorType
    {
        MSGMIN = 11501,   // MSG: largo < permitido
        MSGMAX = 11502,   // MSG: largo > permitido
        MSGFMT = 11503,   // MSG: formato
        MSGLEN = 11504,   // MSG: tamaño string recv
        MSGCMD = 11505,   // MSG: comando no válido
        MSGCKS = 11506,   // MSG: checksum no válido
        MSGDAT = 11507,   // MSG: número de data no válida
        MSGHDR = 11508,   // MSG: header no válido
        ERRTBK = 11541,   // MSG: error en transbank
        GENSCS = 11599,   // Error general
        STATSP = 11600,   // MSG: Estado Puerta

        UnableToPayAmount = 15000,  // Unable to pay requested amount
        IncompletePayout = 15001,    // The payout process could not be completed
        TimeoutVuelto = 15002    //time out in payout and  hopper
    }
}
