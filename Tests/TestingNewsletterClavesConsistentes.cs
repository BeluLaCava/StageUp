using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

class TestingNewsletterClavesConsistentes
{
    static readonly string Cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static readonly string CorreoDir=ConfigurationManager.AppSettings["CorreoPruebaDirectorio"];
    static int usuario,admin,checks;
    static object Sql(string s,params object[] p)
    {
        using(var c=new SqlConnection(Cs)) using(var q=new SqlCommand(s,c))
        {
            for(int i=0;i<p.Length;i++) q.Parameters.AddWithValue("@p"+i,p[i]);
            c.Open();return q.ExecuteScalar();
        }
    }
    static int N(string s,params object[] p){return Convert.ToInt32(Sql(s,p));}
    static string S(string s,params object[] p){return Convert.ToString(Sql(s,p));}
    static void Check(bool ok,string nombre){if(!ok)throw new Exception(nombre);checks++;Console.WriteLine("OK "+nombre);}
    static void Preparar(ResultadoOperacion r){if(!r.Exitoso)throw new Exception("Preparacion: "+r.Mensaje);}
    static int Correos(){return Directory.GetFiles(CorreoDir).Length;}
    static string TokenCorreo(string campo)
    {
        string raw=File.ReadAllText(Directory.GetFiles(CorreoDir).OrderByDescending(File.GetLastWriteTimeUtc).First());
        var codificado=Regex.Match(raw,@"Content-Transfer-Encoding: base64\r?\n\r?\n([A-Za-z0-9+/=\r\n]+)");
        if(codificado.Success)raw=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(codificado.Groups[1].Value));
        raw=Regex.Replace(raw,@"=\r?\n","").Replace("=3D","=");
        var m=Regex.Match(raw,campo+@"=([0-9A-F]{64})");
        if(!m.Success)throw new Exception("No se encontro enlace en el correo local");
        return m.Groups[1].Value;
    }
    static void FalloAudit(Action prueba,string condicion="")
    {
        int antes=N("SELECT COUNT(*) FROM RegistroActividad"),correos=Correos();
        Sql("CREATE TRIGGER QA_NewsletterClavesAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN "+condicion+" THROW 51000,'Fallo QA auditoria',1; END");
        try{prueba();Check(N("SELECT COUNT(*) FROM RegistroActividad")==antes && Correos()==correos,"Fallo no deja auditoria parcial ni envia correos");}
        finally{Sql("DROP TRIGGER QA_NewsletterClavesAudit");}
    }
    static void Newsletter()
    {
        var b=new BLL_Newsletter();
        string correo="qa-"+Guid.NewGuid().ToString("N")+"@example.test";
        Sql("UPDATE UsuarioExterno SET correoElectronico=@p0 WHERE idUsuarioExterno=@p1",correo,usuario);
        string cat=S("SELECT TOP 1 codigo FROM CategoriaNovedad WHERE activa=1 ORDER BY codigo");
        string otra=S("SELECT TOP 1 codigo FROM CategoriaNovedad WHERE activa=1 AND codigo<>@p0 ORDER BY codigo",cat);
        if(otra.Length==0)throw new Exception("Se requieren dos categorias activas en QA");
        int emails=Correos();
        Sql("CREATE TRIGGER QA_CategoriaFallo ON dbo.SuscriptorNewsletterCategoria AFTER INSERT AS BEGIN THROW 51000,'Fallo QA categoria',1; END");
        try
        {
            var r=b.SuscribirDesdeCuenta(usuario,new[]{cat},"http://localhost/");
            Check(!r.Exitoso && N("SELECT COUNT(*) FROM SuscriptorNewsletter WHERE correoElectronico=@p0",correo)==0 && Correos()==emails,"Fallo de categorias revierte el alta completa");
        }
        finally{Sql("DROP TRIGGER QA_CategoriaFallo");}
        FalloAudit(()=>Check(!b.SuscribirDesdeCuenta(usuario,new[]{cat},"http://localhost/").Exitoso && N("SELECT COUNT(*) FROM SuscriptorNewsletter WHERE correoElectronico=@p0",correo)==0,"Fallo de bitacora revierte alta de suscripcion"));
        Preparar(b.SuscribirDesdeCuenta(usuario,new[]{cat},"http://localhost/"));
        int id=N("SELECT idSuscriptorNewsletter FROM SuscriptorNewsletter WHERE correoElectronico=@p0",correo);
        string token=TokenCorreo("confirmar"),preferencias=S("SELECT tokenPreferencias FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id);
        Check(Correos()==emails+1 && S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Pendiente","Reintento inmediato prepara suscripcion y envia correo local");
        Check(!b.Confirmar("invalido").Exitoso,"Confirmacion rechaza token invalido");
        FalloAudit(()=>Check(!b.Confirmar(token).Exitoso && S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Pendiente","Fallo al confirmar conserva estado y token pendientes"));
        Preparar(b.Confirmar(token));Check(!b.Confirmar(token).Exitoso,"Confirmacion normal conserva uso unico del token");
        FalloAudit(()=>Check(!b.GuardarPreferencias(preferencias,new[]{otra}).Exitoso && S("SELECT categoria FROM SuscriptorNewsletterCategoria WHERE idSuscriptorNewsletter=@p0",id)==cat,"Fallo de preferencias conserva categorias anteriores"));
        Preparar(b.GuardarPreferencias(preferencias,new[]{otra}));Check(S("SELECT categoria FROM SuscriptorNewsletterCategoria WHERE idSuscriptorNewsletter=@p0",id)==otra,"Preferencias por enlace se guardan normalmente");
        FalloAudit(()=>Check(!b.GuardarPreferenciasDeCuenta(usuario,new[]{cat}).Exitoso && S("SELECT categoria FROM SuscriptorNewsletterCategoria WHERE idSuscriptorNewsletter=@p0",id)==otra,"Preferencias de Mi perfil tambien revierten ante fallo"));
        Preparar(b.GuardarPreferenciasDeCuenta(usuario,new[]{cat}));
        FalloAudit(()=>Check(!b.DarDeBaja(preferencias).Exitoso && S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Activo","Baja por enlace no queda sin bitacora"));
        Preparar(b.DarDeBaja(preferencias));
        FalloAudit(()=>Check(!b.Reactivar(preferencias).Exitoso && S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Baja","Reactivacion fallida mantiene la baja"));
        Preparar(b.Reactivar(preferencias));
        FalloAudit(()=>Check(!b.DarDeBajaDesdeCuenta(usuario).Exitoso && S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Activo","Baja desde Mi perfil conserva auditoria obligatoria"));
        Preparar(b.DarDeBajaDesdeCuenta(usuario));Preparar(b.Reactivar(preferencias));
        FalloAudit(()=>Check(!b.DarDeBajaAdministrativa(id,admin).Exitoso && S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Activo","Baja administrativa revierte si falla bitacora"));
        Preparar(b.DarDeBajaAdministrativa(id,admin));Preparar(b.Reactivar(preferencias));
        Check(S("SELECT estado FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)=="Activo","Bajas y reactivaciones normales mantienen funcionamiento");

        Sql("UPDATE SuscriptorNewsletter SET idUsuarioExterno=NULL,fechaUltimoEnvioConfirmacion=NULL WHERE idSuscriptorNewsletter=@p0",id);
        FalloAudit(()=>Check(!b.SolicitarVinculoDesdeCuenta(usuario,"http://localhost/").Exitoso && N("SELECT COUNT(*) FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0 AND hashTokenVinculo IS NULL",id)==1,"Solicitud de vinculo fallida no deja token ni envia correo"));
        Preparar(b.SolicitarVinculoDesdeCuenta(usuario,"http://localhost/"));string vinculo=TokenCorreo("vincular");
        FalloAudit(()=>Check(!b.ConfirmarVinculo(vinculo).Exitoso && N("SELECT COUNT(*) FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0 AND idUsuarioExterno IS NULL",id)==1,"Confirmacion de vinculo fallida conserva cuenta sin asociar"));
        Preparar(b.ConfirmarVinculo(vinculo));Check(b.ObtenerDeCuenta(usuario).Valor.Vinculada,"Vinculacion normal permite gestionar desde Mi perfil");

        Preparar(b.DarDeBaja(preferencias));Sql("UPDATE SuscriptorNewsletter SET fechaUltimoEnvioConfirmacion=NULL WHERE idSuscriptorNewsletter=@p0",id);
        Preparar(b.SuscribirDesdeCuenta(usuario,new[]{cat},"http://localhost/"));string anterior=TokenCorreo("confirmar");
        Sql("UPDATE SuscriptorNewsletter SET fechaUltimoEnvioConfirmacion=NULL WHERE idSuscriptorNewsletter=@p0",id);
        string hash=S("SELECT hashTokenConfirmacion FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id);
        FalloAudit(()=>Check(!b.ReenviarConfirmacion(id,admin,"http://localhost/").Exitoso && S("SELECT hashTokenConfirmacion FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",id)==hash,"Reenvio fallido conserva confirmacion anterior"));
        Preparar(b.ReenviarConfirmacion(id,admin,"http://localhost/"));string vigente=TokenCorreo("confirmar");
        Check(!b.Confirmar(anterior).Exitoso,"Reenvio exitoso invalida enlace anterior");
        using(var inicio=new ManualResetEventSlim(false))
        {
            var ts=Enumerable.Range(0,2).Select(i=>Task.Factory.StartNew(()=>{inicio.Wait();return new BLL_Newsletter().Confirmar(vigente);})).ToArray();
            inicio.Set();Task.WaitAll(ts);Check(ts.Count(t=>t.Result.Exitoso)==1,"Confirmaciones simultaneas registran una sola activacion");
        }
        Preparar(b.DarDeBaja(preferencias));Sql("UPDATE SuscriptorNewsletter SET fechaUltimoEnvioConfirmacion=NULL WHERE idSuscriptorNewsletter=@p0",id);
        string pausa=CorreoDir+"_pausa";Directory.Move(CorreoDir,pausa);
        try{Check(!b.SuscribirDesdeCuenta(usuario,new[]{cat},"http://localhost/").Exitoso && N("SELECT COUNT(*) FROM SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0 AND fechaUltimoEnvioConfirmacion IS NULL",id)==1,"Fallo de correo devuelve error y permite reintentar sin esperar");}
        finally{Directory.Move(pausa,CorreoDir);}
        Preparar(b.SuscribirDesdeCuenta(usuario,new[]{cat},"http://localhost/"));Preparar(b.Confirmar(TokenCorreo("confirmar")));
        Check(b.ObtenerDeCuenta(usuario).Valor.Estado=="Activo","Recuperacion tras fallo de correo conserva el circuito completo");
    }
    static DatosTarjeta Datos(string numero){return new DatosTarjeta{Numero=numero,Titular="QA Prueba",MesVencimiento=12,AnioVencimiento=DateTime.Today.Year+3};}
    static void Claves()
    {
        var b=new BLL_ClaveCifrado();var billetera=new BLL_Billetera();
        Sql("UPDATE TarjetaGuardada SET activa=0; UPDATE ClaveCifrado SET estado='Retirada',fechaRetiro=GETDATE() WHERE estado='Activa'");
        int total=N("SELECT COUNT(*) FROM ClaveCifrado");
        FalloAudit(()=>{bool fallo=false;try{b.ObtenerActiva("Prueba QA de clave inicial");}catch{fallo=true;}Check(fallo && N("SELECT COUNT(*) FROM ClaveCifrado")==total,"Clave inicial y bitacora se revierten juntas");});
        b.ObtenerActiva("Clave inicial QA");
        var una=billetera.Guardar(usuario,Datos("4111111111111111"),"QA uno",true);Preparar(una);
        var dos=billetera.Guardar(usuario,Datos("5555555555554444"),"QA dos",false);Preparar(dos);
        int id1=una.Valor.IdTarjetaGuardada,id2=dos.Valor.IdTarjetaGuardada;
        int anterior=N("SELECT idClaveCifrado FROM ClaveCifrado WHERE estado='Activa'");
        total=N("SELECT COUNT(*) FROM ClaveCifrado");
        string cifrada=S("SELECT claveCifrada FROM TarjetaGuardada WHERE idTarjetaGuardada=@p0",id1);
        Check(!b.Rotar(admin,"corto",true).Exitoso && !b.Rotar(admin,"Rotacion de prueba QA",false).Exitoso,"Rotacion mantiene validacion de motivo y confirmacion");
        FalloAudit(()=>Check(!b.Rotar(admin,"Rotacion de prueba QA",true).Exitoso && N("SELECT COUNT(*) FROM ClaveCifrado")==total && N("SELECT idClaveCifrado FROM ClaveCifrado WHERE estado='Activa'")==anterior,"Fallo inicial de auditoria conserva clave activa"));
        FalloAudit(()=>Check(!b.Rotar(admin,"Rotacion de prueba QA",true).Exitoso && N("SELECT COUNT(*) FROM ClaveCifrado")==total && S("SELECT claveCifrada FROM TarjetaGuardada WHERE idTarjetaGuardada=@p0",id1)==cifrada,"Fallo de auditoria final revierte claves y tarjetas"),"IF EXISTS(SELECT 1 FROM inserted WHERE descripcionOperacion LIKE N'Tarjetas de la billetera protegidas%')");
        Sql("CREATE TRIGGER QA_TarjetaFallo ON dbo.TarjetaGuardada AFTER UPDATE AS BEGIN THROW 51000,'Fallo QA tarjeta',1; END");
        try{Check(!b.Rotar(admin,"Rotacion de prueba QA",true).Exitoso && N("SELECT COUNT(*) FROM ClaveCifrado")==total,"Fallo al actualizar tarjetas revierte toda la rotacion");}
        finally{Sql("DROP TRIGGER QA_TarjetaFallo");}
        var exportador=new BLL_ExportacionSegura();
        var exportado=exportador.ExportarBitacoraCifrada(new FiltroRegistroActividad{FechaDesde=DateTime.Today,FechaHasta=DateTime.Today});
        Preparar(exportado);var textoAntes=exportador.ImportarYDescifrar(exportado.Valor);
        Check(textoAntes.Exitoso,"Exportacion cifrada conserva generacion y lectura");
        Preparar(b.Rotar(admin,"Rotacion de prueba QA",true));
        var textoDespues=exportador.ImportarYDescifrar(exportado.Valor);
        Check(textoDespues.Exitoso && textoDespues.Valor==textoAntes.Valor,"Exportacion anterior sigue legible despues de rotar");
        Check(N("SELECT COUNT(*) FROM ClaveCifrado WHERE estado='Activa'")==1 && N("SELECT COUNT(*) FROM TarjetaGuardada t JOIN ClaveCifrado c ON c.idClaveCifrado=t.idClaveCifrado WHERE t.activa=1 AND c.estado='Activa'")==2,"Rotacion normal migra tarjetas y conserva una sola clave activa");
        Check(billetera.ObtenerDatosParaPagar(usuario,id1,"123").Valor.Numero=="4111111111111111" && billetera.ObtenerDatosParaPagar(usuario,id2,"123").Valor.Numero=="5555555555554444","Tarjetas siguen disponibles para pagar despues de rotar");
        Check(b.ObtenerClavePrivada(anterior).Length>0,"Claves anteriores siguen disponibles para datos historicos");
        string original=S("SELECT claveCifrada FROM TarjetaGuardada WHERE idTarjetaGuardada=@p0",id2);
        int versionTarjeta=N("SELECT idClaveCifrado FROM TarjetaGuardada WHERE idTarjetaGuardada=@p0",id2);
        Sql("UPDATE TarjetaGuardada SET claveCifrada='x' WHERE idTarjetaGuardada=@p0",id2);
        var parcial=b.Rotar(admin,"Rotacion con tarjeta pendiente QA",true);
        Check(parcial.Exitoso && parcial.CodigoAlternativo=="REENVOLVER_PENDIENTE" && b.ContarPendientesDeReenvolver()==1,"Problema de una tarjeta conserva aviso y posibilidad de reintento");
        Sql("UPDATE TarjetaGuardada SET claveCifrada=@p0 WHERE idTarjetaGuardada=@p1",original,id2);
        FalloAudit(()=>Check(!b.ReintentarPendientes(admin).Exitoso && N("SELECT idClaveCifrado FROM TarjetaGuardada WHERE idTarjetaGuardada=@p0",id2)==versionTarjeta,"Reintento con fallo de auditoria no deja cambios parciales"));
        Preparar(b.ReintentarPendientes(admin));Check(b.ContarPendientesDeReenvolver()==0 && billetera.ObtenerDatosParaPagar(usuario,id2,"123").Exitoso,"Reintento normal recupera tarjeta pendiente");
        using(var inicio=new ManualResetEventSlim(false))
        {
            var ts=Enumerable.Range(0,2).Select(i=>Task.Factory.StartNew(()=>{inicio.Wait();return new BLL_ClaveCifrado().Rotar(admin,"Rotacion simultanea QA "+i,true);})).ToArray();
            inicio.Set();Task.WaitAll(ts);Check(ts.All(t=>t.Result.Exitoso) && N("SELECT COUNT(*) FROM ClaveCifrado WHERE estado='Activa'")==1 && b.ContarPendientesDeReenvolver()==0,"Rotaciones simultaneas mantienen claves y tarjetas coherentes");
        }
    }
    static int Main()
    {
        try
        {
            if(!new SqlConnectionStringBuilder(Cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_"))throw new Exception("Solo QA");
            var smtp=(System.Net.Configuration.SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
            if(smtp.DeliveryMethod!=System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory || smtp.SpecifiedPickupDirectory.PickupDirectoryLocation!=CorreoDir)throw new Exception("Solo correos a archivos locales");
            byte[] key=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(key);
            Environment.SetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION",Convert.ToBase64String(key));
            usuario=N("SELECT TOP 1 idUsuarioExterno FROM UsuarioExterno WHERE activo=1 AND estadoCuenta='Activa'");
            admin=N("SELECT TOP 1 idUsuarioInterno FROM UsuarioInterno WHERE activo=1 AND estadoCuenta='Activa'");
            Newsletter();Claves();Console.WriteLine("TOTAL "+checks+" comprobaciones correctas");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
