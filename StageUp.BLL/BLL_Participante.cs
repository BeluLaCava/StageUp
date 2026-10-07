using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    // CU-001-010 Gestionar participantes de actividades internas.
    //
    // Directorio de participantes de un gestor (no son usuarios de StageUp),
    // que después se asocian a sus actividades internas. Validaciones del
    // documento: campos obligatorios (A3, todos juntos), DNI con formato
    // válido (A4) y sin repetir entre los participantes activos del gestor
    // (A5 / A12). También se valida el correo si se carga y se evita repetirlo.
    // La baja es lógica y conserva las asociaciones anteriores (A14).
    public class BLL_Participante
    {
        private readonly MPP_Participante _mppParticipante = new MPP_Participante();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const string TipoEntidadBitacora = "Participante";

        public const string FiltroActivos = "Activos";
        public const string FiltroInactivos = "Inactivos";
        public const string FiltroTodos = "Todos";

        public const int LongitudMaximaNombre = 120;
        public const int LongitudMaximaNotas = 800;

        private static readonly Regex FormatoCorreo =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static readonly Regex FormatoTelefono =
            new Regex(@"^\+?[0-9][0-9 \-()]{5,28}$", RegexOptions.Compiled);

        // ------------------------------------------------------------------
        // Alta y modificación (pasos 15 a 19, A11)
        // ------------------------------------------------------------------
        public ResultadoOperacion<int> Guardar(Participante participante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                if (participante == null)
                {
                    return ResultadoOperacion<int>.Error("Completá los datos del participante.", "A3");
                }

                bool esNuevo = participante.IdParticipante == 0;
                Participante anterior = null;
                if (!esNuevo)
                {
                    anterior = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = participante.IdParticipante });
                    if (anterior == null || anterior.IdUsuarioGestor != idUsuarioGestor)
                    {
                        return ResultadoOperacion<int>.Error("El participante indicado no existe o no te pertenece.");
                    }

                    if (!anterior.Activo)
                    {
                        return ResultadoOperacion<int>.Error("El participante está dado de baja: no se puede modificar.");
                    }
                }

                Normalizar(participante);
                participante.IdUsuarioGestor = idUsuarioGestor;

                // A3, A4 / A12.
                ResultadoOperacion validacion = Validar(participante, esNuevo);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje, validacion.CodigoAlternativo);
                }

                // A5 / A12: otro participante activo con el mismo DNI o correo.
                string coincidencia;
                Participante duplicado = _mppParticipante.BuscarDuplicado(participante, out coincidencia);
                if (duplicado != null)
                {
                    string dato = coincidencia == "Correo" ? "el correo " + duplicado.Correo : "el DNI " + duplicado.Dni;
                    return ResultadoOperacion<int>.Error(
                        "Ya tenés registrado a " + duplicado.NombreCompleto + " con " + dato + ".", esNuevo ? "A5" : "A12");
                }

                int idParticipante = esNuevo
                    ? _mppParticipante.Insertar(participante)
                    : ModificarYDevolverId(participante);

                _bitacora.Registrar(
                    idUsuarioGestor, esNuevo ? "ALTA" : "MODIFICACION", TipoEntidadBitacora, idParticipante,
                    (esNuevo ? "Alta del participante " : "Modificación del participante ") + participante.NombreCompleto +
                    " (DNI " + participante.Dni + ")" +
                    (anterior != null && (anterior.NombreCompleto != participante.NombreCompleto || anterior.Dni != participante.Dni)
                        ? ". Antes: " + anterior.NombreCompleto + " (DNI " + anterior.Dni + ")"
                        : string.Empty) + ".");

                return ResultadoOperacion<int>.Ok(idParticipante, esNuevo
                    ? "El participante fue registrado correctamente."
                    : "El participante fue actualizado correctamente.");
            });
        }

        private int ModificarYDevolverId(Participante participante)
        {
            _mppParticipante.Modificar(participante);
            return participante.IdParticipante;
        }

        // A5 paso 3: participante activo del gestor con ese DNI (para ofrecer
        // asociarlo a la actividad en lugar de registrarlo de nuevo).
        public Participante BuscarActivoPorDni(string dni, int idUsuarioGestor)
        {
            try
            {
                string limpio = NormalizarDni(dni);
                if (string.IsNullOrEmpty(limpio))
                {
                    return null;
                }

                string coincidencia;
                Participante duplicado = _mppParticipante.BuscarDuplicado(
                    new Participante { IdUsuarioGestor = idUsuarioGestor, Dni = limpio }, out coincidencia);
                return duplicado != null && coincidencia == "Dni" ? duplicado : null;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        // A14: baja lógica
        // ------------------------------------------------------------------
        public ResultadoOperacion DarDeBaja(int idParticipante, int idUsuarioGestor)
        {
            return EjecutarProtegido(() =>
            {
                Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });
                if (participante == null || participante.IdUsuarioGestor != idUsuarioGestor)
                {
                    return ResultadoOperacion.Error("El participante indicado no existe o no te pertenece.");
                }

                if (!participante.Activo)
                {
                    return ResultadoOperacion.Error("El participante ya estaba dado de baja.");
                }

                _mppParticipante.DarDeBaja(participante);

                _bitacora.Registrar(
                    idUsuarioGestor, "BAJA", TipoEntidadBitacora, idParticipante,
                    "Baja lógica del participante " + participante.NombreCompleto + " (DNI " + participante.Dni + ").");

                return ResultadoOperacion.Ok(
                    "El participante fue dado de baja correctamente. Ya no se puede asociar a nuevas actividades y se conserva su historial.");
            });
        }

        // ------------------------------------------------------------------
        // Consultas
        // ------------------------------------------------------------------
        public List<Participante> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            try
            {
                return _mppParticipante.ListarPorUsuarioGestor(new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor });
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Participante>();
            }
        }

        // Solapa Participantes: filtro de estado y búsqueda (en la consulta).
        public List<Participante> Buscar(int idUsuarioGestor, string estado, string texto)
        {
            try
            {
                string filtro = estado == FiltroInactivos || estado == FiltroTodos ? estado : FiltroActivos;
                string busqueda = (texto ?? string.Empty).Trim();
                if (busqueda.Length > 100)
                {
                    busqueda = busqueda.Substring(0, 100);
                }

                // Los comodines de LIKE se buscan como texto.
                busqueda = busqueda.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
                return _mppParticipante.Buscar(new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor }, filtro,
                    busqueda.Length == 0 ? null : busqueda);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<Participante>();
            }
        }

        public Participante ObtenerParaEditar(int idParticipante, int idUsuarioGestor)
        {
            Participante participante = ObtenerDetalle(idParticipante, idUsuarioGestor);
            return participante != null && participante.Activo ? participante : null;
        }

        // A10: detalle con actividades vigentes e historial.
        public Participante ObtenerDetalle(int idParticipante, int idUsuarioGestor)
        {
            try
            {
                Participante participante = _mppParticipante.ObtenerPorId(new Participante { IdParticipante = idParticipante });
                if (participante == null || participante.IdUsuarioGestor != idUsuarioGestor)
                {
                    return null;
                }

                participante.Actividades = _mppParticipante.ListarActividades(participante);
                return participante;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Validaciones
        // ------------------------------------------------------------------

        // DNI: se aceptan puntos y espacios al cargarlo; se guarda solo con
        // los números.
        public static string NormalizarDni(string dni)
        {
            return Regex.Replace(dni ?? string.Empty, @"[\s\.\-]", string.Empty);
        }

        private static void Normalizar(Participante participante)
        {
            participante.Nombre = (participante.Nombre ?? string.Empty).Trim();
            participante.Apellido = (participante.Apellido ?? string.Empty).Trim();
            participante.Dni = NormalizarDni(participante.Dni);
            participante.Correo = string.IsNullOrWhiteSpace(participante.Correo) ? null : participante.Correo.Trim().ToLowerInvariant();
            participante.Telefono = string.IsNullOrWhiteSpace(participante.Telefono) ? null : participante.Telefono.Trim();
            participante.Notas = string.IsNullOrWhiteSpace(participante.Notas) ? null : participante.Notas.Trim();
        }

        private static ResultadoOperacion Validar(Participante participante, bool esNuevo)
        {
            List<string> faltantes = new List<string>();
            if (participante.Nombre.Length == 0)
            {
                faltantes.Add("el nombre");
            }

            if (participante.Apellido.Length == 0)
            {
                faltantes.Add("el apellido");
            }

            if (participante.Dni.Length == 0)
            {
                faltantes.Add("el DNI");
            }

            string codigoDatos = esNuevo ? "A3" : "A12";
            if (faltantes.Count > 0)
            {
                string lista = faltantes.Count == 1
                    ? faltantes[0]
                    : string.Join(", ", faltantes.Take(faltantes.Count - 1)) + " y " + faltantes[faltantes.Count - 1];
                return ResultadoOperacion.Error("Completá " + lista + " del participante.", codigoDatos);
            }

            if (participante.Nombre.Length > LongitudMaximaNombre || participante.Apellido.Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error("El nombre y el apellido pueden tener hasta " + LongitudMaximaNombre + " caracteres.", codigoDatos);
            }

            // A4: DNI argentino de 7 u 8 números.
            if (!Regex.IsMatch(participante.Dni, @"^[0-9]{7,8}$"))
            {
                return ResultadoOperacion.Error("El DNI no tiene un formato válido: tiene que tener 7 u 8 números (podés escribirlo con o sin puntos).",
                    esNuevo ? "A4" : "A12");
            }

            if (participante.Correo != null && (participante.Correo.Length > 254 || !FormatoCorreo.IsMatch(participante.Correo)))
            {
                return ResultadoOperacion.Error("El correo electrónico no tiene un formato válido.", codigoDatos);
            }

            if (participante.Telefono != null && !FormatoTelefono.IsMatch(participante.Telefono))
            {
                return ResultadoOperacion.Error("El teléfono no tiene un formato válido: usá solo números, espacios, guiones o el signo +.", codigoDatos);
            }

            if (participante.Notas != null && participante.Notas.Length > LongitudMaximaNotas)
            {
                return ResultadoOperacion.Error("Las notas pueden tener hasta " + LongitudMaximaNotas + " caracteres.", codigoDatos);
            }

            return ResultadoOperacion.Ok();
        }

        private static ResultadoOperacion<T> EjecutarProtegido<T>(Func<ResultadoOperacion<T>> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<T>.Error(ex.Message);
            }
        }

        private static ResultadoOperacion EjecutarProtegido(Func<ResultadoOperacion> operacion)
        {
            try
            {
                return operacion();
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
        }
    }
}
