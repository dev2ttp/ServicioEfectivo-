using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.MessageProtocol
{
    public class StructureLength
    {
        public const int HEAD_LEN = 6;
        public const int LEN_LEN = 5;
        public const int CMD_LEN = 3;
        public const int CKS_LEN = 4;
        public const int FOOT_LEN = 5;                                                                // 4(Cks) + 1(@)
        public const int MAX_DAT_LEN = 8192;                                                          // Largo maximo Data
        public const int FIX_LEN = (HEAD_LEN + LEN_LEN + CMD_LEN + FOOT_LEN);
        public const int LEN_POS = (HEAD_LEN);                                                        // Posición Largo en Mensaje
        public const int CMD_POS = (HEAD_LEN + LEN_LEN);                                              // Posición Comando en Mensaje
        public const int DAT_POS = (HEAD_LEN + LEN_LEN + CMD_LEN);                                    // Posición Data en Mensaje
        public const int MIN_MSG_LEN = (HEAD_LEN + LEN_LEN + CMD_LEN + FOOT_LEN);                     // Largo Min Mensaje
        public const int MAX_MSG_LEN = (HEAD_LEN + LEN_LEN + CMD_LEN + MAX_DAT_LEN + FOOT_LEN);       // Largo Max Mensaje
    }
}
