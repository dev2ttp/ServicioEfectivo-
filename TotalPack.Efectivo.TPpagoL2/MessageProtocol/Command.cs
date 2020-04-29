using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.MessageProtocol
{
    enum Command
    {
        InicioCoin = 300,
        FinCoin = 301,
        GetDineroIngresado = 302,
        DarVuelto = 303,
        insertarMoneda = 395,
        AbrirPuertaMaquinas = 304,
        RegresarDinero = 305,
        GetEstadoVuelto = 306,
        CargarDineroGaveta = 307,
        RetiroDineroGaveta = 308,
        Discrepancia = 309,
        DiscrepanciaB = 399,
        DiscrepanciaM = 398,
        AutoDiscrepanciaB = 396,
        AutoDiscrepanciaM = 397,
        ResumenSaldo = 310,
        GetGavetas = 311,
        SetDispositivo = 312,
        RetiroDispositivo = 313,
        SetGaveta = 314,
        RetiroGavetaB = 315,
        GetDenominaciones = 316,
        CheckSerieDispositivoB = 317,
        SetEvento = 318,
        CheckSerieDispositivoM = 319,
        RetiroGavetaM = 320,
        vacioGavetaM = 321,
        vacioGavetaB = 322,
        estadoVaciadoB = 323,
        estadoVaciadoM = 324,
        GetSaldosMaquinas = 325,
        GetEstadoPuerta = 326,
        GetStatusUps = 127,
        DesactivarAceptadorMonedas = 400,
        GetStatusDiscrepancia = 401,
        GetEstadoServicio = 500,
        GetEstadoStorage = 504,
        Error = 999,        
        GetEstadoDinero = 333,
        
        DetenerPago = 989,
        FloatBilletes = 991,
        EstadoSalud = 992,
        FloatMonedas = 993,

        APP_FRTFIN = 101,       // Comando enviado para Finalizar elversionamiento de una APP
        APP_FRTINI = 100,       // Comando enviado para iniciar elversionamiento de una APP

        InicioPayout = 600,
        InicioHopper = 601,
        FinPayout = 602,
        FinHopper = 603,
        GetAllLevelsNota = 604,
        GetAllLevelsCoin = 605,
        

    }
    enum CommandEventos
    {
        DifSerialDispM = 500,
        DifSerialDispB = 501,
        GavetaBVaciada = 502,
        GavetaMVaciada = 503,
        GetEstadoDB = 505,
        GetEstadoMaquina = 506,

        SspCmdReset = 0X01,                 // SSP_CMD_RESET
        SspCmdChannelInhibits = 0X02,       // SSP_CMD_SET_CHANNEL_INHIBITS
        SspCmdDisplayOn = 0X03,             // SSP_CMD_DISPLAY_ON
        SspCmdDisplayOff = 0X04,            // SSP_CMD_DISPLAY_OFF
        SspCmdSetupRequest = 0X05,          // SSP_CMD_SETUP_REQUEST
        SspCmdHostProtocolVersion = 0X06,   // SSP_CMD_HOST_PROTOCOL_VERSION
        SspCmdPoll = 0X07,                  // SSP_CMD_POLL
        SspCmdRejectBanknote = 0X08,        // SSP_CMD_REJECT_BANKNOTE
        SspCmdDisable = 0X09,               // SSP_CMD_DISABLE
        SspCmdEnable = 0X0A,                // SSP_CMD_ENABLE
        SspCmdGetSerialNumber = 0X0C,       // SSP_CMD_GET_SERIAL_NUMBER
        SspCmdUnitData = 0X0D,              // SSP_CMD_UNIT_DATA
        SspCmdChannelValueRequest = 0X0E,   // SSP_CMD_CHANNEL_VALUE_REQUEST
        SspCmdChannelSecurityData = 0X0F,   // SSP_CMD_CHANNEL_SECURITY_DATA
        SspCmdChannelReTeachData = 0X10,    // SSP_CMD_CHANNEL_RE_TEACH_DATA
        SspCmdSync = 0X11,                  // SSP_CMD_SYNC
        SspCmdLastRejectCode = 0X17,        // SSP_CMD_LAST_REJECT_CODE
        SspCmdHold = 0X18,                  // SSP_CMD_HOLD
        SspCmdGetFirmwareVersion = 0X20,                      //SSP_CMD_GET_FIRMWARE_VERSION
        SspCmdGetDatasetVersion = 0X21,                       //SSP_CMD_GET_DATASET_VERSION
        SspCmdGetAllLevels = 0X22,                            //SSP_CMD_GET_ALL_LEVELS
        SspCmdGetBarCodeReaderConfiguration = 0X23,           //SSP_CMD_GET_BAR_CODE_READER_CONFIGURATION
        SspcmdSetBarCodeConfiguration = 0X24,                 //SSP_CMD_SET_BAR_CODE_CONFIGURATION
        SspCmdGetBarCodeInhibitStatus = 0X25,                 //SSP_CMD_GET_BAR_CODE_INHIBIT_STATUS
        SspCmdSetBarCodeInhbitStatus = 0X26,                  //SSP_CMD_SET_BAR_CODE_INHIBIT_STATUS
        SspCmdGetBarCodeData = 0X27,                          //SSP_CMD_GET_BAR_CODE_DATA
        SspCmdSetRefillMode = 0X30,                           //SSP_CMD_SET_REFILL_MODE
        SspCmdPayoutAmount = 0X33,                            //SSP_CMD_PAYOUT_AMOUNT
        SspCmdSetDenominationLevel = 0X34,                    //SSP_CMD_SET_DENOMINATION_LEVEL
        SspCmdGetDenominationLevel = 0X35,                    //SSP_CMD_GET_DENOMINATION_LEVEL
        SspCmdCommuncationPassThrough = 0X37,                 //SSP_CMD_COMMUNICATION_PASS_THROUGH
        SspCmdHaltPayout = 0X38,                              //SSP_CMD_HALT_PAYOUT
        SspCmdSetDenominationRoute = 0X3B,                    //SSP_CMD_SET_DENOMINATION_ROUTE
        SspCmdGetDenominationRoute = 0X3C,                    //SSP_CMD_GET_DENOMINATION_ROUTE
        SspCmdFloatAmount = 0X3D,                             //SSP_CMD_FLOAT_AMOUNT
        SspCmdGetMinimunPauout = 0X3E,                        //SSP_CMD_GET_MINIMUM_PAYOUT
        SspCmdEmptyAll = 0X3F,                                //SSP_CMD_EMPTY_ALL
        SspCmdSetCoinMechInhibits = 0X40,                     //SSP_CMD_SET_COIN_MECH_INHIBITS
        SspCmdGetNotePositions = 0X41,                        //SSP_CMD_GET_NOTE_POSITIONS
        SspCmdPayoutNote = 0X42,                              //SSP_CMD_PAYOUT_NOTE
        SspCmdStackNote = 0X43,                               //SSP_CMD_STACK_NOTE
        SspCmdFloatByDenomination = 0X44,                     //SSP_CMD_FLOAT_BY_DENOMINATION
        SspCmdSetValueReportingType = 0X45,                   //SSP_CMD_SET_VALUE_REPORTING_TYPE
        SspCmdPayoutByDenomination = 0X46,                    //SSP_CMD_PAYOUT_BY_DENOMINATION
        SspCmdSetCoinMechGlobalInhibit = 0X49,                //SSP_CMD_SET_COIN_MECH_GLOBAL_INHIBIT
        SspCmdSetGenerator = 0X4A,                            //SSP_CMD_SET_GENERATOR
        SspCmdSetModulus = 0X4B,                              //SSP_CMD_SET_MODULUS
        SspCmdRequestKeyExchange = 0X4C,                      //SSP_CMD_REQUEST_KEY_EXCHANGE
        SspCmdSetBaudRate = 0X4D,                             //SSP_CMD_SET_BAUD_RATE
        sspCmdGetBuildrevision = 0X4F,                        //SSP_CMD_GET_BUILD_REVISION
        SspCmdSetHopperOptions = 0X50,                        //SSP_CMD_SET_HOPPER_OPTIONS
        SspCmdGetHopperOption = 0X51,                         //SSP_CMD_GET_HOPPER_OPTIONS
        SspCmdSmartEmpty = 0X52,                              //SSP_CMD_SMART_EMPTY
        SspCmdCashBoxPayoutOperationData = 0X53,              //SSP_CMD_CASHBOX_PAYOUT_OPERATION_DATA
        SspCmdConfigureBezel = 0X54,                    //SSP_CMD_CONFIGURE_BEZEL
        SspCmdPollWithAck = 0x56,                    //SSP_CMD_POLL_WITH_ACK
        SspCmdEventAck = 0x57,                    //SSP_CMD_EVENT_ACK
        SspCmdGetCounters = 0X58,                    //SSP_CMD_GET_COUNTERS
        SspCmdResetCounters = 0X59,                    //SSP_CMD_RESET_COUNTERS
        SspCmdCoinMechOptions = 0X5A,                    //SSP_CMD_COIN_MECH_OPTIONS
        SspCmdDisablePayoutDevice = 0X5B,                    //SSP_CMD_DISABLE_PAYOUT_DEVICE
        sspCmdEnablePayoutDevice = 0X5C,                    //SSP_CMD_ENABLE_PAYOUT_DEVICE
        SspCmdSetFixedEncryptionKey = 0X60,                 //SSP_CMD_SET_FIXED_ENCRYPTION_KEY
        SspCmdResetFixedEncryptionKey = 0X61,               //SSP_CMD_RESET_FIXED_ENCRYPTION_KEY
        SspCmdRequestTebsBarcode = 0X65,                    //SSP_CMD_REQUEST_TEBS_BARCODE
        SspCmdRequestTebsLog = 0X66,                        //SSP_CMD_REQUEST_TEBS_LOG
        SspCmdTebsUnlockEnable = 0X67,                      //SSP_CMD_TEBS_UNLOCK_ENABLE
        SspCmdTebsUnlockDiable = 0X68,                      //SSP_CMD_TEBS_UNLOCK_DISABLE

        SspPollTebsCashboxOutOfService = 0X90,       //SSP_POLL_TEBS_CASHBOX_OUT_OF_SERVICE
        SspPollTebsCashboxTamper = 0X91,             //SSP_POLL_TEBS_CASHBOX_TAMPER
        sspPollTebsCashboxInService = 0X92,          //SSP_POLL_TEBS_CASHBOX_IN_SERVICE
        SspPollTebsChashboxUnlockEnabled = 0X93,     //SSP_POLL_TEBS_CASHBOX_UNLOCK_ENABLED
        SspPollJamRecovery = 0XB0,                   //SSP_POLL_JAM_RECOVERY
        SspPollErrorDuringPayout = 0XB1,             //SSP_POLL_ERROR_DURING_PAYOUT
        SspPollSmartEmptying = 0XB2,                 //SSP_POLL_SMART_EMPTYING
        SspPollSmartEmptied = 0XB3,                  //SSP_POLL_SMART_EMPTIED
        SspPollChannelDisable = 0XB4,                //SSP_POLL_CHANNEL_DISABLE
        SspPollInitialising = 0XB5,                  //SSP_POLL_INITIALISING
        SspPollCoinMechError = 0XB6,                 //SSP_POLL_COIN_MECH_ERROR
        SspPollEmptying = 0XB7,                      //SSP_POLL_EMPTYING
        SspPollEmptied = 0XC2,                   //SSP_POLL_EMPTIED
        SspPollCoinMechJammed = 0XC4,            //SSP_POLL_COIN_MECH_JAMMED
        SspPollCoinMechReturnPressed = 0XC5,     //SSP_POLL_COIN_MECH_RETURN_PRESSED
        SspPollPayoutOutofService = 0XC6,        //SSP_POLL_PAYOUT_OUT_OF_SERVICE
        SspPollNoteFloatRemoved = 0XC7,          //SSP_POLL_NOTE_FLOAT_REMOVED
        SspPPollNoteFloatAttached = 0XC8,        //SSP_POLL_NOTE_FLOAT_ATTACHED
        SspPollNoteTransferedToStacker = 0XC9,   //SSP_POLL_NOTE_TRANSFERED_TO_STACKER
        SspPollNotePaidIntoStackerAtPowerUp = 0XCA,    //SSP_POLL_NOTE_PAID_INTO_STACKER_AT_POWER_UP
        SspPollNotePaidIntoStoreAtPowerUp = 0XCB,      //SSP_POLL_NOTE_PAID_INTO_STORE_AT_POWER_UP
        SspPollNoteStacking = 0XCC,                    //SSP_POLL_NOTE_STACKING
        SspPollNoteDispenseatPowerUp = 0XCD,           //SSP_POLL_NOTE_DISPENSED_AT_POWER_UP
        SspPollNoteHeldInBezel = 0XCE,                 //SSP_POLL_NOTE_HELD_IN_BEZEL
        sspPollBarCodeTicketAcknwledge = 0XD1,         //SSP_POLL_BAR_CODE_TICKET_ACKNOWLEDGE
        SspPollDispensed = 0XD2,                       //SSP_POLL_DISPENSED
        SspPollJammed = 0XD5,                          //SSP_POLL_JAMMED
        SspPollHaled = 0xD6,                           //SSP_POLL_HALTED
        SspPollFloating = 0XD7,                        //SSP_POLL_FLOATING
        SspPollFloated = 0XD8,                         //SSP_POLL_FLOATED
        SspPollTimeOut = 0XD9,                         //SSP_POLL_TIME_OUT
        SspPollDispensing = 0XDA,                      //SSP_POLL_DISPENSING
        SppPollNoteStoredInPayout = 0XDB,              //SSP_POLL_NOTE_STORED_IN_PAYOUT
        SspPollIncompletePayout = 0XDC,                //SSP_POLL_INCOMPLETE_PAYOUT
        SspPollIncompleteFloat = 0XDD,                 //SSP_POLL_INCOMPLETE_FLOAT
        SspPollCashBoxPaid = 0XDE,                     //SSP_POLL_CASHBOX_PAID
        SspPollCoinCredit = 0XDF,                      //SSP_POLL_COIN_CREDIT
        SspPollNotePathOpen = 0XE0,                    //SSP_POLL_NOTE_PATH_OPEN
        SspPollNoteClearedFromFront = 0XE1,            //SSP_POLL_NOTE_CLEARED_FROM_FRONT
        sspPolllearedCashBox = 0XE2,                   //SSP_POLL_NOTE_CLEARED_TO_CASHBOX
        SspPollCashBoxRemoved = 0XE3,                  //SSP_POLL_CASHBOX_REMOVED
        SspPollCashBoxReplaced = 0XE4,                 //SSP_POLL_CASHBOX_REPLACED
        SspPollBarCodeTicketValidated = 0XE5,          //SSP_POLL_BAR_CODE_TICKET_VALIDATED
        SspPollfraudAttempt = 0XE6,                    //SSP_POLL_FRAUD_ATTEMPT
        SspPollStackerFull = 0XE7,                     //SSP_POLL_STACKER_FULL
        SspPollDisabled = 0XE8,                        //SSP_POLL_DISABLED
        SspPollUnsafeNoteJam = 0XE9,                   //SSP_POLL_UNSAFE_NOTE_JAM
        SspPollSafeNoteJam = 0XEA,                     //SSP_POLL_SAFE_NOTE_JAM
        SspPollNoteStacked = 0XEB,                     //SSP_POLL_NOTE_STACKED
        SspPollNoteRejected = 0XEC,                    //SSP_POLL_NOTE_REJECTED
        SspPollNoteRejecting = 0XED,                   //SSP_POLL_NOTE_REJECTING
        SspPollCreditNote = 0XEE,                      //SSP_POLL_CREDIT_NOTE
        SspPollReadNote = 0XEF,                        //SSP_POLL_READ_NOTE
        SsPollSlaveReset = 0XF1,                       //SSP_POLL_SLAVE_RESET

        SspResponseOk = 0XF0,                          //SSP_RESPONSE_OK
        SspResponseCommandNotKnown = 0XF2,             //SSP_RESPONSE_COMMAND_NOT_KNOWN
        SspResponseWrongNoParameters = 0XF3,           //SSP_RESPONSE_WRONG_NO_PARAMETERS
        SspResponseParameterOutofRange = 0XF4,         //SSP_RESPONSE_PARAMETER_OUT_OF_RANGE
        SspResponseCommandCannotBeProcessed = 0XF5,    //SSP_RESPONSE_COMMAND_CANNOT_BE_PROCESSED
        SspResponseSoftwareError = 0XF6,               //SSP_RESPONSE_SOFTWARE_ERROR
        SspResponseFail = 0XF8,                        //SSP_RESPONSE_FAIL
        SspRespnseKeyNotSet = 0XFA,                    //SSP_RESPONSE_KEY_NOT_SET
        SspResponseDeviceBusy = 0X03                   //SSP_RESPONSE_DEVICE_BUSY

    }
}
