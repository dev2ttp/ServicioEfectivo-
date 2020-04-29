using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TotalPack.Efectivo.TPpagoL2.Controllers;
using TPpagoL2.MessageProtocol;

namespace TotalPack.Efectivo.TPpagoL2
{
    class MessageMiddleware
    {
        private Dictionary<Command, Action> callbacks;
        private MoneyDevicesController moneyDevicesController;

        public MessageMiddleware()
        {
            callbacks = new Dictionary<Command, Action>();

            callbacks.Add(Command.InicioCoin, moneyDevicesController.IniciarPago);
        }

        public void Invoke(Command command)
        {
            if (!callbacks.ContainsKey(command))
            {
                throw new Exception("The command is not implemented");
            }

            callbacks[command].Invoke();
        }
    }
}
