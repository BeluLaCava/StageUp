using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-004-003 Alianzas estratégicas (script 81).
    public class MPP_AlianzaEstrategica
    {
        public List<AlianzaEstrategica> Listar(FiltroAlianzaEstrategica oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_AlianzaEstrategica_Listar",
                new Hashtable
                {
                    { "@vista", oFiltro.Vista ?? FiltroAlianzaEstrategica.VistaEnGestion },
                    { "@estado", MPP_CatalogoComercial.Nulo(oFiltro.Estado) },
                    { "@tipoInstitucion", MPP_CatalogoComercial.Nulo(oFiltro.TipoInstitucion) },
                    { "@idResponsable", MPP_CatalogoComercial.Nulo(oFiltro.IdUsuarioInternoResponsable) },
                    { "@desde", MPP_CatalogoComercial.Nulo(oFiltro.Desde) },
                    { "@hasta", MPP_CatalogoComercial.Nulo(oFiltro.Hasta) },
                    { "@texto", MPP_CatalogoComercial.Nulo(oFiltro.Texto) }
                });

            List<AlianzaEstrategica> lista = new List<AlianzaEstrategica>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public ResumenAlianzas Contar()
        {
            DataRow fila = Conexion.Instance.Leer("sp_AlianzaEstrategica_Contar").Rows[0];
            return new ResumenAlianzas
            {
                Total = MPP_CatalogoComercial.Entero(fila["total"]) ?? 0,
                EnGestion = MPP_CatalogoComercial.Entero(fila["enGestion"]) ?? 0,
                Activas = MPP_CatalogoComercial.Entero(fila["activas"]) ?? 0,
                EnEvaluacion = MPP_CatalogoComercial.Entero(fila["enEvaluacion"]) ?? 0,
                Vencidas = MPP_CatalogoComercial.Entero(fila["vencidas"]) ?? 0,
                Historicas = MPP_CatalogoComercial.Entero(fila["historicas"]) ?? 0
            };
        }

        public AlianzaEstrategica Obtener(AlianzaEstrategica oAlianza)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_AlianzaEstrategica_Obtener",
                new Hashtable { { "@idAlianzaEstrategica", oAlianza.IdAlianzaEstrategica } });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public List<VinculoComercial> ListarVinculos(AlianzaEstrategica oAlianza)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_AlianzaEstrategica_ListarVinculos",
                new Hashtable { { "@idAlianzaEstrategica", oAlianza.IdAlianzaEstrategica } });

            List<VinculoComercial> lista = new List<VinculoComercial>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new VinculoComercial
                {
                    Tipo = fila["tipo"].ToString(),
                    Id = Convert.ToInt32(fila["id"]),
                    Nombre = fila["nombre"].ToString(),
                    Estado = MPP_CatalogoComercial.Texto(fila["estado"]),
                    Cantidad = MPP_CatalogoComercial.Entero(fila["cantidad"])
                });
            }
            return lista;
        }

        public AlianzaEstrategica BuscarDuplicado(AlianzaEstrategica oAlianza)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_AlianzaEstrategica_BuscarDuplicado",
                new Hashtable
                {
                    { "@idAlianzaExcluida", oAlianza.IdAlianzaEstrategica > 0 ? (object)oAlianza.IdAlianzaEstrategica : DBNull.Value },
                    { "@nombreEntidad", oAlianza.NombreEntidad },
                    { "@fechaInicio", oAlianza.FechaInicio },
                    { "@fechaFin", MPP_CatalogoComercial.Nulo(oAlianza.FechaFin) },
                    { "@objetivoComercial", oAlianza.ObjetivoComercial }
                });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            return new AlianzaEstrategica
            {
                IdAlianzaEstrategica = Convert.ToInt32(tabla.Rows[0]["idAlianzaEstrategica"]),
                NombreEntidad = tabla.Rows[0]["nombreEntidad"].ToString(),
                EstadoAlianza = tabla.Rows[0]["estadoAlianza"].ToString()
            };
        }

        // descripcionBitacora: la auditoría se registra en la misma transacción.
        public int Guardar(AlianzaEstrategica oAlianza, UsuarioInterno oUsuario, string descripcionBitacora = null)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_AlianzaEstrategica_Guardar",
                new Hashtable
                {
                    { "@idAlianzaEstrategica", oAlianza.IdAlianzaEstrategica > 0 ? (object)oAlianza.IdAlianzaEstrategica : DBNull.Value },
                    { "@nombreEntidad", oAlianza.NombreEntidad },
                    { "@tipoInstitucion", oAlianza.TipoInstitucion },
                    { "@datosContacto", oAlianza.DatosContacto },
                    { "@idUsuarioInternoResponsable", oAlianza.IdUsuarioInternoResponsable },
                    { "@objetivoComercial", oAlianza.ObjetivoComercial },
                    { "@alcance", oAlianza.Alcance },
                    { "@descripcionAcuerdo", oAlianza.DescripcionAcuerdo },
                    { "@beneficiosAsociados", oAlianza.BeneficiosAsociados },
                    { "@fechaInicio", oAlianza.FechaInicio },
                    { "@fechaFin", MPP_CatalogoComercial.Nulo(oAlianza.FechaFin) },
                    { "@estadoAlianza", oAlianza.EstadoAlianza },
                    { "@observaciones", MPP_CatalogoComercial.Nulo(oAlianza.Observaciones) },
                    { "@observacionCambio", MPP_CatalogoComercial.Nulo(oAlianza.ObservacionCambioEstado) },
                    { "@idUsuarioInterno", oUsuario.IdUsuarioInterno },
                    { "@idsContactos", MPP_CatalogoComercial.Ids(oAlianza.IdsContactos) },
                    { "@idsCampanias", MPP_CatalogoComercial.Ids(oAlianza.IdsCampanias) },
                    { "@idsContenidos", MPP_CatalogoComercial.Ids(oAlianza.IdsContenidos) },
                    { "@idsOrigenes", MPP_CatalogoComercial.Ids(oAlianza.IdsOrigenes) },
                    { "@descripcionBitacora", MPP_CatalogoComercial.Nulo(descripcionBitacora) }
                });

            return Convert.ToInt32(resultado);
        }

        public void RegistrarSeguimiento(SeguimientoAlianza oSeguimiento)
        {
            Conexion.Instance.Leer(
                "sp_SeguimientoAlianza_Insertar",
                new Hashtable
                {
                    { "@idAlianzaEstrategica", oSeguimiento.IdAlianzaEstrategica },
                    { "@fechaInteraccion", oSeguimiento.FechaInteraccion },
                    { "@tipoInteraccion", oSeguimiento.TipoInteraccion },
                    { "@observacion", oSeguimiento.Observacion },
                    { "@avance", MPP_CatalogoComercial.Nulo(oSeguimiento.Avance) },
                    { "@accionPendiente", MPP_CatalogoComercial.Nulo(oSeguimiento.AccionPendiente) },
                    { "@idUsuarioInternoAsignado", MPP_CatalogoComercial.Nulo(oSeguimiento.IdUsuarioInternoAsignado) },
                    { "@estadoResultante", MPP_CatalogoComercial.Nulo(oSeguimiento.EstadoResultante) },
                    { "@idUsuarioInterno", oSeguimiento.IdUsuarioInternoRegistro }
                });
        }

        public List<SeguimientoAlianza> ListarSeguimientos(AlianzaEstrategica oAlianza)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SeguimientoAlianza_Listar",
                new Hashtable { { "@idAlianzaEstrategica", oAlianza.IdAlianzaEstrategica } });

            List<SeguimientoAlianza> lista = new List<SeguimientoAlianza>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new SeguimientoAlianza
                {
                    IdSeguimientoAlianza = Convert.ToInt32(fila["idSeguimientoAlianza"]),
                    IdAlianzaEstrategica = Convert.ToInt32(fila["idAlianzaEstrategica"]),
                    TipoRegistro = fila["tipoRegistro"].ToString(),
                    FechaInteraccion = Convert.ToDateTime(fila["fechaInteraccion"]),
                    TipoInteraccion = MPP_CatalogoComercial.Texto(fila["tipoInteraccion"]),
                    Observacion = fila["observacion"].ToString(),
                    Avance = MPP_CatalogoComercial.Texto(fila["avance"]),
                    AccionPendiente = MPP_CatalogoComercial.Texto(fila["accionPendiente"]),
                    IdUsuarioInternoAsignado = MPP_CatalogoComercial.Entero(fila["idUsuarioInternoAsignado"]),
                    NombreAsignado = MPP_CatalogoComercial.Texto(fila["nombreAsignado"]),
                    EstadoAnterior = MPP_CatalogoComercial.Texto(fila["estadoAnterior"]),
                    EstadoResultante = MPP_CatalogoComercial.Texto(fila["estadoResultante"]),
                    IdUsuarioInternoRegistro = Convert.ToInt32(fila["idUsuarioInternoRegistro"]),
                    NombreRegistro = MPP_CatalogoComercial.Texto(fila["nombreRegistro"]),
                    FechaRegistro = Convert.ToDateTime(fila["fechaRegistro"])
                });
            }
            return lista;
        }

        public void Renovar(RenovacionAlianza oRenovacion, UsuarioInterno oUsuario)
        {
            Conexion.Instance.Guardar(
                "sp_AlianzaEstrategica_Renovar",
                new Hashtable
                {
                    { "@idAlianzaEstrategica", oRenovacion.IdAlianzaEstrategica },
                    { "@fechaInicio", oRenovacion.FechaInicio },
                    { "@fechaFin", MPP_CatalogoComercial.Nulo(oRenovacion.FechaFin) },
                    { "@alcance", MPP_CatalogoComercial.Nulo(oRenovacion.Alcance) },
                    { "@beneficiosAsociados", MPP_CatalogoComercial.Nulo(oRenovacion.BeneficiosAsociados) },
                    { "@idUsuarioInternoResponsable", MPP_CatalogoComercial.Nulo(oRenovacion.IdUsuarioInternoResponsable) },
                    { "@observacion", MPP_CatalogoComercial.Nulo(oRenovacion.Observacion) },
                    { "@reactivar", oRenovacion.Reactivar },
                    { "@idUsuarioInterno", oUsuario.IdUsuarioInterno }
                });
        }

        public List<VigenciaAlianza> ListarVigencias(AlianzaEstrategica oAlianza)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_AlianzaEstrategica_ListarVigencias",
                new Hashtable { { "@idAlianzaEstrategica", oAlianza.IdAlianzaEstrategica } });

            List<VigenciaAlianza> lista = new List<VigenciaAlianza>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new VigenciaAlianza
                {
                    IdAlianzaVigencia = Convert.ToInt32(fila["idAlianzaVigencia"]),
                    FechaInicioAnterior = Convert.ToDateTime(fila["fechaInicioAnterior"]),
                    FechaFinAnterior = MPP_CatalogoComercial.Fecha(fila["fechaFinAnterior"]),
                    FechaInicioNueva = Convert.ToDateTime(fila["fechaInicioNueva"]),
                    FechaFinNueva = MPP_CatalogoComercial.Fecha(fila["fechaFinNueva"]),
                    Observacion = MPP_CatalogoComercial.Texto(fila["observacion"]),
                    NombreUsuario = MPP_CatalogoComercial.Texto(fila["nombreUsuario"]),
                    FechaRegistro = Convert.ToDateTime(fila["fechaRegistro"])
                });
            }
            return lista;
        }

        // A10 (Finalizada) y A11 (DadaDeBaja).
        public void Cerrar(AlianzaEstrategica oAlianza, SeguimientoAlianza oCierre)
        {
            Conexion.Instance.Guardar(
                "sp_AlianzaEstrategica_Cerrar",
                new Hashtable
                {
                    { "@idAlianzaEstrategica", oAlianza.IdAlianzaEstrategica },
                    { "@estadoNuevo", oCierre.EstadoResultante },
                    { "@observacion", MPP_CatalogoComercial.Nulo(oCierre.Observacion) },
                    { "@idUsuarioInterno", oCierre.IdUsuarioInternoRegistro }
                });
        }

        private static AlianzaEstrategica Mapear(DataRow fila)
        {
            return new AlianzaEstrategica
            {
                IdAlianzaEstrategica = Convert.ToInt32(fila["idAlianzaEstrategica"]),
                NombreEntidad = fila["nombreEntidad"].ToString(),
                TipoInstitucion = fila["tipoInstitucion"].ToString(),
                DatosContacto = fila["datosContacto"].ToString(),
                IdUsuarioInternoResponsable = Convert.ToInt32(fila["idUsuarioInternoResponsable"]),
                NombreResponsable = MPP_CatalogoComercial.Texto(fila["nombreResponsable"]),
                ObjetivoComercial = fila["objetivoComercial"].ToString(),
                Alcance = fila["alcance"].ToString(),
                DescripcionAcuerdo = fila["descripcionAcuerdo"].ToString(),
                BeneficiosAsociados = fila["beneficiosAsociados"].ToString(),
                FechaInicio = Convert.ToDateTime(fila["fechaInicio"]),
                FechaFin = MPP_CatalogoComercial.Fecha(fila["fechaFin"]),
                EstadoAlianza = fila["estadoAlianza"].ToString(),
                Observaciones = MPP_CatalogoComercial.Texto(fila["observaciones"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                FechaBaja = MPP_CatalogoComercial.Fecha(fila["fechaBaja"]),
                Activa = Convert.ToBoolean(fila["activa"]),
                CantidadContactos = Convert.ToInt32(fila["cantidadContactos"]),
                CantidadCampanias = Convert.ToInt32(fila["cantidadCampanias"]),
                CantidadContenidos = Convert.ToInt32(fila["cantidadContenidos"]),
                CantidadOrigenes = Convert.ToInt32(fila["cantidadOrigenes"])
            };
        }
    }
}
