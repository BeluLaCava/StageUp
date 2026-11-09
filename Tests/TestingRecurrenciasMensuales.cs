using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.MPP;

class TestingRecurrenciasMensuales
{
    static string cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int gestor,cliente,checks;
    static object Sql(string s, params object[] p){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(s,c)){for(int i=0;i<p.Length;i++)q.Parameters.AddWithValue("@p"+i,p[i]);c.Open();return q.ExecuteScalar();}}
    static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;Console.WriteLine("PASS: "+name);}
    static void Need(ResultadoOperacion r){if(!r.Exitoso)throw new Exception("Preparacion: "+r.Mensaje);}
    static DateTime Lunes(int meses){var f=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).AddMonths(meses);while(f.DayOfWeek!=DayOfWeek.Monday)f=f.AddDays(1);return f;}
    static int Espacio(){var e=new EspacioArtistico{IdUsuarioGestor=gestor,NombreEspacio="QA mensual "+Guid.NewGuid().ToString("N"),TipoEspacio="Sala",Descripcion="QA recurrencias",Ficha=new FichaEspacio{Provincia="Buenos Aires",Ciudad="CABA",Direccion="QA 123",CapacidadMaxima=20,PrecioHora=1000m,Moneda="ARS",TipoPiso="Madera",Disponibilidad=Enumerable.Range(1,7).Select(d=>new FranjaEspacio{DiaSemana=d,MinutoDesde=0,MinutoHasta=1440}).ToList()}};int id=new MPP_EspacioArtistico().GuardarFicha(e);Sql("UPDATE EspacioArtistico SET publicado=1,estadoEspacio='Publicado' WHERE idEspacioArtistico="+id);return id;}
    static Actividad Mensual(int espacio,int semana=1,int dia=1,int desde=600){return new Actividad{IdEspacioArtistico=espacio,Nombre="QA mensual",ModoRecurrencia="Mensual",SemanaDelMes=semana,DiaSemanaMensual=dia,MinutoDesde=desde,MinutoHasta=desde+60,CupoMaximo=10,Activa=true};}
    static Actividad Puntual(int espacio,DateTime fecha){return new Actividad{IdEspacioArtistico=espacio,Nombre="QA puntual",ModoRecurrencia="Fecha",Fecha=fecha.ToString("yyyy-MM-dd"),MinutoDesde=600,MinutoHasta=660,CupoMaximo=10,Activa=true};}
    static EspacioArtistico Leer(int id){return new MPP_EspacioArtistico().ObtenerPorId(new EspacioArtistico{IdEspacioArtistico=id});}
    static ResultadoOperacion<int> Reservar(int id,DateTime fecha,int hora=600){return new BLL_Reserva().SolicitarReserva(cliente,id,fecha,hora,60,"QA mensual");}
    static int Buscar(string sp,int id,DateTime fecha,int hora){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(sp,c)){q.CommandType=CommandType.StoredProcedure;q.Parameters.AddWithValue("@fechaDisponibilidad",fecha);q.Parameters.AddWithValue("@minutoDesde",hora);q.Parameters.AddWithValue("@minutoHasta",hora+60);q.Parameters.AddWithValue(sp=="sp_Busqueda_Publica"?"@texto":"@textoBusqueda",Leer(id).NombreEspacio);c.Open();using(var r=q.ExecuteReader()){int n=0;while(r.Read())n++;return n;}}}
    static void Reglas()
    {
        int muestras=0;
        foreach(int year in new[]{2028,2030,2096,2100,2400,9999})foreach(int month in new[]{1,2,4,12})foreach(int dow in Enumerable.Range(1,7))foreach(int ordinal in Enumerable.Range(1,5)){
            var ocurrencias=Enumerable.Range(1,DateTime.DaysInMonth(year,month)).Select(d=>new DateTime(year,month,d)).Where(d=>BLL_DisponibilidadEspacio.DiaSemanaDe(d)==dow).ToList();
            DateTime esperado=ordinal==5?ocurrencias.Last():ocurrencias[ordinal-1];
            var f=new FranjaEspacio{Bloqueado=true,DiaSemana=dow,SemanaDelMes=ordinal};
            foreach(int day in Enumerable.Range(1,DateTime.DaysInMonth(year,month))){var fecha=new DateTime(year,month,day);if(BLL_DisponibilidadEspacio.BloqueoRigeEnFecha(f,fecha)!=(fecha==esperado))throw new Exception("Regla mensual incorrecta "+fecha);muestras++;}
        }
        Check(true,"reglas primero a cuarto y ultimo, bisiestos y fecha extrema: "+muestras+" fechas contrastadas");
    }
    static void Flujos()
    {
        var b=new BLL_Actividad();int a=Espacio();var mensual=Mensual(a);Need(b.Guardar(mensual,gestor));
        Check(!Reservar(a,Lunes(1)).Exitoso,"bloqueo cercano se conserva");
        Check(!Reservar(a,Lunes(14)).Exitoso,"mensual impide reserva a 14 meses");
        Check(!Reservar(a,Lunes(60)).Exitoso,"mensual impide reserva a cinco anios");
        Check(Reservar(a,Lunes(14),660).Exitoso,"reserva contigua sigue permitida");
        Check(Reservar(a,Lunes(14).AddDays(7)).Exitoso,"otro lunes del mes sigue disponible");
        Check(!b.Guardar(Puntual(a,Lunes(14)),gestor).Exitoso,"mensual impide actividad puntual lejana");
        Check(b.Guardar(Puntual(a,Lunes(14).AddDays(1)),gestor).Exitoso,"actividad en otro dia sigue permitida");
        int previo=Espacio();int reserva=Reservar(previo,Lunes(14)).Valor;
        Check(!b.Guardar(Mensual(previo),gestor).Exitoso,"reserva pendiente previa impide nueva mensual");
        Need(new BLL_Reserva().Aceptar(reserva,gestor,"QA"));
        Check(!b.Guardar(Mensual(previo),gestor).Exitoso,"reserva aceptada previa impide nueva mensual");
        int puntual=Espacio();Need(b.Guardar(Puntual(puntual,Lunes(14)),gestor));
        Check(!b.Guardar(Mensual(puntual),gestor).Exitoso,"puntual previa impide mensual lejana");
        int pares=Espacio();Need(b.Guardar(Mensual(pares,1),gestor));
        Check(b.Guardar(Mensual(pares,2),gestor).Exitoso,"primer y segundo lunes pueden coexistir");
        Check(!b.Guardar(Mensual(pares,1),gestor).Exitoso,"dos mensuales iguales no se superponen");
        int ultimo=Espacio();Need(b.Guardar(Mensual(ultimo,5),gestor));
        Check(!b.Guardar(Mensual(ultimo,4),gestor).Exitoso,"cuarto y ultimo lunes detectan meses compartidos");
        Check(b.Guardar(Mensual(ultimo,3),gestor).Exitoso,"tercero y ultimo lunes no se confunden");
        int semanal=Espacio();Need(b.Guardar(Mensual(semanal),gestor));
        Check(!b.Guardar(new Actividad{IdEspacioArtistico=semanal,Nombre="QA semanal",ModoRecurrencia="Semanal",DiasSemana=new List<int>{1},MinutoDesde=600,MinutoHasta=660,CupoMaximo=10,Activa=true},gestor).Exitoso,"semanal respeta mensual");
        var dispo=new BLL_DisponibilidadEspacio();
        Check(!dispo.BloquearHorario(a,gestor,true,null,Lunes(14).ToString("yyyy-MM-dd"),"10:00","11:00","QA bloqueo futuro").Exitoso,"bloqueo manual futuro respeta mensual");
        int manual=Espacio();Need(dispo.BloquearHorario(manual,gestor,true,null,Lunes(14).ToString("yyyy-MM-dd"),"10:00","11:00","QA bloqueo futuro"));
        Check(!b.Guardar(Mensual(manual),gestor).Exitoso,"mensual respeta bloqueo manual futuro previo");
        var espacio=Leer(a);
        var patron=espacio.Ficha.Disponibilidad.Single(f=>f.IdActividad==mensual.IdActividad);
        Check(BLL_DisponibilidadEspacio.DescribirCuando(patron)=="Primer lunes de cada mes","descripcion mensual distingue recurrencia semanal");
        var describir=typeof(StageUp.UI.Explorar.DetalleEspacio).GetMethod("FormatearDisponibilidad",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        var textos=(List<string>)describir.Invoke(null,new object[]{new List<FranjaEspacio>{patron}});
        Check(textos.Single().Contains("Primer lunes de cada mes") && textos.Single().Contains("10:00 a 11:00"),"ficha publica describe solo el horario mensual ocupado");
        Check(espacio.Ficha.Disponibilidad.Count(f=>f.IdActividad==mensual.IdActividad)==1,"lectura usa una regla sin duplicar las doce fechas almacenadas");
        Check(new MPP_EspacioArtistico().ListarPorUsuarioGestor(new UsuarioExterno{IdUsuarioExterno=gestor}).Single(e=>e.IdEspacioArtistico==a).Ficha.Disponibilidad.Any(f=>f.SemanaDelMes==1),"listado del gestor conserva regla mensual");
        Check(new MPP_EspacioArtistico().ListarPublicados().Single(e=>e.IdEspacioArtistico==a).Ficha.Disponibilidad.Any(f=>f.SemanaDelMes==1),"listado publico conserva regla mensual");
        var dias=BLL_DisponibilidadEspacio.ArmarCalendario(espacio,new List<Reserva>(),Lunes(14),8);
        Check(dias[0].Items.Count(i=>i.Tipo==ItemCalendarioDisponibilidad.TipoActividad)==1 && dias[7].Items.All(i=>i.Tipo!=ItemCalendarioDisponibilidad.TipoActividad),"calendario muestra solo la ocurrencia mensual lejana");
        int busqueda=Espacio();var act=Mensual(busqueda);Need(b.Guardar(act,gestor));
        foreach(string sp in new[]{"sp_Busqueda_Publica","sp_EspacioArtistico_BuscarPublicados"}){
            Check(Buscar(sp,busqueda,Lunes(14),600)==0,"busqueda excluye horario mensual lejano: "+sp);
            Check(Buscar(sp,busqueda,Lunes(14),660)==1,"busqueda conserva horario contiguo: "+sp);
            Check(Buscar(sp,busqueda,Lunes(14).AddDays(7),600)==1,"busqueda conserva otro lunes: "+sp);
        }
        Check(new MPP_EspacioArtistico().BuscarPublicados(new FiltroBusquedaEspacios{TextoBusqueda=Leer(busqueda).NombreEspacio}).Single().Ficha.Disponibilidad.Any(f=>f.SemanaDelMes==1),"busqueda por IDs conserva regla mensual");
        int ultimoBusqueda=Espacio();Need(b.Guardar(Mensual(ultimoBusqueda,5),gestor));
        var mes=new DateTime(DateTime.Today.Year+3,1,1);
        for(int i=0;i<12;i++){
            var inicio=mes.AddMonths(i);var lunes=Enumerable.Range(1,DateTime.DaysInMonth(inicio.Year,inicio.Month)).Select(d=>new DateTime(inicio.Year,inicio.Month,d)).Where(d=>d.DayOfWeek==DayOfWeek.Monday).ToArray();
            foreach(string sp in new[]{"sp_Busqueda_Publica","sp_EspacioArtistico_BuscarPublicados"})
                if(Buscar(sp,ultimoBusqueda,lunes.Last(),600)!=0 || Buscar(sp,ultimoBusqueda,lunes[lunes.Length-2],600)!=1)throw new Exception("Busqueda ultimo lunes incorrecta");
        }
        Check(true,"busquedas distinguen ultimo lunes de penultimo durante doce meses");
        act.MinutoDesde=720;act.MinutoHasta=780;Need(b.Guardar(act,gestor));
        Check(Reservar(busqueda,Lunes(14)).Exitoso && !Reservar(busqueda,Lunes(14),720).Exitoso,"editar mensual libera horario anterior y bloquea el nuevo");
        Check(b.DarDeBaja(act.IdActividad,gestor).Exitoso,"baja de actividad conserva flujo");
        Check(Reservar(busqueda,Lunes(14),720).Exitoso,"baja libera tambien horarios lejanos");
        int simultaneo=Espacio();var gate=new ManualResetEventSlim(false);
        var t1=Task.Run(()=>{gate.Wait();return (ResultadoOperacion)b.Guardar(Mensual(simultaneo),gestor);});
        var t2=Task.Run(()=>{gate.Wait();return (ResultadoOperacion)Reservar(simultaneo,Lunes(14));});
        gate.Set();Task.WaitAll(t1,t2);
        Check(t1.Result.Exitoso!=t2.Result.Exitoso,"reserva y actividad mensual simultaneas: solo una ocupa horario");
    }
    static int Main(){try{
        if(!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_"))throw new Exception("Solo QA");
        gestor=Convert.ToInt32(Sql("SELECT TOP 1 idUsuarioGestor FROM EspacioArtistico WHERE activo=1 AND publicado=1 ORDER BY idEspacioArtistico"));
        cliente=Convert.ToInt32(Sql("SELECT TOP 1 idUsuarioExterno FROM UsuarioExterno WHERE estadoCuenta='Activa' AND idUsuarioExterno<>"+gestor+" ORDER BY idUsuarioExterno"));
        Reglas();Flujos();Console.WriteLine("TOTAL "+checks+" comprobaciones correctas");return 0;
    }catch(Exception ex){Console.WriteLine(ex);return 1;}}
}
