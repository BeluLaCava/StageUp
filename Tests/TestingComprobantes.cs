
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

class TestingComprobantes
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
    static int Main()
    {
        try
        {
            if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_")) throw new Exception("Se exige base QA aislada");
            Fixture();
            var bll = new BLL_Comprobante();
            int admin = N("SELECT TOP 1 idUsuarioInterno FROM dbo.UsuarioInterno WHERE activo=1 AND estadoCuenta=N'Activa' ORDER BY idUsuarioInterno");
            int nc=0, nd=0, reservaCargo=0;
            foreach(int dias in new [] {30,10,2})
            {
                fecha=DateTime.Today.AddDays(dias);
                int r=Reserva(600);
                Check(Pagar(r).Exitoso,"pagar reserva a " + dias + " días");
                Check(new BLL_Reserva().Cancelar(r,cliente).Exitoso,"cancelar reserva a " + dias + " días");
                var notas=bll.ListarNotasDeReservaParaUsuario(r,cliente);
                Check(notas.Exitoso && notas.Valor.Count == (dias==30 ? 1 : 2),"cantidad de notas según cargo: " + dias);
                Check(notas.Valor.Single(c=>c.Tipo=="NC").Importe==1000m,"NC por total abonado");
                if(dias!=30) Check(notas.Valor.Single(c=>c.Tipo=="ND").Importe==(dias==10?500m:1000m),"ND por cargo original");
                Check(bll.ListarNotasDeReservaParaUsuario(r,gestor).Valor.Count==0,"gestor no accede a notas del solicitante");
                nc=notas.Valor.Single(c=>c.Tipo=="NC").IdComprobante;
                Check(bll.ObtenerParaUsuario(nc,cliente).Exitoso,"titular consulta NC");
                Check(!bll.ObtenerParaUsuario(nc,gestor).Exitoso,"otro usuario no consulta NC por ID");
                Check(bll.ObtenerParaInterno(nc,admin).Exitoso,"administrador autorizado consulta NC");
                Check(!bll.ObtenerParaInterno(nc,int.MaxValue).Exitoso,"interno inexistente rechazado");
                if(dias==2) { nd=notas.Valor.Single(c=>c.Tipo=="ND").IdComprobante; reservaCargo=r; }
            }
            fecha=DateTime.Today.AddDays(40);
            int sinPagar=Reserva(720);
            Check(new BLL_Reserva().Cancelar(sinPagar,cliente).Exitoso,"cancelar sin pagar");
            Check(bll.ListarNotasDeReservaParaUsuario(sinPagar,cliente).Valor.Count==0,"sin pago no se inventan notas");
            Check(!bll.ObtenerParaUsuario(-1,cliente).Exitoso && !bll.ObtenerParaUsuario(int.MaxValue,cliente).Exitoso,"IDs inválidos rechazados");
            int fc=N("SELECT TOP 1 idComprobante FROM dbo.Comprobante WHERE tipo=N'FC'");
            Check(!bll.ObtenerParaInterno(fc,admin).Exitoso,"factura conserva su circuito propio");
            var cuenta=new BLL_CuentaCorriente();
            var manual=cuenta.EmitirComprobanteManual("NC",gestor,"Gestor",23.5m,"USD","Ajuste de prueba <script> & comillas '",null,admin);
            Check(manual.Exitoso,"nota manual sin reserva en USD");
            int manualId=manual.Valor;
            Check(bll.ObtenerParaUsuario(manualId,gestor).Valor.Moneda=="USD","titular gestor consulta su nota");
            Check(cuenta.AnularComprobante(manualId,"Anulación de prueba",admin).Exitoso,"anular nota manual");
            Check(bll.ObtenerParaUsuario(manualId,gestor).Valor.Estado=="Anulado","nota anulada conserva estado");
            string estado=Convert.ToString(Sql("SELECT estadoCuenta FROM dbo.UsuarioExterno WHERE idUsuarioExterno="+cliente));
            try { Sql("UPDATE dbo.UsuarioExterno SET estadoCuenta=N'Inactiva' WHERE idUsuarioExterno="+cliente); Check(!bll.ObtenerParaUsuario(nc,cliente).Exitoso,"cuenta inactiva no accede"); }
            finally { Sql("UPDATE dbo.UsuarioExterno SET estadoCuenta=N'"+estado+"' WHERE idUsuarioExterno="+cliente); }
            string huella=Huella();
            for(int i=0;i<3;i++) { bll.ObtenerParaUsuario(nc,cliente); bll.ObtenerParaInterno(nd,admin); bll.ListarNotasDeReservaParaUsuario(reservaCargo,cliente); }
            Check(huella==Huella(),"consultas repetidas no emiten notas ni alteran reservas, movimientos o importes");
            System.IO.File.WriteAllText("notes-qa-ids.json","{\"cliente\":"+cliente+",\"gestor\":"+gestor+",\"admin\":"+admin+",\"nc\":"+nc+",\"nd\":"+nd+",\"anulada\":"+manualId+",\"reserva\":"+reservaCargo+"}");
            Console.WriteLine(checks+" checks passed");return 0;
        }
        catch(Exception ex) { Console.WriteLine(ex.ToString()); return 1; }
    }
    static string Huella()
    {
        return string.Join("/",new [] {"Comprobante","MovimientoCuentaCorriente","Reserva","TransaccionEconomica","Pago"}.Select(t=>Convert.ToString(Sql("SELECT CONCAT(COUNT_BIG(*),':',CHECKSUM_AGG(BINARY_CHECKSUM(*))) FROM dbo."+t))));
    }
}
