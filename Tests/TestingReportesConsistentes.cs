using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using StageUp.BE.Entidades;
using StageUp.BLL;

class TestingReportesConsistentes
{
    static string Cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int checks;
    static object Sql(string s)
    {
        using(var c=new SqlConnection(Cs))using(var q=new SqlCommand(s,c)){c.Open();return q.ExecuteScalar();}
    }
    static void Check(bool ok,string nombre){if(!ok)throw new Exception(nombre);checks++;Console.WriteLine("OK "+nombre);}
    static FiltroReporte Filtro(){return new FiltroReporte{Desde=new DateTime(2026,1,1),Hasta=new DateTime(2026,12,31),Moneda="ARS",Agrupacion="Mes"};}
    static ReporteCompleto Leer(FiltroReporte f)
    {
        var r=new BLL_Reporte().ObtenerReporte(f);if(!r.Exitoso)throw new Exception(r.Mensaje);return r.Valor;
    }
    static void Fechas()
    {
        var b=new BLL_Reporte();
        Check(!b.ObtenerReporte(null).Exitoso,"Filtro ausente se rechaza");
        var f=Filtro();f.Hasta=f.Desde.AddDays(-1);Check(!b.ObtenerReporte(f).Exitoso,"Fechas invertidas se rechazan");
        foreach(var fecha in new[]{DateTime.MinValue,new DateTime(1752,12,31),new DateTime(9999,1,1),DateTime.MaxValue.Date})
        {
            f=Filtro();f.Desde=f.Hasta=fecha;var r=b.ObtenerReporte(f);
            Check(!r.Exitoso && r.Mensaje.Contains("Ingresá fechas"),"Fecha fuera del rango devuelve aviso sin excepcion: "+fecha.ToString("yyyy-MM-dd"));
        }
        foreach(var agrupacion in new[]{"Dia","Semana","Mes","Anio"})
        {
            foreach(var fecha in new[]{new DateTime(1753,1,1),new DateTime(9998,12,31)})
            {
                f=Filtro();f.Desde=f.Hasta=fecha;f.Agrupacion=agrupacion;
                Check(Leer(f).Ingresos.Count==1,"Fecha limite funciona con agrupacion "+agrupacion+": "+fecha.ToString("yyyy-MM-dd"));
            }
        }
        f=Filtro();f.Desde=new DateTime(9990,1,1);f.Hasta=new DateTime(9998,12,31);f.Agrupacion="Anio";
        Check(Leer(f).Ingresos.Count==9,"Rango alto valido no desborda al calcular diez anios");
        f=Filtro();f.Desde=new DateTime(2016,2,29);f.Hasta=new DateTime(2026,2,28);f.Agrupacion="Anio";
        Check(b.ObtenerReporte(f).Exitoso,"Se conserva limite de diez anios desde fecha bisiesta");
        f=Filtro();f.Desde=new DateTime(2010,2,28);f.Hasta=new DateTime(2020,2,29);
        Check(!b.ObtenerReporte(f).Exitoso,"Se rechaza el dia adicional sobre diez anios");
        f=Filtro();f.Hasta=f.Desde.AddYears(11);Check(!b.ObtenerReporte(f).Exitoso,"Rango mayor a diez anios sigue rechazado");
        f=Filtro();f.Moneda="XXX";Check(!b.ObtenerReporte(f).Exitoso,"Moneda invalida sigue rechazada");
    }
    static void Provincia()
    {
        Sql("UPDATE FichaEspacio SET provincia=N'QA Provincia' WHERE idEspacioArtistico=(SELECT TOP 1 idEspacioArtistico FROM Reserva WHERE estadoReserva IN(N'Aceptada',N'Finalizada') AND estadoPago IN(N'Pagado',N'NoRequerido') AND fechaSolicitada BETWEEN '20260101' AND '20261231' AND moneda='ARS' ORDER BY idEspacioArtistico)");
        var completo=Leer(Filtro());
        Check(completo.Ingresos.Count==12 && completo.ZonasDetalle.Select(z=>z.Provincia).Distinct().Count()>1,"Reporte anual sin provincia conserva todas las zonas");
        var f=Filtro();f.Provincia="QA Provincia";var filtrado=Leer(f);
        Check(filtrado.ZonasDetalle.Count>0 && filtrado.ZonasDetalle.All(z=>z.Provincia==f.Provincia),"Detalle de zonas aplica la provincia elegida");
        Check(filtrado.Zonas.All(z=>z.Provincia==f.Provincia),"Grafico de zonas aplica la provincia elegida");
        Check(filtrado.ZonasDetalle.Sum(z=>z.CantidadReservas)==filtrado.TotalReservasPeriodo && filtrado.ZonasDetalle.Sum(z=>z.ImporteReservas)==filtrado.TotalImporteReservas,"Totales de zonas y periodos coinciden con el mismo filtro");
        Check(filtrado.TotalReservasPeriodo<completo.TotalReservasPeriodo,"El filtro excluye reservas de otras provincias");
        Check(filtrado.Indicadores.ImporteReservas==completo.Indicadores.ImporteReservas,"Indicadores globales conservan el alcance indicado en pantalla");
        string csv=BLL_Reporte.GenerarCsvReporteCompleto(filtrado);
        string zonas=csv.Split(new[]{"Ingresos por zona"},StringSplitOptions.None)[1].Split(new[]{"Reservas por estado"},StringSplitOptions.None)[0];
        Check(zonas.Contains("QA Provincia") && !zonas.Contains("Buenos Aires"),"CSV de zonas respeta el filtro aplicado");
        foreach(string agrupacion in new[]{"Dia","Semana","Mes","Anio"})
        {
            f=Filtro();f.Provincia="QA Provincia";f.Agrupacion=agrupacion;var r=Leer(f);
            Check(r.TotalImporteReservas==filtrado.TotalImporteReservas && r.ZonasDetalle.Sum(z=>z.CantidadReservas)==r.TotalReservasPeriodo,"Agrupacion "+agrupacion+" conserva resultados filtrados");
        }
        f=Filtro();f.Provincia="QA Provincia";f.Moneda="USD";var dolares=Leer(f);
        Check(dolares.ZonasDetalle.All(z=>z.Provincia==f.Provincia) && dolares.ZonasDetalle.Sum(z=>z.ImporteReservas)==dolares.TotalImporteReservas,"Moneda y provincia se combinan correctamente");
        f=Filtro();f.Provincia="Provincia inexistente QA";var vacio=Leer(f);
        Check(vacio.Zonas.Count==0 && vacio.TotalReservasPeriodo==0,"Provincia sin movimientos devuelve reporte vacio coherente");
        f=Filtro();f.Provincia="QA Provincia";f.TipoEspacio="Tipo inexistente QA";vacio=Leer(f);
        Check(vacio.Zonas.Count==0 && vacio.TotalReservasPeriodo==0,"Tipo de espacio se combina con provincia");
        f=Filtro();f.Provincia="  QA Provincia  ";Check(Leer(f).TotalImporteReservas==filtrado.TotalImporteReservas,"Se conserva normalizacion de espacios en el filtro");
        var final=Leer(Filtro());Check(final.TotalReservasPeriodo==completo.TotalReservasPeriodo && final.TotalImporteReservas==completo.TotalImporteReservas,"Quitar filtro recupera resultados originales");
    }
    static int Main()
    {
        try
        {
            if(!new SqlConnectionStringBuilder(Cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_"))throw new Exception("Solo bases QA");
            Fechas();Provincia();Console.WriteLine("TOTAL "+checks+" comprobaciones correctas");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
