using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // Suscriptores del newsletter (script 78).
    public class MPP_SuscriptorNewsletter
    {
        public SuscriptorNewsletter ObtenerPorCorreo(SuscriptorNewsletter oSuscriptor)
        {
            return Obtener(new Hashtable { { "@correoElectronico", oSuscriptor.CorreoElectronico } });
        }

        public SuscriptorNewsletter ObtenerPorToken(SuscriptorNewsletter oSuscriptor)
        {
            return Obtener(new Hashtable { { "@tokenPreferencias", oSuscriptor.TokenPreferencias } });
        }

        public SuscriptorNewsletter ObtenerPorHashConfirmacion(string hashTokenConfirmacion)
        {
            return Obtener(new Hashtable { { "@hashTokenConfirmacion", hashTokenConfirmacion } });
        }

        public int Insertar(SuscriptorNewsletter oSuscriptor, string hashTokenConfirmacion)
        {
            return Convert.ToInt32(Conexion.Instance.LeerEscalar(
                "sp_SuscriptorNewsletter_Insertar",
                new Hashtable
                {
                    { "@correoElectronico", oSuscriptor.CorreoElectronico },
                    { "@nombre", (object)oSuscriptor.Nombre ?? DBNull.Value },
                    { "@hashTokenConfirmacion", hashTokenConfirmacion },
                    { "@vencimientoConfirmacion", oSuscriptor.VencimientoConfirmacion.Value },
                    { "@tokenPreferencias", oSuscriptor.TokenPreferencias }
                }));
        }

        public bool RenovarConfirmacion(SuscriptorNewsletter oSuscriptor, string hashTokenConfirmacion)
        {
            return Filas(Conexion.Instance.LeerEscalar(
                "sp_SuscriptorNewsletter_RenovarConfirmacion",
                new Hashtable
                {
                    { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter },
                    { "@nombre", (object)oSuscriptor.Nombre ?? DBNull.Value },
                    { "@hashTokenConfirmacion", hashTokenConfirmacion },
                    { "@vencimientoConfirmacion", oSuscriptor.VencimientoConfirmacion.Value }
                })) > 0;
        }

        public void MarcarAvisoEnviado(SuscriptorNewsletter oSuscriptor)
        {
            Conexion.Instance.Guardar(
                "sp_SuscriptorNewsletter_MarcarAvisoEnviado",
                new Hashtable { { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter } });
        }

        public bool Confirmar(SuscriptorNewsletter oSuscriptor, string hashTokenConfirmacion)
        {
            return Filas(Conexion.Instance.LeerEscalar(
                "sp_SuscriptorNewsletter_Confirmar",
                new Hashtable
                {
                    { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter },
                    { "@hashTokenConfirmacion", hashTokenConfirmacion }
                })) > 0;
        }

        public bool CambiarEstado(SuscriptorNewsletter oSuscriptor, string estadoNuevo)
        {
            return Filas(Conexion.Instance.LeerEscalar(
                "sp_SuscriptorNewsletter_CambiarEstado",
                new Hashtable
                {
                    { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter },
                    { "@estadoAnterior", oSuscriptor.Estado },
                    { "@estadoNuevo", estadoNuevo }
                })) > 0;
        }

        public void GuardarCategorias(SuscriptorNewsletter oSuscriptor)
        {
            List<string> categorias = oSuscriptor.Categorias ?? new List<string>();
            Conexion.Instance.Guardar(
                "sp_SuscriptorNewsletter_GuardarCategorias",
                new Hashtable
                {
                    { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter },
                    { "@institucional", categorias.Contains("Institucional") },
                    { "@catalogo", categorias.Contains("Catalogo") },
                    { "@consejos", categorias.Contains("Consejos") },
                    { "@comunidad", categorias.Contains("Comunidad") }
                });
        }

        public ResumenSuscriptoresNewsletter ObtenerResumen()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_SuscriptorNewsletter_Resumen");
            if (tabla.Rows.Count == 0)
            {
                return new ResumenSuscriptoresNewsletter();
            }

            DataRow fila = tabla.Rows[0];
            return new ResumenSuscriptoresNewsletter
            {
                Activos = Convert.ToInt32(fila["activos"]),
                Pendientes = Convert.ToInt32(fila["pendientes"]),
                Bajas = Convert.ToInt32(fila["bajas"]),
                ActivosSinCuenta = Convert.ToInt32(fila["activosSinCuenta"])
            };
        }

        public List<DestinatarioNewsletter> ListarDestinatarios(string criterio, Novedad oNovedad)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_Newsletter_ListarDestinatarios",
                new Hashtable
                {
                    { "@criterio", criterio },
                    { "@categoria", oNovedad.Categoria }
                });

            List<DestinatarioNewsletter> lista = new List<DestinatarioNewsletter>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new DestinatarioNewsletter
                {
                    CorreoElectronico = fila["correoElectronico"].ToString(),
                    Nombre = fila["nombre"] == DBNull.Value ? null : fila["nombre"].ToString(),
                    TokenPreferencias = fila["tokenPreferencias"].ToString()
                });
            }
            return lista;
        }

        private static SuscriptorNewsletter Obtener(Hashtable parametros)
        {
            DataTable tabla = Conexion.Instance.Leer("sp_SuscriptorNewsletter_Obtener", parametros);
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            string categorias = fila["categorias"] == DBNull.Value ? string.Empty : fila["categorias"].ToString();
            return new SuscriptorNewsletter
            {
                IdSuscriptorNewsletter = Convert.ToInt32(fila["idSuscriptorNewsletter"]),
                CorreoElectronico = fila["correoElectronico"].ToString(),
                Nombre = fila["nombre"] == DBNull.Value ? null : fila["nombre"].ToString(),
                IdUsuarioExterno = fila["idUsuarioExterno"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idUsuarioExterno"]),
                Estado = fila["estado"].ToString(),
                Origen = fila["origen"].ToString(),
                VencimientoConfirmacion = Fecha(fila["vencimientoConfirmacion"]),
                FechaUltimoEnvioConfirmacion = Fecha(fila["fechaUltimoEnvioConfirmacion"]),
                TokenPreferencias = fila["tokenPreferencias"].ToString(),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaConfirmacion = Fecha(fila["fechaConfirmacion"]),
                FechaBaja = Fecha(fila["fechaBaja"]),
                Categorias = categorias.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
        }

        private static DateTime? Fecha(object valor)
        {
            return valor == DBNull.Value || valor == null ? (DateTime?)null : Convert.ToDateTime(valor);
        }

        private static int Filas(object valor)
        {
            return valor == null || valor == DBNull.Value ? 0 : Convert.ToInt32(valor);
        }
    }
}
