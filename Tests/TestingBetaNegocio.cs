
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.MPP;
using StageUp.DAL;
using StageUp.Servicios;

class TestingBetaNegocio
{
    static int checks, espacio, gestor, cliente;
    static string cs = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static DateTime fecha = DateTime.Today.AddDays(30);
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
    static object Sql(string query)
    {
        using (var cn = new SqlConnection(cs)) using (var cmd = new SqlCommand(query, cn)) { cn.Open(); return cmd.ExecuteScalar(); }
    }
    static int N(string query) { return Convert.ToInt32(Sql(query)); }
    static DatosTarjeta Tarjeta() { return new DatosTarjeta { Numero = "4111111111111111", Titular = "QA Prueba", MesVencimiento = 12, AnioVencimiento = DateTime.Today.Year+3, CodigoSeguridad = "123" }; }
    static ResultadoOperacion<int> Solicitar(int desde) { return new BLL_Reserva().SolicitarReserva(cliente, espacio, fecha, desde, 60, "QA aislado"); }
    static int Reserva(int desde)
    {
        var r=Solicitar(desde); if (!r.Exitoso) throw new Exception(r.Mensaje);
        var a=new BLL_Reserva().Aceptar(r.Valor,gestor,"QA"); if (!a.Exitoso) throw new Exception(a.Mensaje);
        return r.Valor;
    }
    static ResultadoOperacion<Pago> Pagar(int id) { return new BLL_Pago().PagarReserva(id,cliente,Tarjeta(),new EleccionPago { Modo=ModoPago.Tarjeta }); }
    static void Fixture()
    {
        gestor=N("SELECT TOP 1 idUsuarioGestor FROM dbo.EspacioArtistico WHERE activo=1 AND publicado=1 ORDER BY idEspacioArtistico");
        cliente=N("SELECT TOP 1 idUsuarioExterno FROM dbo.UsuarioExterno WHERE estadoCuenta=N'Activa' AND idUsuarioExterno<>"+gestor+" ORDER BY idUsuarioExterno");
        var e=new EspacioArtistico { IdUsuarioGestor=gestor, NombreEspacio="QA Beta2 "+Guid.NewGuid().ToString("N"), TipoEspacio="Sala", Descripcion="Prueba aislada", Ficha=new FichaEspacio {
            Provincia="Buenos Aires", Ciudad="CABA", Direccion="QA 123", CapacidadMaxima=20, PrecioHora=1000m, Moneda="ARS", TipoPiso="Madera",
            Disponibilidad=Enumerable.Range(1,7).Select(d=>new FranjaEspacio { DiaSemana=d, MinutoDesde=0, MinutoHasta=1440 }).ToList() } };
        espacio=new MPP_EspacioArtistico().GuardarFicha(e);
        Sql("UPDATE dbo.EspacioArtistico SET publicado=1, estadoEspacio=N'Publicado' WHERE idEspacioArtistico="+espacio);
    }
    static void ReservasYFinanzas()
    {
        var gate=new ManualResetEventSlim(false);
        var tasks=Enumerable.Range(0,2).Select(i=>Task.Run(()=> { gate.Wait();return Solicitar(600); })).ToArray();
        gate.Set();Task.WaitAll(tasks);
        Check(tasks.Count(t=>t.Result.Exitoso)==1,"dos solicitudes simultáneas: una sola se registra");
        Check(N("SELECT COUNT(*) FROM dbo.Reserva WHERE idEspacioArtistico="+espacio+" AND minutoDesde=600")==1,"una única reserva persistida en la franja");
        var bloqueado=new BLL_DisponibilidadEspacio().BloquearHorario(espacio,gestor,true,null,fecha.ToString("yyyy-MM-dd"),"10:00","11:00","QA mantenimiento");
        Check(!bloqueado.Exitoso,"bloqueo manual rechaza una solicitud pendiente");
        var act=new Actividad { IdEspacioArtistico=espacio, Nombre="QA actividad", ModoRecurrencia="Fecha", Fecha=fecha.ToString("yyyy-MM-dd"), MinutoDesde=600, MinutoHasta=660, CupoMaximo=10, Activa=true };
        Check(!new BLL_Actividad().Guardar(act,gestor).Exitoso,"actividad rechaza una solicitud pendiente");

        var p=Solicitar(720);Check(p.Exitoso,"solicitud válida conserva el flujo");

        int block=new MPP_FranjaEspacio().InsertarManual(espacio,new FranjaEspacio { Fecha=fecha.ToString("yyyy-MM-dd"), MinutoDesde=720, MinutoHasta=780, Bloqueado=true, Origen="Manual", MotivoBloqueo="QA conflicto" });
        Check(!new BLL_Reserva().Aceptar(p.Valor,gestor,"QA").Exitoso,"aceptación vuelve a verificar bloqueos actuales");
        new MPP_FranjaEspacio().EliminarManual(espacio,block);
        Check(new BLL_Reserva().Aceptar(p.Valor,gestor,"QA").Exitoso,"aceptación válida después de resolver el bloqueo");

        Sql("CREATE OR ALTER TRIGGER dbo.QA_FalloTransaccion ON dbo.TransaccionEconomica AFTER INSERT AS BEGIN THROW 51000,'Fallo QA provocado',1; END");
        try
        {
            var fail=Pagar(p.Valor);Check(!fail.Exitoso,"fallo de transacción económica informa error en el pago");
            Check(N("SELECT COUNT(*) FROM dbo.Pago WHERE idReserva="+p.Valor+" AND estado=N'Aprobado'")==0,"pago fallido no deja un pago aprobado parcial");
            Check(N("SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente WHERE idReserva="+p.Valor)==0,"pago fallido revierte movimientos de ambas cuentas");
            Check(N("SELECT COUNT(*) FROM dbo.Comprobante WHERE idReserva="+p.Valor)==0,"pago fallido revierte la factura");
            Check(Convert.ToString(Sql("SELECT estadoPago FROM dbo.Reserva WHERE idReserva="+p.Valor))=="Pendiente","pago fallido mantiene la reserva pendiente de pago");
        }
        finally { Sql("DROP TRIGGER dbo.QA_FalloTransaccion"); }
        var pago=Pagar(p.Valor);Check(pago.Exitoso,"pago completo permite reintentar después del rollback");
        Check(N("SELECT COUNT(*) FROM dbo.TransaccionEconomica WHERE idReserva="+p.Valor+" AND tipoOperacion=N'ReservaAbonada'")==1,"pago completo genera una sola transacción");
        Check(!Pagar(p.Valor).Exitoso,"pago repetido se rechaza sin duplicar");
        Check(new BLL_Reserva().Cancelar(p.Valor,cliente).Exitoso,"cancelación sin cargo conserva el flujo");
        Check(Convert.ToString(Sql("SELECT estadoPago FROM dbo.TransaccionEconomica WHERE idReserva="+p.Valor+" AND tipoOperacion=N'ReservaAbonada'"))=="Devuelto","cancelación actualiza la transacción original");

        fecha=DateTime.Today.AddDays(2);
        int cargo=Reserva(840);Check(Pagar(cargo).Exitoso,"pago de reserva próxima");
        Sql("CREATE OR ALTER TRIGGER dbo.QA_FalloTransaccion ON dbo.TransaccionEconomica AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE tipoOperacion=N'CargoCancelacion') THROW 51000,'Fallo QA cancelacion',1; END");
        try
        {
            Check(!new BLL_Reserva().Cancelar(cargo,cliente).Exitoso,"fallo de transacción de cargo rechaza la cancelación");
            Check(Convert.ToString(Sql("SELECT estadoReserva FROM dbo.Reserva WHERE idReserva="+cargo))=="Aceptada","cancelación fallida conserva reserva aceptada");
            Check(Convert.ToString(Sql("SELECT estadoPago FROM dbo.TransaccionEconomica WHERE idReserva="+cargo+" AND tipoOperacion=N'ReservaAbonada'"))=="Pagado","cancelación fallida revierte el estado financiero");
            Check(N("SELECT COUNT(*) FROM dbo.Comprobante WHERE idReserva="+cargo+" AND tipo IN(N'NC',N'ND')")==0,"cancelación fallida no deja notas parciales");
        }
        finally { Sql("DROP TRIGGER dbo.QA_FalloTransaccion"); }
        Check(new BLL_Reserva().Cancelar(cargo,cliente).Exitoso,"cancelación con cargo se completa al reintentar");
        Check(N("SELECT COUNT(*) FROM dbo.TransaccionEconomica WHERE idReserva="+cargo+" AND tipoOperacion=N'CargoCancelacion'")==1,"una sola transacción de cargo");
        Check(N("SELECT COUNT(*) FROM dbo.Comprobante WHERE idReserva="+cargo+" AND tipo IN(N'NC',N'ND')")==2,"cancelación con cargo conserva NC y ND");
    }
    static void Accesos()
    {
        var type=typeof(BLL_Autenticacion).Assembly.GetType("StageUp.BLL.ControlIntentosAcceso");
        var method=type.GetMethod("Ejecutar",BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(typeof(ResultadoOperacion));
        string email="qa-"+Guid.NewGuid().ToString("N")+"@example.test";int llamadas=0;
        Func<bool,ResultadoOperacion> intento=ok => (ResultadoOperacion)method.Invoke(null,new object[]{email,new Func<ResultadoOperacion>(()=>{ Interlocked.Increment(ref llamadas); return ok?ResultadoOperacion.Ok():ResultadoOperacion.Error("incorrecto"); }),new Func<string,ResultadoOperacion>(m=>ResultadoOperacion.Error(m))});
        for(int i=0;i<4;i++) Check(intento(false).Mensaje=="incorrecto","intento fallido previo al límite "+(i+1));
        Check(intento(false).Mensaje.Contains("15 minutos"),"quinto intento activa bloqueo de 15 minutos");
        int prev=llamadas;Check(!intento(true).Exitoso && llamadas==prev,"durante el bloqueo no se evalúan credenciales correctas");

        string key;using(var sha=System.Security.Cryptography.SHA256.Create())key=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(email))).Replace("-","");
        Sql("UPDATE dbo.IntentoAcceso SET bloqueadoHasta=DATEADD(SECOND,-1,SYSUTCDATETIME()) WHERE cuenta='"+key+"'");
        Check(intento(true).Exitoso,"al vencer el bloqueo vuelve a permitirse el acceso");
        Check(N("SELECT intentos FROM dbo.IntentoAcceso WHERE cuenta='"+key+"'")==0,"inicio exitoso reinicia el contador");
        var gate=new ManualResetEventSlim(false);var concurrentes=Enumerable.Range(0,8).Select(i=>Task.Run(()=>{gate.Wait();return intento(false);})).ToArray();
        prev=llamadas;gate.Set();Task.WaitAll(concurrentes);
        Check(llamadas-prev==5,"ocho intentos simultáneos solo evalúan cinco antes del bloqueo");
        Check(N("SELECT intentos FROM dbo.IntentoAcceso WHERE cuenta='"+key+"'")==5,"contador concurrente no pierde intentos ni supera el límite");
    }
    static void AgendaYSaldo()
    {
        fecha=DateTime.Today.AddDays(40);
        var gate=new ManualResetEventSlim(false);
        var reserva=Task.Run(()=>{gate.Wait();return Solicitar(900);});
        var bloqueo=Task.Run(()=>{gate.Wait();return new BLL_DisponibilidadEspacio().BloquearHorario(espacio,gestor,true,null,fecha.ToString("yyyy-MM-dd"),"15:00","16:00","QA mantenimiento");});
        gate.Set();Task.WaitAll(reserva,bloqueo);
        Check(reserva.Result.Exitoso != bloqueo.Result.Exitoso,"solicitud y bloqueo simultáneos: solo uno ocupa la franja");
        var act=new Actividad { IdEspacioArtistico=espacio,Nombre="QA concurrente",ModoRecurrencia="Fecha",Fecha=fecha.ToString("yyyy-MM-dd"),MinutoDesde=960,MinutoHasta=1020,CupoMaximo=10,Activa=true };
        gate=new ManualResetEventSlim(false);
        var r=Task.Run(()=>{gate.Wait();return Solicitar(960);});
        var a=Task.Run(()=>{gate.Wait();return new BLL_Actividad().Guardar(act,gestor);});
        gate.Set();Task.WaitAll(r,a);
        Check(r.Result.Exitoso != a.Result.Exitoso,"solicitud y actividad simultáneas: solo una ocupa la franja");
        Check(new BLL_DisponibilidadEspacio().BloquearHorario(espacio,gestor,true,null,fecha.ToString("yyyy-MM-dd"),"17:00","18:00","QA mantenimiento").Exitoso,"alta de bloqueo válido");
        act.MinutoDesde=1020;act.MinutoHasta=1080;act.IdActividad=0;
        Check(!new BLL_Actividad().Guardar(act,gestor).Exitoso,"actividad no pisa un bloqueo manual ya guardado");

        int porSaldo=Reserva(1080);
        var saldo=new BLL_Pago().PagarReserva(porSaldo,cliente,(DatosTarjeta)null,new EleccionPago {Modo=ModoPago.Saldo});
        Check(saldo.Exitoso,"pago íntegro con saldo a favor conserva el flujo");
        Check(N("SELECT COUNT(*) FROM dbo.TransaccionEconomica WHERE idReserva="+porSaldo+" AND tipoOperacion=N'ReservaAbonada'")==1,"pago con saldo genera su transacción económica");
        Check(new BLL_Reserva().Cancelar(porSaldo,cliente).Exitoso,"cancelación devuelve saldo utilizado");
        int mixto=Reserva(1200);
        var combinado=new BLL_Pago().PagarReserva(mixto,cliente,Tarjeta(),new EleccionPago {Modo=ModoPago.Combinado,ImporteSaldo=300m});
        Check(combinado.Exitoso,"pago combinado conserva el flujo");
        Check(combinado.Valor.ImporteSaldo==300m && combinado.Valor.ImporteTarjeta==700m,"pago combinado conserva los importes elegidos");
        Check(N("SELECT COUNT(*) FROM dbo.TransaccionEconomica WHERE idReserva="+mixto)==1,"pago combinado genera una única transacción");

        int efectos=0;
        bool result=Conexion.Instance.EjecutarAtomico("QA:Rollback",()=>{Conexion.Instance.DespuesDeConfirmar(()=>efectos++);return false;},x=>x);
        Check(!result && efectos==0,"rollback no ejecuta correos ni otros efectos diferidos");
        Conexion.Instance.EjecutarAtomico("QA:Commit",()=>{Conexion.Instance.DespuesDeConfirmar(()=>efectos++);return true;},x=>x);
        Check(efectos==1,"commit ejecuta el efecto diferido una sola vez");
    }
    static void Correos()
    {
        Check(N("SELECT COUNT(*) FROM dbo.CorreoAdministrativo WHERE estado=N'Fallido'")>0,"falla SMTP queda registrada sin revertir operaciones");
        var bll=new BLL_CorreoAdministrativo();
        int id=bll.Iniciar(new CorreoAdministrativo {Destinatario="qa@example.test",Asunto="Prueba de trazabilidad"});
        bll.Finalizar(id,true);
        Check(bll.Listar("Enviado",DateTime.Today,DateTime.Today,null,"qa@example.test").Valor.Any(c=>c.IdCorreo==id),"historial permite filtrar resultados enviados");
        Check(!bll.Listar("inventado",null,null,null,null).Exitoso,"historial valida el estado solicitado");
        Check(!bll.Listar(null,DateTime.Today,DateTime.Today.AddDays(-1),null,null).Exitoso,"historial valida rango de fechas");
        Check(bll.Listar(null,null,null,null,"' OR 1=1 --").Valor.Count==0,"filtro de destinatario usa datos parametrizados");
    }
    static int Main()
    {
        try
        {
            if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_")) throw new Exception("Se exige una base QA aislada");
            if (ConfigurationManager.AppSettings["SmtpHost"]!="127.0.0.1" || ConfigurationManager.AppSettings["SmtpPuerto"]!="1") throw new Exception("SMTP debe apuntar al puerto local cerrado de prueba");
            Fixture();ReservasYFinanzas();AgendaYSaldo();Accesos();Correos();
            Console.WriteLine(checks+" checks passed; only isolated QA data changed.");return 0;
        }
        catch(Exception ex) { Console.WriteLine(ex.ToString());return 1; }
    }
}
