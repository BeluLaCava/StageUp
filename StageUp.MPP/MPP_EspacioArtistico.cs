using System;
using System.Collections;
using System.Configuration;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
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

        public int GuardarFicha(EspacioArtistico oEspacioArtistico)
        {
            if (!FichaCompletaHabilitada)
                throw new InvalidOperationException("La ficha completa todavía no está habilitada.");

            int idEspacioArtistico = oEspacioArtistico.IdEspacioArtistico == 0
                ? Insertar(oEspacioArtistico)
                : ModificarYDevolverId(oEspacioArtistico);

            FichaEspacio ficha = oEspacioArtistico.Ficha ?? new FichaEspacio();

            Conexion.Instance.Guardar(
                "sp_FichaEspacio_EliminarPorEspacio",
                new Hashtable { { "@idEspacioArtistico", idEspacioArtistico } });

            Conexion.Instance.Guardar(
                "sp_FichaEspacio_Insertar",
                new Hashtable
                {
                    { "@idEspacioArtistico", idEspacioArtistico },
                    { "@fotoRuta", (object)ficha.FotoRuta ?? DBNull.Value },
                    { "@provincia", ficha.Provincia },
                    { "@ciudad", ficha.Ciudad },
                    { "@direccion", ficha.Direccion },
                    { "@capacidadMaxima", ficha.CapacidadMaxima.Value },
                    { "@precioHora", ficha.PrecioHora.Value },
                    { "@moneda", ficha.Moneda },
                    { "@tipoPiso", (object)ficha.TipoPiso ?? DBNull.Value },
                    { "@detalleEquipamiento", (object)ficha.DetalleEquipamiento ?? DBNull.Value }
                });

            foreach (string codigo in ficha.Equipamiento ?? new List<string>())
            {
                Conexion.Instance.Guardar(
                    "sp_FichaEspacioEquipamiento_Insertar",
                    new Hashtable
                    {
                        { "@idEspacioArtistico", idEspacioArtistico },
                        { "@codigoEquipamiento", codigo }
                    });
            }

            foreach (FranjaEspacio franja in ficha.Disponibilidad ?? new List<FranjaEspacio>())
            {
                bool fechaConcreta = !string.IsNullOrEmpty(franja.Fecha);
                Conexion.Instance.Guardar(
                    "sp_FranjaEspacio_Insertar",
                    new Hashtable
                    {
                        { "@idEspacioArtistico", idEspacioArtistico },
                        { "@diaSemana", franja.DiaSemana.HasValue ? (object)franja.DiaSemana.Value : DBNull.Value },
                        { "@fecha", fechaConcreta
                            ? (object)DateTime.ParseExact(franja.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : DBNull.Value },
                        { "@minutoDesde", franja.MinutoDesde },
                        { "@minutoHasta", franja.MinutoHasta },
                        { "@bloqueado", franja.Bloqueado }
                    });
            }

            List<string> fotos = ficha.Fotos ?? new List<string>();
            for (int indice = 0; indice < fotos.Count; indice++)
            {
                Conexion.Instance.Guardar(
                    "sp_EspacioFoto_Insertar",
                    new Hashtable
                    {
                        { "@idEspacioArtistico", idEspacioArtistico },
                        { "@rutaFoto", fotos[indice] },
                        { "@orden", indice },
                        { "@esPrincipal", indice == 0 }
                    });
            }

            return idEspacioArtistico;
        }

        public int Insertar(EspacioArtistico oEspacioArtistico)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_EspacioArtistico_Insertar",
                new Hashtable
                {
                    { "@idUsuarioGestor", oEspacioArtistico.IdUsuarioGestor },
                    { "@nombreEspacio", oEspacioArtistico.NombreEspacio },
                    { "@descripcion", (object)oEspacioArtistico.Descripcion ?? DBNull.Value },
                    { "@tipoEspacio", oEspacioArtistico.TipoEspacio }
                });

            return Convert.ToInt32(resultado);
        }

        public void Modificar(EspacioArtistico oEspacioArtistico)
        {
            ModificarYDevolverId(oEspacioArtistico);
        }

        private int ModificarYDevolverId(EspacioArtistico oEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Modificar",
                new Hashtable
                {
                    { "@idEspacioArtistico", oEspacioArtistico.IdEspacioArtistico },
                    { "@nombreEspacio", oEspacioArtistico.NombreEspacio },
                    { "@descripcion", (object)oEspacioArtistico.Descripcion ?? DBNull.Value },
                    { "@tipoEspacio", oEspacioArtistico.TipoEspacio }
                });

            return oEspacioArtistico.IdEspacioArtistico;
        }

        public EspacioArtistico ObtenerPorId(EspacioArtistico oEspacioArtistico)
        {
            DataTable tabla = Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ObtenerPorIdV2" : "sp_EspacioArtistico_ObtenerPorId",
                new Hashtable { { "@idEspacioArtistico", oEspacioArtistico.IdEspacioArtistico } });

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
                    () => new Hashtable { { "@idEspacioArtistico", oEspacioArtistico.IdEspacioArtistico } });
            }

            return espacio;
        }

        public List<EspacioArtistico> ListarPorUsuarioGestor(UsuarioExterno oUsuarioExterno)
        {
            List<EspacioArtistico> lista = MapearDesdeTabla(Conexion.Instance.Leer(
                FichaCompletaHabilitada ? "sp_EspacioArtistico_ListarPorUsuarioGestorV2" : "sp_EspacioArtistico_ListarPorUsuarioGestor",
                new Hashtable { { "@idUsuarioGestor", oUsuarioExterno.IdUsuarioExterno } }));

            if (FichaCompletaHabilitada)
            {
                CompletarDatosHijosDeFicha(
                    lista,
                    "sp_FichaEspacioEquipamiento_ListarPorUsuarioGestor", "sp_FranjaEspacio_ListarPorUsuarioGestor", "sp_EspacioFoto_ListarPorUsuarioGestor",
                    () => new Hashtable { { "@idUsuarioGestor", oUsuarioExterno.IdUsuarioExterno } });
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
                    () => null);
            }

            return lista;
        }

        public List<EspacioArtistico> BuscarPublicados(FiltroBusquedaEspacios oFiltroBusquedaEspacios)
        {
            if (!FichaCompletaHabilitada)
            {
                throw new InvalidOperationException("La búsqueda avanzada todavía no está habilitada.");
            }

            DateTime? fechaDisponibilidad = string.IsNullOrEmpty(oFiltroBusquedaEspacios.FechaDisponibilidad)
                ? (DateTime?)null
                : DateTime.ParseExact(oFiltroBusquedaEspacios.FechaDisponibilidad, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            List<string> equipamiento = oFiltroBusquedaEspacios.Equipamiento ?? new List<string>();

            List<EspacioArtistico> lista = MapearDesdeTabla(Conexion.Instance.Leer(
                "sp_EspacioArtistico_BuscarPublicados",
                new Hashtable
                {
                    { "@textoBusqueda", (object)oFiltroBusquedaEspacios.TextoBusqueda ?? DBNull.Value },
                    { "@tipoEspacio", (object)oFiltroBusquedaEspacios.TipoEspacio ?? DBNull.Value },
                    { "@ubicacion", (object)oFiltroBusquedaEspacios.Ubicacion ?? DBNull.Value },
                    { "@precioMaximo", (object)oFiltroBusquedaEspacios.PrecioMaximo ?? DBNull.Value },
                    { "@capacidadMinima", (object)oFiltroBusquedaEspacios.CapacidadMinima ?? DBNull.Value },
                    { "@tipoPiso", (object)oFiltroBusquedaEspacios.TipoPiso ?? DBNull.Value },
                    { "@fechaDisponibilidad", (object)fechaDisponibilidad ?? DBNull.Value },
                    { "@minutoDesde", (object)oFiltroBusquedaEspacios.MinutoDesde ?? DBNull.Value },
                    { "@minutoHasta", (object)oFiltroBusquedaEspacios.MinutoHasta ?? DBNull.Value },
                    { "@reqEspejos", equipamiento.Contains("ESPEJOS") },
                    { "@reqSonido", equipamiento.Contains("SONIDO") },
                    { "@reqInstrumentos", equipamiento.Contains("INSTRUMENTOS") },
                    { "@reqEquipamiento", equipamiento.Contains("EQUIPAMIENTO") },
                    { "@reqEscenario", equipamiento.Contains("ESCENARIO") },
                    { "@reqIluminacion", equipamiento.Contains("ILUMINACION") }
                }));

            CompletarDatosHijosDeFicha(
                lista,
                "sp_FichaEspacioEquipamiento_ListarPublicados", "sp_FranjaEspacio_ListarPublicados", "sp_EspacioFoto_ListarPublicados",
                () => null);

            return lista;
        }

        public void Publicar(EspacioArtistico oEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Publicar",
                new Hashtable { { "@idEspacioArtistico", oEspacioArtistico.IdEspacioArtistico } });
        }

        public void Pausar(EspacioArtistico oEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_Pausar",
                new Hashtable { { "@idEspacioArtistico", oEspacioArtistico.IdEspacioArtistico } });
        }

        public void BajaLogica(EspacioArtistico oEspacioArtistico)
        {
            Conexion.Instance.Guardar(
                "sp_EspacioArtistico_BajaLogica",
                new Hashtable { { "@idEspacioArtistico", oEspacioArtistico.IdEspacioArtistico } });
        }

        private static List<EspacioArtistico> MapearDesdeTabla(DataTable tabla)
        {
            List<EspacioArtistico> lista = new List<EspacioArtistico>();
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

        private static DateTime? LeerGestorDesde(DataRow fila)
        {
            if (fila.Table.Columns.Contains("gestorFechaActivacion") && fila["gestorFechaActivacion"] != DBNull.Value)
                return Convert.ToDateTime(fila["gestorFechaActivacion"]);
            if (fila.Table.Columns.Contains("gestorFechaAlta") && fila["gestorFechaAlta"] != DBNull.Value)
                return Convert.ToDateTime(fila["gestorFechaAlta"]);
            return null;
        }

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

        private static void CompletarDatosHijosDeFicha(
            List<EspacioArtistico> espacios, string spEquipamiento, string spFranjas, string spFotos, Func<Hashtable> crearParametros)
        {
            if (espacios.Count == 0)
            {
                return;
            }

            Dictionary<int, EspacioArtistico> porId = new Dictionary<int, EspacioArtistico>();
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
