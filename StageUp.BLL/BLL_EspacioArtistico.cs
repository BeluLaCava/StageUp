using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private readonly BLL_Actividad _bllActividad = new BLL_Actividad();

        private const int LongitudMaximaNombre = 300;
        private const int LongitudMaximaDescripcion = 2000;
        private const int LongitudMaximaTipoEspacio = 200;
        private const int MaxFotosPorEspacio = 8;
        private const string TipoEntidadBitacora = "EspacioArtistico";

        private static readonly HashSet<string> EquipamientoPermitido =
            new HashSet<string> { "ESPEJOS", "SONIDO", "INSTRUMENTOS", "EQUIPAMIENTO", "ESCENARIO", "ILUMINACION" };

        // Nombres del equipamiento para la descripción automática (A12).
        private static readonly Dictionary<string, string> NombresEquipamiento = new Dictionary<string, string>
        {
            { "ESPEJOS", "espejos" }, { "SONIDO", "equipo de sonido" }, { "INSTRUMENTOS", "instrumentos" },
            { "EQUIPAMIENTO", "equipamiento técnico" }, { "ESCENARIO", "escenario" }, { "ILUMINACION", "iluminación" }
        };

        // CU-001-007: mínimos para considerar que la información alcanza para
        // presentar el espacio (A18).
        private const int LongitudMinimaDescripcionFicha = 30;
        private const int LongitudMinimaCondiciones = 10;
        private const int LongitudMaximaCondiciones = 1000;

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
            ResultadoOperacion completitud = ValidarDatosDePresentacion(espacio);
            if (!completitud.Exitoso) return completitud;
            HashSet<string> permitidos = EquipamientoPermitido;
            if (ficha.Equipamiento == null || ficha.Equipamiento.Count > permitidos.Count)
                return ResultadoOperacion.Error("Revisá las características seleccionadas.");
            HashSet<string> seleccionados = new HashSet<string>();
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
                // Antes acá se exigía que todo bloqueo (Bloqueado = true) cubriera el día
                // completo (0 a 1440) y tuviera una fecha puntual. Eso impedía cargar un
                // bloqueo manual de gestor para una franja horaria específica (p. ej. "todos
                // los lunes de 17 a 18hs tengo una clase fija"), que es justo el caso que
                // pidió el profesor. Ahora un bloqueo puede ser tanto un día completo como
                // una franja parcial, y tanto recurrente (día de la semana) como puntual
                // (fecha concreta) — las validaciones de abajo (horario válido en intervalos
                // de 30 minutos, sin superposición) ya alcanzan para que quede consistente.
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

        // CU-001-007 A14, A16, A17 y A18: datos que el documento pide para
        // poder presentar el espacio (descripción, medidas, condiciones y
        // reglas de uso, al menos una imagen).
        private static ResultadoOperacion ValidarDatosDePresentacion(EspacioArtistico espacio)
        {
            FichaEspacio ficha = espacio.Ficha;

            if (string.IsNullOrWhiteSpace(espacio.Descripcion) || espacio.Descripcion.Trim().Length < LongitudMinimaDescripcionFicha)
                return ResultadoOperacion.Error(
                    "Contá un poco más del espacio en la descripción (al menos " + LongitudMinimaDescripcionFicha +
                    " caracteres). Si querés, usá «Generar descripción automática».", "A18");

            if (!ficha.SuperficieM2.HasValue)
                return ResultadoOperacion.Error("Ingresá la superficie del espacio en metros cuadrados.", "A14");
            if (ficha.SuperficieM2 <= 0 || ficha.SuperficieM2 > 100000m || decimal.Round(ficha.SuperficieM2.Value, 2) != ficha.SuperficieM2.Value)
                return ResultadoOperacion.Error("La superficie tiene que ser un número mayor a 0 (hasta 100.000 m², con hasta dos decimales).", "A16");
            if (ficha.AlturaM.HasValue &&
                (ficha.AlturaM <= 0 || ficha.AlturaM > 50m || decimal.Round(ficha.AlturaM.Value, 2) != ficha.AlturaM.Value))
                return ResultadoOperacion.Error("La altura tiene que ser un número mayor a 0 y de hasta 50 metros, con hasta dos decimales.", "A16");

            string condiciones = (ficha.CondicionesUso ?? string.Empty).Trim();
            if (condiciones.Length < LongitudMinimaCondiciones)
                return ResultadoOperacion.Error("Completá las condiciones de uso del espacio (al menos " + LongitudMinimaCondiciones + " caracteres).", "A14");
            if (condiciones.Length > LongitudMaximaCondiciones)
                return ResultadoOperacion.Error("Las condiciones de uso no pueden superar los " + LongitudMaximaCondiciones + " caracteres.", "A15");

            string reglas = (ficha.ReglasUso ?? string.Empty).Trim();
            if (reglas.Length < LongitudMinimaCondiciones)
                return ResultadoOperacion.Error("Completá las reglas de uso del espacio (al menos " + LongitudMinimaCondiciones + " caracteres).", "A14");
            if (reglas.Length > LongitudMaximaCondiciones)
                return ResultadoOperacion.Error("Las reglas de uso no pueden superar los " + LongitudMaximaCondiciones + " caracteres.", "A15");

            if (ficha.Fotos == null || ficha.Fotos.Count == 0)
                return ResultadoOperacion.Error("Cargá al menos una foto del espacio (JPG o PNG).", "A17");

            return ResultadoOperacion.Ok();
        }

        // CU-001-007 A12: descripción redactada a partir de los datos cargados.
        // Es una ayuda opcional: el gestor la puede cambiar antes de guardar.
        public static string GenerarDescripcion(EspacioArtistico espacio)
        {
            if (espacio == null)
            {
                return string.Empty;
            }

            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();
            CultureInfo cultura = new CultureInfo("es-AR");
            List<string> oraciones = new List<string>();

            string tipo = string.IsNullOrWhiteSpace(espacio.TipoEspacio) ? "artístico" : espacio.TipoEspacio.Trim();
            string nombre = string.IsNullOrWhiteSpace(espacio.NombreEspacio) ? null : espacio.NombreEspacio.Trim();
            string lugar = string.Join(", ", new[] { ficha.Ciudad, ficha.Provincia }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));

            // Redacción neutra para cualquier tipo ("un teatro", "una galería"...).
            oraciones.Add((nombre != null ? "«" + nombre + "» es un espacio" : "Espacio") +
                " de tipo " + tipo.ToLower(cultura) + (lugar.Length > 0 ? ", en " + lugar : string.Empty) + ".");

            List<string> medidas = new List<string>();
            if (ficha.SuperficieM2.HasValue)
                medidas.Add(ficha.SuperficieM2.Value.ToString("0.##", cultura) + " m²");
            if (ficha.AlturaM.HasValue)
                medidas.Add(ficha.AlturaM.Value.ToString("0.##", cultura) + " m de altura");
            if (ficha.CapacidadMaxima.HasValue)
                medidas.Add("capacidad para " + ficha.CapacidadMaxima.Value.ToString("N0", cultura) + " personas");
            if (medidas.Count > 0)
                oraciones.Add("Cuenta con " + UnirConY(medidas) + ".");

            if (!string.IsNullOrWhiteSpace(ficha.TipoPiso))
                oraciones.Add("El piso es de " + ficha.TipoPiso.Trim().ToLower(cultura) + ".");

            List<string> equipamiento = (ficha.Equipamiento ?? new List<string>())
                .Where(codigo => NombresEquipamiento.ContainsKey(codigo))
                .Select(codigo => NombresEquipamiento[codigo]).ToList();
            if (equipamiento.Count > 0)
                oraciones.Add("Incluye " + UnirConY(equipamiento) + ".");

            if (!string.IsNullOrWhiteSpace(ficha.DetalleEquipamiento))
                oraciones.Add(TerminarConPunto(ficha.DetalleEquipamiento.Trim()));

            oraciones.Add("Ideal para ensayos, clases, presentaciones y producciones artísticas.");

            if (ficha.PrecioHora.HasValue)
                oraciones.Add("Valor de referencia: " + (ficha.Moneda == "USD" ? "US$ " : "$ ") +
                    ficha.PrecioHora.Value.ToString("N2", cultura) + " por hora.");

            string descripcion = string.Join(" ", oraciones);
            return descripcion.Length > LongitudMaximaDescripcion ? descripcion.Substring(0, LongitudMaximaDescripcion) : descripcion;
        }

        // CU-001-007 A13: rango orientativo de precio por hora comparando con
        // espacios publicados en la misma moneda. Se busca primero lo más
        // parecido (mismo tipo, misma provincia y capacidad cercana) y, si no
        // hay al menos 3 espacios para comparar, se amplía el criterio.
        public ResultadoOperacion<SugerenciaPrecio> SugerirValores(EspacioArtistico borrador)
        {
            if (borrador == null || borrador.Ficha == null)
                return ResultadoOperacion<SugerenciaPrecio>.Error("Completá los datos del espacio para pedir una sugerencia.");

            FichaEspacio ficha = borrador.Ficha;
            if (string.IsNullOrWhiteSpace(borrador.TipoEspacio) || string.IsNullOrWhiteSpace(ficha.Provincia))
                return ResultadoOperacion<SugerenciaPrecio>.Error("Para sugerirte un valor necesitamos al menos el tipo de espacio y la provincia.");

            if (ficha.Moneda != "ARS" && ficha.Moneda != "USD")
                ficha.Moneda = "ARS";

            List<EspacioArtistico> referencias;
            try
            {
                referencias = _mppEspacio.ListarPreciosReferencia(borrador);
            }
            catch (ErrorAccesoDatosException ex)
            {
                return ResultadoOperacion<SugerenciaPrecio>.Error(ex.Message);
            }

            string tipo = borrador.TipoEspacio.Trim();
            string provincia = ficha.Provincia.Trim();
            int? capacidad = ficha.CapacidadMaxima;

            Func<EspacioArtistico, bool> mismoTipo = e => string.Equals((e.TipoEspacio ?? "").Trim(), tipo, StringComparison.OrdinalIgnoreCase);
            Func<EspacioArtistico, bool> mismaProvincia = e => string.Equals((e.Ficha.Provincia ?? "").Trim(), provincia, StringComparison.OrdinalIgnoreCase);
            Func<EspacioArtistico, bool> capacidadCercana = e => !capacidad.HasValue || !e.Ficha.CapacidadMaxima.HasValue ||
                (e.Ficha.CapacidadMaxima.Value >= capacidad.Value * 0.5 && e.Ficha.CapacidadMaxima.Value <= capacidad.Value * 1.5);

            var criterios = new List<KeyValuePair<string, Func<EspacioArtistico, bool>>>
            {
                new KeyValuePair<string, Func<EspacioArtistico, bool>>(
                    "espacios del mismo tipo, en " + provincia + " y con capacidad parecida", e => mismoTipo(e) && mismaProvincia(e) && capacidadCercana(e)),
                new KeyValuePair<string, Func<EspacioArtistico, bool>>("espacios del mismo tipo en " + provincia, e => mismoTipo(e) && mismaProvincia(e)),
                new KeyValuePair<string, Func<EspacioArtistico, bool>>("espacios del mismo tipo en todo el país", mismoTipo),
                new KeyValuePair<string, Func<EspacioArtistico, bool>>("espacios de " + provincia, mismaProvincia),
                new KeyValuePair<string, Func<EspacioArtistico, bool>>("todos los espacios publicados", e => true)
            };

            foreach (var criterio in criterios)
            {
                List<decimal> precios = referencias.Where(criterio.Value)
                    .Select(e => e.Ficha.PrecioHora.Value).OrderBy(p => p).ToList();
                if (precios.Count < 3 && criterio.Key != criterios[criterios.Count - 1].Key)
                {
                    continue;
                }

                if (precios.Count == 0)
                {
                    break;
                }

                // Con 4 o más precios se usa el rango intercuartil (deja afuera
                // los extremos); con menos, el mínimo y el máximo.
                decimal minimo = precios.Count >= 4 ? Percentil(precios, 0.25m) : precios[0];
                decimal maximo = precios.Count >= 4 ? Percentil(precios, 0.75m) : precios[precios.Count - 1];

                return ResultadoOperacion<SugerenciaPrecio>.Ok(new SugerenciaPrecio
                {
                    Moneda = ficha.Moneda,
                    Minimo = decimal.Round(minimo, 2),
                    Maximo = decimal.Round(maximo, 2),
                    Mediana = decimal.Round(Percentil(precios, 0.5m), 2),
                    CantidadComparados = precios.Count,
                    Criterio = criterio.Key
                });
            }

            return ResultadoOperacion<SugerenciaPrecio>.Error(
                "Todavía no hay espacios publicados en " + ficha.Moneda + " para comparar. Definí el valor según tus costos.");
        }

        private static decimal Percentil(List<decimal> ordenados, decimal p)
        {
            if (ordenados.Count == 1)
                return ordenados[0];
            decimal posicion = (ordenados.Count - 1) * p;
            int inferior = (int)Math.Floor(posicion);
            int superior = (int)Math.Ceiling(posicion);
            return ordenados[inferior] + (ordenados[superior] - ordenados[inferior]) * (posicion - inferior);
        }

        private static string UnirConY(List<string> partes)
        {
            if (partes.Count == 1)
                return partes[0];
            return string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }

        private static string TerminarConPunto(string texto)
        {
            return texto.EndsWith(".") || texto.EndsWith("!") || texto.EndsWith("?") ? texto : texto + ".";
        }

        // CU-001-007 A8: el gestor ve el detalle de su espacio aunque no esté
        // publicado (borrador o pausado). null si no es suyo o está dado de baja.
        public EspacioArtistico ObtenerDetalleParaGestor(int idEspacioArtistico, int idUsuarioGestor)
        {
            try
            {
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
                return espacio != null && espacio.Activo && espacio.IdUsuarioGestor == idUsuarioGestor ? espacio : null;
            }
            catch (ErrorAccesoDatosException)
            {
                return null;
            }
        }

        public ResultadoOperacion<int> GuardarFicha(EspacioArtistico espacio, int idUsuarioGestor)
        {
            if (!FichaCompletaHabilitada)
                return ResultadoOperacion<int>.Error("El guardado de los datos adicionales todavía no está habilitado.");
            ResultadoOperacion validacion = ValidarFicha(espacio);
            if (!validacion.Exitoso) return ResultadoOperacion<int>.Error(validacion.Mensaje);
            if (espacio.IdEspacioArtistico != 0)
            {
                ResultadoOperacion propiedad = ValidarPropiedad(_mppEspacio.ObtenerPorId(espacio), idUsuarioGestor);
                if (!propiedad.Exitoso) return ResultadoOperacion<int>.Error(propiedad.Mensaje);
            }
            espacio.IdUsuarioGestor = idUsuarioGestor;
            espacio.NombreEspacio = espacio.NombreEspacio.Trim();
            espacio.TipoEspacio = espacio.TipoEspacio.Trim();
            espacio.Descripcion = string.IsNullOrWhiteSpace(espacio.Descripcion) ? null : espacio.Descripcion.Trim();
            if (espacio.Ficha.Fotos != null && espacio.Ficha.Fotos.Count > 0)
                espacio.Ficha.FotoRuta = espacio.Ficha.Fotos[0];

            // Observación de María sobre los scripts SQL (09/26, cascada):
            // guardar la ficha ya NO borra y reinserta FichaEspacio. El
            // mapper la actualiza (sp_FichaEspacio_Guardar, script 41) y solo
            // reemplaza equipamiento, fotos y franjas con origen 'Manual', así
            // que el ON DELETE CASCADE nunca se dispara desde acá y los
            // bloqueos generados desde "Mis actividades" (origen 'Actividad')
            // no se tocan. Por las dudas, si el formulario mandara alguna
            // franja de actividad, se descarta antes de guardar para que no
            // quede duplicada como manual.
            espacio.Ficha.Disponibilidad = ObtenerFranjasEditables(espacio.Ficha);

            // La regeneración de bloqueos de actividad se mantiene: es
            // idempotente (borra y recrea solo las franjas de cada actividad
            // desde dbo.Actividad, la fuente de verdad) y deja todo
            // consistente si alguna actividad cambió mientras tanto.
            return EjecutarProtegido(() =>
            {
                int id = _mppEspacio.GuardarFicha(espacio);
                _bllActividad.RegenerarFranjasBloqueadasDelEspacio(id);
                _bitacora.Registrar(idUsuarioGestor, espacio.IdEspacioArtistico == 0 ? "ALTA" : "MODIFICACION",
                    TipoEntidadBitacora, id, "Guardado de ficha completa del espacio artístico.");
                return ResultadoOperacion<int>.Ok(id, "El espacio se guardó correctamente.");
            });
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

                EspacioArtistico espacio = new EspacioArtistico
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

                EspacioArtistico espacioExistente = _mppEspacio.ObtenerPorId(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacioExistente, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                EspacioArtistico espacio = new EspacioArtistico
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

        // Franjas que el gestor puede editar desde la ficha de "Mis espacios":
        // solo las manuales. Los bloqueos de una actividad se administran
        // desde "Mis actividades" y nunca se mandan al formulario de la ficha.
        public static List<FranjaEspacio> ObtenerFranjasEditables(FichaEspacio ficha)
        {
            List<FranjaEspacio> editables = new List<FranjaEspacio>();
            if (ficha == null || ficha.Disponibilidad == null)
            {
                return editables;
            }

            foreach (FranjaEspacio franja in ficha.Disponibilidad)
            {
                if (franja != null && franja.Origen != "Actividad")
                {
                    editables.Add(franja);
                }
            }

            return editables;
        }

        public List<EspacioArtistico> ListarMisEspacios(int idUsuarioGestor)
        {
            try
            {
                return _mppEspacio.ListarPorUsuarioGestor(
                    new UsuarioExterno { IdUsuarioExterno = idUsuarioGestor });
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

        // Ordenamiento de los resultados del catálogo (ítems 4 y 8 de la
        // segunda entrega). "Relevancia" deja el orden del SP (más recientes
        // primero). "MejorValorados" usa el puntaje ponderado de
        // BLL_Calificacion, así que la lista tiene que venir con las
        // reputaciones ya completadas.
        public const string OrdenRelevancia = "";
        public const string OrdenMejorValorados = "valoracion";
        public const string OrdenPrecioMenor = "precio-asc";
        public const string OrdenPrecioMayor = "precio-desc";
        public const string OrdenCapacidad = "capacidad";
        public const string OrdenRecientes = "recientes";

        public static List<EspacioArtistico> Ordenar(List<EspacioArtistico> espacios, string criterio)
        {
            if (espacios == null)
            {
                return new List<EspacioArtistico>();
            }

            switch (criterio)
            {
                case OrdenMejorValorados:
                    return espacios
                        .OrderByDescending(e => e.PuntajeRanking)
                        .ThenByDescending(e => e.CantidadCalificaciones)
                        .ToList();
                case OrdenPrecioMenor:
                    // Los que no informan precio van al final.
                    return espacios
                        .OrderBy(e => e.Ficha != null && e.Ficha.PrecioHora.HasValue ? 0 : 1)
                        .ThenBy(e => e.Ficha != null && e.Ficha.PrecioHora.HasValue ? e.Ficha.PrecioHora.Value : 0m)
                        .ToList();
                case OrdenPrecioMayor:
                    return espacios
                        .OrderBy(e => e.Ficha != null && e.Ficha.PrecioHora.HasValue ? 0 : 1)
                        .ThenByDescending(e => e.Ficha != null && e.Ficha.PrecioHora.HasValue ? e.Ficha.PrecioHora.Value : 0m)
                        .ToList();
                case OrdenCapacidad:
                    return espacios
                        .OrderByDescending(e => e.Ficha != null && e.Ficha.CapacidadMaxima.HasValue ? e.Ficha.CapacidadMaxima.Value : 0)
                        .ToList();
                case OrdenRecientes:
                    return espacios
                        .OrderByDescending(e => e.FechaPublicacion ?? e.FechaAlta)
                        .ToList();
                default:
                    return espacios;
            }
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
            FiltroBusquedaEspacios limpio = new FiltroBusquedaEspacios
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
                espacio = _mppEspacio.ObtenerPorId(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
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
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                _mppEspacio.Publicar(espacio);

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
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                _mppEspacio.Pausar(espacio);

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
                EspacioArtistico espacio = _mppEspacio.ObtenerPorId(
                    new EspacioArtistico { IdEspacioArtistico = idEspacioArtistico });
                ResultadoOperacion validacionPropiedad = ValidarPropiedad(espacio, idUsuarioGestorSolicitante);
                if (!validacionPropiedad.Exitoso)
                {
                    return validacionPropiedad;
                }

                _mppEspacio.BajaLogica(espacio);

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
