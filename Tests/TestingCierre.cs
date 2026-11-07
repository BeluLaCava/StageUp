using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.MPP;

class TestingCierre
{
    static string cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int checks,usuario,interno,ticket;
    static string correo;
    static object Sql(string q) { using(var c=new SqlConnection(cs))using(var cmd=new SqlCommand(q,c)){c.Open();return cmd.ExecuteScalar();} }
    static int N(string q){return Convert.ToInt32(Sql(q));}
    static void Check(bool ok,string nombre){if(!ok)throw new Exception("FAIL: "+nombre);checks++;Console.WriteLine("PASS: "+nombre);}
    static void FalloAuditoria(){Sql("CREATE OR ALTER TRIGGER dbo.QA_CierreBitacora ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000, 'Fallo QA de auditoria', 1; END");}
    static void QuitarFallo(){Sql("IF OBJECT_ID('dbo.QA_CierreBitacora','TR') IS NOT NULL DROP TRIGGER dbo.QA_CierreBitacora");}
    static int Codigo(string texto){return new MPP_CodigoRecuperacion().Insertar(new CodigoRecuperacion{IdUsuarioExterno=usuario,Codigo=texto,FechaVencimiento=DateTime.Now.AddMinutes(15)});}
    static void Recuperacion()
    {
        var b=new BLL_UsuarioExterno();
        int primero=Codigo("111111"),segundo=Codigo("222222");
        Check(N("SELECT CONVERT(int,utilizado) FROM dbo.CodigoRecuperacion WHERE idCodigoRecuperacion="+primero)==1,"nuevo codigo invalida el anterior");
        Check(!b.ValidarCodigoYActualizarPassword(correo,"111111","NuevaStageUp2026","NuevaStageUp2026").Exitoso,"codigo anterior no cambia contrasena");
        Check(!b.ValidarCodigoYActualizarPassword(correo,"222222","corta","corta").Exitoso,"contrasena invalida no consume codigo");
        Check(N("SELECT CONVERT(int,utilizado) FROM dbo.CodigoRecuperacion WHERE idCodigoRecuperacion="+segundo)==0,"codigo sigue disponible tras validacion fallida");
        string hash=Convert.ToString(Sql("SELECT passwordHash FROM dbo.UsuarioExterno WHERE idUsuarioExterno="+usuario));
        FalloAuditoria();
        try {
            Check(!b.ValidarCodigoYActualizarPassword(correo,"222222","NuevaStageUp2026","NuevaStageUp2026").Exitoso,"fallo de auditoria devuelve error");
            Check(hash==Convert.ToString(Sql("SELECT passwordHash FROM dbo.UsuarioExterno WHERE idUsuarioExterno="+usuario)),"fallo revierte contrasena");
            Check(N("SELECT CONVERT(int,utilizado) FROM dbo.CodigoRecuperacion WHERE idCodigoRecuperacion="+segundo)==0,"fallo revierte consumo del codigo");
        } finally {QuitarFallo();}
        var puerta=new ManualResetEventSlim(false);
        var tareas=Enumerable.Range(0,2).Select(i=>Task.Run(()=>{puerta.Wait();return new BLL_UsuarioExterno().ValidarCodigoYActualizarPassword(correo,"222222","NuevaStageUp2026","NuevaStageUp2026");})).ToArray();
        puerta.Set();Task.WaitAll(tareas);
        Check(tareas.Count(t=>t.Result.Exitoso)==1,"dos consumos simultaneos: uno solo exitoso");
        Check(new MPP_CodigoRecuperacion().ObtenerVigentePorUsuario(new UsuarioExterno{IdUsuarioExterno=usuario})==null,"consumir el ultimo no revive codigos anteriores");
        Check(!b.ValidarCodigoYActualizarPassword(correo,"222222","OtraClave2026","OtraClave2026").Exitoso,"codigo usado no se reutiliza");
        int expirado=Codigo("333333");
        Sql("UPDATE dbo.CodigoRecuperacion SET fechaVencimiento=DATEADD(MINUTE,-1,GETDATE()) WHERE idCodigoRecuperacion="+expirado);
        Check(!b.ValidarCodigoYActualizarPassword(correo,"333333","OtraClave2026","OtraClave2026").Exitoso,"codigo vencido se rechaza");
    }
    static void Finanzas()
    {
        var b=new BLL_CuentaCorriente();
        int comprobantes=N("SELECT COUNT(*) FROM dbo.Comprobante"),movimientos=N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente");
        FalloAuditoria();
        try{
            Check(!b.EmitirComprobanteManual("NC",usuario,"Cliente",100,"ARS","QA prueba de atomicidad",null,interno).Exitoso,"emision falla si no puede auditarse");
            Check(N("SELECT COUNT(*) FROM dbo.Comprobante")==comprobantes,"emision fallida no deja comprobante");
            Check(N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente")==movimientos,"emision fallida no deja movimiento");
        } finally{QuitarFallo();}
        var nota=b.EmitirComprobanteManual("NC",usuario,"Cliente",100,"ARS","QA prueba de atomicidad",null,interno);
        Check(nota.Exitoso,"emision exitosa despues del fallo");
        Check(N("SELECT COUNT(*) FROM dbo.Comprobante")==comprobantes+1,"reintento deja solo un comprobante");
        movimientos=N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente");
        FalloAuditoria();
        try{
            Check(!b.AnularComprobante(nota.Valor,"QA prueba de anulacion",interno).Exitoso,"anulacion falla si no puede auditarse");
            Check(Convert.ToString(Sql("SELECT estado FROM dbo.Comprobante WHERE idComprobante="+nota.Valor))=="Emitido","anulacion fallida conserva estado");
            Check(N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente")==movimientos,"anulacion fallida conserva movimientos");
        } finally{QuitarFallo();}
        Check(b.AnularComprobante(nota.Valor,"QA prueba de anulacion",interno).Exitoso,"anulacion exitosa");
        Check(!b.AnularComprobante(nota.Valor,"QA prueba repetida",interno).Exitoso,"anulacion repetida rechazada");
        Check(b.EmitirComprobanteManual("NC",usuario,"Gestor",100,"ARS","QA saldo para liquidacion",null,interno).Exitoso,"prepara saldo de gestor en copia QA");
        movimientos=N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente");
        FalloAuditoria();
        try{
            Check(!b.RegistrarLiquidacion(usuario,"ARS",10,"QA liquidacion",interno).Exitoso,"liquidacion falla si no puede auditarse");
            Check(N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente")==movimientos,"liquidacion fallida conserva saldo");
        } finally{QuitarFallo();}
        Check(b.RegistrarLiquidacion(usuario,"ARS",10,"QA liquidacion",interno).Exitoso,"liquidacion exitosa");
    }
    static int Mensaje(bool soporte){return N("INSERT dbo.TicketMensaje(idTicket,idUsuarioExterno,idUsuarioInterno,mensaje,fechaEnvio,esInicial) VALUES("+ticket+","+(soporte?"NULL":usuario.ToString())+","+(soporte?interno.ToString():"NULL")+",N'QA lectura',GETDATE(),0); SELECT CONVERT(int,SCOPE_IDENTITY());");}
    static int Actualizacion(){return N("INSERT dbo.TicketActualizacion(idTicket,tipoActualizacion,idUsuarioInterno,detalle,fecha) VALUES("+ticket+",N'CambioEstado',"+interno+",N'QA lectura',GETDATE());SELECT CONVERT(int,SCOPE_IDENTITY());");}
    static void Soporte()
    {
        var m=new MPP_Ticket();var t=new Ticket{IdTicket=ticket};
        foreach(bool soporte in new[]{true,false}){
            int visto=Mensaje(soporte);var mostrados=m.ListarMensajesPorTicket(t);
            int posterior=Mensaje(soporte);
            m.MarcarLeido(t,soporte?"Usuario":"Soporte",mostrados.Select(x=>x.IdTicketMensaje),new int[0]);
            Check(N("SELECT COUNT(*) FROM dbo.TicketMensaje WHERE idTicketMensaje="+visto+" AND fechaLecturaDestinatario IS NOT NULL")==1,"mensaje mostrado queda leido: "+soporte);
            Check(N("SELECT COUNT(*) FROM dbo.TicketMensaje WHERE idTicketMensaje="+posterior+" AND fechaLecturaDestinatario IS NULL")==1,"mensaje posterior sigue sin leer: "+soporte);
            var resultado=soporte?new BLL_Ticket().ObtenerSeguimiento(ticket,usuario):new BLL_Ticket().ObtenerDetalleInterno(ticket,false);
            Check(resultado.Exitoso && resultado.Valor.Mensajes.Any(x=>x.IdTicketMensaje==posterior && x.EsNuevo),"BLL muestra el mensaje nuevo antes de marcarlo: "+soporte);
        }
        int a=Actualizacion();var cambios=m.ListarActualizacionesPorTicket(t);int b=Actualizacion();
        m.MarcarLeido(t,"Usuario",new int[0],cambios.Select(x=>x.IdTicketActualizacion));
        Check(N("SELECT COUNT(*) FROM dbo.TicketActualizacion WHERE idTicketActualizacion="+a+" AND fechaLecturaDestinatario IS NOT NULL")==1,"actualizacion mostrada queda leida");
        Check(N("SELECT COUNT(*) FROM dbo.TicketActualizacion WHERE idTicketActualizacion="+b+" AND fechaLecturaDestinatario IS NULL")==1,"actualizacion posterior sigue sin leer");
        Check(N("SELECT novedadesUsuario FROM dbo.vw_TicketSoporte WHERE idTicket="+ticket)>0,"contador conserva actualizacion pendiente");
        Check(!new BLL_Ticket().ObtenerSeguimiento(ticket,int.MaxValue).Exitoso,"otro usuario no puede leer el ticket");
    }
    static void Listados()
    {
        string definicion=Convert.ToString(Sql("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_Reserva_ListarPorSolicitante'))"));
        Sql("ALTER PROCEDURE dbo.sp_Reserva_ListarPorSolicitante @idUsuarioExternoSolicitante INT AS BEGIN THROW 51000,'Fallo QA de consulta',1; END");
        try{
            bool error=false;try{new BLL_Reserva().ListarMisReservas(usuario);}catch(ErrorCargaListadoException){error=true;}
            Check(error,"consulta fallida no se transforma en lista vacia");
        }finally{Sql(System.Text.RegularExpressions.Regex.Replace(definicion,@"(?i)CREATE\s+(?:OR\s+ALTER\s+)?PROCEDURE","ALTER PROCEDURE"));}
        Check(new BLL_Reserva().ListarMisReservas(int.MaxValue).Count==0,"consulta exitosa sin reservas sigue devolviendo lista vacia");
    }
    static int Main(){try{
        if(!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_"))throw new Exception("Solo copia QA");
        if(ConfigurationManager.AppSettings["SmtpHost"]!="127.0.0.1" || ConfigurationManager.AppSettings["SmtpPuerto"]!="1")throw new Exception("Solo SMTP local inactivo");
        usuario=N("SELECT TOP 1 idUsuarioExterno FROM dbo.UsuarioExterno WHERE activo=1 AND estadoCuenta=N'Activa' ORDER BY idUsuarioExterno");
        var nuevo=new BLL_Ticket().RegistrarSolicitud(new SolicitudSoporteNueva{IdUsuarioExterno=usuario,Motivo="QA lectura de soporte",FuncionalidadVinculada="Cuenta",Descripcion="Esta es una solicitud de prueba aislada para comprobar las lecturas del soporte.",Origen="Soporte"});
        Check(nuevo.Exitoso,"creacion de solicitud de soporte QA");ticket=nuevo.Valor;
        interno=N("SELECT TOP 1 idUsuarioInterno FROM dbo.UsuarioInterno WHERE activo=1 ORDER BY idUsuarioInterno");
        correo=Convert.ToString(Sql("SELECT correoElectronico FROM dbo.UsuarioExterno WHERE idUsuarioExterno="+usuario));
        Recuperacion();Finanzas();Soporte();Listados();Console.WriteLine(checks+" checks passed.");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}finally{QuitarFallo();}}
}
