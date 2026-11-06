using System;
using System.Security.Cryptography;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StageUp.UI.Infraestructura
{
    public static class ProteccionFormularios
    {
        public const string CampoToken = "__StageUpCsrf";
        public const string CabeceraToken = "X-StageUp-CSRF";
        private const string ClaveSesion = "StageUp.Formularios.Token";

        public static void Preparar(Page pagina, HttpContext contexto)
        {
            if (contexto.Session == null) return;
            string metodo = contexto.Request.HttpMethod;
            if (metodo != "GET" && metodo != "HEAD" && metodo != "POST") throw new HttpException(405, "Método no permitido.");
            contexto.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            contexto.Response.Cache.SetNoStore();
            if (string.Equals(contexto.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && !EsPeticionValida(contexto))
                throw new HttpException(400, "El formulario venció o no pertenece a esta sesión. Volvé a abrir la página e intentá nuevamente.");

            string token = contexto.Session[ClaveSesion] as string;
            if (token == null)
            {
                byte[] bytes = new byte[32];
                using (RandomNumberGenerator generador = RandomNumberGenerator.Create()) generador.GetBytes(bytes);
                token = Convert.ToBase64String(bytes);
                contexto.Session[ClaveSesion] = token;
            }
            string tokenPagina = token;
            pagina.PreInit += (sender, args) => pagina.ViewStateUserKey = tokenPagina;
            pagina.PreLoad += (sender, args) => ValidarLongitudes(pagina, contexto);
            pagina.PreRender += (sender, args) => pagina.ClientScript.RegisterHiddenField(CampoToken, tokenPagina);
        }

        public static bool EsPeticionValida(HttpContext contexto)
        {
            string esperado = contexto.Session == null ? null : contexto.Session[ClaveSesion] as string;
            string recibido = contexto.Request.Headers[CabeceraToken] ?? contexto.Request.Form[CampoToken];
            if (string.IsNullOrEmpty(esperado) || recibido == null || esperado.Length != recibido.Length) return false;
            int diferencia = 0;
            for (int i = 0; i < esperado.Length; i++) diferencia |= esperado[i] ^ recibido[i];
            return diferencia == 0;
        }

        private static void ValidarLongitudes(Control control, HttpContext contexto)
        {
            TextBox campo = control as TextBox;
            if (campo != null && campo.MaxLength > 0)
            {
                string valor = contexto.Request.Form[campo.UniqueID];
                if (valor != null && valor.Length > campo.MaxLength)
                    throw new HttpException(400, "Uno de los campos supera la longitud permitida. Revisá los datos y volvé a enviarlos.");
            }
            foreach (Control hijo in control.Controls) ValidarLongitudes(hijo, contexto);
        }
    }
}
