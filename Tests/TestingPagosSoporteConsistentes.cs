using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;

class TestingPagosSoporteConsistentes
{
    static readonly string Cadena = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int admin, usuario, ajeno, pruebas;
    static object Sql(string s, params object[] p)
    {
        using (var c = new SqlConnection(Cadena)) using (var q = new SqlCommand(s, c))
        {
            for (int i = 0; i < p.Length; i++) q.Parameters.AddWithValue("@p" + i, p[i]);
            c.Open(); return q.ExecuteScalar();
        }
    }
    static int N(string s, params object[] p) { return Convert.ToInt32(Sql(s, p)); }
    static string S(string s, params object[] p) { return Convert.ToString(Sql(s, p)); }
    static string Id() { return Guid.NewGuid().ToString("N"); }
    static decimal Saldo() { return Convert.ToDecimal(Sql("SELECT ISNULL(SUM(importe),0) FROM MovimientoCuentaCorriente WHERE idUsuarioExterno=@p0 AND rolCuenta='Cliente' AND moneda='ARS'", usuario)); }
    static void Check(bool ok, string texto) { if (!ok) throw new Exception(texto); pruebas++; Console.WriteLine("OK " + texto); }
    static void Preparado(ResultadoOperacion r) { if (!r.Exitoso) throw new Exception("Preparacion: " + r.Mensaje); }
    static DatosTarjeta Tarjeta() { return new DatosTarjeta { Numero = "4111111111111111", Titular = "QA prueba", MesVencimiento = 12, AnioVencimiento = DateTime.Today.Year + 3, CodigoSeguridad = "123" }; }
    static void Nota(decimal cambio)
    {
        if (cambio != 0) Preparado(new BLL_CuentaCorriente().EmitirComprobanteManual(cambio > 0 ? "NC" : "ND", usuario, "Cliente", Math.Abs(cambio), "ARS", "Prueba QA aislada de consistencia", null, admin));
    }
    static int PagosAprobados() { return N("SELECT COUNT(*) FROM Pago WHERE idUsuarioExterno=@p0 AND concepto='Deuda' AND estado='Aprobado'", usuario); }
    static void ConFallo(Action accion)
    {
        int registros = N("SELECT COUNT(*) FROM RegistroActividad"); int avisos = N("SELECT COUNT(*) FROM Notificacion");
        Sql("CREATE TRIGGER QA_PagosSoporteAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END");
        try { accion(); Check(N("SELECT COUNT(*) FROM RegistroActividad") == registros && N("SELECT COUNT(*) FROM Notificacion") == avisos, "Fallo no deja auditoria ni avisos parciales"); }
        finally { Sql("DROP TRIGGER QA_PagosSoporteAudit"); }
    }
    static string DefSaldo()
    {
        return System.Text.RegularExpressions.Regex.Replace(S("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_CuentaCorriente_ObtenerSaldos'))"), @"\bCREATE\s+PROCEDURE\b", "ALTER PROCEDURE", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
    static void PagoConMovimiento(decimal movimiento)
    {
        Nota(-100m - Saldo()); string original = DefSaldo();
        Sql("CREATE TABLE QA_PagoIntercalado(marca int NOT NULL)");
        try
        {
            Sql(original.Replace("ORDER BY moneda;", "ORDER BY moneda; IF @@TRANCOUNT=0 AND NOT EXISTS(SELECT 1 FROM QA_PagoIntercalado) BEGIN INSERT INTO QA_PagoIntercalado VALUES(1); WAITFOR DELAY '00:00:03'; END;"));
            var tarea = Task.Factory.StartNew(() => new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()));
            bool llego = false;
            for (int i = 0; i < 100; i++) { if (N("SELECT COUNT(*) FROM QA_PagoIntercalado") > 0) { llego = true; break; } Thread.Sleep(20); }
            if (!llego) throw new Exception("No se alcanzo la ventana de concurrencia");
            Nota(movimiento); tarea.Wait(); var r = tarea.Result;
            if (movimiento < 0)
            {
                Check(r.Exitoso && r.Valor.ImporteTarjeta == 100m && Saldo() == movimiento, "Cargo concurrente no aumenta automaticamente el cobro");
                Check(r.Mensaje.Contains("queda una deuda de") && r.Mensaje.Contains(BLL_CuentaCorriente.FormatearImporte(-movimiento, "ARS")) && !r.Mensaje.Contains("sin deuda"), "Confirmacion informa la deuda restante real");
            }
            else Check(!r.Exitoso && Saldo() == -100m + movimiento && r.Mensaje.Contains("No se realizó ningún cobro"), "Credito concurrente impide cobrar mas que la deuda vigente");
        }
        finally { Sql(original); Sql("DROP TABLE QA_PagoIntercalado"); }
    }
    static void Pagos()
    {
        Nota(-100m - Saldo()); int antes = PagosAprobados();
        ConFallo(() => { var r = new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()); Check(!r.Exitoso && Saldo() == -100m && PagosAprobados() == antes, "Fallo de auditoria revierte pago y saldo"); });
        var normal = new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()); Preparado(normal);
        Check(Saldo() == 0 && normal.Valor.ImporteTarjeta == 100m && PagosAprobados() == antes + 1 && normal.Mensaje.Contains("sin deuda"), "Pago normal cancela deuda y devuelve importe correcto");
        Check(!new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()).Exitoso && PagosAprobados() == antes + 1, "Reintento sin deuda no genera otro pago");
        Nota(-100m); var invalida = Tarjeta(); invalida.CodigoSeguridad = "x";
        Check(!new BLL_Pago().PagarDeuda(usuario, "ARS", invalida).Exitoso && Saldo() == -100m, "Validacion de tarjeta conserva el saldo");
        string original = DefSaldo(); antes = PagosAprobados();
        try
        {
            Sql(original.Replace("SET NOCOUNT ON;", "SET NOCOUNT ON; IF @@TRANCOUNT>0 THROW 51000,'Fallo QA saldo posterior',1;"));
            Check(!new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()).Exitoso && Saldo() == -100m && PagosAprobados() == antes, "Fallo al verificar saldo final revierte pago completo");
            Sql(original.Replace("SET NOCOUNT ON;", "SET NOCOUNT ON; THROW 51000,'Fallo QA saldo inicial',1;"));
            Check(!new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()).Exitoso && PagosAprobados() == antes, "Fallo de saldo inicial devuelve error controlado");
        }
        finally { Sql(original); }
        PagoConMovimiento(-50m); PagoConMovimiento(50m); PagoConMovimiento(100m);
        Nota(-100m - Saldo()); antes = PagosAprobados();
        using (var inicio = new ManualResetEventSlim(false))
        {
            var tareas = Enumerable.Range(0, 4).Select(i => Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_Pago().PagarDeuda(usuario, "ARS", Tarjeta()); })).ToArray();
            inicio.Set(); Task.WaitAll(tareas);
            Check(tareas.Count(t => t.Result.Exitoso) == 1 && Saldo() == 0 && PagosAprobados() == antes + 1, "Cuatro pagos simultaneos no cobran dos veces la deuda");
        }
        string codigo = "qa-" + Id(); var aviso = new AvisoMedioPago { IdentificadorExterno = codigo, Moneda = "ARS" };
        ConFallo(() => { var r = new BLL_Pago().RegistrarAvisoMedioExterno(aviso, admin); Check(!r.Exitoso && N("SELECT COUNT(*) FROM Pago WHERE identificadorExterno=@p0", codigo) == 0, "Aviso observado fallido no queda guardado"); });
        var registrado = new BLL_Pago().RegistrarAvisoMedioExterno(aviso, admin); Preparado(registrado);
        Check(S("SELECT estado FROM Pago WHERE idPago=@p0", registrado.Valor) == "Observado" && registrado.CodigoAlternativo == "A1", "Aviso incompleto normal conserva estado y mensaje");
        aviso = new AvisoMedioPago { IdentificadorExterno = "qa-" + Id(), IdReserva = 2147483000, Moneda = "ARS", Importe = 100m, Medio = "Simulado", EstadoInformado = "Aprobado", FechaOperacion = DateTime.Now };
        ConFallo(() => { var r = new BLL_Pago().RegistrarAvisoMedioExterno(aviso, admin); Check(!r.Exitoso && N("SELECT COUNT(*) FROM Pago WHERE identificadorExterno=@p0", aviso.IdentificadorExterno) == 0, "Aviso no asociado fallido tampoco queda guardado"); });
        var noAsociado = new BLL_Pago().RegistrarAvisoMedioExterno(aviso, admin); Preparado(noAsociado);
        Check(S("SELECT estado FROM Pago WHERE idPago=@p0", noAsociado.Valor) == "NoAsociado" && Saldo() == 0, "Aviso no asociado normal no altera saldo");
    }
    static SolicitudSoporteNueva Solicitud()
    {
        return new SolicitudSoporteNueva { IdUsuarioExterno = usuario, Motivo = "Consulta QA " + Id(), FuncionalidadVinculada = "Otra", InformacionAdicional = "Ocurre en el centro de ayuda de StageUp", Descripcion = "Intentaba consultar informacion de una pantalla y encontre un problema con la visualizacion del detalle de mi solicitud.", Origen = "Soporte" };
    }
    static string Estado(int id) { return S("SELECT estado FROM Ticket WHERE idTicket=@p0", id); }
    static int Historial(int id) { return N("SELECT COUNT(*) FROM TicketActualizacion WHERE idTicket=@p0", id); }
    static int Mensajes(int id) { return N("SELECT COUNT(*) FROM TicketMensaje WHERE idTicket=@p0", id); }
    static void Soporte()
    {
        var b = new BLL_Ticket(); var datos = Solicitud(); int tickets = N("SELECT COUNT(*) FROM Ticket");
        ConFallo(() => { Check(!b.RegistrarSolicitud(datos).Exitoso && N("SELECT COUNT(*) FROM Ticket") == tickets, "Alta de soporte fallida revierte ticket e historial"); });
        var alta = b.RegistrarSolicitud(datos); Preparado(alta); int id = alta.Valor;
        Check(Estado(id) == "Pendiente" && Historial(id) == 1 && b.ObtenerDetalleParaUsuario(id, usuario).Valor.Ticket.Codigo.StartsWith("SOP-"), "Alta normal conserva codigo y estado inicial");
        Check(!b.ObtenerDetalleParaUsuario(id, ajeno).Exitoso && !b.AgregarMensajeUsuario(id, ajeno, "Mensaje ajeno").Exitoso, "Soporte mantiene acceso exclusivo del usuario");
        int mensajes = Mensajes(id);
        ConFallo(() => { Check(!b.AgregarMensajeUsuario(id, usuario, "Mensaje QA del usuario").Exitoso && Mensajes(id) == mensajes, "Mensaje de usuario fallido no persiste"); });
        Preparado(b.AgregarMensajeUsuario(id, usuario, "Mensaje QA del usuario")); Check(Mensajes(id) == mensajes + 1, "Mensaje normal se agrega a la conversacion");
        int historial = Historial(id); mensajes = Mensajes(id);
        ConFallo(() => { Check(!b.RegistrarRespuesta(id, admin, "Respuesta QA suficientemente detallada", true, "Pendiente").Exitoso && Estado(id) == "Pendiente" && Historial(id) == historial && Mensajes(id) == mensajes, "Respuesta fallida revierte mensaje, estado e historial"); });
        Preparado(b.RegistrarRespuesta(id, admin, "Respuesta QA suficientemente detallada", true, "Pendiente"));
        Check(Estado(id) == "Resuelta" && Mensajes(id) == mensajes + 1, "Respuesta normal puede resolver solicitud");
        historial = Historial(id); mensajes = Mensajes(id);
        ConFallo(() => { Check(!b.AgregarMensajeUsuario(id, usuario, "Todavia necesito ayuda con la consulta").Exitoso && Estado(id) == "Resuelta" && Historial(id) == historial && Mensajes(id) == mensajes, "Mensaje fallido en resuelta no reabre parcialmente"); });
        Preparado(b.AgregarMensajeUsuario(id, usuario, "Todavia necesito ayuda con la consulta")); Check(Estado(id) == "EnProceso", "Mensaje normal reabre solicitud resuelta");
        Check(!b.CambiarEstado(id, "Cerrada", admin, false, "EnProceso").Exitoso, "Transicion directa no permitida sigue bloqueada");
        Preparado(b.CambiarEstado(id, "Resuelta", admin, false, "EnProceso")); historial = Historial(id);
        ConFallo(() => { Check(!b.CambiarEstado(id, "Cerrada", admin, false, "Resuelta").Exitoso && Estado(id) == "Resuelta" && Historial(id) == historial, "Cierre fallido conserva estado e historial"); });
        Preparado(b.CambiarEstado(id, "Cerrada", admin, false, "Resuelta"));
        Check(!b.AgregarMensajeUsuario(id, usuario, "Mensaje despues del cierre").Exitoso && !b.RegistrarRespuesta(id, admin, "Respuesta despues del cierre", false, "Cerrada").Exitoso, "Solicitudes cerradas mantienen solo consulta");
        Check(!b.CambiarEstado(id, "EnProceso", admin, false, "Cerrada").Exitoso, "Reapertura sin permiso sigue rechazada");
        ConFallo(() => { Check(!b.CambiarEstado(id, "EnProceso", admin, true, "Cerrada").Exitoso && Estado(id) == "Cerrada", "Reapertura fallida no cambia estado"); });
        Preparado(b.CambiarEstado(id, "EnProceso", admin, true, "Cerrada")); Check(Estado(id) == "EnProceso", "Reapertura autorizada funciona");
        Check(!b.CambiarEstado(id, "Resuelta", admin, false, "Pendiente").Exitoso, "Estado desactualizado sigue rechazado");
        int responsable = b.ListarResponsables().First().IdUsuarioInterno; Preparado(b.Asignar(id, null, admin)); historial = Historial(id);
        ConFallo(() => { Check(!b.Asignar(id, responsable, admin).Exitoso && N("SELECT COUNT(*) FROM Ticket WHERE idTicket=@p0 AND idUsuarioInternoAsignado IS NULL", id) == 1 && Historial(id) == historial, "Asignacion fallida no deja responsable ni historial parcial"); });
        Preparado(b.Asignar(id, responsable, admin)); Check(N("SELECT idUsuarioInternoAsignado FROM Ticket WHERE idTicket=@p0", id) == responsable, "Asignacion normal funciona");
        Preparado(b.Asignar(id, null, admin)); Check(N("SELECT COUNT(*) FROM Ticket WHERE idTicket=@p0 AND idUsuarioInternoAsignado IS NULL", id) == 1, "Quitar responsable funciona");
        var tomar = b.RegistrarSolicitud(Solicitud()); Preparado(tomar); historial = Historial(tomar.Valor);
        ConFallo(() => { Check(!b.Tomar(tomar.Valor, responsable).Exitoso && Estado(tomar.Valor) == "Pendiente" && Historial(tomar.Valor) == historial && N("SELECT COUNT(*) FROM Ticket WHERE idTicket=@p0 AND idUsuarioInternoAsignado IS NULL", tomar.Valor) == 1, "Tomar fallido revierte responsable, estado e historial"); });
        Preparado(b.Tomar(tomar.Valor, responsable)); Check(Estado(tomar.Valor) == "EnProceso", "Tomar normalmente asigna e inicia atencion");
        using (var inicio = new ManualResetEventSlim(false))
        {
            var tareas = Enumerable.Range(0, 2).Select(i => Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_Ticket().RegistrarRespuesta(tomar.Valor, responsable, "Respuesta simultanea QA " + i, true, "EnProceso"); })).ToArray();
            inicio.Set(); Task.WaitAll(tareas);
            Check(tareas.Count(t => t.Result.Exitoso) == 1 && Estado(tomar.Valor) == "Resuelta", "Respuestas simultaneas respetan estado visto");
        }
        Check(b.ObtenerSeguimiento(id, usuario).Exitoso && b.ObtenerDetalleInterno(id, true).Exitoso, "Detalle, conversacion e historial siguen consultables");
    }
    static int Main()
    {
        try
        {
            if (!new SqlConnectionStringBuilder(Cadena).InitialCatalog.StartsWith("StageUp_Beta2_QA_", StringComparison.Ordinal)) throw new Exception("Solo bases QA");
            ThreadPool.SetMinThreads(12, 12);
            admin = N("SELECT TOP 1 idUsuarioInterno FROM UsuarioInterno WHERE estadoCuenta='Activa' AND activo=1");
            usuario = N("SELECT TOP 1 idUsuarioExterno FROM UsuarioExterno WHERE estadoCuenta='Activa' AND activo=1");
            ajeno = N("SELECT TOP 1 idUsuarioExterno FROM UsuarioExterno WHERE activo=1 AND idUsuarioExterno<>@p0", usuario);
            Sql("UPDATE UsuarioExterno SET correoElectronico=@p0 WHERE idUsuarioExterno=@p1", "qa-" + Id() + "@example.test", usuario);
            Pagos(); Soporte(); Console.WriteLine("TOTAL " + pruebas + " comprobaciones correctas"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
