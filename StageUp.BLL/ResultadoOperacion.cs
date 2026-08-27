namespace StageUp.BLL
{
    /// <summary>
    /// Resultado uniforme de una operación de BLL, para que la UI pueda
    /// mostrar el mensaje correspondiente a cada camino alternativo del
    /// caso de uso sin depender de excepciones para el control de flujo
    /// normal (las excepciones quedan para errores no esperados).
    /// </summary>
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

    /// <summary>
    /// Variante de ResultadoOperacion que además devuelve un valor
    /// (por ejemplo, el id del usuario recién registrado).
    /// </summary>
    public class ResultadoOperacion<T> : ResultadoOperacion
    {
        public T Valor { get; private set; }

        private ResultadoOperacion(bool exitoso, string mensaje, string codigoAlternativo, T valor)
            : base(exitoso, mensaje, codigoAlternativo)
        {
            Valor = valor;
        }

        public static ResultadoOperacion<T> Ok(T valor, string mensaje = null)
        {
            return new ResultadoOperacion<T>(true, mensaje, null, valor);
        }

        public static new ResultadoOperacion<T> Error(string mensaje, string codigoAlternativo = null)
        {
            return new ResultadoOperacion<T>(false, mensaje, codigoAlternativo, default(T));
        }
    }
}
