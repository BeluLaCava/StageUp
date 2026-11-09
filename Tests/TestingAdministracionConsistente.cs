using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BE.Permisos;
using StageUp.BLL;
using StageUp.MPP;
using StageUp.Seguridad;

class TestingAdministracionConsistente
{
    static string cs = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int checks, admin, area, permisoUsuarios, permisoRoles, permisoOtro;
    static BLL_PermisoInterno permisos = new BLL_PermisoInterno();
    static object Sql(string s, params object[] p)
    {
        using (var c = new SqlConnection(cs)) using (var q = new SqlCommand(s, c))
        {
            for (int i = 0; i < p.Length; i++) q.Parameters.AddWithValue("@p" + i, p[i] ?? DBNull.Value);
            c.Open(); return q.ExecuteScalar();
        }
    }
    static int N(string s, params object[] p) { return Convert.ToInt32(Sql(s, p)); }
    static bool Bit(string s, params object[] p) { return Convert.ToBoolean(Sql(s, p)); }
    static void Check(bool ok, string nombre) { if (!ok) throw new Exception("FAIL: " + nombre); checks++; Console.WriteLine("PASS: " + nombre); }
    static string Nombre() { return "QA " + Guid.NewGuid().ToString("N"); }
    static string Correo() { return Guid.NewGuid().ToString("N") + "@example.test"; }
    static void Fallo() { Sql("CREATE OR ALTER TRIGGER dbo.QA_AdminConsistenteAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END"); }
    static void Quitar() { Sql("IF OBJECT_ID('dbo.QA_AdminConsistenteAudit','TR') IS NOT NULL DROP TRIGGER dbo.QA_AdminConsistenteAudit"); }
    static int Audits(int id, string entidad) { return N("SELECT COUNT(*) FROM dbo.RegistroActividad WHERE idEntidadAfectada=@p0 AND tipoEntidadAfectada=@p1", id, entidad); }
    static UsuarioExterno Externo(string perfil = "ExternoSolicitante")
    {
        var u = new UsuarioExterno { Nombre = "QA", Apellido = "Pruebas", CorreoElectronico = Correo(), EstadoCuenta = "Activa", PerfilUsuario = perfil, AceptaTerminos = true, AceptaPoliticaPrivacidad = true, FechaAceptacionTerminos = DateTime.Now };
        ProtectorDeCredenciales.ProtegerPassword(u, "QaPrueba2026"); u.IdUsuarioExterno = new MPP_UsuarioExterno().Insertar(u); return u;
    }
    static SolicitudHabilitacionGestor Datos(UsuarioExterno u)
    {
        return new SolicitudHabilitacionGestor { NombreResponsable = "QA Pruebas", DocumentoResponsable = "30123456", TelefonoContacto = "1144445555", CorreoContacto = u.CorreoElectronico, CondicionFiscal = "ConsumidorFinal", NombreEspacio = "QA Espacio", TipoEspacio = "Sala", Provincia = "Buenos Aires", Ciudad = "CABA", DescripcionPropuesta = "Espacio de prueba para revision aislada de habilitaciones." };
    }
    static string Perfil(UsuarioExterno u) { return Convert.ToString(Sql("SELECT perfilUsuario FROM dbo.UsuarioExterno WHERE idUsuarioExterno=@p0", u.IdUsuarioExterno)); }
    static string EstadoSolicitud(int id) { return Convert.ToString(Sql("SELECT estado FROM dbo.SolicitudHabilitacionGestor WHERE idSolicitud=@p0", id)); }
    static int Avisos(UsuarioExterno u) { return N("SELECT COUNT(*) FROM dbo.Notificacion WHERE idUsuarioExterno=@p0", u.IdUsuarioExterno); }
    static void Habilitaciones()
    {
        var b = new BLL_SolicitudHabilitacionGestor(); var u = Externo();
        Fallo(); try
        {
            Check(!b.Registrar(u.IdUsuarioExterno, Datos(u)).Exitoso, "solicitud informa fallo de auditoria");
            Check(N("SELECT COUNT(*) FROM dbo.SolicitudHabilitacionGestor WHERE idUsuarioExterno=@p0", u.IdUsuarioExterno) == 0 && Perfil(u) == "ExternoSolicitante", "solicitud fallida revierte registro y perfil");
        } finally { Quitar(); }
        var alta = b.Registrar(u.IdUsuarioExterno, Datos(u)); Check(alta.Exitoso && Perfil(u) == "PendienteHabilitacionGestor", "solicitud normal permite reintentar");
        Check(!b.Registrar(u.IdUsuarioExterno, Datos(u)).Exitoso, "se conserva bloqueo de solicitud duplicada");
        int audit = Audits(alta.Valor, "SolicitudHabilitacionGestor");
        Fallo(); try
        {
            Check(!b.Aprobar(alta.Valor, admin).Exitoso, "aprobacion informa fallo de auditoria");
            Check(EstadoSolicitud(alta.Valor) == "PendienteRevision" && Perfil(u) == "PendienteHabilitacionGestor" && Avisos(u) == 0 && Audits(alta.Valor, "SolicitudHabilitacionGestor") == audit, "aprobacion fallida conserva estado perfil avisos y auditoria");
        } finally { Quitar(); }
        Check(b.Aprobar(alta.Valor, admin).Exitoso && Perfil(u) == "GestorEspacios" && Avisos(u) == 1, "aprobacion normal guarda perfil y notificacion");
        Check(!b.Aprobar(alta.Valor, admin).Exitoso && Avisos(u) == 1, "repetir aprobacion no duplica efectos");
        var rechazo = Externo(); int idRechazo = b.Registrar(rechazo.IdUsuarioExterno, Datos(rechazo)).Valor;
        Check(!b.Rechazar(idRechazo, "no", admin).Exitoso, "rechazo conserva longitud minima del motivo");
        Fallo(); try
        {
            Check(!b.Rechazar(idRechazo, "Motivo de prueba suficiente", admin).Exitoso && EstadoSolicitud(idRechazo) == "PendienteRevision" && Perfil(rechazo) == "PendienteHabilitacionGestor" && Avisos(rechazo) == 0, "rechazo fallido no cambia solicitud perfil ni avisos");
        } finally { Quitar(); }
        Check(b.Rechazar(idRechazo, "Motivo de prueba suficiente", admin).Exitoso && Perfil(rechazo) == "ExternoSolicitante" && Avisos(rechazo) == 1, "rechazo normal conserva flujo y notificacion");
        Check(b.Registrar(rechazo.IdUsuarioExterno, Datos(rechazo)).Exitoso, "solicitante rechazado puede volver a solicitar");
        var baja = Externo(); int idBaja = b.Registrar(baja.IdUsuarioExterno, Datos(baja)).Valor;
        Check(new BLL_UsuarioExterno().DarDeBajaCuenta(baja.IdUsuarioExterno).Exitoso, "baja de cuenta con solicitud pendiente conserva flujo");
        var denegado = b.Aprobar(idBaja, admin);
        Check(!denegado.Exitoso && denegado.Mensaje.Contains("ya no está activa"), "aprobacion de cuenta dada de baja muestra motivo correcto");
        Check(EstadoSolicitud(idBaja) == "PendienteRevision" && Perfil(baja) == "PendienteHabilitacionGestor" && Avisos(baja) == 0, "cuenta inactiva no obtiene perfil de gestor ni aviso de aprobacion");
        Check(!b.Rechazar(idBaja, "Motivo de prueba suficiente", admin).Exitoso, "resolucion de cuenta inactiva se rechaza coherentemente");
        Check(new MPP_SolicitudHabilitacionGestor().Resolver(new SolicitudHabilitacionGestor { IdSolicitud = idBaja, IdUsuarioInternoRevisor = admin }, true) == "CUENTA_NO_ACTIVA", "procedimiento valida cuenta activa por si mismo");
        Check(!b.Aprobar(int.MaxValue, admin).Exitoso, "solicitud inexistente no modifica datos");
        var simultanea = Externo(); int id = b.Registrar(simultanea.IdUsuarioExterno, Datos(simultanea)).Valor;
        var resultados = Paralelo(() => b.Aprobar(id, admin), () => b.Rechazar(id, "Motivo de prueba suficiente", admin));
        Check(resultados.Count(r => r.Exitoso) == 1 && Avisos(simultanea) == 1, "aprobar y rechazar simultaneamente solo confirma una resolucion");
    }
    static void Espacios()
    {
        var gestor = Externo("GestorEspacios"); var b = new BLL_EspacioArtistico(); string nombre = Nombre();
        Fallo(); try { Check(!b.Registrar(gestor.IdUsuarioExterno, nombre, "QA", "Sala").Exitoso && N("SELECT COUNT(*) FROM dbo.EspacioArtistico WHERE nombreEspacio=@p0", nombre) == 0, "alta de espacio revierte ante fallo de auditoria"); } finally { Quitar(); }
        var alta = b.Registrar(gestor.IdUsuarioExterno, nombre, "QA", "Sala"); Check(alta.Exitoso, "alta normal de espacio"); int id = alta.Valor;
        Fallo(); try
        {
            Check(!b.Modificar(id, gestor.IdUsuarioExterno, "QA Modificado", "QA", "Sala").Exitoso && Convert.ToString(Sql("SELECT nombreEspacio FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id)) == nombre, "edicion de espacio revierte ante fallo de auditoria");
            Check(!b.Publicar(id, gestor.IdUsuarioExterno).Exitoso && !Bit("SELECT publicado FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id), "publicacion fallida no queda publicada");
        } finally { Quitar(); }
        Check(!b.Publicar(id, Externo().IdUsuarioExterno).Exitoso, "publicacion conserva control de propietario");
        Check(b.Publicar(id, gestor.IdUsuarioExterno).Exitoso && Bit("SELECT publicado FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id), "publicacion normal funciona");
        Fallo(); try { Check(!b.Pausar(id, gestor.IdUsuarioExterno).Exitoso && Bit("SELECT publicado FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id), "pausa fallida conserva publicacion anterior"); } finally { Quitar(); }
        Check(b.Pausar(id, gestor.IdUsuarioExterno).Exitoso && !Bit("SELECT publicado FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id), "pausa normal funciona");
        Fallo(); try { Check(!b.DarDeBaja(id, gestor.IdUsuarioExterno).Exitoso && Bit("SELECT activo FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id), "baja fallida conserva espacio activo"); } finally { Quitar(); }
        Check(b.DarDeBaja(id, gestor.IdUsuarioExterno).Exitoso && !Bit("SELECT activo FROM dbo.EspacioArtistico WHERE idEspacioArtistico=@p0", id), "baja normal conserva historial");
    }
    static RolInterno Rol(int id) { return new MPP_RolInterno().ObtenerPorId(new RolInterno { IdRolInterno = id }); }
    static int CrearRol(params int[] ids) { return new MPP_RolInterno().GuardarConPermisos(new RolInterno { NombreRol = Nombre(), EstadoRol = "Activo" }, ids); }
    static UsuarioInterno Interno(int rol)
    {
        var u = new UsuarioInterno { Nombre = "QA", Apellido = "Interno", IdAreaInterna = area, IdRolInterno = rol, EstadoCuenta = "Activa", CorreoElectronico = Correo() };
        ProtectorDeCredenciales.ProtegerPassword(u, "QaPrueba2026"); u.IdUsuarioInterno = new MPP_UsuarioInterno().Insertar(u); return u;
    }
    static int Administradores()
    {
        return new MPP_UsuarioInterno().Listar().Count(u => u.Activo && u.EstadoCuenta == "Activa" && new[] { "GESTIONAR_USUARIOS_INTERNOS", "GESTIONAR_ROLES" }.All(p => permisos.ListarCodigosPermisosVigentesDeRol(u.IdRolInterno).Contains(p)));
    }
    static ResultadoOperacion[] Paralelo(Func<ResultadoOperacion> primera, Func<ResultadoOperacion> segunda)
    {
        using (var inicio = new ManualResetEventSlim(false))
        {
            var a = Task.Run(() => { inicio.Wait(); return primera(); }); var b = Task.Run(() => { inicio.Wait(); return segunda(); });
            inicio.Set(); Task.WaitAll(a, b); return new[] { a.Result, b.Result };
        }
    }
    static void RolesYUsuarios()
    {
        int rol = CrearRol(permisoOtro); var b = new BLL_RolInterno(); var antes = Rol(rol); var nuevo = Rol(rol); nuevo.NombreRol = Nombre();
        permisos.ListarCodigosPermisosDeRol(rol);
        Fallo(); try
        {
            Check(!b.Guardar(nuevo, new List<int> { permisoUsuarios, permisoRoles }, admin).Exitoso && Rol(rol).NombreRol == antes.NombreRol, "edicion de rol y nombre revierte ante fallo de auditoria");
            Check(!permisos.ListarCodigosPermisosDeRol(rol).Contains("GESTIONAR_ROLES") && !permisos.ListarCodigosPermisosVigentesDeRol(rol).Contains("GESTIONAR_ROLES"), "permisos y cache conservan asignacion anterior al fallo");
        } finally { Quitar(); }
        Check(b.Guardar(nuevo, new List<int> { permisoUsuarios, permisoRoles }, admin).Exitoso && permisos.ListarCodigosPermisosDeRol(rol).Contains("GESTIONAR_ROLES"), "rol normal confirma permisos e invalida cache");
        string correo = Correo(); var usuarios = new BLL_UsuarioInterno();
        Fallo(); try { Check(!usuarios.Registrar("QA", "Interno", correo, area, rol, "Activa", "QaPrueba2026", "QaPrueba2026", admin).Exitoso && N("SELECT COUNT(*) FROM dbo.UsuarioInterno WHERE correoElectronico=@p0", correo) == 0, "alta interna revierte ante fallo de auditoria"); } finally { Quitar(); }
        var alta = usuarios.Registrar("QA", "Interno", correo, area, rol, "Activa", "QaPrueba2026", "QaPrueba2026", admin); Check(alta.Exitoso, "alta interna normal");
        Fallo(); try
        {
            Check(!usuarios.Modificar(alta.Valor, "QA Modificado", "Interno", correo, area, rol, "Activa", null, null, admin).Exitoso && Convert.ToString(Sql("SELECT nombre FROM dbo.UsuarioInterno WHERE idUsuarioInterno=@p0", alta.Valor)) == "QA", "edicion interna revierte ante fallo de auditoria");
            Check(!usuarios.DarDeBaja(alta.Valor, admin).Exitoso && Bit("SELECT activo FROM dbo.UsuarioInterno WHERE idUsuarioInterno=@p0", alta.Valor), "baja interna revierte ante fallo de auditoria");
        } finally { Quitar(); }
        Check(usuarios.Modificar(alta.Valor, "QA Modificado", "Interno", correo, area, rol, "Activa", null, null, admin).Exitoso, "edicion interna normal");
        Check(!usuarios.DarDeBaja(admin, admin).Exitoso, "se conserva prohibicion de baja propia");
        Check(!b.DarDeBaja(rol, admin).Exitoso, "rol con usuario activo sigue protegido");
        Check(usuarios.DarDeBaja(alta.Valor, admin).Exitoso, "baja interna valida funciona");
        Fallo(); try { Check(!b.DarDeBaja(rol, admin).Exitoso && Rol(rol).Activo, "baja de rol revierte ante fallo de auditoria"); } finally { Quitar(); }
        Check(b.DarDeBaja(rol, admin).Exitoso && !Rol(rol).Activo, "baja normal de rol sin usuarios activos");
    }
    static void Grupos()
    {
        string nombre = Nombre(); Fallo(); try { Check(!permisos.CrearGrupo(null, nombre, admin).Exitoso && N("SELECT COUNT(*) FROM dbo.ComponentePermiso WHERE nombre=@p0", nombre) == 0, "alta de grupo revierte ante fallo de auditoria"); } finally { Quitar(); }
        int grupo = permisos.CrearGrupo(null, Nombre(), admin).Valor, destino = permisos.CrearGrupo(null, Nombre(), admin).Valor;
        permisos.MoverComponente(permisoUsuarios, grupo, admin); permisos.MoverComponente(permisoRoles, grupo, admin);
        int rol = CrearRol(grupo); var unico = Interno(rol);
        Sql("UPDATE dbo.UsuarioInterno SET activo=0,estadoCuenta=N'Inactiva' WHERE idUsuarioInterno<>@p0", unico.IdUsuarioInterno);
        Check(Administradores() == 1, "fixture con un administrador por grupo");
        permisos.ListarCodigosPermisosDeRol(rol);
        var bloqueado = permisos.MoverComponente(permisoUsuarios, destino, unico.IdUsuarioInterno);
        Check(!bloqueado.Exitoso && bloqueado.Mensaje.Contains("sin ningún administrador") && Administradores() == 1, "mover permiso fuera del ultimo grupo administrador se rechaza");
        Check(N("SELECT idComponentePadre FROM dbo.ComponentePermiso WHERE idComponentePermiso=@p0", permisoUsuarios) == grupo && permisos.ListarCodigosPermisosDeRol(rol).Contains("GESTIONAR_USUARIOS_INTERNOS"), "movimiento rechazado conserva arbol y cache");
        int subgrupo = permisos.CrearGrupo(grupo, Nombre(), unico.IdUsuarioInterno).Valor;
        Check(permisos.MoverComponente(permisoUsuarios, subgrupo, unico.IdUsuarioInterno).Exitoso && Administradores() == 1, "reorganizacion dentro del grupo sigue permitida");
        Check(!permisos.MoverComponente(grupo, subgrupo, unico.IdUsuarioInterno).Exitoso, "se conserva prevencion de ciclos de grupos");
        Check(!permisos.EliminarGrupo(grupo, unico.IdUsuarioInterno).Exitoso, "grupo con hijos no se elimina");
        Fallo(); try { Check(!permisos.MoverComponente(permisoUsuarios, grupo, unico.IdUsuarioInterno).Exitoso && N("SELECT idComponentePadre FROM dbo.ComponentePermiso WHERE idComponentePermiso=@p0", permisoUsuarios) == subgrupo, "movimiento revierte ante fallo de auditoria"); } finally { Quitar(); }
        var respaldo = Interno(CrearRol(permisoUsuarios, permisoRoles));
        Check(permisos.MoverComponente(permisoUsuarios, destino, respaldo.IdUsuarioInterno).Exitoso && Administradores() == 1, "movimiento que conserva otro administrador esta permitido");
        Check(!permisos.ListarCodigosPermisosDeRol(rol).Contains("GESTIONAR_USUARIOS_INTERNOS"), "movimiento confirmado actualiza cache de permisos");
        int vacio = permisos.CrearGrupo(null, Nombre(), respaldo.IdUsuarioInterno).Valor;
        Fallo(); try { Check(!permisos.EliminarGrupo(vacio, respaldo.IdUsuarioInterno).Exitoso && Bit("SELECT activo FROM dbo.ComponentePermiso WHERE idComponentePermiso=@p0", vacio), "eliminacion de grupo revierte ante fallo de auditoria"); } finally { Quitar(); }
        Check(permisos.EliminarGrupo(vacio, respaldo.IdUsuarioInterno).Exitoso, "eliminacion normal de grupo vacio");
        admin = respaldo.IdUsuarioInterno;
    }
    static void Concurrencia()
    {
        int ra = CrearRol(permisoUsuarios, permisoRoles), rb = CrearRol(permisoUsuarios, permisoRoles);
        var a = Interno(ra); var b = Interno(rb); var roles = new BLL_RolInterno();
        Action reiniciar = () =>
        {
            Sql("UPDATE dbo.UsuarioInterno SET activo=0,estadoCuenta=N'Inactiva'; UPDATE dbo.UsuarioInterno SET activo=1,estadoCuenta=N'Activa' WHERE idUsuarioInterno IN(@p0,@p1)", a.IdUsuarioInterno, b.IdUsuarioInterno);
            roles.Guardar(Rol(ra), new List<int> { permisoUsuarios, permisoRoles }, a.IdUsuarioInterno);
            roles.Guardar(Rol(rb), new List<int> { permisoUsuarios, permisoRoles }, a.IdUsuarioInterno);
        };
        reiniciar();
        string proc = Convert.ToString(Sql("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_UsuarioInterno_Baja'))"));
        string alter = Regex.Replace(proc, @"(?i)CREATE\s+(?:OR\s+ALTER\s+)?PROCEDURE", "ALTER PROCEDURE");
        Sql(alter.Replace("UPDATE dbo.UsuarioInterno", "WAITFOR DELAY '00:00:01'; UPDATE dbo.UsuarioInterno"));
        try
        {
            var r = Paralelo(() => new BLL_UsuarioInterno().DarDeBaja(a.IdUsuarioInterno, b.IdUsuarioInterno), () => new BLL_UsuarioInterno().DarDeBaja(b.IdUsuarioInterno, a.IdUsuarioInterno));
            Check(r.Count(x => x.Exitoso) == 1 && Administradores() == 1, "dos bajas simultaneas conservan un administrador");
        } finally { Sql(alter); }
        reiniciar();
        var rr = Paralelo(() => roles.Guardar(Rol(ra), new List<int> { permisoOtro }, a.IdUsuarioInterno), () => roles.Guardar(Rol(rb), new List<int> { permisoOtro }, b.IdUsuarioInterno));
        Check(rr.Count(x => x.Exitoso) == 1 && Administradores() == 1, "dos cambios de roles simultaneos conservan un administrador");
        reiniciar();
        var mixto = Paralelo(() => roles.Guardar(Rol(ra), new List<int> { permisoOtro }, a.IdUsuarioInterno), () => new BLL_UsuarioInterno().DarDeBaja(b.IdUsuarioInterno, a.IdUsuarioInterno));
        Check(mixto.Count(x => x.Exitoso) == 1 && Administradores() == 1, "cambio de rol y baja simultaneos comparten proteccion");
        reiniciar();
        int grupo = permisos.CrearGrupo(null, Nombre(), a.IdUsuarioInterno).Valor, destino = permisos.CrearGrupo(null, Nombre(), a.IdUsuarioInterno).Valor;
        permisos.MoverComponente(permisoUsuarios, grupo, a.IdUsuarioInterno); permisos.MoverComponente(permisoRoles, grupo, a.IdUsuarioInterno);
        Check(roles.Guardar(Rol(ra), new List<int> { grupo }, a.IdUsuarioInterno).Exitoso, "asignacion de grupo conserva permisos equivalentes");
        var combinado = Paralelo(() => permisos.MoverComponente(permisoUsuarios, destino, a.IdUsuarioInterno), () => roles.Guardar(Rol(rb), new List<int> { permisoOtro }, b.IdUsuarioInterno));
        Check(combinado.Count(x => x.Exitoso) == 1 && Administradores() == 1, "movimiento de grupo y cambio de rol simultaneos conservan administrador");
    }
    static int Main()
    {
        if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_")) throw new Exception("Solo copia QA");
        Environment.SetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION", Convert.ToBase64String(new byte[32]));
        try
        {
            admin = N("SELECT TOP 1 idUsuarioInterno FROM dbo.UsuarioInterno WHERE activo=1 AND estadoCuenta=N'Activa' ORDER BY idUsuarioInterno");
            area = N("SELECT TOP 1 idAreaInterna FROM dbo.AreaInterna WHERE activo=1");
            permisoUsuarios = N("SELECT idComponentePermiso FROM dbo.ComponentePermiso WHERE codigoPermiso=N'GESTIONAR_USUARIOS_INTERNOS' AND activo=1");
            permisoRoles = N("SELECT idComponentePermiso FROM dbo.ComponentePermiso WHERE codigoPermiso=N'GESTIONAR_ROLES' AND activo=1");
            permisoOtro = N("SELECT TOP 1 idComponentePermiso FROM dbo.ComponentePermiso WHERE codigoPermiso IS NOT NULL AND activo=1 AND codigoPermiso NOT IN(N'GESTIONAR_USUARIOS_INTERNOS',N'GESTIONAR_ROLES')");
            Habilitaciones(); Espacios(); RolesYUsuarios(); Grupos(); Concurrencia();
            Console.WriteLine(checks + " comprobaciones de administracion pasaron en QA."); return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
        finally { Quitar(); }
    }
}
