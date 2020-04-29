using System;
using System.IO;
using TotalPack.Efectivo.SSP.Properties;

namespace TotalPack.Efectivo.SSP
{
    public class SaludServicio
    {
        public string obtenerEstadoSalud()
        {
            string sRsp = "";
            string path = "";
            MensajesEST();
            BloqueoEST();

            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_puerta) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_puerta);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_puerta) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_puerta) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_puerta) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_puerta) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Puerta.Abierta.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Puerta.Abierta.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_corriente) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_corriente);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_corriente) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_corriente) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_corriente) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_corriente) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Corte.Corriente.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Corte.Corriente.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_minBillete) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_minBillete);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_minBillete) > 0 ? ",1" : ",0");   
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_minBillete) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_minBillete) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_minBillete) > 0 ? ",1" : ",0");
                //alerta monitoreo
                path = @"C:\inetpub\wwwroot\tpwsremotos\Minimo.Billetes.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Minimo.Billetes.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_maxBillete) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += ComandosSalud.exc_maxBillete;

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_maxBillete) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_maxBillete) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_maxBillete) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_maxBillete) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Maximo.Billetes.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Maximo.Billetes.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_minMonedas) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_minMonedas);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_minMonedas) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_minMonedas) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_minMonedas) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_minMonedas) > 0 ? ",1" : ",0");

                path = @"C:\inetpub\wwwroot\tpwsremotos\Minimo.Monedas.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Minimo.Monedas.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_maxMonedas) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_maxMonedas);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_maxMonedas) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_maxMonedas) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_maxMonedas) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_maxMonedas) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Maximo.Monedas.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Maximo.Monedas.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_disDiferente) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_disDiferente);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_disDiferente) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_disDiferente) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_disDiferente) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_disDiferente) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Dispositivo.Diferente.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Dispositivo.Diferente.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_atascoSeguro) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_atascoSeguro);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_atascoSeguro) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_atascoSeguro) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_atascoSeguro) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_atascoSeguro) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Atasco.Seguro.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Atasco.Seguro.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_atascoInSeguro) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_atascoInSeguro);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_atascoInSeguro) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_atascoInSeguro) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_atascoInSeguro) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_atascoInSeguro) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Atasco.Inseguro.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Atasco.Inseguro.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_intentoFraudeB) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_intentoFraudeB);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_intentoFraudeB) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_intentoFraudeB) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_intentoFraudeB) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_intentoFraudeB) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Intento.Fraude.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Intento.Fraude.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_cajaFull) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_cajaFull);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_cajaFull) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_cajaFull) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_cajaFull) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_cajaFull) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Caja.Full.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Caja.Full.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_unidadAtascada) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_unidadAtascada);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_unidadAtascada) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_unidadAtascada) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_unidadAtascada) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_unidadAtascada) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Unidad.Atascada.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Unidad.Atascada.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_monedaAtascada) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_monedaAtascada);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_monedaAtascada) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_monedaAtascada) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_monedaAtascada) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_monedaAtascada) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Moneda.Atascada.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Moneda.Atascada.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_busquedaFallida) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_busquedaFallida);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_busquedaFallida) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_busquedaFallida) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_busquedaFallida) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_busquedaFallida) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Busqueda.Fallida.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Busqueda.Fallida.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_intentoFraudeM) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_intentoFraudeM);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_intentoFraudeM) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_intentoFraudeM) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_intentoFraudeM) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_intentoFraudeM) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Intento.FraudeM.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Intento.FraudeM.tpr";
                if (File.Exists(path)) File.Delete(path);
            }
            if ((ComandosSalud.b_estadoSalud & ComandosSalud.exc_flotacion) > 0)
            {
                if (sRsp != "") sRsp += ";";
                sRsp += getHexadecimal(ComandosSalud.exc_flotacion);

                sRsp += ((ComandosSalud.b_mensaje & ComandosSalud.men_flotacion) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoEf & ComandosSalud.bloEf_flotacion) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTbk & ComandosSalud.bloTbk_flotacion) > 0 ? ",1" : ",0");
                sRsp += ((ComandosSalud.b_bloqueoTotal & ComandosSalud.bloT_flotacion) > 0 ? ",1" : ",0");
                path = @"C:\inetpub\wwwroot\tpwsremotos\Flotacion.tpr";
                if (!File.Exists(path)) File.Create(path);
            }
            else
            {
                path = @"C:\inetpub\wwwroot\tpwsremotos\Flotacion.tpr";
                if (File.Exists(path)) File.Delete(path);
            }

            return sRsp;
        }
        public void MensajesEST() 
        {
            if (Settings.Default.men_puerta)            ComandosSalud.b_mensaje |= ComandosSalud.men_puerta;
            if (Settings.Default.men_corriente)         ComandosSalud.b_mensaje |= ComandosSalud.men_corriente;
            if (Settings.Default.men_minBillete)        ComandosSalud.b_mensaje |= ComandosSalud.men_minBillete;
            if (Settings.Default.men_maxBillete)        ComandosSalud.b_mensaje |= ComandosSalud.men_maxBillete;
            if (Settings.Default.men_minMonedas)        ComandosSalud.b_mensaje |= ComandosSalud.men_minMonedas;
            if (Settings.Default.men_maxMonedas)        ComandosSalud.b_mensaje |= ComandosSalud.men_maxMonedas;
            if (Settings.Default.men_disDiferente)      ComandosSalud.b_mensaje |= ComandosSalud.men_disDiferente;

            //smartPayout
            if (Settings.Default.men_atascoSeguro)      ComandosSalud.b_mensaje |= ComandosSalud.men_atascoSeguro;
            if (Settings.Default.men_atascoInSeguro)    ComandosSalud.b_mensaje |= ComandosSalud.men_atascoInSeguro;
            if (Settings.Default.men_intentoFraudeB)    ComandosSalud.b_mensaje |= ComandosSalud.men_intentoFraudeB;
            if (Settings.Default.men_cajaFull)          ComandosSalud.b_mensaje |= ComandosSalud.men_cajaFull;
            if (Settings.Default.men_unidadAtascada)    ComandosSalud.b_mensaje |= ComandosSalud.men_unidadAtascada;

            //smartHopper
            if (Settings.Default.men_monedaAtascada)    ComandosSalud.b_mensaje |= ComandosSalud.men_monedaAtascada;
            if (Settings.Default.men_busquedaFallida)   ComandosSalud.b_mensaje |= ComandosSalud.men_busquedaFallida;
            if (Settings.Default.men_intentoFraudeM)    ComandosSalud.b_mensaje |= ComandosSalud.men_intentoFraudeM;
            if (Settings.Default.men_flotacion)         ComandosSalud.b_mensaje |= ComandosSalud.men_flotacion;
        }
        public void BloqueoEST()
        {
            if (Settings.Default.bloEf_corriente)       ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_corriente;
            if (Settings.Default.bloEf_puerta)          ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_puerta;
            if (Settings.Default.bloEf_maxBillete)      ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_maxBillete;
            if (Settings.Default.bloEf_maxMonedas)      ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_maxMonedas;
            if (Settings.Default.bloEf_disDiferente)    ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_disDiferente;

            //smartPayout
            if (Settings.Default.bloEf_atascoSeguro)    ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_atascoSeguro;
            if (Settings.Default.bloEf_atascoInSeguro)  ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_atascoInSeguro;
            if (Settings.Default.bloEf_intentoFraudeB)  ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_intentoFraudeB;
            if (Settings.Default.bloEf_cajaFull)        ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_cajaFull;
            if (Settings.Default.bloEf_unidadAtascada)  ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_unidadAtascada;

            ////smartHopper
            if (Settings.Default.bloEf_monedaAtascada)  ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_monedaAtascada;
            if (Settings.Default.bloEf_busquedaFallida) ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_busquedaFallida;
            if (Settings.Default.bloEf_intentoFraudeM)  ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_intentoFraudeM;
            if (Settings.Default.bloEf_flotacion)       ComandosSalud.b_bloqueoEf |= ComandosSalud.bloEf_flotacion;
        }
        private String getHexadecimal(int numero)
        {
            if (numero.ToString() != getValor(numero))
            {
                return getValor(numero);
            }
            String digito = getValor(numero % 16);
            if (numero >= 16)
            {
                int resto = numero / 16;
                string restoString = getHexadecimal(resto);
                Console.WriteLine(restoString);

                return restoString + digito;
            }
            return numero.ToString();
        }
        private String getValor(int numero)
        {
            switch (numero)
            {
                case 10: return "A";
                case 11: return "B";
                case 12: return "C";
                case 13: return "D";
                case 14: return "E";
                case 15: return "F";
            }
            return numero.ToString();
        }
    }
}                                                                           
