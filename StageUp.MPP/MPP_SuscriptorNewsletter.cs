using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{

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

        public SuscriptorNewsletter ObtenerPorHashVinculo(string hashTokenVinculo)
        {
            return Obtener(new Hashtable { { "@hashTokenVinculo", hashTokenVinculo } });
        }



        public bool SolicitarVinculo(SuscriptorNewsletter oSuscriptor, UsuarioExterno oCuenta, string hashTokenVinculo, DateTime vencimiento)
        {
            return Filas(Conexion.Instance.LeerEscalar(
                "sp_SuscriptorNewsletter_SolicitarVinculo",
                new Hashtable
                {
                    { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter },
                    { "@idUsuarioExterno", oCuenta.IdUsuarioExterno },
                    { "@hashTokenVinculo", hashTokenVinculo },
                    { "@vencimientoVinculo", vencimiento }
                })) > 0;
        }

        public bool ConfirmarVinculo(SuscriptorNewsletter oSuscriptor, string hashTokenVinculo)
        {
            return Filas(Conexion.Instance.LeerEscalar(
                "sp_SuscriptorNewsletter_ConfirmarVinculo",
                new Hashtable
                {
                    { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter },
                    { "@hashTokenVinculo", hashTokenVinculo }
                })) > 0;
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
                    { "@tokenPreferencias", oSuscriptor.TokenPreferencias },
                    { "@idUsuarioExterno", (object)oSuscriptor.IdUsuarioExterno ?? DBNull.Value },
                    { "@origen", oSuscriptor.Origen ?? SuscriptorNewsletter.OrigenFormulario }
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
                    { "@vencimientoConfirmacion", oSuscriptor.VencimientoConfirmacion.Value },
                    { "@idUsuarioExterno", (object)oSuscriptor.IdUsuarioExterno ?? DBNull.Value }
                })) > 0;
        }

        public void MarcarAvisoEnviado(SuscriptorNewsletter oSuscriptor)
        {
            Conexion.Instance.Guardar(
                "sp_SuscriptorNewsletter_MarcarAvisoEnviado",
                new Hashtable { { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter } });
        }



        public void AnularEnvioConfirmacion(SuscriptorNewsletter oSuscriptor)
        {
            Conexion.Instance.Guardar(
                "sp_SuscriptorNewsletter_AnularEnvioConfirmacion",
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
                    { "@categorias", string.Join(",", categorias) }
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
                    IdSuscriptorNewsletter = Convert.ToInt32(fila["idSuscriptorNewsletter"]),
                    CorreoElectronico = fila["correoElectronico"].ToString(),
                    Nombre = fila["nombre"] == DBNull.Value ? null : fila["nombre"].ToString(),
                    TokenPreferencias = fila["tokenPreferencias"].ToString()
                });
            }
            return lista;
        }

        public SuscriptorNewsletter ObtenerPorId(SuscriptorNewsletter oSuscriptor)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SuscriptorNewsletter_ObtenerPorId",
                new Hashtable { { "@idSuscriptorNewsletter", oSuscriptor.IdSuscriptorNewsletter } });
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }


        public List<SuscriptorNewsletter> Listar(FiltroSuscriptoresNewsletter oFiltro, out int totalFiltrado)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SuscriptorNewsletter_Listar",
                new Hashtable
                {
                    { "@texto", (object)oFiltro.Texto ?? DBNull.Value },
                    { "@estado", (object)oFiltro.Estado ?? DBNull.Value },
                    { "@conCuenta", oFiltro.ConCuenta.HasValue ? (object)oFiltro.ConCuenta.Value : DBNull.Value },
                    { "@maximo", oFiltro.Maximo }
                });

            totalFiltrado = 0;
            List<SuscriptorNewsletter> lista = new List<SuscriptorNewsletter>();
            foreach (DataRow fila in tabla.Rows)
            {
                SuscriptorNewsletter suscriptor = Mapear(fila);
                suscriptor.IdCuenta = fila["idCuenta"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["idCuenta"]);
                suscriptor.PerfilCuenta = fila["perfilCuenta"] == DBNull.Value ? null : fila["perfilCuenta"].ToString();
                totalFiltrado = Convert.ToInt32(fila["totalFiltrado"]);
                lista.Add(suscriptor);
            }
            return lista;
        }

        private static SuscriptorNewsletter Obtener(Hashtable parametros)
        {
            DataTable tabla = Conexion.Instance.Leer("sp_SuscriptorNewsletter_Obtener", parametros);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        private static SuscriptorNewsletter Mapear(DataRow fila)
        {
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
                IdUsuarioExternoVinculoPendiente = fila.Table.Columns.Contains("idUsuarioExternoVinculoPendiente") && fila["idUsuarioExternoVinculoPendiente"] != DBNull.Value
                    ? Convert.ToInt32(fila["idUsuarioExternoVinculoPendiente"]) : (int?)null,
                VencimientoVinculo = fila.Table.Columns.Contains("vencimientoVinculo") ? Fecha(fila["vencimientoVinculo"]) : null,
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
