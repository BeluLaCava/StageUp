using System;
using System.Configuration;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    public class MPP_EspacioArtistico
    {
        public static bool FichaCompletaHabilitada
        {
            get { return string.Equals(ConfigurationManager.AppSettings["EspaciosFichaCompletaHabilitada"], "true", StringComparison.OrdinalIgnoreCase); }
        }

        // La ficha completa (foto, ubicación, precio, equipamiento y disponibilidad
        // semanal) se guarda con columnas y tablas normales: FichaEspacio (1 a 1 con
        // EspacioArtistico), FichaEspacioEquipamiento y FranjaEspacio (1 a N cada una).
        // El formulario de "Mis espacios" siempre manda la ficha completa, así que en
        // vez de un UPDATE fila por fila se borra todo lo anterior y se reinserta de
        // cero — mismo patrón que ya usa RolInternoPermiso para reasignar permisos.
        public int GuardarFicha(EspacioArtistico espacio)
        {
            if (!FichaCompletaHabilitada)
                throw new InvalidOperationException("La ficha completa todavía no está habilitada.");

            int idEspacioArtistico = espacio.IdEspacioArtistico == 0
                ? Insertar(espacio)
                : ModificarYDevolverId(espacio);

            FichaEspacio ficha = espacio.Ficha ?? new FichaEspacio();

            Conexion.Instance.Guardar(
                "sp_FichaEspacio_EliminarPorEspacio",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));

            Conexion.Instance.Guardar(
                "sp_FichaEspacio_Insertar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico),
                new SqlParameter("@fotoRuta", (object)ficha.FotoRuta ?? DBNull.Value),
                new SqlParameter("@provincia", ficha.Provincia),
                new SqlParameter("@ciudad", ficha.Ciudad),
                new SqlParameter("@direccion", ficha.Direccion),
                new SqlParameter("@capacidadMaxima", ficha.CapacidadMaxima.Value),
                new SqlParameter("@precioHora", ficha.PrecioHora.Value),
                new SqlParameter("@moneda", ficha.Moneda),
                new SqlParameter("@tipoPiso", (object)ficha.TipoPiso ?? DBNull.Value),
                new SqlParameter("@detalleEquipamiento", (object)ficha.DetalleEquipamiento ?? DBNull.Value));

            foreach (string codigo in ficha.Equipamiento ?? new List<string>())
            {
                Conexion.Instance.Guardar(
                    "sp_FichaEspacioEquipamiento_Insertar",
                    new SqlParameter("@idEspacioArtistico", idEspacioArtistico),
                    new SqlParameter("@codigoEquipamiento", codigo));
            }

            foreach (FranjaEspacio franja in ficha.Disponibilidad ?? new List<FranjaEspacio>())
            {
                bool fechaConcreta = !string.IsNullOrEmpty(franja.Fecha);
                Conexion.Instance.Guardar(
                    "sp_FranjaEspacio_Insertar",
                    new SqlParameter("@idEspacioArtistico", idEspacioArtistico),
                    new SqlParameter("@diaSemana", franja.DiaSemana.HasValue ? (object)franja.DiaSemana.Value : DBNull.Value),
                    new SqlParameter("@fecha", fechaConcreta
                        ? (object)DateTime.ParseExact(franja.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : DBNull.Value),
                    new SqlParameter("@minutoDesde", franja.MinutoDesde),
                    new SqlParameter("@minutoHasta", franja.MinutoHasta),
                    new SqlParameter("@bloqueado", franja.Bloqueado));
            }

            // Ítem 3 (varias fotografías): mismo patrón que equipamiento/franjas — no
            // hace falta un DELETE aparte para EspacioFoto, porque sp_FichaEspacio_EliminarPorEspacio
            // ya vació la FichaEspacio anterior y el ON DELETE CASCADE se llevó sus fotos.
            // El orden de la lista define "orden"; la primera es la principal/portada.
            List<string> fotos = ficha.Fotos ?? new List<string>();
            for (int indice = 0; indice < fotos.Count; indice++)
            {
                Conexion.Instance.Guardar(
                    "sp_EspacioFoto_Insertar",
                    new SqlParameter("@idEspacioArtistico", idEspacioArtistico),
                    new SqlParameter("@rutaFoto", fotos[indice]),
                    new SqlParameter("@orden", indice),
                    new SqlParameter("@esPrincipal", indice == 0));
            }

            return idEspacioArtistico;
        }

        public int Insertar(EspacioArtistico espacio)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_EspacioArtistico_Insertar",
                new SqlParameter("@idUsuarioGestor", espacio.IdUsuarioGestor),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio));

            return Convert.ToInt32(resultado);
        }

        public void Modificar(EspacioArtistico espacio)
        {
            ModificarYDevolverId(espacio);
        }

        private int ModificarYDevolverId(EspacioArtistico espacio)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Modificar",
                new SqlParameter("@idEspacioArtistico", espacio.IdEspacioArtistico),
                new SqlParameter("@nombreEspacio", espacio.NombreEspacio),
                new SqlParameter("@descripcion", (object)espacio.Descripcion ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", espacio.TipoEspacio));

            return espacio.IdEspacioArtistico;
        }

        public EspacioArtistico ObtenerPorId(int idEspacioArtistico)
        {
            DataTable tabla = Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ObtenerPorIdV2" : "sp_EspacioArtistico_ObtenerPorId",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            EspacioArtistico espacio = MapearDesdeFila(tabla.Rows[0]);

            if (FichaCompletaHabilitada)
            {
                CompletarDatosHijosDeFicha(
                    new List<EspacioArtistico> { espacio },
                    "sp_FichaEspacioEquipamiento_ListarPorEspacio", "sp_FranjaEspacio_ListarPorEspacio", "sp_EspacioFoto_ListarPorEspacio",
                    () => new[] { new SqlParameter("@idEspacioArtistico", idEspacioArtistico) });
            }

            return espacio;
        }

        public List<EspacioArtistico> ListarPorUsuarioGestor(int idUsuarioGestor)
        {
            List<EspacioArtistico> lista = MapearDesdeTabla(Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ListarPorUsuarioGestorV2" : "sp_EspacioArtistico_ListarPorUsuarioGestor",
                new SqlParameter("@idUsuarioGestor", idUsuarioGestor)));

            if (FichaCompletaHabilitada)
            {
                CompletarDatosHijosDeFicha(
                    lista,
                    "sp_FichaEspacioEquipamiento_ListarPorUsuarioGestor", "sp_FranjaEspacio_ListarPorUsuarioGestor", "sp_EspacioFoto_ListarPorUsuarioGestor",
                    () => new[] { new SqlParameter("@idUsuarioGestor", idUsuarioGestor) });
            }

            return lista;
        }

        public List<EspacioArtistico> ListarPublicados()
        {
            List<EspacioArtistico> lista = MapearDesdeTabla(Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ListarPublicadosV2" : "sp_EspacioArtistico_ListarPublicados"));

            if (FichaCompletaHabilitada)
            {
                CompletarDatosHijosDeFicha(
                    lista,
                    "sp_FichaEspacioEquipamiento_ListarPublicados", "sp_FranjaEspacio_ListarPublicados", "sp_EspacioFoto_ListarPublicados",
                    () => new SqlParameter[0]);
            }

            return lista;
        }

        // Ítem 5 (filtros completos del catálogo): el filtrado por ubicación,
        // precio, capacidad, tipo de piso, equipamiento y disponibilidad se
        // resuelve en SQL (sp_EspacioArtistico_BuscarPublicados) en vez de traer
        // todo y filtrar en memoria. Solo tiene sentido con la ficha completa
        // habilitada (sin ella no existen las columnas para filtrar).
        public List<EspacioArtistico> BuscarPublicados(FiltroBusquedaEspacios filtro)
        {
            if (!FichaCompletaHabilitada)
            {
                throw new InvalidOperationException("La búsqueda avanzada todavía no está habilitada.");
            }

            DateTime? fechaDisponibilidad = string.IsNullOrEmpty(filtro.FechaDisponibilidad)
                ? (DateTime?)null
                : DateTime.ParseExact(filtro.FechaDisponibilidad, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            List<string> equipamiento = filtro.Equipamiento ?? new List<string>();

            List<EspacioArtistico> lista = MapearDesdeTabla(Conexion.Instance.Leer(
                "sp_EspacioArtistico_BuscarPublicados",
                new SqlParameter("@textoBusqueda", (object)filtro.TextoBusqueda ?? DBNull.Value),
                new SqlParameter("@tipoEspacio", (object)filtro.TipoEspacio ?? DBNull.Value),
                new SqlParameter("@ubicacion", (object)filtro.Ubicacion ?? DBNull.Value),
                new SqlParameter("@precioMaximo", (object)filtro.PrecioMaximo ?? DBNull.Value),
                new SqlParameter("@capacidadMinima", (object)filtro.CapacidadMinima ?? DBNull.Value),
                new SqlParameter("@tipoPiso", (object)filtro.TipoPiso ?? DBNull.Value),
                new SqlParameter("@fechaDisponibilidad", (object)fechaDisponibilidad ?? DBNull.Value),
                new SqlParameter("@minutoDesde", (object)filtro.MinutoDesde ?? DBNull.Value),
                new SqlParameter("@minutoHasta", (object)filtro.MinutoHasta ?? DBNull.Value),
                new SqlParameter("@reqEspejos", equipamiento.Contains("ESPEJOS")),
                new SqlParameter("@reqSonido", equipamiento.Contains("SONIDO")),
                new SqlParameter("@reqInstrumentos", equipamiento.Contains("INSTRUMENTOS")),
                new SqlParameter("@reqEquipamiento", equipamiento.Contains("EQUIPAMIENTO")),
                new SqlParameter("@reqEscenario", equipamiento.Contains("ESCENARIO")),
                new SqlParameter("@reqIluminacion", equipamiento.Contains("ILUMINACION"))));

            // Mismo patrón que ListarPublicados: las tres consultas hijas traen
            // equipamiento/franjas/fotos de TODOS los espacios publicados, pero
            // CompletarDatosHijosDeFicha solo les presta atención a los que están
            // en el diccionario armado a partir de "lista" (ya filtrada por SQL).
            CompletarDatosHijosDeFicha(
                lista,
                "sp_FichaEspacioEquipamiento_ListarPublicados", "sp_FranjaEspacio_ListarPublicados", "sp_EspacioFoto_ListarPublicados",
                () => new SqlParameter[0]);

            return lista;
        }

        public void Publicar(int idEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Publicar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void Pausar(int idEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Pausar",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        public void BajaLogica(int idEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_BajaLogica",
                new SqlParameter("@idEspacioArtistico", idEspacioArtistico));
        }

        private static List<EspacioArtistico> MapearDesdeTabla(DataTable tabla)
        {
            var lista = new List<EspacioArtistico>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(MapearDesdeFila(fila));
            }
            return lista;
        }

        private static EspacioArtistico MapearDesdeFila(DataRow fila)
        {
            return new EspacioArtistico
            {
                Ficha = LeerFicha(fila),
                IdEspacioArtistico = Convert.ToInt32(fila["idEspacioArtistico"]),
                IdUsuarioGestor = Convert.ToInt32(fila["idUsuarioGestor"]),
                NombreEspacio = fila["nombreEspacio"].ToString(),
                Descripcion = fila["descripcion"] == DBNull.Value ? null : fila["descripcion"].ToString(),
                TipoEspacio = fila["tipoEspacio"].ToString(),
                EstadoEspacio = fila["estadoEspacio"].ToString(),
                Publicado = Convert.ToBoolean(fila["publicado"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaPublicacion = fila["fechaPublicacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaPublicacion"]),
                FechaBaja = fila["fechaBaja"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaBaja"]),
                FechaUltimaModificacion = fila["fechaUltimaModificacion"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                NombreGestor = fila.Table.Columns.Contains("nombreGestor") && fila["nombreGestor"] != DBNull.Value
                    ? fila["nombreGestor"].ToString() : null,
                ApellidoGestor = fila.Table.Columns.Contains("apellidoGestor") && fila["apellidoGestor"] != DBNull.Value
                    ? fila["apellidoGestor"].ToString() : null,
                GestorDesde = LeerGestorDesde(fila),
                CantidadEspaciosPublicadosGestor = fila.Table.Columns.Contains("cantidadEspaciosPublicadosGestor")
                    ? Convert.ToInt32(fila["cantidadEspaciosPublicadosGestor"]) : 0
            };
        }

        // "Reputación" liviana del gestor (tanda 4): en vez de un sistema de
        // calificaciones (que todavía no existe — queda para el Avance 2), se muestra
        // hace cuánto es gestor en la plataforma y cuántos espacios tiene publicados.
        // Preferimos fechaActivacion (cuenta ya activa) y si no vino, fechaAlta.
        private static DateTime? LeerGestorDesde(DataRow fila)
        {
            if (fila.Table.Columns.Contains("gestorFechaActivacion") && fila["gestorFechaActivacion"] != DBNull.Value)
                return Convert.ToDateTime(fila["gestorFechaActivacion"]);
            if (fila.Table.Columns.Contains("gestorFechaAlta") && fila["gestorFechaAlta"] != DBNull.Value)
                return Convert.ToDateTime(fila["gestorFechaAlta"]);
            return null;
        }

        // Lee las columnas de FichaEspacio si vinieron en la fila (LEFT JOIN de las
        // V2) y el espacio ya tiene una ficha cargada; devuelve una ficha vacía si la
        // ficha completa no está habilitada o el espacio todavía no tiene ficha.
        // Equipamiento, Disponibilidad y Fotos se completan aparte, con
        // CompletarDatosHijosDeFicha, porque son listas 1 a N.
        private static FichaEspacio LeerFicha(DataRow fila)
        {
            if (!FichaCompletaHabilitada || !fila.Table.Columns.Contains("provincia") || fila["provincia"] == DBNull.Value)
            {
                return new FichaEspacio();
            }

            return new FichaEspacio
            {
                FotoRuta = fila["fotoRuta"] == DBNull.Value ? null : fila["fotoRuta"].ToString(),
                Provincia = fila["provincia"].ToString(),
                Ciudad = fila["ciudad"].ToString(),
                Direccion = fila["direccion"].ToString(),
                CapacidadMaxima = Convert.ToInt32(fila["capacidadMaxima"]),
                PrecioHora = Convert.ToDecimal(fila["precioHora"]),
                Moneda = fila["moneda"].ToString(),
                TipoPiso = fila["tipoPiso"] == DBNull.Value ? null : fila["tipoPiso"].ToString(),
                DetalleEquipamiento = fila["detalleEquipamiento"] == DBNull.Value ? null : fila["detalleEquipamiento"].ToString()
            };
        }

        // Completa Equipamiento, Disponibilidad y Fotos de una lista de espacios ya
        // mapeados, con tres consultas (una por tabla hija) en vez de una por espacio.
        // Cada consulta se arma con crearParametros() por separado: los SqlParameter
        // no se pueden reutilizar entre dos comandos distintos.
        private static void CompletarDatosHijosDeFicha(
            List<EspacioArtistico> espacios, string spEquipamiento, string spFranjas, string spFotos, Func<SqlParameter[]> crearParametros)
        {
            if (espacios.Count == 0)
            {
                return;
            }

            var porId = new Dictionary<int, EspacioArtistico>();
            foreach (EspacioArtistico espacio in espacios)
            {
                porId[espacio.IdEspacioArtistico] = espacio;
            }

            DataTable equipamiento = Conexion.Instance.Leer(spEquipamiento, crearParametros());
            foreach (DataRow fila in equipamiento.Rows)
            {
                EspacioArtistico espacio;
                if (porId.TryGetValue(Convert.ToInt32(fila["idEspacioArtistico"]), out espacio))
                {
                    espacio.Ficha.Equipamiento.Add(fila["codigoEquipamiento"].ToString());
                }
            }

            DataTable franjas = Conexion.Instance.Leer(spFranjas, crearParametros());
            foreach (DataRow fila in franjas.Rows)
            {
                EspacioArtistico espacio;
                if (porId.TryGetValue(Convert.ToInt32(fila["idEspacioArtistico"]), out espacio))
                {
                    espacio.Ficha.Disponibilidad.Add(new FranjaEspacio
                    {
                        DiaSemana = fila["diaSemana"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["diaSemana"]),
                        Fecha = fila["fecha"] == DBNull.Value ? null : Convert.ToDateTime(fila["fecha"]).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        MinutoDesde = Convert.ToInt32(fila["minutoDesde"]),
                        MinutoHasta = Convert.ToInt32(fila["minutoHasta"]),
                        Bloqueado = Convert.ToBoolean(fila["bloqueado"])
                    });
                }
            }

            // Ítem 3: las fotos ya vienen ordenadas por "orden" desde el SP, así que
            // alcanza con agregarlas en el orden en que llegan (fotos[0] = principal).
            DataTable fotos = Conexion.Instance.Leer(spFotos, crearParametros());
            foreach (DataRow fila in fotos.Rows)
            {
                EspacioArtistico espacio;
                if (porId.TryGetValue(Convert.ToInt32(fila["idEspacioArtistico"]), out espacio))
                {
                    espacio.Ficha.Fotos.Add(fila["rutaFoto"].ToString());
                }
            }
        }
    }
}
