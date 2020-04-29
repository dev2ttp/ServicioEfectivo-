using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;

namespace TotalPack.Efectivo.TPpagoL2.HostPipe
{
    class PipeClient
    {
        public struct Results
        {
            public int CodigoError;
            public List<string> Data;
        }

        public int Timeout { get; set; }
        public string Message { get; set; } //aca se guarda el mensaje ya listo para enviar al componente pipe
        public Results Resultado;

        public string _Resp;

        public PipeClient()
        {
            this.Timeout = 30 * 1000;
        }

        public void SendMessage(ServicioPago.Comandos command)
        {
            if (MessageSentSuccessfully())
            {


               
            }
            else
            {
                Resultado.CodigoError = -1;
                Resultado.Data = new List<string> { _Resp };
            }
        }

        private bool MessageSentSuccessfully()
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "totalpago", PipeDirection.InOut, PipeOptions.None))
                {
                    pipeClient.Connect(this.Timeout);
                    byte[] buffer = Encoding.ASCII.GetBytes(this.Message);
                    pipeClient.Write(buffer, 0, buffer.Length);
                    pipeClient.WaitForPipeDrain();

                    StreamReader stream = new StreamReader(pipeClient);
                    _Resp = stream.ReadToEnd();
                    //string aux = _Resp.Substring(1, _Resp.LastIndexOf("@") + 1);
                    //_Resp = aux;
                }

                return true;
            }
            catch (Exception ex)
            {
                _Resp = ex.ToString();
                return false;
            }
        }
    }
}
