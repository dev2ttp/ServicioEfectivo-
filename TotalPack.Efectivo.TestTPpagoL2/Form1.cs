using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TotalPack.Efectivo.TestTPpagoL2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            var message = new MessageProtocol.Message();
            var commandInt = int.Parse(txtComando.Text);

            message.Command = (MessageProtocol.Command)commandInt;
            message.Data = new List<string>() { txtRequest.Text };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", txtNombrePipe.Text, PipeDirection.InOut, PipeOptions.None))
            {
                pipeClient.Connect();
                var reader = new StreamReader(pipeClient);
                var writer = new StreamWriter(pipeClient);
                var request = message.ToString();
                var response = "";

                writer.Write(request);
                writer.Flush();

                response = reader.ReadToEnd();
                txtResponse.Text = response;
            }
        }
    }
}
