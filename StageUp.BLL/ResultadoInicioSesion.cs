namespace StageUp.BLL
{





    public class ResultadoInicioSesion : ResultadoOperacion
    {
        public bool EsInterno { get; private set; }

        private ResultadoInicioSesion(bool exitoso, string mensaje, string codigoAlternativo, bool esInterno)
            : base(exitoso, mensaje, codigoAlternativo)
        {
            EsInterno = esInterno;
        }

        public static ResultadoInicioSesion OkExterno(string mensaje = null)
        {
            return new ResultadoInicioSesion(true, mensaje, null, false);
        }

        public static ResultadoInicioSesion OkInterno(string mensaje = null)
        {
            return new ResultadoInicioSesion(true, mensaje, null, true);
        }

        public static new ResultadoInicioSesion Error(string mensaje, string codigoAlternativo = null)
        {
            return new ResultadoInicioSesion(false, mensaje, codigoAlternativo, false);
        }
    }
}
