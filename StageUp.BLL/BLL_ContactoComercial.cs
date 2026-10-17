using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-004-002 Gestionar contactos comerciales.
    //   * Alta, consulta y modificación de contactos (no se borran: el
    //     historial de seguimiento se conserva).
    //   * Seguimiento (A7) y cambio de estado comercial (A8), que siempre
    //     deja un registro con el estado anterior y el nuevo.
    //   * Incorporado (A9): se puede asociar con la cuenta de StageUp; el
    //     contacto queda como antecedente comercial y la cuenta toma su
    //     origen si no tenía uno.
    //   * Detección de duplicados (A13) por correo, teléfono o nombre y
    //     entidad.
    public class BLL_ContactoComercial
    {
        public const string PermisoGestionar = "GESTIONAR_CONTACTOS_COMERCIALES";
        public const string EntidadBitacora = "ContactoComercial";
        public const string CodigoDuplicado = "A13";

        private static readonly Regex PatronCorreo = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public static readonly Dictionary<string, string> Estados = new Dictionary<string, string>
        {
            { ContactoComercial.EstadoPendiente, "Pendiente" },
            { ContactoComercial.EstadoContactado, "Contactado" },
            { ContactoComercial.EstadoInteresado, "Interesado" },
            { ContactoComercial.EstadoRechazado, "Rechazado" },
            { ContactoComercial.EstadoIncorporado, "Incorporado a la plataforma" }
        };

        private readonly MPP_ContactoComercial _mpp = new MPP_ContactoComercial();
        private readonly BLL_CatalogoComercial _catalogos = new BLL_CatalogoComercial();

        public static string TextoEstado(string estado)
        {
            string texto;
            return Estados.TryGetValue(estado ?? string.Empty, out texto) ? texto : estado;
        }

        public static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case ContactoComercial.EstadoPendiente: return "comercial-badge-planificada";
                case ContactoComercial.EstadoContactado: return "comercial-badge-contactado";
                case ContactoComercial.EstadoInteresado: return "comercial-badge-activa";
                case ContactoComercial.EstadoIncorporado: return "comercial-badge-finalizada";
                default: return "comercial-badge-cancelada";
            }
        }

        // Id del contacto ya registrado que informa un error A13 (o null).
        public static int? IdDuplicado(ResultadoOperacion resultado)
        {
            int id;
            string codigo = resultado == null ? null : resultado.CodigoAlternativo;
            return codigo != null && codigo.StartsWith(CodigoDuplicado + ":", StringComparison.Ordinal) &&
                int.TryParse(codigo.Substring(CodigoDuplicado.Length + 1), out id)
                ? id
                : (int?)null;
        }

        public ResultadoOperacion<ResumenContactos> ObtenerResumen()
        {
            try
            {
                return ResultadoOperacion<ResumenContactos>.Ok(_mpp.Contar());
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<ResumenContactos>.Error("No se pudieron cargar los contactos comerciales. Probá nuevamente en unos minutos.");
            }
        }

        public ResultadoOperacion<List<ContactoComercial>> Listar(FiltroContactoComercial filtro)
        {
            filtro = filtro ?? new FiltroContactoComercial();
            if (filtro.Estado != null && !Estados.ContainsKey(filtro.Estado)) filtro.Estado = null;
            if (filtro.TipoEntidad != null && !_catalogos.EsValido(ItemCatalogoComercial.TipoEntidad, filtro.TipoEntidad)) filtro.TipoEntidad = null;
            filtro.Texto = BLL_CatalogoComercial.Limpiar(filtro.Texto);
            if (filtro.Texto != null && filtro.Texto.Length > 100) filtro.Texto = filtro.Texto.Substring(0, 100);

            try
            {
                return ResultadoOperacion<List<ContactoComercial>>.Ok(_mpp.Listar(filtro));
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<List<ContactoComercial>>.Error("No se pudieron cargar los contactos comerciales. Probá nuevamente en unos minutos.");
            }
        }

        public ContactoComercial Obtener(int idContactoComercial)
        {
            try
            {
                ContactoComercial contacto = _mpp.Obtener(new ContactoComercial { IdContactoComercial = idContactoComercial });
                return contacto != null && contacto.Activo ? contacto : null;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public List<SeguimientoContacto> ListarSeguimientos(int idContactoComercial)
        {
            try
            {
                return _mpp.ListarSeguimientos(new ContactoComercial { IdContactoComercial = idContactoComercial });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<SeguimientoContacto>();
            }
        }

        // A9: cuenta de StageUp por correo.
        public ResultadoOperacion<CuentaParaVincular> BuscarCuenta(string correo)
        {
            string normalizado = (correo ?? string.Empty).Trim().ToLowerInvariant();
            if (!PatronCorreo.IsMatch(normalizado) || normalizado.Length > 254)
            {
                return ResultadoOperacion<CuentaParaVincular>.Error("Ingresá el correo con el que la persona se registró en StageUp.");
            }

            try
            {
                CuentaParaVincular cuenta = _mpp.BuscarCuenta(new UsuarioExterno { CorreoElectronico = normalizado });
                return cuenta == null
                    ? ResultadoOperacion<CuentaParaVincular>.Error("No hay ninguna cuenta de StageUp con ese correo. El contacto se puede marcar como incorporado igual y asociar la cuenta más adelante.")
                    : ResultadoOperacion<CuentaParaVincular>.Ok(cuenta);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<CuentaParaVincular>.Error("No se pudo buscar la cuenta en este momento.");
            }
        }

        // Pasos 9 a 28 (alta) y A6/A8/A9/A10 a A12 (modificación), con A13 a A19.
        public ResultadoOperacion<int> Guardar(ContactoComercial contacto, int idUsuarioInternoResponsable)
        {
            if (contacto == null)
            {
                return ResultadoOperacion<int>.Error("Completá los datos del contacto.");
            }

            bool esAlta = contacto.IdContactoComercial <= 0;
            Normalizar(contacto);

            // A14: obligatorios.
            List<string> faltantes = new List<string>();
            if (contacto.NombreContacto == null) faltantes.Add("nombre del contacto");
            if (string.IsNullOrEmpty(contacto.TipoEntidad)) faltantes.Add("tipo de entidad");
            if (string.IsNullOrEmpty(contacto.MedioContacto)) faltantes.Add("medio de contacto");
            if (contacto.IdOrigenUsuario <= 0) faltantes.Add("origen del contacto");
            if (contacto.Observaciones == null) faltantes.Add(esAlta ? "observaciones iniciales" : "observaciones");
            if (string.IsNullOrEmpty(contacto.EstadoContacto)) faltantes.Add("estado comercial");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion<int>.Error("Para guardar el contacto completá: " + string.Join(", ", faltantes) + ".", "A14");
            }

            if (contacto.NombreContacto.Length > 150 || (contacto.ApellidoContacto ?? string.Empty).Length > 150 ||
                (contacto.EntidadRepresentada ?? string.Empty).Length > 200 || (contacto.OtroDatoContacto ?? string.Empty).Length > 300 ||
                contacto.Observaciones.Length > 2000 || (contacto.ObservacionCambioEstado ?? string.Empty).Length > 2000)
            {
                return ResultadoOperacion<int>.Error("Algún dato supera el largo permitido (nombre y apellido 150, entidad 200, otro dato 300, observaciones 2000 caracteres).", "A14");
            }

            // A15: tipo de entidad.
            if (!_catalogos.EsValido(ItemCatalogoComercial.TipoEntidad, contacto.TipoEntidad))
            {
                return ResultadoOperacion<int>.Error("Seleccioná el tipo de entidad para clasificar el contacto comercial.", "A15");
            }

            if (!_catalogos.EsValido(ItemCatalogoComercial.MedioContacto, contacto.MedioContacto))
            {
                return ResultadoOperacion<int>.Error("Seleccioná un medio de contacto válido.", "A16");
            }

            // A16: datos de contacto suficientes para el medio elegido.
            string errorDatos = ValidarDatosDeContacto(contacto);
            if (errorDatos != null)
            {
                return ResultadoOperacion<int>.Error(errorDatos, "A16");
            }

            // A17: estado.
            if (!Estados.ContainsKey(contacto.EstadoContacto))
            {
                return ResultadoOperacion<int>.Error("Seleccioná un estado comercial válido (pendiente, contactado, interesado, rechazado o incorporado a la plataforma).", "A17");
            }

            if (contacto.EstadoContacto != ContactoComercial.EstadoIncorporado)
            {
                contacto.IdUsuarioExterno = null;
            }

            ContactoComercial anterior = null;
            if (!esAlta)
            {
                anterior = Obtener(contacto.IdContactoComercial);
                if (anterior == null)
                {
                    return ResultadoOperacion<int>.Error("El contacto ya no existe.");
                }
            }

            // A13: posible duplicado.
            try
            {
                PosibleContactoDuplicado duplicado = _mpp.BuscarDuplicado(contacto);
                if (duplicado != null)
                {
                    return ResultadoOperacion<int>.Error(
                        "El contacto podría estar duplicado: ya está registrado " + duplicado.Nombre + " con el mismo " + duplicado.Coincidencia +
                        ". Revisá el contacto existente, corregí los datos o cancelá la carga.",
                        CodigoDuplicado + ":" + duplicado.IdContactoComercial);
                }
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion<int>.Error("No fue posible guardar el contacto en este momento. Intentá nuevamente.", "A19");
            }

            int id;
            try
            {
                id = _mpp.Guardar(contacto, new UsuarioInterno { IdUsuarioInterno = idUsuarioInternoResponsable });
            }
            catch (ErrorAccesoDatosException ex)
            {
                if (ex.NumeroErrorSql == 51310)
                {
                    return ResultadoOperacion<int>.Error("El contacto ya no existe.");
                }

                if (ex.NumeroErrorSql == 51311)
                {
                    return ResultadoOperacion<int>.Error("La campaña, la alianza o el origen elegido no existe o no está disponible para asociación. Corregí o quitá la vinculación.", "A18");
                }

                if (ex.NumeroErrorSql == 51312)
                {
                    return ResultadoOperacion<int>.Error("La cuenta de StageUp elegida ya no existe. Buscala de nuevo.", "A18");
                }

                return ResultadoOperacion<int>.Error("No fue posible guardar el contacto en este momento. Intentá nuevamente.", "A19");
            }

            RegistrarEnBitacora(id, contacto, anterior, idUsuarioInternoResponsable);

            return ResultadoOperacion<int>.Ok(id, esAlta
                ? "El contacto comercial fue guardado correctamente."
                : "El contacto comercial fue actualizado correctamente.");
        }

        // A7 (y A8 cuando se indica un nuevo estado).
        public ResultadoOperacion RegistrarSeguimiento(SeguimientoContacto seguimiento, string fechaTexto, string fechaProximaTexto, int idUsuarioInternoResponsable)
        {
            if (seguimiento == null)
            {
                return ResultadoOperacion.Error("Completá los datos del seguimiento.");
            }

            ContactoComercial contacto = Obtener(seguimiento.IdContactoComercial);
            if (contacto == null)
            {
                return ResultadoOperacion.Error("El contacto ya no existe.");
            }

            seguimiento.Descripcion = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.Descripcion);
            seguimiento.Resultado = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.Resultado);
            seguimiento.InteresManifestado = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.InteresManifestado);
            seguimiento.ProximaAccion = BLL_CatalogoComercial.LimpiarTextoLargo(seguimiento.ProximaAccion);
            if (string.IsNullOrEmpty(seguimiento.EstadoResultante) || seguimiento.EstadoResultante == contacto.EstadoContacto)
            {
                seguimiento.EstadoResultante = null;
            }

            // A14: información mínima.
            List<string> faltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(fechaTexto)) faltantes.Add("fecha de interacción");
            if (string.IsNullOrEmpty(seguimiento.TipoSeguimiento)) faltantes.Add("tipo de interacción");
            if (seguimiento.Descripcion == null) faltantes.Add("observación");
            if (faltantes.Count > 0)
            {
                return ResultadoOperacion.Error("Para guardar el seguimiento completá: " + string.Join(", ", faltantes) + ".", "A14");
            }

            DateTime? fecha, fechaProxima;
            if (!BLL_CatalogoComercial.LeerFecha(fechaTexto, out fecha) || !BLL_CatalogoComercial.LeerFecha(fechaProximaTexto, out fechaProxima))
            {
                return ResultadoOperacion.Error("Las fechas no tienen un formato válido.", "A14");
            }

            if (fecha.Value.Date > DateTime.Today)
            {
                return ResultadoOperacion.Error("La fecha de interacción no puede ser posterior a hoy.", "A14");
            }

            if (fechaProxima.HasValue && fechaProxima.Value.Date < fecha.Value.Date)
            {
                return ResultadoOperacion.Error("La fecha de la acción pendiente no puede ser anterior a la de la interacción.", "A14");
            }

            if (fechaProxima.HasValue && seguimiento.ProximaAccion == null)
            {
                return ResultadoOperacion.Error("Si indicás una fecha para la acción pendiente, describí la acción.", "A14");
            }

            if (!_catalogos.EsValido(ItemCatalogoComercial.TipoInteraccion, seguimiento.TipoSeguimiento))
            {
                return ResultadoOperacion.Error("Seleccioná el tipo de interacción realizada.", "A14");
            }

            if (seguimiento.Descripcion.Length < 5)
            {
                return ResultadoOperacion.Error("Describí la interacción con un poco más de detalle.", "A14");
            }

            if (seguimiento.Descripcion.Length > 2000 || (seguimiento.Resultado ?? string.Empty).Length > 1000 ||
                (seguimiento.InteresManifestado ?? string.Empty).Length > 1000 || (seguimiento.ProximaAccion ?? string.Empty).Length > 1000)
            {
                return ResultadoOperacion.Error("La observación admite hasta 2000 caracteres y la respuesta, el interés y la acción pendiente hasta 1000.", "A14");
            }

            // A17.
            if (seguimiento.EstadoResultante != null && !Estados.ContainsKey(seguimiento.EstadoResultante))
            {
                return ResultadoOperacion.Error("Seleccioná un estado comercial válido.", "A17");
            }

            seguimiento.FechaSeguimiento = fecha.Value;
            seguimiento.FechaProximaAccion = fechaProxima;
            seguimiento.IdUsuarioInternoResponsable = idUsuarioInternoResponsable;

            try
            {
                _mpp.RegistrarSeguimiento(seguimiento);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.NumeroErrorSql == 51310
                    ? "El contacto ya no existe."
                    : "No fue posible guardar el seguimiento en este momento. Intentá nuevamente.", "A19");
            }

            BLL_CatalogoComercial.Registrar(idUsuarioInternoResponsable, "SEGUIMIENTO", EntidadBitacora, contacto.IdContactoComercial,
                "Seguimiento del contacto comercial \"" + contacto.NombreCompleto + "\" (" +
                _catalogos.Nombre(ItemCatalogoComercial.TipoInteraccion, seguimiento.TipoSeguimiento) + " del " +
                seguimiento.FechaSeguimiento.ToString("dd/MM/yyyy") + ")." +
                (seguimiento.EstadoResultante != null
                    ? " Estado: " + TextoEstado(contacto.EstadoContacto) + " → " + TextoEstado(seguimiento.EstadoResultante) + "."
                    : string.Empty));

            return ResultadoOperacion.Ok(seguimiento.EstadoResultante != null
                ? "El seguimiento fue registrado correctamente y el contacto pasó a \"" + TextoEstado(seguimiento.EstadoResultante) + "\"."
                : "El seguimiento fue registrado correctamente.");
        }

        private void RegistrarEnBitacora(int id, ContactoComercial contacto, ContactoComercial anterior, int idUsuarioInterno)
        {
            if (anterior == null)
            {
                BLL_CatalogoComercial.Registrar(idUsuarioInterno, "ALTA", EntidadBitacora, id,
                    "Alta del contacto comercial \"" + contacto.NombreCompleto + "\" (" +
                    _catalogos.Nombre(ItemCatalogoComercial.TipoEntidad, contacto.TipoEntidad) + ", estado " + TextoEstado(contacto.EstadoContacto) + ")." +
                    DescribirVinculos(contacto));
                return;
            }

            List<string> cambios = new List<string>();
            if (anterior.EstadoContacto != contacto.EstadoContacto)
            {
                cambios.Add("estado " + TextoEstado(anterior.EstadoContacto) + " → " + TextoEstado(contacto.EstadoContacto));
            }

            if (anterior.IdCampaniaComercial != contacto.IdCampaniaComercial)
            {
                cambios.Add(contacto.IdCampaniaComercial.HasValue ? "vinculado a la campaña " + contacto.IdCampaniaComercial : "sin campaña");
            }

            if (anterior.IdAlianzaEstrategica != contacto.IdAlianzaEstrategica)
            {
                cambios.Add(contacto.IdAlianzaEstrategica.HasValue ? "vinculado a la alianza " + contacto.IdAlianzaEstrategica : "sin alianza");
            }

            if (anterior.IdOrigenUsuario != contacto.IdOrigenUsuario)
            {
                cambios.Add("origen " + contacto.IdOrigenUsuario);
            }

            if (anterior.IdUsuarioExterno != contacto.IdUsuarioExterno)
            {
                cambios.Add(contacto.IdUsuarioExterno.HasValue ? "asociado a la cuenta de usuario " + contacto.IdUsuarioExterno : "sin cuenta asociada");
            }

            bool soloVinculos = anterior.EstadoContacto == contacto.EstadoContacto && cambios.Count > 0;
            BLL_CatalogoComercial.Registrar(idUsuarioInterno, soloVinculos ? "ASOCIACION" : "MODIFICACION", EntidadBitacora, id,
                "Modificación del contacto comercial \"" + contacto.NombreCompleto + "\"" +
                (cambios.Count > 0 ? ": " + string.Join("; ", cambios) + "." : "."));
        }

        private static string DescribirVinculos(ContactoComercial contacto)
        {
            List<string> partes = new List<string>();
            if (contacto.IdCampaniaComercial.HasValue) partes.Add("campaña " + contacto.IdCampaniaComercial);
            if (contacto.IdAlianzaEstrategica.HasValue) partes.Add("alianza " + contacto.IdAlianzaEstrategica);
            if (contacto.IdUsuarioExterno.HasValue) partes.Add("cuenta de usuario " + contacto.IdUsuarioExterno);
            return partes.Count == 0 ? string.Empty : " Vínculos: " + string.Join(", ", partes) + ".";
        }

        private static void Normalizar(ContactoComercial contacto)
        {
            contacto.NombreContacto = BLL_CatalogoComercial.Limpiar(contacto.NombreContacto);
            contacto.ApellidoContacto = BLL_CatalogoComercial.Limpiar(contacto.ApellidoContacto);
            contacto.EntidadRepresentada = BLL_CatalogoComercial.Limpiar(contacto.EntidadRepresentada);
            contacto.OtroDatoContacto = BLL_CatalogoComercial.Limpiar(contacto.OtroDatoContacto);
            contacto.Observaciones = BLL_CatalogoComercial.LimpiarTextoLargo(contacto.Observaciones);
            contacto.ObservacionCambioEstado = BLL_CatalogoComercial.LimpiarTextoLargo(contacto.ObservacionCambioEstado);
            contacto.CorreoElectronico = string.IsNullOrWhiteSpace(contacto.CorreoElectronico) ? null : contacto.CorreoElectronico.Trim().ToLowerInvariant();
            contacto.Telefono = BLL_CatalogoComercial.Limpiar(contacto.Telefono);

            string digitos = contacto.Telefono == null ? string.Empty : new string(contacto.Telefono.Where(char.IsDigit).ToArray());
            contacto.TelefonoNormalizado = digitos.Length == 0 ? null : (digitos.Length > 20 ? digitos.Substring(0, 20) : digitos);
        }

        // A16: el medio elegido tiene que tener un dato con el que se pueda
        // usar; además, lo que se cargue tiene que ser válido.
        private static string ValidarDatosDeContacto(ContactoComercial contacto)
        {
            bool correoValido = contacto.CorreoElectronico != null && contacto.CorreoElectronico.Length <= 254 && PatronCorreo.IsMatch(contacto.CorreoElectronico);
            bool telefonoValido = contacto.TelefonoNormalizado != null && contacto.TelefonoNormalizado.Length >= 8 && contacto.TelefonoNormalizado.Length <= 15 &&
                contacto.Telefono.Length <= 40 && contacto.Telefono.All(c => char.IsDigit(c) || " +-()".IndexOf(c) >= 0);

            if (contacto.CorreoElectronico != null && !correoValido)
            {
                return "El correo electrónico no tiene un formato válido.";
            }

            if (contacto.Telefono != null && !telefonoValido)
            {
                return "El teléfono tiene que tener entre 8 y 15 números (se aceptan espacios, guiones, paréntesis y el signo +).";
            }

            switch (contacto.MedioContacto)
            {
                case "Correo":
                    return correoValido ? null : "Para el medio de contacto \"correo electrónico\" ingresá un correo válido.";
                case "Telefono":
                case "WhatsApp":
                    return telefonoValido ? null : "Para el medio de contacto elegido ingresá un teléfono válido.";
                case "RedSocial":
                case "SitioWeb":
                    return contacto.OtroDatoContacto != null ? null : "Para el medio de contacto elegido ingresá el usuario, perfil o dirección web en \"Otro dato de contacto\".";
                case "Presencial":
                    return correoValido || telefonoValido || contacto.OtroDatoContacto != null
                        ? null
                        : "Ingresá al menos un dato con el que se pueda volver a contactar a la persona (correo, teléfono u otro dato).";
                default:
                    return "Seleccioná un medio de contacto válido.";
            }
        }
    }
}
