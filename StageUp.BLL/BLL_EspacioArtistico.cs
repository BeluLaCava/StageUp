using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;

namespace StageUp.BLL
{
    public class BLL_EspacioArtistico
    {
        private readonly MPP_EspacioArtistico _mppEspacio = new MPP_EspacioArtistico();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        private const int LongitudMaximaNombre = 300;
        private const int LongitudMaximaDescripcion = 2000;
        private const int LongitudMaximaTipoEspacio = 200;
        private const int MaxFotosPorEspacio = 8;
        private const string TipoEntidadBitacora = "EspacioArtistico";

        private static readonly HashSet<string> EquipamientoPermitido =
            new HashSet<string> { "ESPEJOS", "SONIDO", "INSTRUMENTOS", "EQUIPAMIENTO", "ESCENARIO", "ILUMINACION" };

        public bool FichaCompletaHabilitada
        {
            get { return MPP_EspacioArtistico.FichaCompletaHabilitada; }
        }

        public ResultadoOperacion ValidarFicha(EspacioArtistico espacio)
        {
            if (espacio == null || espacio.Ficha == null)
                return ResultadoOperacion.Error("Completá la información del espacio.");
            ResultadoOperacion basicos = ValidarDatosBasicos(espacio.NombreEspacio, espacio.Descripcion, espacio.TipoEspacio);
            if (!basicos.Exitoso) return basicos;
            FichaEspacio ficha = espacio.Ficha;
            if (string.IsNullOrWhiteSpace(ficha.Provincia) || ficha.Provincia.Length > 100 ||
                string.IsNullOrWhiteSpace(ficha.Ciudad) || ficha.Ciudad.Length > 150 ||
                string.IsNullOrWhiteSpace(ficha.Direccion) || ficha.Direccion.Length > 300)
                return ResultadoOperacion.Error("Completá provincia, ciudad y dirección dentro de los límites indicados.");
            if (!ficha.CapacidadMaxima.HasValue || ficha.CapacidadMaxima < 1 || ficha.CapacidadMaxima > 100000)
                return ResultadoOperacion.Error("La capacidad debe ser un número entero entre 1 y 100.000 personas.");
            if (!ficha.PrecioHora.HasValue || ficha.PrecioHora <= 0 || ficha.PrecioHora > 99999999.99m ||
                decimal.Round(ficha.PrecioHora.Value, 2) != ficha.PrecioHora.Value)
                return ResultadoOperacion.Error("Ingresá un precio por hora positivo, con hasta dos decimales.");
            if (ficha.Moneda != "ARS" && ficha.Moneda != "USD")
                return ResultadoOperacion.Error("Seleccioná la moneda del precio.");
            if (!string.IsNullOrEmpty(ficha.FotoRuta) &&
                !Regex.IsMatch(ficha.FotoRuta, @"^~/Content/Uploads/Espacios/[a-f0-9]{32}\.jpg$"))
                return ResultadoOperacion.Error("La ruta de la foto no es válida.");
            if (ficha.Fotos != null)
            {
                if (ficha.Fotos.Count > MaxFotosPorEspacio)
                    return ResultadoOperacion.Error("Podés cargar hasta " + MaxFotosPorEspacio + " fotos por espacio.");
                foreach (string foto in ficha.Fotos)
                    if (string.IsNullOrEmpty(foto) || !Regex.IsMatch(foto, @"^~/Content/Uploads/Espacios/[a-f0-9]{32}\.jpg$"))
                        return ResultadoOperacion.Error("Hay una foto con una ruta no válida.");
            }
            if ((ficha.TipoPiso ?? "").Length > 100 || (ficha.DetalleEquipamiento ?? "").Length > 1000)
                return ResultadoOperacion.Error("Revisá la extensión del tipo de piso y el detalle de equipamiento.");
            HashSet<string> permitidos = EquipamientoPermitido;
            if (ficha.Equipamiento == null || ficha.Equipamiento.Count > permitidos.Count)
                return ResultadoOperacion.Error("Revisá las características seleccionadas.");
            var seleccionados = new HashSet<string>();
            foreach (string codigo in ficha.Equipamiento)
                if (!permitidos.Contains(codigo) || !seleccionados.Add(codigo))
                    return ResultadoOperacion.Error("Hay características no válidas o repetidas.");
            if (ficha.Disponibilidad == null || ficha.Disponibilidad.Count == 0 || ficha.Disponibilidad.Count > 100)
                return ResultadoOperacion.Error("Agregá al menos una franja de disponibilidad (máximo 100).");
            bool tieneHorario = false;
            for (int i = 0; i < ficha.Disponibilidad.Count; i++)
            {
                FranjaEspacio franja = ficha.Disponibilidad[i];
                if (franja == null) return ResultadoOperacion.Error("Revisá las franjas horarias.");
                bool fechaConcreta = !string.IsNullOrEmpty(franja.Fecha);
                DateTime fecha;
                if (fechaConcreta == franja.DiaSemana.HasValue ||
                    (fechaConcreta && !DateTime.TryParseExact(franja.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)) ||
                    (!fechaConcreta && (franja.DiaSemana < 1 || franja.DiaSemana > 7)))
                    return ResultadoOperacion.Error("Cada horario debe indicar un día de la semana o una fecha válida.");
                if (franja.Bloqueado && (!fechaConcreta || franja.MinutoDesde != 0 || franja.MinutoHasta != 1440))
                    return ResultadoOperacion.Error("El cierre debe corresponder a una fecha completa.");
                if (franja.MinutoDesde < 0 || franja.MinutoHasta > 1440 || franja.MinutoHasta <= franja.MinutoDesde ||
                    franja.MinutoDesde % 30 != 0 || franja.MinutoHasta % 30 != 0)
                    return ResultadoOperacion.Error("Usá horarios en intervalos de 30 minutos, con fin posterior al inicio.");
                tieneHorario |= !franja.Bloqueado;
                for (int j = 0; j < i; j++)
                {
                    FranjaEspacio otra = ficha.Disponibilidad[j];
                    bool mismoDia = fechaConcreta ? franja.Fecha == otra.Fecha :
                        string.IsNullOrEmpty(otra.Fecha) && franja.DiaSemana == otra.DiaSemana;
                    if (mismoDia && franja.MinutoDesde < otra.MinutoHasta && otra.MinutoDesde < franja.MinutoHasta)
                        return ResultadoOperacion.Error("Hay horarios superpuestos para el mismo día.");
                }
            }
            return tieneHorario ? ResultadoOperacion.Ok() : ResultadoOperacion.Error("Agregá al menos un horario abierto.");
        }

        public ResultadoOperacion<int> GuardarFicha(EspacioArtistico espacio, int idUsuarioGestor)
        {
            if (!FichaCompletaHabilitada)
                return ResultadoOperacion<int>.Error("El guardado de los datos adicionales todavía no está habilitado.");
            ResultadoOperacion validacion = ValidarFicha(espacio);
            if (!validacion.Exitoso) return ResultadoOperacion<int>.Error(validacion.Mensaje);
            if (espacio.IdEspacioArtistico != 0)
            {
                ResultadoOperacion propiedad = ValidarPropiedad(_mppEspacio.ObtenerPorId(espacio.IdEspacioArtistico), idUsuarioGestor);
                if (!propiedad.Exitoso) return ResultadoOperacion<int>.Error(propiedad.Mensaje);
            }
            espacio.IdUsuarioGestor = idUsuarioGestor;
            espacio.NombreEspacio = espacio.NombreEspacio.Trim();
            espacio.TipoEspacio = espacio.TipoEspacio.Trim();
            espacio.Descripcion = string.IsNullOrWhiteSpace(espacio.Descripcion) ? null : espacio.Descripcion.Trim();
            if (espacio.Ficha.Fotos != null && espacio.Ficha.Fotos.Count > 0)
                espacio.Ficha.FotoRuta = espacio.Ficha.Fotos[0];
            int id = _mppEspacio.GuardarFicha(espacio);
            _bitacora.Registrar(idUsuarioGestor, espacio.IdEspacioArtistico == 0 ? "ALTA" : "MODIFICACION",
                TipoEntidadBitacora, id, "Guardado de ficha completa del espacio artístico.");
            return ResultadoOperacion<int>.Ok(id, "El espacio se guardó correctamente.");
        }

        public ResultadoOperacion<int> Registrar(int idUsuarioGestor, string nombreEspacio, string descripcion, string tipoEspacio)
        {
            return EjecutarProtegido(() =>
            {
                ResultadoOperacion validacion = ValidarDatosBasicos(nombreEspacio, descripcion, tipoEspacio);
                if (!validacion.Exitoso)
                {
                    return ResultadoOperacion<int>.Error(validacion.Mensaje, validacion.CodigoAlternativo);
                }

                var espacio = new EspacioArtistico
                {
                    IdUsuarioGestor = idUsuarioGestor,
                    NombreEspacio = nombreEspacio.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                    TipoEspacio = tipoEspacio.Trim()
                };

                int idEspacioArtistico = _mppEspacio.Insertar(espacio);

                _bitacora.Registrar(
                    idUsuarioGestor, "ALTA", TipoEntidadBitacora, idEspacioArtistico,
                    "Alta de espacio artístico \"" + espacio.NombreEspacio + "\" (borrador).");

                return ResultadoOperacion<int>.Ok(idEspacioArtistico, "El espacio se guardó como borrador. Podés publicarlo cuando quieras.");
            });
        }

        public ResultadoOperacion Modificar(int idEspacioArtistico, int idUsuarioGestorSolicitante, string nombreEspacio, string descripcion, string tipoEspacio)
        {
            return EjecutarProtegido(() =>
            {
                ResultadoOperacion validacion = ValidarDatosBasicos(nombreEspacio, descripcion, tipoEspacio);
                if (!validacion.Exitoso)
                {
                    return validacion;
                }

                EspacioArtistico espacioExistente = _mppEspacio.ObtenerPorId(idEspacioArtistico);
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacioExistente, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                var espacio = new EspacioArtistico
                {
                    IdEspacioArtistico = idEspacioArtistico,
                    NombreEspacio = nombreEspacio.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                    TipoEspacio = tipoEspacio.Trim()
                };

                _mppEspacio.Modificar(espacio);

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idEspacioArtistico,
                    "Modificación de espacio artístico \"" + espacio.NombreEspacio + "\".");

                return ResultadoOperacion.Ok("Los cambios se guardaron correctamente.");
            });
        }

        public List<EspacioArtistico> ListarMisEspacios(int idUsuarioGestor)
        {
            try
            {
                return _mppEspacio.ListarPorUsuarioGestor(idUsuarioGestor);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<EspacioArtistico>();
            }
        }

        public List<EspacioArtistico> ListarPublicados(string textoBusqueda, string tipoEspacio = null)
        {
            List<EspacioArtistico> publicados;
            try
            {
                publicados = _mppEspacio.ListarPublicados();
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<EspacioArtistico>();
            }

            string termino = string.IsNullOrWhiteSpace(textoBusqueda) ? null : textoBusqueda.Trim();
            string tipo = string.IsNullOrWhiteSpace(tipoEspacio) ? null : tipoEspacio.Trim();

            if (termino == null && tipo == null)
            {
                return publicados;
            }

            return publicados.FindAll(espacio =>
                (termino == null ||
                    ContieneTexto(espacio.NombreEspacio, termino) ||
                    ContieneTexto(espacio.TipoEspacio, termino) ||
                    ContieneTexto(espacio.Descripcion, termino)) &&
                (tipo == null || ContieneTexto(espacio.TipoEspacio, tipo)));
        }

        public List<EspacioArtistico> Buscar(FiltroBusquedaEspacios filtro)
        {
            filtro = Sanitizar(filtro ?? new FiltroBusquedaEspacios());

            if (!FichaCompletaHabilitada)
            {
                return ListarPublicados(filtro.TextoBusqueda, filtro.TipoEspacio);
            }

            try
            {
                return _mppEspacio.BuscarPublicados(filtro);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<EspacioArtistico>();
            }
        }

        private static FiltroBusquedaEspacios Sanitizar(FiltroBusquedaEspacios filtro)
        {
            var limpio = new FiltroBusquedaEspacios
            {
                TextoBusqueda = NormalizarTexto(filtro.TextoBusqueda),
                TipoEspacio = NormalizarTexto(filtro.TipoEspacio),
                Ubicacion = NormalizarTexto(filtro.Ubicacion),
                TipoPiso = NormalizarTexto(filtro.TipoPiso),
                PrecioMaximo = filtro.PrecioMaximo.HasValue && filtro.PrecioMaximo.Value > 0 ? filtro.PrecioMaximo : null,
                CapacidadMinima = filtro.CapacidadMinima.HasValue && filtro.CapacidadMinima.Value > 0 ? filtro.CapacidadMinima : null
            };

            string fecha = NormalizarTexto(filtro.FechaDisponibilidad);
            DateTime valorFecha;
            limpio.FechaDisponibilidad = fecha != null &&
                DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valorFecha)
                ? fecha
                : null;

            if (limpio.FechaDisponibilidad != null &&
                filtro.MinutoDesde.HasValue && filtro.MinutoHasta.HasValue &&
                filtro.MinutoDesde.Value >= 0 && filtro.MinutoHasta.Value <= 1440 &&
                filtro.MinutoHasta.Value > filtro.MinutoDesde.Value &&
                filtro.MinutoDesde.Value % 30 == 0 && filtro.MinutoHasta.Value % 30 == 0)
            {
                limpio.MinutoDesde = filtro.MinutoDesde;
                limpio.MinutoHasta = filtro.MinutoHasta;
            }

            if (filtro.Equipamiento != null)
            {
                foreach (string codigo in filtro.Equipamiento)
                {
                    if (codigo != null && EquipamientoPermitido.Contains(codigo) && !limpio.Equipamiento.Contains(codigo))
                    {
                        limpio.Equipamiento.Add(codigo);
                    }
                }
            }

            return limpio;
        }

        private static string NormalizarTexto(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        public EspacioArtistico ObtenerDetallePublicado(int idEspacioArtistico)
        {
            EspacioArtistico espacio;
            try
            {
                espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }

            if (espacio == null || !espacio.Activo || !espacio.Publicado)
            {
                return null;
            }

            return espacio;
        }

        private static bool ContieneTexto(string valor, string termino)
        {
            return !string.IsNullOrEmpty(valor) &&
                valor.IndexOf(termino, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public ResultadoOperacion Publicar(int idEspacioArtistico, int idUsuarioGestorSolicitante)
        {
            return EjecutarProtegido(() =>
            {
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                _mppEspacio.Publicar(idEspacioArtistico);

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idEspacioArtistico,
                    "Publicación del espacio artístico \"" + espacio.NombreEspacio + "\".");

                return ResultadoOperacion.Ok("El espacio ya está publicado y va a poder verse en el catálogo público.");
            });
        }

        public ResultadoOperacion Pausar(int idEspacioArtistico, int idUsuarioGestorSolicitante)
        {
            return EjecutarProtegido(() =>
            {
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                _mppEspacio.Pausar(idEspacioArtistico);

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "MODIFICACION", TipoEntidadBitacora, idEspacioArtistico,
                    "Pausa del espacio artístico \"" + espacio.NombreEspacio + "\" (dejó de estar publicado).");

                return ResultadoOperacion.Ok("El espacio quedó pausado y ya no se muestra en el catálogo público.");
            });
        }

        public ResultadoOperacion DarDeBaja(int idEspacioArtistico, int idUsuarioGestorSolicitante)
        {
            return EjecutarProtegido(() =>
            {
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(idEspacioArtistico);
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                _mppEspacio.BajaLogica(idEspacioArtistico);

                _bitacora.Registrar(
                    idUsuarioGestorSolicitante, "BAJA", TipoEntidadBitacora, idEspacioArtistico,
                    "Baja lógica del espacio artístico \"" + espacio.NombreEspacio + "\".");

                return ResultadoOperacion.Ok("El espacio se dio de baja. Se mantiene en el historial pero ya no está disponible.");
            });
        }

        private static ResultadoOperacion ValidarDatosBasicos(string nombreEspacio, string descripcion, string tipoEspacio)
        {
            if (string.IsNullOrWhiteSpace(nombreEspacio))
            {
                return ResultadoOperacion.Error("Ingresá el nombre del espacio.", "A15");
            }

            if (nombreEspacio.Trim().Length > LongitudMaximaNombre)
            {
                return ResultadoOperacion.Error("El nombre del espacio no puede superar los " + LongitudMaximaNombre + " caracteres.", "A15");
            }

            if (string.IsNullOrWhiteSpace(tipoEspacio))
            {
                return ResultadoOperacion.Error("Ingresá el tipo de espacio.", "A15");
            }

            if (tipoEspacio.Trim().Length > LongitudMaximaTipoEspacio)
            {
                return ResultadoOperacion.Error("El tipo de espacio no puede superar los " + LongitudMaximaTipoEspacio + " caracteres.", "A15");
            }

            if (!string.IsNullOrEmpty(descripcion) && descripcion.Trim().Length > LongitudMaximaDescripcion)
            {
                return ResultadoOperacion.Error("La descripción no puede superar los " + LongitudMaximaDescripcion + " caracteres.", "A15");
            }

            return ResultadoOperacion.Ok();
        }

        private static ResultadoOperacion ValidarPropiedad(EspacioArtistico espacio, int idUsuarioGestorSolicitante)
        {
            if (espacio == null || !espacio.Activo)
            {
                return ResultadoOperacion.Error("El espacio no existe o ya fue dado de baja.");
            }

            if (espacio.IdUsuarioGestor != idUsuarioGestorSolicitante)
            {
                return ResultadoOperacion.Error("No tenés permiso para administrar este espacio.");
            }

            return ResultadoOperacion.Ok();
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
    }
}
