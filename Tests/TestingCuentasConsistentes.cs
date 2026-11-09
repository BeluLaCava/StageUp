using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.MPP;
using StageUp.Seguridad;

class TestingCuentasConsistentes
{
    static string cs = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int checks;
    static BLL_UsuarioExterno b = new BLL_UsuarioExterno();
    static object Sql(string texto, params object[] valores)
    {
        using (var c = new SqlConnection(cs)) using (var q = new SqlCommand(texto, c))
        {
            for (int i = 0; i < valores.Length; i++) q.Parameters.AddWithValue("@p" + i, valores[i] ?? DBNull.Value);
            c.Open(); return q.ExecuteScalar();
        }
    }
    static int N(string texto, params object[] valores) { return Convert.ToInt32(Sql(texto, valores)); }
    static void Check(bool ok, string nombre) { if (!ok) throw new Exception("FAIL: " + nombre); checks++; Console.WriteLine("PASS: " + nombre); }
    static void Fallo() { Sql("CREATE OR ALTER TRIGGER dbo.QA_CuentasAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END"); }
    static void Quitar() { Sql("IF OBJECT_ID('dbo.QA_CuentasAudit','TR') IS NOT NULL DROP TRIGGER dbo.QA_CuentasAudit"); }
    static UsuarioExterno Nuevo(string estado = "Activa")
    {
        var u = new UsuarioExterno { Nombre = "QA", Apellido = "Cuentas", CorreoElectronico = Correo(), EstadoCuenta = estado, PerfilUsuario = "ExternoSolicitante", AceptaTerminos = true, AceptaPoliticaPrivacidad = true, FechaAceptacionTerminos = DateTime.Now };
        ProtectorDeCredenciales.ProtegerPassword(u, "QaPrueba2026"); u.IdUsuarioExterno = new MPP_UsuarioExterno().Insertar(u); return Leer(u);
    }
    static string Correo() { return "qa-" + Guid.NewGuid().ToString("N") + "@example.test"; }
    static UsuarioExterno Leer(UsuarioExterno u) { return new MPP_UsuarioExterno().ObtenerPorId(u); }
    static int Codigo(UsuarioExterno u, string valor) { return new MPP_CodigoActivacion().Insertar(new CodigoActivacion { IdUsuarioExterno = u.IdUsuarioExterno, Codigo = valor, FechaVencimiento = DateTime.Now.AddHours(1) }); }
    static bool Usado(int id) { return Convert.ToBoolean(Sql("SELECT utilizado FROM dbo.CodigoActivacion WHERE idCodigoActivacion=@p0", id)); }
    static void Restaurar(string proc) { Sql(Regex.Replace(proc, @"(?i)CREATE\s+(?:OR\s+ALTER\s+)?PROCEDURE", "ALTER PROCEDURE")); }
    static ResultadoOperacion<int> Registrar(string correo) { return b.Registrar("QA", "Registro", correo, "QaPrueba2026", "QaPrueba2026", true, true, null); }
    static void Registro()
    {
        string correo = Correo();
        Fallo(); try { Check(!Registrar(correo).Exitoso, "registro informa el fallo de auditoria"); Check(N("SELECT COUNT(*) FROM dbo.UsuarioExterno WHERE correoElectronico=@p0", correo) == 0, "registro fallido no deja una cuenta parcial"); } finally { Quitar(); }
        var r = Registrar(correo);
        Check(r.Exitoso && r.Valor > 0, "se puede reintentar el registro");
        Check(N("SELECT COUNT(*) FROM dbo.CodigoActivacion WHERE idUsuarioExterno=@p0 AND utilizado=0", r.Valor) == 1, "registro crea un unico codigo vigente");
        Check(r.Mensaje.Contains("no pudimos enviar"), "fallo SMTP se informa sin deshacer la cuenta completa");
        Check(!Registrar(correo).Exitoso, "registro duplicado sigue rechazado");
        string proc = Convert.ToString(Sql("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_CodigoActivacion_Insertar'))"));
        correo = Correo();
        Sql("ALTER PROCEDURE dbo.sp_CodigoActivacion_Insertar @idUsuarioExterno INT,@codigo NVARCHAR(40),@fechaVencimiento DATETIME AS BEGIN RAISERROR('QA codigo',16,1); END");
        try { Check(!Registrar(correo).Exitoso && N("SELECT COUNT(*) FROM dbo.UsuarioExterno WHERE correoElectronico=@p0", correo) == 0, "fallo al crear codigo revierte cuenta y auditoria"); } finally { Restaurar(proc); }
    }
    static void Activacion()
    {
        var u = Nuevo("PendienteActivacion"); int a = Codigo(u, "111111"), c = Codigo(u, "222222");
        Check(Usado(a) && !Usado(c), "nuevo codigo invalida el anterior");
        Check(!b.ValidarActivacion(u.IdUsuarioExterno, "111111").Exitoso, "codigo reemplazado no activa");
        Check(!b.ValidarActivacion(u.IdUsuarioExterno, "999999").Exitoso && !Usado(c), "codigo incorrecto no consume el vigente");
        string proc = Convert.ToString(Sql("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_UsuarioExterno_ActivarCuenta'))"));
        Sql("ALTER PROCEDURE dbo.sp_UsuarioExterno_ActivarCuenta @idUsuarioExterno INT,@estadoCuenta NVARCHAR(100) AS BEGIN RAISERROR('QA activar',16,1); END");
        try { Check(!b.ValidarActivacion(u.IdUsuarioExterno, "222222").Exitoso && !Usado(c) && Leer(u).EstadoCuenta == "PendienteActivacion", "fallo al activar no consume el codigo"); } finally { Restaurar(proc); }
        Fallo(); try { Check(!b.ValidarActivacion(u.IdUsuarioExterno, "222222").Exitoso && !Usado(c) && Leer(u).EstadoCuenta == "PendienteActivacion", "fallo de auditoria revierte toda la activacion"); } finally { Quitar(); }
        Check(b.ValidarActivacion(u.IdUsuarioExterno, "222222").Exitoso && Usado(c) && Leer(u).EstadoCuenta == "Activa", "activacion normal y reintento funcionan");
        Check(!b.ValidarActivacion(u.IdUsuarioExterno, "222222").Exitoso, "codigo usado no se reutiliza");
        Check(!b.ReenviarCodigoActivacion(u.IdUsuarioExterno).Exitoso, "cuenta activa no recibe otro codigo");
        Check(b.DarDeBajaCuenta(u.IdUsuarioExterno).Exitoso, "baja normal sigue funcionando");
        Check(!b.ValidarActivacion(u.IdUsuarioExterno, "111111").Exitoso && Leer(u).EstadoCuenta == "Inactiva" && !Leer(u).Activo, "codigo anterior no reactiva una cuenta dada de baja");
        Check(!b.ReenviarCodigoActivacion(u.IdUsuarioExterno).Exitoso, "cuenta dada de baja no recibe codigo");
        bool rechazo = false; try { Codigo(u, "333333"); } catch { rechazo = true; } Check(rechazo, "procedimiento tambien rechaza emitir codigo a una cuenta inactiva");
        var vencida = Nuevo("PendienteActivacion"); int v = Codigo(vencida, "444444"); Sql("UPDATE dbo.CodigoActivacion SET fechaVencimiento=DATEADD(MINUTE,-1,GETDATE()) WHERE idCodigoActivacion=@p0", v);
        Check(!b.ValidarActivacion(vencida.IdUsuarioExterno, "444444").Exitoso && !Usado(v), "codigo vencido no activa");
        Check(!b.ReenviarCodigoActivacion(vencida.IdUsuarioExterno).Exitoso && Usado(v), "reenvio informa fallo SMTP y reemplaza el codigo anterior");
        Check(N("SELECT COUNT(*) FROM dbo.CodigoActivacion WHERE idUsuarioExterno=@p0 AND utilizado=0", vencida.IdUsuarioExterno) == 1, "reenvio deja solo un codigo vigente");
        var simultanea = Nuevo("PendienteActivacion"); Codigo(simultanea, "555555");
        var t1 = Task.Run(() => b.ValidarActivacion(simultanea.IdUsuarioExterno, "555555"));
        var t2 = Task.Run(() => b.ValidarActivacion(simultanea.IdUsuarioExterno, "555555")); Task.WaitAll(t1, t2);
        Check((t1.Result.Exitoso ? 1 : 0) + (t2.Result.Exitoso ? 1 : 0) == 1, "dos activaciones simultaneas solo confirman una");
        Check(N("SELECT COUNT(*) FROM dbo.RegistroActividad WHERE idEntidadAfectada=@p0 AND tipoEntidadAfectada=N'UsuarioExterno' AND tipoOperacion=N'ACTIVACION'", simultanea.IdUsuarioExterno) == 1, "activacion concurrente deja una sola auditoria");
    }
    static void Contexto()
    {
        var context = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
        var state = new HttpSessionStateContainer("qa", new SessionStateItemCollection(), new HttpStaticObjectsCollection(), 30, true, HttpCookieMode.UseCookies, SessionStateMode.InProc, false);
        SessionStateUtility.AddHttpSessionStateToContext(context, state); HttpContext.Current = context;
    }
    static ResultadoOperacion<UsuarioExterno> Login(UsuarioExterno u, string password)
    {
        return (ResultadoOperacion<UsuarioExterno>)typeof(BLL_UsuarioExterno).GetMethod("IniciarSesionSinCaptcha", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(b, new object[] { u.CorreoElectronico, password });
    }
    static void Acceso()
    {
        var u = Nuevo("PendienteActivacion"); Contexto();
        try
        {
            var r = Login(u, "QaPrueba2026");
            Check(!r.Exitoso && r.CodigoAlternativo == "ACTIVACION_PENDIENTE" && r.Valor.IdUsuarioExterno == u.IdUsuarioExterno, "credenciales correctas habilitan retomar activacion");
            Check(!GestorDeSesion.EstaAutenticado(), "retomar activacion no inicia sesion");
            var incorrecto = Login(u, "no-es-la-clave"); Check(!incorrecto.Exitoso && incorrecto.Valor == null && incorrecto.CodigoAlternativo != "ACTIVACION_PENDIENTE", "clave incorrecta no entrega acceso a activacion");
            Check(!new BLL_Autenticacion().IniciarSesion(u.CorreoElectronico, "QaPrueba2026", "").IdActivacionPendiente.HasValue, "sin CAPTCHA no se habilita activacion");
            var tipo = typeof(BLL_UsuarioExterno).Assembly.GetType("StageUp.BLL.ControlIntentosAcceso");
            var ejecutar = tipo.GetMethod("Ejecutar", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(ResultadoOperacion<UsuarioExterno>));
            for (int i = 0; i < 6; i++) r = (ResultadoOperacion<UsuarioExterno>)ejecutar.Invoke(null, new object[] { u.CorreoElectronico, new Func<ResultadoOperacion<UsuarioExterno>>(() => Login(u, "QaPrueba2026")), new Func<string, ResultadoOperacion<UsuarioExterno>>(m => ResultadoOperacion<UsuarioExterno>.Error(m)) });
            Check(r.CodigoAlternativo == "ACTIVACION_PENDIENTE", "retomar con credenciales validas no suma intentos fallidos");
            Codigo(u, "666666"); Check(b.ValidarActivacion(u.IdUsuarioExterno, "666666").Exitoso && Login(u, "QaPrueba2026").Exitoso && GestorDeSesion.EstaAutenticado(), "despues de activar el ingreso normal funciona");
            Sql("UPDATE dbo.UsuarioExterno SET activo=0 WHERE idUsuarioExterno=@p0", u.IdUsuarioExterno);
            UsuarioExterno validado;
            Check(new BLL_SesionBaseDatos().RevalidarUsuarioExterno(u.IdUsuarioExterno, u.FechaAlta, out validado).ToString() == "Invalida", "revalidacion rechaza bandera inactiva aunque estado diga Activa");
            Check(!Login(u, "QaPrueba2026").Exitoso, "login rechaza bandera inactiva");
        }
        finally { HttpContext.Current = null; }
    }
    static void Perfil()
    {
        var u = Nuevo(); string foto = "~/Content/Uploads/Perfiles/qa-nueva.jpg";
        Fallo(); try
        {
            Check(!b.ActualizarDatosPersonales(u.IdUsuarioExterno, "Nuevo", "Apellido", u.CorreoElectronico, null, foto, "QA").Exitoso, "perfil informa fallo de auditoria");
            Check(Leer(u).Nombre == u.Nombre && Leer(u).FotoPerfilRuta != foto, "perfil fallido conserva datos y ruta anterior de foto");
            Check(!b.CambiarPassword(u.IdUsuarioExterno, "QaPrueba2026", "QaOtraClave2026", "QaOtraClave2026").Exitoso, "password informa fallo de auditoria");
            Check(ProtectorDeCredenciales.VerificarPassword(Leer(u), "QaPrueba2026") && !ProtectorDeCredenciales.VerificarPassword(Leer(u), "QaOtraClave2026"), "fallo conserva la clave anterior");
            Check(!b.DarDeBajaCuenta(u.IdUsuarioExterno).Exitoso && Leer(u).Activo && Leer(u).EstadoCuenta == "Activa", "baja revierte si no puede auditarse");
        } finally { Quitar(); }
        Check(b.ActualizarDatosPersonales(u.IdUsuarioExterno, "Nuevo", "Apellido", u.CorreoElectronico, null, foto, "QA").Exitoso && Leer(u).FotoPerfilRuta == foto, "perfil normal guarda datos y foto");
        Check(!b.CambiarPassword(u.IdUsuarioExterno, "incorrecta", "QaOtraClave2026", "QaOtraClave2026").Exitoso, "se conserva validacion de clave actual");
        Check(b.CambiarPassword(u.IdUsuarioExterno, "QaPrueba2026", "QaOtraClave2026", "QaOtraClave2026").Exitoso && ProtectorDeCredenciales.VerificarPassword(Leer(u), "QaOtraClave2026"), "cambio normal de clave funciona");
    }
    static DatosTarjeta Tarjeta(string numero) { return new DatosTarjeta { Numero = numero, Titular = "QA Prueba", MesVencimiento = 12, AnioVencimiento = DateTime.Today.Year + 3 }; }
    static void Billetera()
    {
        var u = Nuevo(); var w = new BLL_Billetera();
        Fallo(); try { Check(!w.Guardar(u.IdUsuarioExterno, Tarjeta("4111111111111111"), "Primera", true).Exitoso && w.Listar(u.IdUsuarioExterno).Count == 0, "alta de tarjeta revierte ante fallo de auditoria"); } finally { Quitar(); }
        var a = w.Guardar(u.IdUsuarioExterno, Tarjeta("4111111111111111"), "Primera", true); Check(a.Exitoso, "alta normal de tarjeta: " + a.Mensaje);
        var c = w.Guardar(u.IdUsuarioExterno, Tarjeta("5555555555554444"), "Segunda", false); Check(c.Exitoso, "segunda tarjeta conserva preferida anterior");
        Fallo(); try
        {
            Check(!w.CambiarAlias(u.IdUsuarioExterno, a.Valor.IdTarjetaGuardada, "Nuevo alias").Exitoso && w.Listar(u.IdUsuarioExterno).Single(t => t.IdTarjetaGuardada == a.Valor.IdTarjetaGuardada).Alias == "Primera", "alias revierte ante fallo de auditoria");
            Check(!w.MarcarPreferida(u.IdUsuarioExterno, c.Valor.IdTarjetaGuardada).Exitoso && w.Listar(u.IdUsuarioExterno).Single(t => t.Preferida).IdTarjetaGuardada == a.Valor.IdTarjetaGuardada, "preferida revierte ante fallo de auditoria");
            Check(!w.Eliminar(u.IdUsuarioExterno, a.Valor.IdTarjetaGuardada).Exitoso && w.Listar(u.IdUsuarioExterno).Count == 2, "eliminar tarjeta revierte ante fallo de auditoria");
        } finally { Quitar(); }
        Check(w.CambiarAlias(u.IdUsuarioExterno, a.Valor.IdTarjetaGuardada, "Nuevo alias").Exitoso, "alias normal funciona");
        Check(w.MarcarPreferida(u.IdUsuarioExterno, c.Valor.IdTarjetaGuardada).Exitoso, "preferida normal funciona");
        Check(!w.Eliminar(Nuevo().IdUsuarioExterno, a.Valor.IdTarjetaGuardada).Exitoso, "se mantiene control de propietario de tarjeta");
        Check(w.Eliminar(u.IdUsuarioExterno, a.Valor.IdTarjetaGuardada).Exitoso && w.Listar(u.IdUsuarioExterno).Count == 1, "eliminar normal funciona");
    }
    static void Parametros()
    {
        string clave = BLL_ParametroPlataforma.ComisionPlataformaPorcentaje;
        string previo = Convert.ToString(Sql("SELECT valor FROM dbo.ParametroPlataforma WHERE clave=@p0", clave));
        int interno = N("SELECT TOP 1 idUsuarioInterno FROM dbo.UsuarioInterno WHERE activo=1 ORDER BY idUsuarioInterno");
        var p = new BLL_ParametroPlataforma(); BLL_ParametroPlataforma.InvalidarCache(); decimal numero = p.ObtenerDecimal(clave);
        string nuevo = numero == 27m ? "28" : "27";
        Fallo(); try
        {
            Check(!p.Actualizar(clave, nuevo, interno).Exitoso, "parametro informa fallo sin excepcion fuera del flujo");
            Check(Convert.ToString(Sql("SELECT valor FROM dbo.ParametroPlataforma WHERE clave=@p0", clave)) == previo && p.ObtenerDecimal(clave) == numero, "fallo conserva parametro y cache anteriores");
        } finally { Quitar(); }
        Check(p.Actualizar(clave, nuevo, interno).Exitoso && p.ObtenerDecimal(clave) == decimal.Parse(nuevo), "parametro normal confirma e invalida cache");
        Check(p.Actualizar(clave, previo, interno).Exitoso, "restauracion del parametro de prueba");
    }
    static int Main()
    {
        if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_")) throw new Exception("Solo copia QA");
        byte[] clave = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(clave);
        Environment.SetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION", Convert.ToBase64String(clave));
        try { Registro(); Activacion(); Acceso(); Perfil(); Billetera(); Parametros(); Console.WriteLine(checks + " comprobaciones de cuentas pasaron en QA."); return 0; }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
        finally { Quitar(); HttpContext.Current = null; }
    }
}
