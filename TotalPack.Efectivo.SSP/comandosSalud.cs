namespace TotalPack.Efectivo.SSP
{
    public class ComandosSalud
    {   
        //byte
        public static int b_estadoSalud;
        public static int b_mensaje;
        public static int b_bloqueoEf;
        public static int b_bloqueoTbk;
        public static int b_bloqueoTotal;

        //estadoSalud
        public const int exc_puerta = 0x01;
        public const int exc_corriente = 0x02;
        public const int exc_minBillete = 0x04;
        public const int exc_maxBillete = 0x08;
        public const int exc_minMonedas = 0x10;
        public const int exc_maxMonedas = 0x20;
        public const int exc_disDiferente = 0x40;
        //smartPayout
        public const int exc_atascoSeguro = 0x80;
        public const int exc_atascoInSeguro = 0x100;
        public const int exc_intentoFraudeB = 0x200;
        public const int exc_cajaFull = 0x400;
        public const int exc_unidadAtascada = 0x800;
        //smartHopper
        public const int exc_monedaAtascada = 0x1000;
        public const int exc_busquedaFallida = 0x2000;
        public const int exc_intentoFraudeM = 0x4000;
        public const int exc_flotacion  = 0x8000;

        //prioridad
        public const int pri_puerta = 0x01;
        public const int pri_corriente = 0x02;
        public const int pri_minBillete = 0x04;
        public const int pri_maxBillete = 0x08;
        public const int pri_minMonedas = 0x10;
        public const int pri_maxMonedas = 0x20;
        public const int pri_disDiferente = 0x40;
        //smartPayout
        public const int pri_atascoSeguro = 0x80;
        public const int pri_atascoInSeguro = 0x100;
        public const int pri_intentoFraudeB = 0x200;
        public const int pri_cajaFull = 0x400;
        public const int pri_unidadAtascada = 0x800;
        //smartHopper
        public const int pri_monedaAtascada = 0x1000;
        public const int pri_busquedaFallida = 0x2000;
        public const int pri_intentoFraudeM = 0x4000;
        public const int pri_flotacion = 0x8000;

        //mensaje
        public const int men_puerta = 0x01;
        public const int men_corriente = 0x02;
        public const int men_minBillete = 0x04;
        public const int men_maxBillete = 0x08;
        public const int men_minMonedas = 0x10;
        public const int men_maxMonedas = 0x20;
        public const int men_disDiferente = 0x40;
        //smartPayout
        public const int men_atascoSeguro = 0x80;
        public const int men_atascoInSeguro = 0x100;
        public const int men_intentoFraudeB = 0x200;
        public const int men_cajaFull = 0x400;
        public const int men_unidadAtascada = 0x800;
        //smartHopper
        public const int men_monedaAtascada = 0x1000;
        public const int men_busquedaFallida = 0x2000;
        public const int men_intentoFraudeM = 0x4000;
        public const int men_flotacion = 0x8000;

        //bloqueoEf
        public const int bloEf_puerta = 0x01;
        public const int bloEf_corriente = 0x02;
        public const int bloEf_minBillete = 0x04;
        public const int bloEf_maxBillete = 0x08;
        public const int bloEf_minMonedas = 0x10;
        public const int bloEf_maxMonedas = 0x20;
        public const int bloEf_disDiferente = 0x40;
        //smartPayout
        public const int bloEf_atascoSeguro = 0x80;
        public const int bloEf_atascoInSeguro = 0x100;
        public const int bloEf_intentoFraudeB = 0x200;
        public const int bloEf_cajaFull = 0x400;
        public const int bloEf_unidadAtascada = 0x800;
        //smartHopper
        public const int bloEf_monedaAtascada = 0x1000;
        public const int bloEf_busquedaFallida = 0x2000;
        public const int bloEf_intentoFraudeM = 0x4000;
        public const int bloEf_flotacion = 0x8000;

        //bloqueoTbk
        public const int bloTbk_puerta = 0x01;
        public const int bloTbk_corriente = 0x02;
        public const int bloTbk_minBillete = 0x04;
        public const int bloTbk_maxBillete = 0x08;
        public const int bloTbk_minMonedas = 0x10;
        public const int bloTbk_maxMonedas = 0x20;
        public const int bloTbk_disDiferente = 0x40;
        //smartPayout
        public const int bloTbk_atascoSeguro = 0x80;
        public const int bloTbk_atascoInSeguro = 0x100;
        public const int bloTbk_intentoFraudeB = 0x200;
        public const int bloTbk_cajaFull = 0x400;
        public const int bloTbk_unidadAtascada = 0x800;
        //smartHopper
        public const int bloTbk_monedaAtascada = 0x1000;
        public const int bloTbk_busquedaFallida = 0x2000;
        public const int bloTbk_intentoFraudeM = 0x4000;
        public const int bloTbk_flotacion = 0x8000;

        //bloqueoTotal
        public const int bloT_puerta = 0x01;
        public const int bloT_corriente = 0x02;
        public const int bloT_minBillete = 0x04;
        public const int bloT_maxBillete = 0x08;
        public const int bloT_minMonedas = 0x10;
        public const int bloT_maxMonedas = 0x20;
        public const int bloT_disDiferente = 0x40;
        //smartPayout
        public const int bloT_atascoSeguro = 0x80;
        public const int bloT_atascoInSeguro = 0x100;
        public const int bloT_intentoFraudeB = 0x200;
        public const int bloT_cajaFull = 0x400;
        public const int bloT_unidadAtascada = 0x800;
        //smartHopper
        public const int bloT_monedaAtascada = 0x1000;
        public const int bloT_busquedaFallida = 0x2000;
        public const int bloT_intentoFraudeM = 0x4000;
        public const int bloT_flotacion = 0x8000;

    }
}
