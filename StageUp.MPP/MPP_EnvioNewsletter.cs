using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

    public class MPP_EnvioNewsletter
    {
        public int Iniciar(Novedad oNovedad, string criterio, int idUsuarioInterno)
        {
            return Convert.ToInt32(Conexion.Instance.LeerEscalar(
                "sp_EnvioNewsletter_Iniciar",
                new Hashtable
                {
                    { "@idNovedad", oNovedad.IdNovedad },
                    { "@criterio", criterio },
                    { "@idUsuarioInterno", idUsuarioInterno }
                }));
        }


        public int? TomarParaReintento(Novedad oNovedad)
        {
            object valor = Conexion.Instance.LeerEscalar(
                "sp_EnvioNewsletter_TomarParaReintento",
                new Hashtable { { "@idNovedad", oNovedad.IdNovedad } });
            return valor == null || valor == DBNull.Value ? (int?)null : Convert.ToInt32(valor);
        }

        public List<DestinatarioNewsletter> ListarPorEnviar(int idEnvioNewsletter)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_EnvioNewsletter_ListarPorEnviar",
                new Hashtable { { "@idEnvioNewsletter", idEnvioNewsletter } });

            List<DestinatarioNewsletter> lista = new List<DestinatarioNewsletter>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new DestinatarioNewsletter
                {
                    IdSuscriptorNewsletter = Convert.ToInt32(fila["idSuscriptorNewsletter"]),
                    CorreoElectronico = fila["correoElectronico"].ToString(),
                    Nombre = fila["nombre"] == DBNull.Value ? null : fila["nombre"].ToString(),
                    TokenPreferencias = fila["tokenPreferencias"].ToString()
                });
            }
            return lista;
        }


        public bool MarcarEnviando(int idEnvioNewsletter, DestinatarioNewsletter oDestinatario)
        {
            object filas = Conexion.Instance.LeerEscalar(
                "sp_EnvioNewsletter_MarcarEnviando",
                new Hashtable
                {
                    { "@idEnvioNewsletter", idEnvioNewsletter },
                    { "@idSuscriptorNewsletter", oDestinatario.IdSuscriptorNewsletter }
                });
            return filas != null && filas != DBNull.Value && Convert.ToInt32(filas) > 0;
        }



        public int ReabrirInciertos(int idEnvioNewsletter)
        {
            object filas = Conexion.Instance.LeerEscalar(
                "sp_EnvioNewsletter_ReabrirInciertos",
                new Hashtable { { "@idEnvioNewsletter", idEnvioNewsletter } });
            return filas == null || filas == DBNull.Value ? 0 : Convert.ToInt32(filas);
        }

        public void RegistrarResultado(int idEnvioNewsletter, DestinatarioNewsletter oDestinatario, bool enviado)
        {
            Conexion.Instance.Guardar(
                "sp_EnvioNewsletter_RegistrarResultado",
                new Hashtable
                {
                    { "@idEnvioNewsletter", idEnvioNewsletter },
                    { "@idSuscriptorNewsletter", oDestinatario.IdSuscriptorNewsletter },
                    { "@enviado", enviado }
                });
        }

        public ResultadoEnvioNewsletter Finalizar(int idEnvioNewsletter)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_EnvioNewsletter_Finalizar",
                new Hashtable { { "@idEnvioNewsletter", idEnvioNewsletter } });

            DataRow fila = tabla.Rows[0];
            return new ResultadoEnvioNewsletter
            {
                Estado = fila["estado"].ToString(),
                CantidadDestinatarios = Convert.ToInt32(fila["cantidadDestinatarios"]),
                CantidadEnviados = Convert.ToInt32(fila["cantidadEnviados"]),
                CantidadFallidos = Convert.ToInt32(fila["cantidadFallidos"]),
                CantidadOmitidos = Convert.ToInt32(fila["cantidadOmitidos"]),
                CantidadInciertos = Entero(fila, "cantidadInciertos")
            };
        }

        public List<EnvioNewsletter> Listar(int? idNovedad, int maximo)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_EnvioNewsletter_Listar",
                new Hashtable
                {
                    { "@idNovedad", (object)idNovedad ?? DBNull.Value },
                    { "@maximo", maximo }
                });

            List<EnvioNewsletter> lista = new List<EnvioNewsletter>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new EnvioNewsletter
                {
                    IdEnvioNewsletter = Convert.ToInt32(fila["idEnvioNewsletter"]),
                    IdNovedad = Convert.ToInt32(fila["idNovedad"]),
                    TituloNovedad = fila["tituloNovedad"].ToString(),
                    Criterio = fila["criterio"].ToString(),
                    Categoria = fila["categoria"].ToString(),
                    Estado = fila["estado"].ToString(),
                    CantidadDestinatarios = Convert.ToInt32(fila["cantidadDestinatarios"]),
                    CantidadEnviados = Convert.ToInt32(fila["cantidadEnviados"]),
                    CantidadFallidos = Convert.ToInt32(fila["cantidadFallidos"]),
                    CantidadOmitidos = Convert.ToInt32(fila["cantidadOmitidos"]),
                    CantidadInciertos = Entero(fila, "cantidadInciertos"),
                    FechaInicio = Convert.ToDateTime(fila["fechaInicio"]),
                    FechaUltimaActividad = Convert.ToDateTime(fila["fechaUltimaActividad"]),
                    FechaFin = fila["fechaFin"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaFin"]),
                    Responsable = fila["responsable"] == DBNull.Value ? null : fila["responsable"].ToString()
                });
            }
            return lista;
        }

        private static int Entero(DataRow fila, string columna)
        {
            return fila.Table.Columns.Contains(columna) && fila[columna] != DBNull.Value ? Convert.ToInt32(fila[columna]) : 0;
        }

        public List<EnvioNewsletterDestinatario> ListarDestinatarios(int idEnvioNewsletter)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_EnvioNewsletter_ListarDestinatarios",
                new Hashtable { { "@idEnvioNewsletter", idEnvioNewsletter } });

            List<EnvioNewsletterDestinatario> lista = new List<EnvioNewsletterDestinatario>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new EnvioNewsletterDestinatario
                {
                    IdSuscriptorNewsletter = Convert.ToInt32(fila["idSuscriptorNewsletter"]),
                    CorreoElectronico = fila["correoElectronico"].ToString(),
                    Nombre = fila["nombre"] == DBNull.Value ? null : fila["nombre"].ToString(),
                    Estado = fila["estado"].ToString(),
                    Intentos = Convert.ToInt32(fila["intentos"]),
                    FechaUltimoIntento = fila["fechaUltimoIntento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaUltimoIntento"]),
                    FechaEnvio = fila["fechaEnvio"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(fila["fechaEnvio"])
                });
            }
            return lista;
        }
    }
}
