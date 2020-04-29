using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Timers;
using TotalPack.Efectivo.SSP;
using TotalPack.Efectivo.SSP.Events;
using TotalPack.Efectivo.SSP.Exceptions;
using TotalPack.Efectivo.TPpagoL2.Models; 
using TotalPack.Efectivo.TPpagoL2.Other;
using TotalPack.Efectivo.TPpagoL2.Properties;
using TotalPack.Efectivo.TPpagoL2.Services;
using TotalPack.Locker.ControlBoard;
using TotalPack.Locker.ControlBoard.Exceptions;
using TPpagoL2.Helpers;
using TPpagoL2.MessageProtocol;
using TotalPack.Efectivo.TPpagoL2;

namespace TPpagoL2
{
    public class PipeServer
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Session session = new Session();
        private static ControlBoardBase controlBoard;
        private const int maxBufferSize = 1024 * 1000;
        private const int maxNumberOfServerInstances = NamedPipeServerStream.MaxAllowedServerInstances;
        private const byte smartPayoutAddress = 0;
        private const byte smartHopperAddress = 16;
        private Timer timerPolling;
        private Timer timerPolling2;
        private Timer timerUPS;
        public static bool EstadoCorriente;
        private PipeSecurity pipeSecurity;
        private PipeAccessRule pipeAccessRule;
        public SmartPayout smartPayout;
        public SmartHopper smartHopper;
        private DBConnectionService dbConnection;
        public string est = "EST00";        
        private static List<string> datosVaciado;
        private static List<string> dataQuery;
        public string PipeName { get; }
        Power pw = new Power();
        SaludServicio sld = new SaludServicio();
        public string moneda = "";
        public int amountCPL = 0;
        public int vueltoentregado = 0;
        public PipeServer(string pipeName, string portName, int baudRate, uint timeout, byte retryLevel)
        {
            
            PipeName = pipeName;
            timerPolling = new Timer() { AutoReset = false, Interval = Settings.Default.timerPolling };
            timerPolling.Elapsed += TimerPolling_Elapsed;
            timerPolling.Interval = Settings.Default.timerPolling;

            timerPolling2 = new Timer() { AutoReset = false, Interval = Settings.Default.timerPolling2 };
            timerPolling2.Elapsed += TimerPolling_Elapsed2;
            timerPolling2.Interval = Settings.Default.timerPolling2; 

            timerUPS = new Timer() { AutoReset = false, Interval = Settings.Default.estadoCorriente };
            timerUPS.Elapsed += estadoEnergiaPuerta;
            timerUPS.Interval = Settings.Default.estadoCorriente;

            // Solo para TEST
            Estate.estadoSerieDispM = new List<string>();
            Estate.estadoSerieDispB = new List<string>();
            Estate.estadoSerieDispM.Add("0~OK");
            Estate.estadoSerieDispB.Add("0~OK");

            dbConnection = new DBConnectionService();
            smartPayout = new SmartPayout(portName, baudRate, smartPayoutAddress, timeout, retryLevel);
            smartHopper = new SmartHopper(portName, baudRate, smartHopperAddress, timeout, retryLevel);

            smartPayout.NoteAccepted += SmartPayout_NoteAccepted;
            smartPayout.NotePollEmptied += SmartPayout_EmptiedDisp;
            smartPayout.DispenseOperationFinished += SmartPayout_DispenseOperationFinished;
            smartPayout.DispenseOperationTimedOut += SmartPayout_DispenseOperationTimedOut;

            smartPayout.NoteFloat += SmartPayout_NoteFloat;

            smartHopper.DispenseOperationFinished += SmartHopper_DispenseOperationFinished;
            smartHopper.NotePollEmptied += SmartHopper_EmptiedDisp;
            smartHopper.IncompletePayoutDetected += SmartHopper_IncompletePayoutDetected;
            smartHopper.DispenseOperationTimedOut += SmartHopper_DispenseOperationTimedOut;

            pipeSecurity = new PipeSecurity();
            pipeAccessRule = new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.FullControl, AccessControlType.Allow);
            pipeSecurity.SetAccessRule(pipeAccessRule);

            try
            {
                WriteConsoleAndLog("Inicia conexión a Arduino...");
                //controlBoard = ControlBoardManager.GetControlBoard(ControlBoardType.Arduino);
                WriteConsoleAndLog("Arduino conectado");

                //ConfigurateDoor();

                WriteConsoleAndLog("Inicia conexión al aceptador de monedas...");
                //ConnectToCoinAcceptor();
                WriteConsoleAndLog("Aceptador de monedas conectado");

            }
            catch (Exception ex)
            {
                log.Error(ex);
                MyConsole.WriteLine("* ERROR: {0}", ex.Message);
            }

            try
            {
                smartPayout.OpenPort();
                WriteConsoleAndLog("Inicia conexión a dispositivos...");
                ConnectToSmartPayout();
                ConnectToSmartHopper();
                WriteConsoleAndLog("Dispositivos conectados");

                dbConnection.checkSerieDispositivoB();
                dbConnection.checkSerieDispositivoM();

                smartPayout.DisableValidator();
                smartHopper.DisableValidator();

               
                WriteConsoleAndLog("Inicia polling de dispositivos...");
                smartPayout.DoPolling();
                WriteConsoleAndLog("SmartPayout OK");
                smartHopper.DoPolling();
                WriteConsoleAndLog("SmartHopper OK");
                est = sld.obtenerEstadoSalud();
                WriteConsoleAndLog("Inicia estado salud " + ((est == "" ? "OK" : est)));
                moneda = smartPayout.Currency;
                EnrutamientoNotas();
                //smartHopper.SetCoinLevelsByCoin(50, 0);
                //smartPayout.FloatByDenomination();
                //smartHopper.GetHopperOptions();
                //smartHopper.SetFloat();
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MyConsole.WriteLine("* ERROR: {0}", ex.Message);
            }

            try
            {
                WriteConsoleAndLog("Inicia validación de gavetas...");
                ValidateGavetas();
                WriteConsoleAndLog("Gavetas correctamente configuradas");
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MyConsole.WriteLine("* ERROR: {0}", ex.Message);
            }

            timerPolling.Enabled = true;
            timerUPS.Enabled = true;
            EstadoCorriente = true;
        }

        public void StartListening()
        {
            WaitForConnection();
            log.Info("Inicio de operación");
            MyConsole.WriteLine("Inicio de operación");
        }

        private void WaitForConnection()
        {
            var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1, 1, pipeSecurity);
            log.Info("Inicio de pipeServer" + pipeServer);
            MyConsole.WriteLine("Inicio de pipeServer" + pipeServer);
            pipeServer.BeginWaitForConnection(WaitForConnectionCallback, pipeServer);
        }

        private void EnrutamientoNotas()
        {
            if (Settings.Default.milReciclable)
            {
                smartPayout.RouteChannelToRecycle(1);
            }
            else
            {
                smartPayout.RouteChannelToStorage(1);
            }
            if (Settings.Default.dosMReciclable)
            {
                smartPayout.RouteChannelToRecycle(2);
            }
            else
            {
                smartPayout.RouteChannelToStorage(2);
            }
            if (Settings.Default.cincoMReciclable)
            {
                smartPayout.RouteChannelToRecycle(3);
            }
            else
            {
                smartPayout.RouteChannelToStorage(3);
            }
            if (Settings.Default.diezMReciclable)
            {
                smartPayout.RouteChannelToRecycle(4);
            }
            else
            {
                smartPayout.RouteChannelToStorage(4);
            }
            if (Settings.Default.veinteMReciclable)
            {
                smartPayout.RouteChannelToRecycle(5);
            }
            else
            {
                smartPayout.RouteChannelToStorage(5);
            }
        }

        private void WaitForConnectionCallback(IAsyncResult iar)
        {
            try
            {
                using (var pipeServer = (NamedPipeServerStream)iar.AsyncState)
                {
                    pipeServer.EndWaitForConnection(iar);
                    WaitForConnection();

                    var dataReceived = default(string);
                    var bufferIn = new byte[maxBufferSize];
                    var bufferOut = new byte[maxBufferSize];

                    pipeServer.Read(bufferIn, 0, bufferIn.Length);
                    bufferIn = bufferIn.Where(x => x != 0x00).ToArray();
                    dataReceived = Encoding.GetEncoding("Windows-1252").GetString(bufferIn, 0, bufferIn.Length);
                    log.InfoFormat("REQ: {0}", dataReceived);
                    MyConsole.WriteLine("REQ: {0}", dataReceived);

                    if (string.IsNullOrEmpty(dataReceived))
                    {
                        log.Error("La data recibida es vacía o nula");
                        MyConsole.WriteLine("* ERROR: La data recibida es vacía o nula");
                        return;
                    }

                    var requestMessage = Message.Parse(dataReceived);
                    var responseMessage = ProcessMessage(requestMessage);
                    var responseMessageStr = responseMessage.ToString();
                    bufferOut = responseMessage.ToByteArray();

                    log.InfoFormat("RSP ({0}): {1}", (int)requestMessage.Command, responseMessageStr);
                    MyConsole.WriteLine("RSP: {0}", responseMessageStr);

                    pipeServer.Write(bufferOut, 0, bufferOut.Length);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MyConsole.WriteLine("* ERROR: {0}", ex.Message);
            }
        }

        private Message ProcessMessage(Message message)
        {
            try
            {
                var responseMessage = new Message(message.Command);
                est = sld.obtenerEstadoSalud();
                // est = ((est == "" ? "OK" : "EST"+est + "&&FINSALUD"));
                est = "EST" + est + "&&FINSALUD";
                switch (message.Command)
                {
                    case Command.InicioCoin:
                        RestartPayment();
                        log.Info("inicio de aceptador de monedas");                       
                        try
                        {
                            //controlBoard.EnableCoinAcceptor();
                            smartHopper.dispensingCoins = 0;
                            smartPayout.dispensingNotes = 0;
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                            return Message.CreateErrorMessage(FormatErrorType.GENSCS, "error: EnableCoinAcceptor");
                        }
                        RestartCoins();
                        responseMessage.AppendData("&&" + est +"OK");
                        break;                   
                    case Command.InicioPayout:
                        var resPaEnable = true;
                        resPaEnable = smartPayout.EnableValidator();
                        if (!resPaEnable)
                        {
                            log.Warn("no se pudo habilitar payout");
                        }
                        responseMessage.AppendData("&&" + est + resPaEnable);
                        break;
                    case Command.InicioHopper:
                        var resHoEnable = true;
                        resHoEnable = smartHopper.EnableValidator();
                        if (!resHoEnable)
                        {
                            log.Warn("no se pudo habilitar hopper");
                        }
                        responseMessage.AppendData("&&" + est + resHoEnable);
                        break;
                    case Command.FinCoin:
                        try
                        {
                            //controlBoard.DisableCoinAcceptor();
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                            return Message.CreateErrorMessage(FormatErrorType.GENSCS, "error:DisableCoinAcceptor");
                        }                        
                        responseMessage.AppendData("&&" + est + "OK");
                        break;
                    case Command.FinPayout:
                        var resPaDesable = true;
                        resPaDesable = smartPayout.DisableValidator();
                        if (!resPaDesable)
                        {
                            log.Warn("no se pudo deshabilitar hopper");
                        }
                        responseMessage.AppendData("&&" + est + resPaDesable);
                        break;
                    case Command.FinHopper:
                        var resHoDesable = true;
                        resHoDesable = smartHopper.DisableValidator();
                        if (!resHoDesable)
                        {
                            log.Warn("no se pudo deshabilitar hopper");
                        }
                        responseMessage.AppendData("&&" + est + resHoDesable);
                        break;                   
                    case Command.GetDineroIngresado:
                        if (moneda == "CLP")
                        {
                            var dineroIngresado = 0;
                            try
                            {
                                //var credit = controlBoard.GetCoinAcceptorCredit();
                                //var coinsChange = credit.Sum(x => x.Coin.Value * x.Quantity);
                                dineroIngresado = session.NotesPaid + 0;
                            }
                            catch (Exception ex)
                            {

                                log.Error(ex);
                                return Message.CreateErrorMessage(FormatErrorType.GENSCS, "error: Dinero ingresado");
                            }
                            responseMessage.AppendData("&&" + est + dineroIngresado);
                        }    
                        if (moneda == "USD")
                        {
                            float dineroIngresado = 0;
                            try
                            {
                                var credit = controlBoard.GetCoinAcceptorCredit();
                                float coinsChange = credit.Sum(x => x.Coin.Value * x.Quantity);
                                float coinsUSD = (coinsChange / 100);
                                dineroIngresado = session.NotesPaid + coinsUSD;
                            }
                            catch (Exception ex)
                            {
                                log.Error(ex);
                                return Message.CreateErrorMessage(FormatErrorType.GENSCS, "error: Dinero ingresado");
                            }
                            responseMessage.AppendData("&&" + est + dineroIngresado);
                        }
                        break;
                    case Command.DarVuelto:
                        bool resCalPayout = false;
                        if (Settings.Default.SimularVueltoInsuficiente)
                        {

                            return Message.CreateErrorMessage(FormatErrorType.UnableToPayAmount, "No hay dinero para dar vuelto");

                        }

                        if (!message.HasData())
                        {
                            return Message.CreateErrorMessage(FormatErrorType.GENSCS, "Debe ingresar el monto para dar vuelto");
                        }
                        if (moneda == "CLP")
                        {
                            amountCPL = 0;
                            if (!int.TryParse(message.Data[0], out amountCPL) || amountCPL <= 0)
                            {
                                return Message.CreateErrorMessage(FormatErrorType.GENSCS, "El monto debe ser un número mayor a cero");
                            }

                            try
                            {
                                if (!CalculatePayout(amountCPL))
                                {
                                    resCalPayout = false;
                                }
                                else
                                {
                                    resCalPayout = true;
                                }
                                //session.PayoutAmount = amount;

                            }
                            catch (UnableToPayAmountException ex)
                            {
                                log.Error(ex);
                            }
                            responseMessage.AppendData("&&" + est + resCalPayout);
                        }
                        if (moneda == "USD")
                        {
                            string amount = message.Data[0].ToString();
                            if (amount == "0")
                            {
                                return Message.CreateErrorMessage(FormatErrorType.GENSCS, "El monto debe ser un número mayor a cero");
                            }
                            try
                            {
                                if (!CalculatePayoutUSD(amount))
                                {
                                    resCalPayout = false;
                                }
                                else
                                {
                                    resCalPayout = true;
                                }
                                //session.PayoutAmount = amount;

                            }
                            catch (UnableToPayAmountException ex)
                            {
                                log.Error(ex);
                            }
                            responseMessage.AppendData("&&" + est + resCalPayout);
                        }
                       
                        break;
                    case Command.AbrirPuertaMaquinas:
                        try
                        {
                            controlBoard.OpenLocker(Settings.Default.DireccionPuertaPlaca);
                            //controlBoard.LockerConfiguration
                            responseMessage.AppendData("&&" + est + "OK");
                        }
                        catch (LockerNotOpenedException ex)
                        {
                            log.Error(ex);
                            responseMessage.AppendData("&&" + est + "NOK");
                        }
                        break;
                    case Command.RegresarDinero:
                        var dineroRegresado = (smartPayout.NotasDetenidas/100);
                        responseMessage.AppendData(dineroRegresado);
                        break;
                    case Command.GetEstadoVuelto:
                        // TODO: detectar cuando no se pudo dar vuelto de forma completa
                        var vueltoTotal = amountCPL;
                        log.Info("vuelto que entregar" + vueltoTotal);
                        var allCoinsDispensed = false;
                        var allNotesDispensed = false;
                        var payoutComplete = false;
                        if (moneda == "CLP")
                        {
                            var dispensingTotal = (smartPayout.dispensingNotes / 100) + smartHopper.dispensingCoins;
                            responseMessage.AppendData("&&" + est + dispensingTotal);
                        }
                        if (moneda == "USD")
                        {
                            var dispensingTotal = (smartPayout.dispensingNotes / 100) + (smartHopper.dispensingCoins / 100);
                            responseMessage.AppendData("&&" + est + dispensingTotal);
                        }
                        //Verificar si se produjo timeout al dar vuelto
                        if (session.BillPayoutTimeOut)
                        {
                            if (session.CoinsUsedInPayout)
                            {
                                if (session.CoinPayoutTimeOut)
                                {
                                    // Se produjo timeout de vuelto en billetes y monedas
                                    return Message.CreateErrorMessage(FormatErrorType.TimeoutVuelto, session.BillsDispensedBeforeTimeout + session.CoinsDispensedBeforeTimeout);
                                }
                            }
                            else
                            {
                                // Se produjo timeout de vuelto en billetes
                                return Message.CreateErrorMessage(FormatErrorType.TimeoutVuelto, session.BillsDispensedBeforeTimeout);
                            }
                        }
                        else if (session.CoinPayoutTimeOut)
                        {
                            if (!session.BillsUsedInPayout)
                            {
                                // Se produjo timeout de vuelto en monedas
                                return Message.CreateErrorMessage(FormatErrorType.TimeoutVuelto, session.CoinsDispensedBeforeTimeout);
                            }
                        }
                        // Verifica si ya terminó de dar vuelto
                        if (session.BillsUsedInPayout)
                        {
                            allNotesDispensed = session.BillDispenseFinished;
                        }
                        else
                        {
                            //No se utilizarán billetes para el vuelto, así que se asume que la operación para el SMART Payout está completa
                            allNotesDispensed = true;
                        }
                        //true
                        if (session.CoinsUsedInPayout)
                        {
                            allCoinsDispensed = session.CoinDispenseFinished;
                        }
                        else
                        {
                            // No se utilizarán monedas para el vuelto, así que se asume que la operación para el SMART Hopper está completa
                            //DM
                            allCoinsDispensed = true;
                        }

                        payoutComplete = (allNotesDispensed && allCoinsDispensed);

                        if (payoutComplete)
                        {
                            //System.Threading.Thread.Sleep(5000);
                            responseMessage.AppendData("OK");
                            //vueltoentregado = (smartPayout.dispensingNotes / 100) + smartHopper.dispensingCoins;
                            //if (vueltoentregado == vueltoTotal)
                            //{
                            //    responseMessage.AppendData("OK");
                            //}
                            //else
                            //{
                            //    var vueltoFaltante = vueltoTotal - vueltoentregado;
                            //    log.Info("vuelto faltante: " + vueltoFaltante);
                            //}
                        }
                        else
                        {                          
                            responseMessage.AppendData("NOK");
                        }
                        break;
                    case Command.SetDispositivo:

                        //if (message.Data[0] == "M")
                        //{
                        //    //smartHopper = null;
                        //    ConnectToSmartHopper();
                        //}
                        //else if(message.Data[0] == "B") {
                        //    ConnectToSmartPayout();
                        //}
                                                
                        
                        var setDispositivoResponse = dbConnection.inSetDispositivo(message.Data);
                        if (setDispositivoResponse.Contains("OK"))
                        {
                            if (message.Data[0] == "M")
                            {
                                dbConnection.checkSerieDispositivoM();
                                smartHopper.DisableValidator();
                                WriteConsoleAndLog("SmartHopper OK");
                                smartHopper.DoPolling();
                            }
                            else if (message.Data[0] == "B")
                            {
                                dbConnection.checkSerieDispositivoB();
                                smartPayout.DisableValidator();
                                WriteConsoleAndLog("SmartPayout OK");
                                smartPayout.DoPolling();
                            }
                        }
                        else {
                            if (message.Data[0] == "M")
                            {
                                dbConnection.checkSerieDispositivoM();
                            }
                            else if (message.Data[0] == "B")
                            {
                                dbConnection.checkSerieDispositivoB();
                            }
                        }
                        responseMessage.AppendData("&&" + est + setDispositivoResponse);
                        break;
                    case Command.RetiroDispositivo:
                        var RetiroDispositivoResponse = dbConnection.retiroDispositivo(message.Data);
                        responseMessage.AppendData("&&" + est + RetiroDispositivoResponse);
                        break;
                    case Command.SetGaveta:
                        var setGavetaResponse = dbConnection.inSetGaveta(message.Data);
                        responseMessage.AppendData("&&" + est + setGavetaResponse);
                        break;
                    case Command.GetStatusUps:
                        responseMessage.AppendData("&&" + est + EstadoCorriente);
                        break;
                    case Command.RetiroGavetaB:
                        var smPayoutList = smartPayout.UnitDataList;
                        var RetiroGavetaBResponse = dbConnection.retiroGavetaB(message.Data, smPayoutList);
                        responseMessage.AppendData("&&" + est + RetiroGavetaBResponse);
                        break;
                    case Command.RetiroGavetaM:
                        var smHopperList = smartHopper.UnitDataList;
                        var RetiroGavetaMResponse = dbConnection.retiroGavetaM(message.Data, smHopperList);
                        responseMessage.AppendData("&&" + est + RetiroGavetaMResponse);
                        break;
                    case Command.CargarDineroGaveta:
                        var tipo = message.Data[0];
                        if (tipo == "M")
                        {
                            var denominaciones = message.Data.Skip(2).ToList();
                            for (byte i = 0; i < denominaciones.Count; i++)
                            {
                                var aux = denominaciones[i].Split(',');
                                var coinValue = int.Parse(aux[0]);
                                var coinQuantity = short.Parse(aux[1]);
                                smartHopper.SetCoinLevelsByCoin(coinValue, coinQuantity);
                            }
                        }
                        var cargaDineroResponse = dbConnection.inCargaDinero(message.Data);
                        responseMessage.AppendData("&&" + est + cargaDineroResponse);
                        break;
                    case Command.RetiroDineroGaveta:
                        var retiroDineroResponse = dbConnection.inRetiroDinero(message.Data);
                        responseMessage.AppendData("&&" + est + retiroDineroResponse);
                        break;
                    case Command.Discrepancia:
                        var disResponse = dbConnection.inDiscrepancia(message.Data);
                        responseMessage.AppendData("&&" + est + disResponse);
                        break;
                    case Command.ResumenSaldo:
                        var resSaldoResponse = dbConnection.procResumenSaldo();
                        responseMessage.AppendData("&&" + est + resSaldoResponse);
                        break;
                    case Command.GetGavetas:
                        var resGetGavetas = dbConnection.getGavetas();
                        responseMessage.AppendData(resGetGavetas);
                        if (resGetGavetas.Length > 1500)
                        {
                            smartPayout.DisableValidator();
                            smartHopper.DisableValidator();
                            controlBoard.DisableCoinAcceptor();
                        }
                        break;
                    case Command.GetDenominaciones:
                        var resDenominaciones = dbConnection.getDenominaciones();
                        responseMessage.AppendData("&&" + est + resDenominaciones);
                        break;
                    case Command.insertarMoneda:
                        smartHopper.SetCoinLevelsByCoin(10,1);
                        List<string> dataprueba = new List<string>();
                        dataprueba.Add("1003");
                        dataprueba.Add("10~1");
                        var cargaMonedaResponse = dbConnection.inCargaDinero(dataprueba);
                        responseMessage.AppendData("&&" + est + cargaMonedaResponse);
                        break;
                    case Command.vacioGavetaB:
                        Estate.payoutEmptied = false;
                        var smPayout = smartPayout.UnitDataList;
                        datosVaciado = message.Data;
                        dataQuery = new List<string>();
                        foreach (ChannelData d in smPayout)
                        {
                            var den = d.Value / 100;
                            var q = d.Level;
                            var inData = den + "," + q;
                            dataQuery.Add(inData);
                        }
                        //Estate.payoutEmptied = true;
                        //var resVaccGavetaB = dbConnection.vaciarGavetaB(message.Data, smPayout);
                        smartPayout?.EnablePayout();
                        smartPayout.EmptyPayoutDevice();
                        responseMessage.AppendData("&&" + est + "0~OK");
                        break;
                    case Command.DiscrepanciaB:
                        Estate.payoutEmptied = false;
                        var smPayoutD = smartPayout.UnitDataList;
                        var resVaccGavetaBD = dbConnection.ConsultarGavetaDiscreB(message.Data, smPayoutD);
                        responseMessage.AppendData("&&" + est + resVaccGavetaBD);
                        break;
                    case Command.DiscrepanciaM:
                        Estate.hopperEmptied = false;
                        var smHopperM = smartHopper.UnitDataList;
                        var resVaccGavetaMM = dbConnection.ConsultarGavetaDiscreM(message.Data, smHopperM);
                        responseMessage.AppendData("&&" + est + resVaccGavetaMM);
                        break;
                    case Command.AutoDiscrepanciaM:
                        Estate.hopperEmptied = false;
                        var AutoSmHopperM = smartHopper.UnitDataList;
                        var AutoResVaccGavetaMM = dbConnection.AutoDisM(message.Data, AutoSmHopperM);
                        responseMessage.AppendData("&&" + est + AutoResVaccGavetaMM);
                        break;
                    case Command.AutoDiscrepanciaB:
                        Estate.payoutEmptied = false;
                        var AutoSmPayoutD = smartPayout.UnitDataList;
                        var AutoResVaccGavetaBD = dbConnection.AutoDisB(message.Data, AutoSmPayoutD);
                        responseMessage.AppendData("&&" + est + AutoResVaccGavetaBD);
                        break;
                    case Command.vacioGavetaM:
                        Estate.hopperEmptied = false;
                        var smHopper = smartHopper.UnitDataList;
                        // var resVaccGavetaM = dbConnection.vaciarGavetaM(message.Data, smHopper);
                        datosVaciado = message.Data;
                        dataQuery = new List<string>();
                        foreach (ChannelData d in smHopper)
                        {
                            var den = d.Value;
                            var q = d.Level;
                            var inData = den + "," + q;
                            dataQuery.Add(inData);
                        }
                        smartHopper?.EnableValidator();
                        smartHopper.EmptyDevice();
                        responseMessage.AppendData("&&" + est + "0~OK");
                        break;
                    case Command.estadoVaciadoB:
                        if (Estate.payoutEmptied)
                        {
                            dbConnection.setEvento(CommandEventos.GavetaBVaciada);
                            var respPayoutEmptied = "OK";
                            Estate.payoutEmptied = false;
                            var smPayoutVok = smartPayout.UnitDataList;
                            var resVaccGavetaB = dbConnection.vaciarGavetaB(datosVaciado, smPayoutVok, dataQuery);
                            datosVaciado = new List<string>();
                            responseMessage.AppendData("&&" + est + respPayoutEmptied);
                        }
                        else
                        {
                            var respPayoutEmptied = "NOK";
                            responseMessage.AppendData("&&" + est + respPayoutEmptied);
                        }
                        break;
                    case Command.estadoVaciadoM:
                        if (Estate.hopperEmptied)
                        {
                            dbConnection.setEvento(CommandEventos.GavetaMVaciada);
                            var respHopperEmptied = "OK";
                            Estate.hopperEmptied = false;
                            var smHopperVok = smartHopper.UnitDataList;
                            var resVaccGavetaB = dbConnection.vaciarGavetaM(datosVaciado, smHopperVok, dataQuery);
                            datosVaciado = new List<string>();
                            responseMessage.AppendData("&&" + est + respHopperEmptied);
                        }
                        else
                        {
                            var respHopperEmptied = "NOK";
                            responseMessage.AppendData("&&" + est + respHopperEmptied);
                        }
                        break;
                    case Command.GetSaldosMaquinas:
                        var levelsPayout = "B~";
                        var levelsHopper = "M~";

                        foreach (var channel in smartPayout.UnitDataList)
                        {
                            levelsPayout += string.Format("{0},{1};", channel.Value / 100, channel.Level);
                        }

                        foreach (var channel in smartHopper.UnitDataList)
                        {
                            levelsHopper += string.Format("{0},{1};", channel.Value, channel.Level);
                        }

                        // Removes the last character
                        levelsPayout = levelsPayout.Remove(levelsPayout.Length - 1);
                        levelsHopper = levelsHopper.Remove(levelsHopper.Length - 1);

                        responseMessage.AppendData("&&" + est + levelsPayout);
                        responseMessage.AppendData(levelsHopper);
                        break;
                    case Command.GetEstadoServicio:
                        responseMessage.AppendData("&&" + est + "OK");
                        break;
                    case Command.DetenerPago:
                        var resDetener = true;
                        resDetener = smartPayout.Detener();
                        if (!resDetener)
                        {
                            log.Warn("no se pudo detener vuelto");
                        }
                        responseMessage.AppendData("&&" + est + resDetener);
                        break;
                    case Command.GetEstadoPuerta:
                        var doorStatus = controlBoard.GetLockerStatus(Settings.Default.DireccionPuertaPlaca);
                        responseMessage.AppendData(doorStatus.IsOpen ? "&&" + est + "OPEN" : "&&" + est + "CLOSE");
                        if (doorStatus.IsOpen)
                        {
                            RestartPayment();
                            RestartCoins();
                            //return Message.CreateErrorMessage(FormatErrorType.STATSP, "Favor cerrar la puerta para continuar con la operacion" + doorStatus);
                            responseMessage.AppendData("Favor cerrar la puerta para continuar con la operacion : " + doorStatus);
                        }
                        else
                        {
                            responseMessage.AppendData("Puerta Cerrada :" + doorStatus);
                        }
                        break;
                    case Command.GetAllLevelsNota:
                        try
                        {
                            var getAllLevelsB = smartPayout.GetChannelLevelInfo();
                            responseMessage.AppendData(getAllLevelsB);
                        }
                        catch (Exception)
                        {
                            responseMessage.AppendData("NOK");
                        }
                        break;
                    case Command.GetAllLevelsCoin:
                        try
                        {
                            var getAllLevelsM = smartHopper.GetChannelLevelInfo();
                            responseMessage.AppendData(getAllLevelsM);
                        }
                        catch (Exception)
                        {
                            responseMessage.AppendData("NOK");
                        }
                        break;
                    case Command.FloatBilletes:
                        var resFloatPa = true;
                        resFloatPa = smartPayout.FloatByDenominationCLP();
                        {
                            log.Warn("no se pudo flotar billetes");
                        }
                        responseMessage.AppendData("&&" + est + resFloatPa);
                        break;
                    case Command.FloatMonedas:
                        var resFloatHo = true;
                        resFloatHo = smartHopper.FloatByDenominationCLP();
                        if (!resFloatHo)
                        {
                            log.Warn("no se pudo flotar monedas");
                        }
                        responseMessage.AppendData("&&" + est + resFloatHo);
                        break;
                    case Command.EstadoSalud:
                        responseMessage.AppendData("&&" + est + "ok");
                        break;
                    case Command.APP_FRTFIN:
                        responseMessage.AppendData("&&" + est + "1.2.0");
                        break;
                    case Command.APP_FRTINI:
                        responseMessage.AppendData("&&" + est + "1.2.0");
                        break;
                    default:
                        responseMessage.AppendData("Comando no implementado");
                        break;
                }
                return responseMessage;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MyConsole.WriteLine("* ERROR: {0}", ex.Message);
                return Message.CreateErrorMessage(FormatErrorType.GENSCS);
            }
        }
        private void ConnectToSmartPayout()
        {
            // Try connection to smart payout
            smartPayout.SetEncryption(false);
            smartPayout.NegotiateKeys();
            smartPayout.SetEncryption(true);

            // Find the max protocol version this validator supports
            byte maxProtocolVersion = FindMaxPayoutProtocolVersion();
            if (maxProtocolVersion >= 6)
            {
                smartPayout.SetProtocolVersion(maxProtocolVersion);
            }

            // Request info from the validator
            smartPayout.RequestValidatorInformation();
            //smartPayout.RouteChannelToStorage(5);
            
            session.PayoutChannels = smartPayout.UnitDataList;
            log.Info(session.PayoutChannels.ToJson());
            log.InfoFormat("SmartPayout firmware: {0}", smartPayout.Firmware);

            // Check the right unit is connected
            if (smartPayout.UnitType != (char)0x06)
            {
                throw new Exception("Unsupported type shown by SMART Payout, this SDK supports the SMART Payout and the SMART Hopper only");
            }

            // Sets which channels can receive notes
            smartPayout.SetInhibits();

            // Get serial number.
            var serialNumber = smartPayout.GetSerialNumber();
            Estate.numeroSeriePayout = serialNumber.ToString();
            log.InfoFormat("SmartPayout serial number: {0}", serialNumber);

            // Enable payout
            smartPayout.EnablePayout();

            // TODO: verificar como funcionar el comando HOLD
            // Number of notes to be hold in escrow
            //smartPayout.HoldNumber = 10;
        }

        private void ConnectToSmartHopper()
        {
            smartHopper.SetEncryption(false);
            smartHopper.NegotiateKeys();
            smartHopper.SetEncryption(true);

            // Find the max protocol version this validator supports
            byte maxProtocolVersion = FindMaxHopperProtocolVersion();
            if (maxProtocolVersion >= 6)
            {
                smartHopper.SetProtocolVersion(maxProtocolVersion);
            }

            // Request info from the hopper
            smartHopper.RequestValidatorInformation();
            //smartHopper.RouteChannelToStorage(5);
            session.HopperChannels = smartHopper.UnitDataList;
            log.Info(session.HopperChannels.ToJson());
            log.InfoFormat("SmartHopper firmware: {0}", smartHopper.Firmware);

            // Check the right unit is connected
            if (smartHopper.UnitType != (char)0x03)
            {
                throw new Exception("Unsupported type shown by SMART Hopper, this SDK supports the SMART Payout and the SMART Hopper only");
            }

            // Sets which channels can receive coins
            //smartHopper.SetInhibits();

            // Gets the serial number
            var serialNumber = smartHopper.GetSerialNumber();
            Estate.numeroSerieHopper = serialNumber.ToString();
            log.InfoFormat("SmartHopper serial number: {0}", serialNumber);
        }

        private void ConnectToCoinAcceptor()
        {
            controlBoard.InitializeCoinAcceptor();
            controlBoard.DisableCoinAcceptor();

            var serialNumber = controlBoard.GetCoinAcceptorSerialNumber();
            log.InfoFormat("Coin acceptor serial number: {0}", serialNumber);

            var coinValues = controlBoard.GetCoinValues();
            log.InfoFormat("Coin acceptor values: {0}", coinValues.ToJson());
        }

        private void ConfigurateDoor()
        {
            controlBoard.LockerConfiguration = new LockerConfiguration();
            controlBoard.LockerConfiguration.Doors = new List<Door>();

            var door = new Door();
            var inputPin = new Pin();
            var outputPin = new Pin();

            inputPin.Mode = PinMode.Input;
            inputPin.Number = Settings.Default.PinEntradaArduino;
            outputPin.Mode = PinMode.Output;
            outputPin.Number = Settings.Default.PinSalidaArduino;

            door.Address = Settings.Default.DireccionPuertaPlaca;
            door.InputPin = inputPin;
            door.OutputPin = outputPin;

            controlBoard.LockerConfiguration.Doors.Add(door);
        }

        private void PollDevices()
        {           
            try
            {
                smartPayout.DoPolling();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            try
            {
                smartHopper.DoPolling();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            try
            {
                //var credits = controlBoard.GetCoinAcceptorCredit();
                //var coinsPaid = credits.Sum(x => x.Coin.Value * x.Quantity);

                //if (coinsPaid > 0 && coinsPaid != session.CoinsPaid)
                //{
                //    session.CoinsPaid = coinsPaid;
                //    UpdateCoinsLevels(credits);
                //    session.CreditsPaid = credits.DeepClone();
                //}
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        
        private void TimerPolling_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerPolling2.Enabled = false;
            try
            {
                smartPayout.DoPolling();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                timerPolling2.Enabled = true;
            }
        }

        private void TimerPolling_Elapsed2(object sender, ElapsedEventArgs e)
        {
            timerPolling.Enabled = false;
            try
            {
                smartHopper.DoPolling();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                timerPolling.Enabled = true;
            }
        }

        public void Discrepancia()
        {
            var gavetaBR = "";
            var gavetaBA = "";

            var gavetaMR = "";
            var gavetaMB = "";

            DBConnectionService db = new DBConnectionService();
            var data = db.getGavetas();
            string[] msg = data.Split('|');

            if (msg.Length > 1)
            {
                for (int i = 0; i < msg.Length; i++)
                {
                    string separaRreportes = msg[i];
                    string[] fhi = separaRreportes.Split('~');
                    //ba -- bill ace 
                    //br --- bill recicle
                    //ma-- aceptar monedas
                    //mr--- reciclador de monedas
                    //mb--- alcancia de moneda
                    if (fhi[1].ToLower() == "br")
                    {
                        gavetaBR = fhi[0].ToString();
                    }
                    if (fhi[1].ToLower() == "ba")
                    {
                        gavetaBA = fhi[0];
                    }
                    if (fhi[1].ToLower() == "mr")
                    {
                        gavetaMR = fhi[0];
                    }
                    if (fhi[1].ToLower() == "mb")
                    {
                        gavetaMB = fhi[0];
                    }
                }
            }

            var listaGavetaIdB = new List<string>();
            listaGavetaIdB.Add(gavetaBA);
            listaGavetaIdB.Add(gavetaBR);

            var AutoSmPayoutD = smartPayout.UnitDataList;
            var AutoResVaccGavetaBD = dbConnection.AutoDisB(listaGavetaIdB, AutoSmPayoutD);

            var listaGavetaIdM = new List<string>();
            listaGavetaIdM.Add(gavetaMR);
            listaGavetaIdM.Add(gavetaMB);

            var AutoSmHopperM = smartHopper.UnitDataList;
            var AutoResVaccGavetaMM = dbConnection.AutoDisM(listaGavetaIdM, AutoSmHopperM);
            db.CierreZ();
        }

        private void WriteConsoleAndLog(string value)
        {
            log.Info(value);
            MyConsole.WriteLine(value);
        }

        private void SmartPayout_NoteAccepted(object sender, NoteAcceptedEventArgs e)
        { 
            var noteValue = (e.Value / smartPayout.ValueMultiplier);
            session.NotesPaid += noteValue;
            try
            {
                dbConnection.RegistrarIngresoBillete(noteValue, 1, e.PosicionBillete);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
     
        private void SmartPayout_DispenseOperationFinished(object sender, DispenseOperationFinishedEventArgs e)
        {
            session.BillDispenseFinished = true;
            session.CoinDispenseFinished = true;
            log.InfoFormat("SmartPayout dispense operation finished. Dispensed: {0}", e.ValuesDispensed.ToJson());

            try
            {
                dbConnection.RegistrarRetiroBilletes(e.ValuesDispensed);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        private void SmartPayout_NoteFloat(object sender, NoteFloatEventArgs e)
        {
            log.InfoFormat("SmartPayout Float: {0}", e.NoteFloat.ToJson());
            try
            {
                dbConnection.RegistrarRetiroBilletes(e.NoteFloat);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            try
            {
                dbConnection.RegistrarRetiroBilletesF(e.NoteFloat);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
               
        private void SmartPayout_DispenseOperationTimedOut(object sender, DispenseOperationTimedOutEventArgs e)
        {
            log.InfoFormat("SmartPayout dispense operation timed out. Value dispensed: {0}", e.ValueDispensed);
            session.BillsDispensedBeforeTimeout = e.ValueDispensed;
            session.BillPayoutTimeOut = true;
        }

        private void SmartPayout_EmptiedDisp(object sender, EventArgs e)
        {
            Estate.payoutEmptied = true;
        }

        private void SmartHopper_DispenseOperationFinished(object sender, DispenseOperationFinishedEventArgs e)
        {

            session.CoinDispenseFinished = true;
            log.InfoFormat("SmartHopper dispense operation finished. Dispensed: {0}", e.ValuesDispensed.ToJson());

            try
            {
                dbConnection.RegistrarRetiroMonedas(e.ValuesDispensed);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void SmartHopper_EmptiedDisp(object sender, EventArgs e)
        {
            Estate.hopperEmptied = true;
        }

        private void SmartHopper_IncompletePayoutDetected(object sender, IncompletePayoutDetectedEventArgs e)
        {
            log.InfoFormat("Smart Hopper incomplete payout detected. DispensedValue: {0}, RequestedValue: {1}", e.DispensedValue, e.RequestedValue);
            session.IncompleteCoinPayoutDetected = true;
            session.CoinsPaid = e.DispensedValue;
        }

        private void SmartHopper_DispenseOperationTimedOut(object sender, DispenseOperationTimedOutEventArgs e)
        {
            log.InfoFormat("SmartHopper dispense operation timed out. Value dispensed: {0}", e.ValueDispensed);
            session.CoinsDispensedBeforeTimeout = e.ValueDispensed;
            session.CoinPayoutTimeOut = true;
        }

        private bool CalculatePayout(int amount)
        {
            //float payoutAmount = amount * 100;
            float payoutAmount = amount;
            var payoutList = 0;

            // Obtain the list of sorted channels from the SMART Payout, this is sorted by channel value - lowest first
            //List<ChannelData> reverseList = new List<ChannelData>(smartPayout.UnitDataList);
            List<ChannelData> reverseList = new List<ChannelData>();
            reverseList = smartPayout.UnitDataList.DeepClone();
            // Reverse the list so the highest value is first
            reverseList.Reverse();

            foreach (ChannelData d in reverseList)
            {
                ChannelData temp = d; // Don't overwrite real values

                // Keep testing to see whether we need to payout this note or the next note
                while (true)
                {
                    // If the amount to payout is equal or greater than the value of the current note and there is some of that note available
                    if (payoutAmount >= (temp.Value / 100) && temp.Level > 0)
                    {
                        payoutList += (temp.Value / 100); // Add to the list of notes to payout from the SMART Payout
                        payoutAmount -= (temp.Value / 100); // Minus from the total payout amount
                        temp.Level--; // Take one from the level
                    }
                    else
                    {
                        break; // Don't need any more of this note
                    }
                }
            }
            // Test the proposed payout values
            if (payoutList > 0)
            {
                try
                {
                    payoutList = payoutList * 100;
                    // First test Smart Payout
                    smartPayout.Payout(payoutList, true);
                    foreach (ChannelData d in smartPayout.UnitDataList)
                    session.BillsUsedInPayout = true;
                    log.InfoFormat("Notes will be used in payout. Value: {0}", payoutList);                   
                }
                catch (UnableToPayAmountException ex)
                {
                    log.Error(ex);
                    payoutAmount += payoutList; // Attempt to pay all from Hopper
                }
                if (session.BillsUsedInPayout)
                {
                    if (!smartPayout.Payout(payoutList))
                        return false;
                }
            }
            else
            {
                log.Info("Not enough notes to pay. Trying with coins...");
            }
            // Now if there is any left over, request from Hopper
            // payoutAmount = payoutAmount * 100;
            if (payoutAmount > 0)
            {
                // Test Hopper first
                if (!smartHopper.Payout((int)payoutAmount, true))
                    return false;
                session.CoinsUsedInPayout = true;
                log.InfoFormat("Coins will be used in payout. Value: {0}", payoutAmount);
                // Hopper is ok to pay
                if (!smartHopper.Payout((int)payoutAmount))
                   return false;
            }
            return true;
        }

        private bool CalculatePayoutUSD(string amount)
        {
            string[] montoTotal = amount.Split(',');

            string billetes = montoTotal[0];
            string monedas = montoTotal[1];

            int payoutAmount = int.Parse(billetes);
            var payoutList = 0;
            // Obtain the list of sorted channels from the SMART Payout, this is sorted by channel value - lowest first
            //List<ChannelData> reverseList = new List<ChannelData>(smartPayout.UnitDataList);
            List<ChannelData> reverseList = new List<ChannelData>();
            reverseList = smartPayout.UnitDataList.DeepClone();
            // Reverse the list so the highest value is first
            reverseList.Reverse();

            foreach (ChannelData d in reverseList)
            {
                ChannelData temp = d; // Don't overwrite real values

                // Keep testing to see whether we need to payout this note or the next note
                while (true)
                {
                    // If the amount to payout is equal or greater than the value of the current note and there is some of that note available
                    if (payoutAmount >= (temp.Value / 100) && temp.Level > 0)
                    {
                        payoutList += (temp.Value / 100); // Add to the list of notes to payout from the SMART Payout
                        payoutAmount -= (temp.Value / 100); // Minus from the total payout amount
                        temp.Level--; // Take one from the level
                    }
                    else
                    {
                        break; // Don't need any more of this note
                    }
                }
            }
            if (payoutList == 0 && int.Parse(monedas) > 0)
            {
                payoutAmount = int.Parse(monedas);
            }
            // Test the proposed payout values
            if (payoutList > 0)
            {
                try
                {
                    payoutList = payoutList * 100;
                    // First test Smart Payout
                    smartPayout.Payout(payoutList, true);
                    foreach (ChannelData d in smartPayout.UnitDataList)
                    session.BillsUsedInPayout = true;
                    log.InfoFormat("Notes will be used in payout. Value: {0}", payoutList);
                }
                catch (UnableToPayAmountException ex)
                {
                    log.Error(ex);
                    payoutAmount += payoutList; // Attempt to pay all from Hopper
                }
            }
            else
            {
                log.Info("Not enough notes to pay. Trying with coins...");
            }
            // Now if there is any left over, request from Hopper
            // payoutAmount = payoutAmount * 100;
            if (int.Parse(monedas) > 0)
            {
                payoutAmount = int.Parse(monedas);
            }
            if (payoutAmount > 0)
            {
                // Test Hopper first
                if (!smartHopper.Payout(int.Parse(monedas), true))
                    return false;
                session.CoinsUsedInPayout = true;
                log.InfoFormat("Coins will be used in payout. Value: {0}", payoutAmount);

                if (session.BillsUsedInPayout)
                {
                    if (!smartPayout.Payout(payoutList))
                        return false;
                }
                //prueba
                System.Threading.Thread.Sleep(100);
                // Hopper is ok to pay
                if (!smartHopper.Payout(int.Parse(monedas)))
                {
                    System.Threading.Thread.Sleep(100);
                    if (!smartHopper.Payout(int.Parse(monedas)))
                    {
                        log.Error("error: smartHopper.Payout");
                        return false;
                    }
                }
            }
            else
            {
                // There is enough notes to payout the total
                if (!smartPayout.Payout(payoutList))
                    return false;
            }
            return true;
        }
        private void RestartPayment()
        {
            session.NotesPaid = 0;
            session.BillsDispensedBeforeTimeout = 0;
            session.CoinsDispensedBeforeTimeout = 0;
            session.PayoutAmount = 0;
            session.CoinDispenseFinished = false;
            session.BillDispenseFinished = false;
            session.CoinsUsedInPayout = false;
            session.BillsUsedInPayout = false;
            session.IncompleteCoinPayoutDetected = false;
            session.CoinPayoutTimeOut = false;
            session.BillPayoutTimeOut = false;
            session.CreditsPaid = null;
        }
        private void RestartCoins()
        {
            session.CoinsPaid = 0;
        }
        private void estadoEnergiaPuerta(object sender, ElapsedEventArgs e)
        {
            try
            {
                pw.estadoPower();
                if (!EstadoCorriente)
                {
                    ComandosSalud.b_estadoSalud |= ComandosSalud.exc_corriente;
                }
                else
                {
                    ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_corriente;
                }
                //var doorStatus = controlBoard.GetLockerStatus(Settings.Default.DireccionPuertaPlaca);
                //if (doorStatus.IsOpen)
                //{
                //    ComandosSalud.b_estadoSalud |= ComandosSalud.exc_puerta;
                //    //Discrepancia();
                //    comprobar_dispositivos();
                //}
                //else
                //{
                //    ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_puerta;
                //    calcular_Alertas();
                //    comprobar_dispositivos();
                //}
                //MyConsole.WriteLine("Estado salud; " + sld.obtenerEstadoSalud());
            }
            catch (Exception ex)
            {
                MyConsole.WriteLine("Error al leer el estado de energia o puerta");
                log.Error(ex);
            }
            finally
            {
                timerUPS.Enabled = true;
            }
        }
        // Check the coins level to update the database and the hopper
        private void UpdateCoinsLevels(List<CreditEvent> credits)
        {
            if (session.CreditsPaid == null)
            {
                for (byte i = 0; i < credits.Count; i++)
                {
                    if (credits[i].Coin.Value == 0 || credits[i].Quantity == 0)
                        continue;

                    log.DebugFormat("SetCoinLevelsByCoin -> Coin:{0}, Quantity:{1}", credits[i].Coin.Value, (short)credits[i].Quantity);
                    smartHopper.SetCoinLevelsByCoin(credits[i].Coin.Value, (short)credits[i].Quantity);

                    try
                    {
                        dbConnection.RegistrarIngresoMoneda(credits[i].Coin.Value, credits[i].Quantity);
                    }
                    catch (Exception ex)
                    {

                        log.Error(ex);
                    }
                }
            }
            else
            {
                for (byte i = 0; i < credits.Count; i++)
                {
                    if (credits[i].Quantity != session.CreditsPaid[i].Quantity)
                    {
                        if (credits[i].Coin.Value == 0 || credits[i].Quantity == 0)
                            continue;

                        var coinQuantity = (short)(credits[i].Quantity - session.CreditsPaid[i].Quantity);
                        log.DebugFormat("SetCoinLevelsByCoin -> Coin:{0}, Quantity:{1}", credits[i].Coin.Value, coinQuantity);
                        smartHopper.SetCoinLevelsByCoin(credits[i].Coin.Value, coinQuantity);

                        try
                        {
                            dbConnection.RegistrarIngresoMoneda(credits[i].Coin.Value, coinQuantity);
                        }
                        catch (Exception ex)
                        {

                            log.Error(ex);
                        }
                    }
                }
            }
        }

        private void ValidateGavetas()
        {
            dbConnection.ValidateGaveta(TipoGaveta.BillAcceptor);
            dbConnection.ValidateGaveta(TipoGaveta.BillRecycler);
            dbConnection.ValidateGaveta(TipoGaveta.MoneyBank);
            dbConnection.ValidateGaveta(TipoGaveta.MoneyRecycler);
        }
        //La función devuelve la versión número uno menos que la versión fallida.
        private byte FindMaxPayoutProtocolVersion()
        {
            // Not dealing with protocol under level 6. Attempt to set in validator.
            byte b = 0x06;
            while (true)
            {
                try
                {
                    smartPayout.SetProtocolVersion(b);
                    b++;
                }
                catch (SspException ex)
                {
                    // If it fails, then it can't be set, so fall back to previous iteration and return it
                    if (ex.ErrorCode == CCommands.SSP_RESPONSE_FAIL)
                    {
                        return --b;
                    }

                    b++;
                }

                // If the protocol version 'runs away' because of a drop in comms. Return the default value.
                if (b > 20)
                {
                    return 0x06;
                }
            }
        }
        //La función devuelve la versión número uno menos que la versión fallida.
        private byte FindMaxHopperProtocolVersion()
        {
            // Not dealing with protocol under level 6. Attempt to set in hopper.
            byte b = 0x06;

            while (true)
            {
                try
                {
                    smartHopper.SetProtocolVersion(b);
                    b++;
                }
                catch (SspException ex)
                {
                    // If it fails, then it can't be set, so fall back to previous iteration and return it
                    if (ex.ErrorCode == CCommands.SSP_RESPONSE_FAIL)
                    {
                        return --b;
                    }

                    b++;
                }

                // If the protocol version 'runs away' because of a drop in comms. Return the default value.
                if (b > 20)
                {
                    return 0x06;
                }
            }
        }

        public void calcular_Alertas()
        {
            var gavetaBR = "";
            var BRminimo = 0;
            var BRmaximo = 1;
            var BRAdvertencia = 1;
            var totalBR = 0;

            var gavetaBA = "";
            var BAminimo = 0;
            var BAmaximo = 1;
            var totalBA = 0;


            var gavetaMR = "";
            var MRminimo = 0;
            var MRmaximo = 1;
            var totalMR = 0;

            var gavetaMB = "";
            var MBminimo = 0;
            var MBmaximo = 1;
            var totalMB = 0;

            DBConnectionService db = new DBConnectionService();
            var data = db.getGavetas();

            string[] msg = data.Split('|');

            if (msg.Length > 1)
            {
                for (int i = 0; i < msg.Length; i++)
                {
                    string separaRreportes = msg[i];
                    string[] fhi = separaRreportes.Split('~');
                   // ba-- bill ace
                   // br-- - bill recicle
                   // ma--aceptar monedas
                   //mr---reciclador de monedas
                   // mb-- - alcancia de moneda
                    if (fhi[1].ToLower() == "br")
                    {
                        gavetaBR = fhi[0].ToString();

                        if (string.IsNullOrEmpty(fhi[3])) { BRAdvertencia = 1; }
                        else { BRAdvertencia = Int32.Parse(fhi[3]); }

                        if (string.IsNullOrEmpty(fhi[4])) { BRmaximo = 1; }
                        else { BRmaximo = Int32.Parse(fhi[4]); }

                        if (string.IsNullOrEmpty(fhi[2])) { BRminimo = 0; }
                        else { BRminimo = Int32.Parse(fhi[2]); }

                        string[] denominaciones = fhi[5].Split(';');
                        if (denominaciones.Length >= 1)
                        {
                            for (int k = 0; k < denominaciones.Length; k++)
                            {
                                string[] tipo_cantidades = denominaciones[k].Split(',');
                                //if (denominaciones.Length - 1 == k && msg.Length - 1 == i)
                                //{
                                //    tipo_cantidades[1] = tipo_cantidades[1].Substring(0, tipo_cantidades[1].Length - 5);
                                //}
                                switch (tipo_cantidades[0])
                                {
                                    case "1000": totalBR += int.Parse(tipo_cantidades[1]); break;
                                    case "2000": totalBR += int.Parse(tipo_cantidades[1]); ; break;
                                    case "5000": totalBR += int.Parse(tipo_cantidades[1]); ; break;
                                    case "10000": totalBR += int.Parse(tipo_cantidades[1]); ; break;
                                    case "20000": totalBR += int.Parse(tipo_cantidades[1]); ; break;
                                }
                            }
                        }
                    }
                    if (fhi[1].ToLower() == "ba")
                    {
                        gavetaBA = fhi[0];

                        if (string.IsNullOrEmpty(fhi[3])) { BAmaximo = 1; }
                        else { BAmaximo = Int32.Parse(fhi[3]); }

                        if (string.IsNullOrEmpty(fhi[2])) { BAminimo = 0; }
                        else { BAminimo = Int32.Parse(fhi[2]); }

                        string[] denominaciones = fhi[5].Split(';');
                        if (denominaciones.Length >= 1)
                        {
                            for (int k = 0; k < denominaciones.Length; k++)
                            {
                                string[] tipo_cantidades = denominaciones[k].Split(',');
                                //if (denominaciones.Length - 1 == k && msg.Length - 1 == i)
                                //{
                                //    tipo_cantidades[1] = tipo_cantidades[1].Substring(0, tipo_cantidades[1].Length - 5);
                                //}
                                switch (tipo_cantidades[0])
                                {
                                    case "1000": totalBA += int.Parse(tipo_cantidades[1]); ; break;
                                    case "2000": totalBA += int.Parse(tipo_cantidades[1]); ; break;
                                    case "5000": totalBA += int.Parse(tipo_cantidades[1]); ; break;
                                    case "10000": totalBA += int.Parse(tipo_cantidades[1]); ; break;
                                    case "20000": totalBA += int.Parse(tipo_cantidades[1]); ; break;
                                }
                            }
                        }
                    }
                    if (fhi[1].ToLower() == "mr")
                    {
                        gavetaMR = fhi[0];

                        if (string.IsNullOrEmpty(fhi[3])) { MRmaximo = 1; }
                        else { MRmaximo = Int32.Parse(fhi[3]); }

                        if (string.IsNullOrEmpty(fhi[2])) { MRminimo = 0; }
                        else { MRminimo = Int32.Parse(fhi[2]); }

                        string[] denominaciones = fhi[5].Split(';');
                        if (denominaciones.Length >= 1)
                        {
                            for (int k = 0; k < denominaciones.Length; k++)
                            {
                                string[] tipo_cantidades = denominaciones[k].Split(',');
                                //if (denominaciones.Length - 1 == k && msg.Length - 1 == i)
                                //{
                                //    tipo_cantidades[1] = tipo_cantidades[1].Substring(0, tipo_cantidades[1].Length - 5);
                                //}
                                switch (tipo_cantidades[0])
                                {
                                    case "10": totalMR += int.Parse(tipo_cantidades[1]); ; break;
                                    case "50": totalMR += int.Parse(tipo_cantidades[1]); ; break;
                                    case "100": totalMR += int.Parse(tipo_cantidades[1]); ; break;
                                    case "500": totalMR += int.Parse(tipo_cantidades[1]); ; break;
                                }
                            }
                        }
                    }
                    if (fhi[1].ToLower() == "mb")
                    {
                        gavetaMB = fhi[0];
                        if (string.IsNullOrEmpty(fhi[3])) { MBmaximo = 1; }
                        else { MBmaximo = Int32.Parse(fhi[3]); }

                        if (string.IsNullOrEmpty(fhi[2])) { MBminimo = 0; }
                        else { MBminimo = Int32.Parse(fhi[2]); }

                        string[] denominaciones = fhi[5].Split(';');
                        if (denominaciones.Length >= 1)
                        {
                            for (int k = 0; k < denominaciones.Length; k++)
                            {
                                string[] tipo_cantidades = denominaciones[k].Split(',');
                                //if (denominaciones.Length - 1 == k && msg.Length - 1 == i)
                                //{
                                //    tipo_cantidades[1] = tipo_cantidades[1].Substring(0, tipo_cantidades[1].Length - 5);
                                //}
                                switch (tipo_cantidades[0])
                                {
                                    case "10": totalMB += int.Parse(tipo_cantidades[1]); ; break;
                                    case "50": totalMB += int.Parse(tipo_cantidades[1]); ; break;
                                    case "100": totalMB += int.Parse(tipo_cantidades[1]); ; break;
                                    case "500": totalMB += int.Parse(tipo_cantidades[1]); ; break;
                                }
                            }
                        }
                    }
                }
            }
            if (totalBA <= BAminimo)
            {
                // Minimo vuelto de billete
                ComandosSalud.b_estadoSalud |= ComandosSalud.exc_minBillete;
            }
            else
            {
                ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_minBillete;
            }
            if (totalMR >= MRmaximo)
            {
                // maximo vuelto0 de monedas
                ComandosSalud.b_estadoSalud |= ComandosSalud.exc_maxMonedas;
            }
            else
            {
                ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_maxMonedas;
            }
            if (totalMR <= MRminimo)
            {
                // minimo vuelto de monedas
                ComandosSalud.b_estadoSalud |= ComandosSalud.exc_minMonedas;
            }
            else
            {
                ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_minMonedas;
            }
            if (totalBR >= BRAdvertencia)
            {
                ComandosSalud.b_estadoSalud |= ComandosSalud.exc_maxBillete;
                if (totalBR >= BRmaximo)
                {
                    // maximo alcancia billete
                    ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_maxBillete;
                }
                else
                {
                    ComandosSalud.b_bloqueoEf &= ~ComandosSalud.bloEf_maxBillete;
                }
            }
            else
            {
                ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_maxBillete;
            }
        }
        public void comprobar_dispositivos() {
           
            DBConnectionService db = new DBConnectionService();
            db.checkSerieDispositivoB();
            db.checkSerieDispositivoM();

            if (Estate.estadoSerieDispB.Contains("Dispositivo diferente"))
            {
                ComandosSalud.b_estadoSalud |= ComandosSalud.exc_disDiferente;
            }
            else
            {
                ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_disDiferente;
            }
            if (Estate.estadoSerieDispM.Contains("Dispositivo diferente"))
            {
                ComandosSalud.b_estadoSalud |= ComandosSalud.exc_disDiferente;
            }
            else
            {
                ComandosSalud.b_estadoSalud &= ~ComandosSalud.exc_disDiferente;
            }
        }
    }
}