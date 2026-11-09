namespace StageUp.BLL
{
    public sealed class ErrorCargaListadoException : System.Exception
    {
        public ErrorCargaListadoException(System.Exception error)
            : base("No pudimos cargar la información. Intentá nuevamente en unos minutos.", error) { }
    }

    public class ResultadoOperacion
    {
        public bool Exitoso { get; private set; }
        public string Mensaje { get; private set; }
        public string CodigoAlternativo { get; private set; }

        protected ResultadoOperacion(bool exitoso, string mensaje, string codigoAlternativo)
        {
            Exitoso = exitoso;
            Mensaje = mensaje;
            CodigoAlternativo = codigoAlternativo;
        }

        public static ResultadoOperacion Ok(string mensaje = null)
        {
            return new ResultadoOperacion(true, mensaje, null);
        }

        public static ResultadoOperacion Error(string mensaje, string codigoAlternativo = null)
        {
            return new ResultadoOperacion(false, mensaje, codigoAlternativo);
        }
    }

    public class ResultadoOperacion<T> : ResultadoOperacion
    {
        public T Valor { get; private set; }

        private ResultadoOperacion(bool exitoso, string mensaje, string codigoAlternativo, T valor)
            : base(exitoso, mensaje, codigoAlternativo)
        {
            Valor = valor;
        }

        internal static ResultadoOperacion<T> ErrorConValor(T valor, string mensaje, string codigoAlternativo)
        {
            return new ResultadoOperacion<T>(false, mensaje, codigoAlternativo, valor);
        }

        public static ResultadoOperacion<T> Ok(T valor, string mensaje = null)
        {
            return new ResultadoOperacion<T>(true, mensaje, null, valor);
        }

        public static ResultadoOperacion<T> OkAlternativo(T valor, string mensaje, string codigoAlternativo)
        {
            return new ResultadoOperacion<T>(true, mensaje, codigoAlternativo, valor);
        }

        public static new ResultadoOperacion<T> Error(string mensaje, string codigoAlternativo = null)
        {
            return new ResultadoOperacion<T>(false, mensaje, codigoAlternativo, default(T));
        }
    }
}
