using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.SessionState;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;
class TestingBetaAccesoReal
{
    static string cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int passed;
    static void Check(bool ok,string text) { if(!ok)throw new Exception(text);passed++;Console.WriteLine("PASS: "+text); }
    static object Sql(string text, params object[] valores)
    {
        using(var cn=new SqlConnection(cs))using(var cmd=new SqlCommand(text,cn))
        { for(int i=0;i<valores.Length;i++)cmd.Parameters.AddWithValue("@p"+i,valores[i]);cn.Open();return cmd.ExecuteScalar(); }
    }
    static void Contexto()
    {
        var context=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        var state=new HttpSessionStateContainer("qa",new SessionStateItemCollection(),new HttpStaticObjectsCollection(),30,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false);
        SessionStateUtility.AddHttpSessionStateToContext(context,state);HttpContext.Current=context;
    }
    static ResultadoOperacion Intento(string correo,string password,bool interno)
    {
        Type tipo=interno?typeof(BLL_UsuarioInterno):typeof(BLL_UsuarioExterno);
        var login=tipo.GetMethod("IniciarSesionSinCaptcha",BindingFlags.NonPublic|BindingFlags.Instance);
        var control=typeof(BLL_Autenticacion).Assembly.GetType("StageUp.BLL.ControlIntentosAcceso").GetMethod("Ejecutar",BindingFlags.NonPublic|BindingFlags.Static).MakeGenericMethod(typeof(ResultadoOperacion));
        return (ResultadoOperacion)control.Invoke(null,new object[] {correo,new Func<ResultadoOperacion>(()=>(ResultadoOperacion)login.Invoke(Activator.CreateInstance(tipo),new object[] {correo,password})),new Func<string,ResultadoOperacion>(m=>ResultadoOperacion.Error(m))});
    }
    static int Main()
    {
        try
        {
            if(!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_"))throw new Exception("Solo se permite una base QA aislada");
            string correo=Convert.ToString(Sql("SELECT TOP 1 correoElectronico FROM dbo.UsuarioExterno WHERE estadoCuenta=N'Activa' ORDER BY idUsuarioExterno"));
            var user=new UsuarioExterno();ProtectorDeCredenciales.ProtegerPassword(user,"QA2026_prueba");
            Sql("UPDATE dbo.UsuarioExterno SET passwordHash=@p0 WHERE correoElectronico=@p1",user.PasswordHash,correo);
            Contexto();
            for(int i=0;i<5;i++)Check(!Intento(correo,"incorrecta",false).Exitoso,"credencial incorrecta rechazada "+(i+1));
            Check(!Intento(correo,"QA2026_prueba",false).Exitoso && !GestorDeSesion.EstaAutenticado(),"contraseña correcta no evita el bloqueo activo");
            Sql("UPDATE dbo.IntentoAcceso SET bloqueadoHasta=DATEADD(SECOND,-1,SYSUTCDATETIME()) WHERE bloqueadoHasta IS NOT NULL");
            Check(Intento(correo,"QA2026_prueba",false).Exitoso && GestorDeSesion.EstaAutenticado(),"login externo establece sesión después del desbloqueo");
            Contexto();
            Sql("CREATE OR ALTER TRIGGER dbo.QA_FalloAcceso ON dbo.IntentoAcceso AFTER UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted) THROW 51000,'Fallo QA control de acceso',1; END");
            try { Check(!Intento(correo,"QA2026_prueba",false).Exitoso && !GestorDeSesion.EstaAutenticado(),"fallo de persistencia de acceso no deja una sesión autenticada"); }
            finally { Sql("DROP TRIGGER dbo.QA_FalloAcceso"); }
            string interno=Convert.ToString(Sql("SELECT TOP 1 correoElectronico FROM dbo.UsuarioInterno WHERE estadoCuenta=N'Activa' AND activo=1 ORDER BY idUsuarioInterno"));
            var admin=new UsuarioInterno();ProtectorDeCredenciales.ProtegerPassword(admin,"QA2026_prueba");
            Sql("UPDATE dbo.UsuarioInterno SET passwordHash=@p0 WHERE correoElectronico=@p1",admin.PasswordHash,interno);
            Contexto();
            Check(Intento(interno,"QA2026_prueba",true).Exitoso && GestorDeSesion.EstaAutenticadoComoInterno(),"login interno conserva sesión y permisos");
            Console.WriteLine(passed+" comprobaciones de autenticación real pasaron en QA.");return 0;
        }
        catch(Exception ex) {Console.WriteLine(ex);return 1;}
    }
}
