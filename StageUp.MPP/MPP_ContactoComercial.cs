using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using StageUp.BE.Entidades;
using StageUp.DAL;

namespace StageUp.MPP
{
    // CU-004-002 Contactos comerciales y su seguimiento (script 80).
    public class MPP_ContactoComercial
    {
        public List<ContactoComercial> Listar(FiltroContactoComercial oFiltro)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ContactoComercial_Listar",
                new Hashtable
                {
                    { "@estado", MPP_CatalogoComercial.Nulo(oFiltro.Estado) },
                    { "@tipoEntidad", MPP_CatalogoComercial.Nulo(oFiltro.TipoEntidad) },
                    { "@idOrigenUsuario", MPP_CatalogoComercial.Nulo(oFiltro.IdOrigenUsuario) },
                    { "@idCampaniaComercial", MPP_CatalogoComercial.Nulo(oFiltro.IdCampaniaComercial) },
                    { "@texto", MPP_CatalogoComercial.Nulo(oFiltro.Texto) }
                });

            List<ContactoComercial> lista = new List<ContactoComercial>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public ResumenContactos Contar()
        {
            DataTable tabla = Conexion.Instance.Leer("sp_ContactoComercial_Contar");
            DataRow fila = tabla.Rows[0];
            return new ResumenContactos
            {
                Total = MPP_CatalogoComercial.Entero(fila["total"]) ?? 0,
                Pendientes = MPP_CatalogoComercial.Entero(fila["pendientes"]) ?? 0,
                Contactados = MPP_CatalogoComercial.Entero(fila["contactados"]) ?? 0,
                Interesados = MPP_CatalogoComercial.Entero(fila["interesados"]) ?? 0,
                Rechazados = MPP_CatalogoComercial.Entero(fila["rechazados"]) ?? 0,
                Incorporados = MPP_CatalogoComercial.Entero(fila["incorporados"]) ?? 0
            };
        }

        public ContactoComercial Obtener(ContactoComercial oContacto)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ContactoComercial_Obtener",
                new Hashtable { { "@idContactoComercial", oContacto.IdContactoComercial } });

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public PosibleContactoDuplicado BuscarDuplicado(ContactoComercial oContacto)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_ContactoComercial_BuscarDuplicado",
                new Hashtable
                {
                    { "@idContactoExcluido", oContacto.IdContactoComercial > 0 ? (object)oContacto.IdContactoComercial : DBNull.Value },
                    { "@correoElectronico", MPP_CatalogoComercial.Nulo(oContacto.CorreoElectronico) },
                    { "@telefonoNormalizado", MPP_CatalogoComercial.Nulo(oContacto.TelefonoNormalizado) },
                    { "@nombreContacto", oContacto.NombreContacto },
                    { "@apellidoContacto", MPP_CatalogoComercial.Nulo(oContacto.ApellidoContacto) },
                    { "@entidadRepresentada", MPP_CatalogoComercial.Nulo(oContacto.EntidadRepresentada) }
                });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            return new PosibleContactoDuplicado
            {
                IdContactoComercial = Convert.ToInt32(tabla.Rows[0]["idContactoComercial"]),
                Nombre = tabla.Rows[0]["nombre"].ToString(),
                Coincidencia = tabla.Rows[0]["coincidencia"].ToString()
            };
        }

        public int Guardar(ContactoComercial oContacto, UsuarioInterno oResponsable)
        {
            object resultado = Conexion.Instance.LeerEscalar(
                "sp_ContactoComercial_Guardar",
                new Hashtable
                {
                    { "@idContactoComercial", oContacto.IdContactoComercial > 0 ? (object)oContacto.IdContactoComercial : DBNull.Value },
                    { "@nombreContacto", oContacto.NombreContacto },
                    { "@apellidoContacto", MPP_CatalogoComercial.Nulo(oContacto.ApellidoContacto) },
                    { "@entidadRepresentada", MPP_CatalogoComercial.Nulo(oContacto.EntidadRepresentada) },
                    { "@tipoEntidad", oContacto.TipoEntidad },
                    { "@medioContacto", oContacto.MedioContacto },
                    { "@correoElectronico", MPP_CatalogoComercial.Nulo(oContacto.CorreoElectronico) },
                    { "@telefono", MPP_CatalogoComercial.Nulo(oContacto.Telefono) },
                    { "@telefonoNormalizado", MPP_CatalogoComercial.Nulo(oContacto.TelefonoNormalizado) },
                    { "@otroDatoContacto", MPP_CatalogoComercial.Nulo(oContacto.OtroDatoContacto) },
                    { "@idOrigenUsuario", oContacto.IdOrigenUsuario },
                    { "@observaciones", oContacto.Observaciones },
                    { "@estadoContacto", oContacto.EstadoContacto },
                    { "@idCampaniaComercial", MPP_CatalogoComercial.Nulo(oContacto.IdCampaniaComercial) },
                    { "@idAlianzaEstrategica", MPP_CatalogoComercial.Nulo(oContacto.IdAlianzaEstrategica) },
                    { "@idUsuarioExterno", MPP_CatalogoComercial.Nulo(oContacto.IdUsuarioExterno) },
                    { "@observacionCambio", MPP_CatalogoComercial.Nulo(oContacto.ObservacionCambioEstado) },
                    { "@idUsuarioInterno", oResponsable.IdUsuarioInterno }
                });

            return Convert.ToInt32(resultado);
        }

        // Devuelve el estado que quedó en el contacto.
        public string RegistrarSeguimiento(SeguimientoContacto oSeguimiento)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SeguimientoContacto_Insertar",
                new Hashtable
                {
                    { "@idContactoComercial", oSeguimiento.IdContactoComercial },
                    { "@fechaSeguimiento", oSeguimiento.FechaSeguimiento },
                    { "@tipoSeguimiento", oSeguimiento.TipoSeguimiento },
                    { "@descripcion", oSeguimiento.Descripcion },
                    { "@resultado", MPP_CatalogoComercial.Nulo(oSeguimiento.Resultado) },
                    { "@interesManifestado", MPP_CatalogoComercial.Nulo(oSeguimiento.InteresManifestado) },
                    { "@proximaAccion", MPP_CatalogoComercial.Nulo(oSeguimiento.ProximaAccion) },
                    { "@fechaProximaAccion", MPP_CatalogoComercial.Nulo(oSeguimiento.FechaProximaAccion) },
                    { "@estadoResultante", MPP_CatalogoComercial.Nulo(oSeguimiento.EstadoResultante) },
                    { "@idUsuarioInterno", oSeguimiento.IdUsuarioInternoResponsable }
                });

            return tabla.Rows.Count == 0 ? null : tabla.Rows[0]["estadoActual"].ToString();
        }

        public List<SeguimientoContacto> ListarSeguimientos(ContactoComercial oContacto)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_SeguimientoContacto_Listar",
                new Hashtable { { "@idContactoComercial", oContacto.IdContactoComercial } });

            List<SeguimientoContacto> lista = new List<SeguimientoContacto>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new SeguimientoContacto
                {
                    IdSeguimientoContacto = Convert.ToInt32(fila["idSeguimientoContacto"]),
                    IdContactoComercial = Convert.ToInt32(fila["idContactoComercial"]),
                    FechaSeguimiento = Convert.ToDateTime(fila["fechaSeguimiento"]),
                    TipoSeguimiento = fila["tipoSeguimiento"].ToString(),
                    Descripcion = fila["descripcion"].ToString(),
                    Resultado = MPP_CatalogoComercial.Texto(fila["resultado"]),
                    InteresManifestado = MPP_CatalogoComercial.Texto(fila["interesManifestado"]),
                    ProximaAccion = MPP_CatalogoComercial.Texto(fila["proximaAccion"]),
                    FechaProximaAccion = MPP_CatalogoComercial.Fecha(fila["fechaProximaAccion"]),
                    EstadoAnterior = MPP_CatalogoComercial.Texto(fila["estadoAnterior"]),
                    EstadoResultante = MPP_CatalogoComercial.Texto(fila["estadoResultante"]),
                    IdUsuarioInternoResponsable = Convert.ToInt32(fila["idUsuarioInternoResponsable"]),
                    NombreResponsable = MPP_CatalogoComercial.Texto(fila["nombreResponsable"]),
                    FechaRegistro = Convert.ToDateTime(fila["fechaRegistro"])
                });
            }
            return lista;
        }

        public CuentaParaVincular BuscarCuenta(UsuarioExterno oUsuario)
        {
            DataTable tabla = Conexion.Instance.Leer(
                "sp_UsuarioExterno_BuscarParaVincular",
                new Hashtable { { "@correoElectronico", oUsuario.CorreoElectronico } });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            return new CuentaParaVincular
            {
                IdUsuarioExterno = Convert.ToInt32(fila["idUsuarioExterno"]),
                Nombre = fila["nombre"].ToString(),
                Apellido = fila["apellido"].ToString(),
                CorreoElectronico = fila["correoElectronico"].ToString(),
                PerfilUsuario = MPP_CatalogoComercial.Texto(fila["perfilUsuario"]),
                EstadoCuenta = MPP_CatalogoComercial.Texto(fila["estadoCuenta"])
            };
        }

        private static ContactoComercial Mapear(DataRow fila)
        {
            return new ContactoComercial
            {
                IdContactoComercial = Convert.ToInt32(fila["idContactoComercial"]),
                NombreContacto = fila["nombreContacto"].ToString(),
                ApellidoContacto = MPP_CatalogoComercial.Texto(fila["apellidoContacto"]),
                EntidadRepresentada = MPP_CatalogoComercial.Texto(fila["entidadRepresentada"]),
                TipoEntidad = fila["tipoEntidad"].ToString(),
                MedioContacto = fila["medioContacto"].ToString(),
                CorreoElectronico = MPP_CatalogoComercial.Texto(fila["correoElectronico"]),
                Telefono = MPP_CatalogoComercial.Texto(fila["telefono"]),
                OtroDatoContacto = MPP_CatalogoComercial.Texto(fila["otroDatoContacto"]),
                IdOrigenUsuario = Convert.ToInt32(fila["idOrigenUsuario"]),
                NombreOrigen = MPP_CatalogoComercial.Texto(fila["nombreOrigen"]),
                Observaciones = fila["observaciones"].ToString(),
                EstadoContacto = fila["estadoContacto"].ToString(),
                IdCampaniaComercial = MPP_CatalogoComercial.Entero(fila["idCampaniaComercial"]),
                NombreCampania = MPP_CatalogoComercial.Texto(fila["nombreCampania"]),
                IdAlianzaEstrategica = MPP_CatalogoComercial.Entero(fila["idAlianzaEstrategica"]),
                NombreAlianza = MPP_CatalogoComercial.Texto(fila["nombreAlianza"]),
                IdUsuarioExterno = MPP_CatalogoComercial.Entero(fila["idUsuarioExterno"]),
                NombreUsuarioExterno = MPP_CatalogoComercial.Texto(fila["nombreUsuarioExterno"]),
                CorreoUsuarioExterno = MPP_CatalogoComercial.Texto(fila["correoUsuarioExterno"]),
                FechaIncorporacion = MPP_CatalogoComercial.Fecha(fila["fechaIncorporacion"]),
                FechaAlta = Convert.ToDateTime(fila["fechaAlta"]),
                FechaUltimaModificacion = Convert.ToDateTime(fila["fechaUltimaModificacion"]),
                Activo = Convert.ToBoolean(fila["activo"]),
                CantidadSeguimientos = Convert.ToInt32(fila["cantidadSeguimientos"])
            };
        }
    }
}
