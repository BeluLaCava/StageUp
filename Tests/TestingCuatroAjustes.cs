using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.UI.Infraestructura;

class TestingCuatroAjustes
{
    static string cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int checks,gestor,cliente,espacio;
    static string procedimiento;
    static Dictionary<string,string> originales=new Dictionary<string,string>();
    static readonly string[] claves={BLL_ParametroPlataforma.ComisionPlataformaPorcentaje,BLL_ParametroPlataforma.DiasCancelacionSinCargo,BLL_ParametroPlataforma.DiasCancelacionCargoParcial,BLL_ParametroPlataforma.PorcentajeCargoParcial,BLL_ParametroPlataforma.PorcentajeCargoTotal};
    static object Sql(string texto){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(texto,c)){c.Open();return q.ExecuteScalar();}}
    static int N(string texto){return Convert.ToInt32(Sql(texto));}
    static void Check(bool ok,string nombre){if(!ok)throw new Exception("FAIL: "+nombre);checks++;Console.WriteLine("PASS: "+nombre);}
    static void Valor(string clave,string valor){using(var c=new SqlConnection(cs))using(var q=new SqlCommand("UPDATE dbo.ParametroPlataforma SET valor=@valor WHERE clave=@clave",c)){q.Parameters.AddWithValue("@valor",valor);q.Parameters.AddWithValue("@clave",clave);c.Open();q.ExecuteNonQuery();}BLL_ParametroPlataforma.InvalidarCache();}
    static void RestaurarProcedimiento(){if(procedimiento!=null)Sql(Regex.Replace(procedimiento,@"(?i)CREATE\s+(?:OR\s+ALTER\s+)?PROCEDURE","ALTER PROCEDURE"));BLL_ParametroPlataforma.InvalidarCache();}
    static void FalloParametros(){Sql("ALTER PROCEDURE dbo.sp_ParametroPlataforma_Listar AS BEGIN RAISERROR('Fallo QA controlado',16,1); END");BLL_ParametroPlataforma.InvalidarCache();}
    static void FalloAuditoria(){Sql("CREATE OR ALTER TRIGGER dbo.QA_CuatroAuditoria ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA controlado',1; END");}
    static void QuitarFallo(){Sql("IF OBJECT_ID('dbo.QA_CuatroAuditoria','TR') IS NOT NULL DROP TRIGGER dbo.QA_CuatroAuditoria");}
    static DatosTarjeta Tarjeta(){return new DatosTarjeta {Numero="4111111111111111",Titular="QA Prueba",MesVencimiento=12,AnioVencimiento=DateTime.Today.Year+3,CodigoSeguridad="123"};}
    static int Solicitar(int minuto,int dias){var r=new BLL_Reserva().SolicitarReserva(cliente,espacio,DateTime.Today.AddDays(dias),minuto,60,"QA cuatro ajustes");Check(r.Exitoso,"solicitar reserva de prueba");return r.Valor;}
    static int Aceptar(int minuto,int dias){int id=Solicitar(minuto,dias);Check(new BLL_Reserva().Aceptar(id,gestor,"QA").Exitoso,"aceptar reserva de prueba");return id;}
    static ResultadoOperacion<Pago> Pagar(int id){return new BLL_Pago().PagarReserva(id,cliente,Tarjeta(),new EleccionPago {Modo=ModoPago.Tarjeta});}
    static int Efectos(int id){return N("SELECT (SELECT COUNT(*) FROM dbo.Pago WHERE idReserva="+id+")+(SELECT COUNT(*) FROM dbo.MovimientoCuentaCorriente WHERE idReserva="+id+")+(SELECT COUNT(*) FROM dbo.Comprobante WHERE idReserva="+id+")+(SELECT COUNT(*) FROM dbo.TransaccionEconomica WHERE idReserva="+id+")");}
    static void Fixture()
    {
        procedimiento=Convert.ToString(Sql("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_ParametroPlataforma_Listar'))"));
        foreach(string clave in claves)originales[clave]=Convert.ToString(Sql("SELECT valor FROM dbo.ParametroPlataforma WHERE clave=N'"+clave+"'"));
        string[] valores={"25","14","7","40","90"};for(int i=0;i<claves.Length;i++)Valor(claves[i],valores[i]);
        gestor=N("SELECT TOP 1 idUsuarioGestor FROM dbo.EspacioArtistico WHERE activo=1 AND publicado=1 ORDER BY idEspacioArtistico");
        cliente=N("SELECT TOP 1 idUsuarioExterno FROM dbo.UsuarioExterno WHERE activo=1 AND estadoCuenta=N'Activa' AND idUsuarioExterno<>"+gestor+" ORDER BY idUsuarioExterno");
        espacio=new MPP_EspacioArtistico().GuardarFicha(new EspacioArtistico {IdUsuarioGestor=gestor,NombreEspacio="QA cuatro ajustes "+Guid.NewGuid().ToString("N"),TipoEspacio="Sala",Descripcion="Prueba aislada",Ficha=new FichaEspacio {
            Provincia="Buenos Aires",Ciudad="CABA",Direccion="QA 123",CapacidadMaxima=20,PrecioHora=1000m,Moneda="ARS",TipoPiso="Madera",
            Disponibilidad=Enumerable.Range(1,7).Select(d=>new FranjaEspacio {DiaSemana=d,MinutoDesde=0,MinutoHasta=1440}).ToList() }});
        Sql("UPDATE dbo.EspacioArtistico SET publicado=1,estadoEspacio=N'Publicado' WHERE idEspacioArtistico="+espacio);
    }
    static void Parametros()
    {
        Check(new BLL_ParametroPlataforma().ObtenerDecimal(claves[0])==25m,"se conserva la comision configurada");
        int pendiente=Solicitar(480,30),porPagar=Aceptar(600,30),pagada=Aceptar(600,10);
        var pago=Pagar(pagada);Check(pago.Exitoso,"pago normal con configuracion valida");
        Check(Convert.ToDecimal(Sql("SELECT porcentajeComisionPlataforma FROM dbo.Pago WHERE idReserva="+pagada))==25m,"pago guarda 25 por ciento, sin sustituirlo por 10");
        int efectos=Efectos(pagada);
        FalloParametros();
        try
        {
            bool fallo=false;try{new BLL_ParametroPlataforma().ObtenerDecimal(claves[0]);}catch(ErrorAccesoDatosException){fallo=true;}
            Check(fallo,"lectura fallida no devuelve valor predeterminado");
            Check(!Pagar(porPagar).Exitoso,"pago se detiene si no puede leer parametros");
            Check(Efectos(porPagar)==0,"pago detenido no genera cobro, movimientos, factura ni transaccion");
            Check(!new BLL_Reserva().Aceptar(pendiente,gestor,"QA").Exitoso,"aceptacion se detiene si no puede consultar plazo");
            Check(Convert.ToString(Sql("SELECT estadoReserva FROM dbo.Reserva WHERE idReserva="+pendiente))=="Pendiente","aceptacion fallida conserva solicitud pendiente");
            Check(!new BLL_Reserva().Cancelar(pagada,cliente).Exitoso,"cancelacion se detiene si no puede consultar politica");
            Check(Efectos(pagada)==efectos && Convert.ToString(Sql("SELECT estadoReserva FROM dbo.Reserva WHERE idReserva="+pagada))=="Aceptada","cancelacion fallida conserva reserva y movimientos");
        }finally{RestaurarProcedimiento();}
        Check(Pagar(porPagar).Exitoso,"pago permite reintento al recuperar configuracion");
        Check(new BLL_Reserva().Cancelar(pagada,cliente).Exitoso,"cancelacion permite reintento normal");
        Check(Convert.ToDecimal(Sql("SELECT importeComision FROM dbo.Reserva WHERE idReserva="+pagada))==400m,"cancelacion usa cargo configurado de 40 por ciento");
        foreach(string invalido in new[]{"no-numero","51"})
        {
            Valor(claves[0],invalido);bool fallo=false;
            try{new BLL_ParametroPlataforma().ObtenerDecimal(claves[0]);}catch(ErrorAccesoDatosException){fallo=true;}
            Check(fallo,"comision invalida se rechaza: "+invalido);
        }
        Valor(claves[0],"25");
        Sql("ALTER PROCEDURE dbo.sp_ParametroPlataforma_Listar AS SELECT clave,valor,descripcion,fechaUltimaModificacion,CAST(NULL AS nvarchar(300)) valorProgramado,CAST(NULL AS datetime) fechaVigenciaProgramada FROM dbo.ParametroPlataforma WHERE clave<>N'ComisionPlataformaPorcentaje'");
        try{BLL_ParametroPlataforma.InvalidarCache();bool fallo=false;try{new BLL_ParametroPlataforma().ObtenerDecimal(claves[0]);}catch(ErrorAccesoDatosException){fallo=true;}Check(fallo,"comision ausente no se sustituye por un valor inventado");}finally{RestaurarProcedimiento();}
        Valor(claves[1],"14.5");bool fraccion=false;try{new BLL_ParametroPlataforma().ObtenerEntero(claves[1]);}catch(ErrorAccesoDatosException){fraccion=true;}Check(fraccion,"plazos enteros no se redondean silenciosamente");Valor(claves[1],"14");
    }
    static void Atomicidad()
    {
        int rechazo=Solicitar(720,30);
        FalloAuditoria();try{
            Check(!new BLL_Reserva().Rechazar(rechazo,gestor,"QA rechazo").Exitoso,"rechazo informa fallo de auditoria");
            Check(Convert.ToString(Sql("SELECT estadoReserva FROM dbo.Reserva WHERE idReserva="+rechazo))=="Pendiente","rechazo fallido no queda aplicado");
        }finally{QuitarFallo();}
        Check(!new BLL_Reserva().Rechazar(rechazo,cliente,"QA").Exitoso,"otro usuario no puede rechazar");
        Check(new BLL_Reserva().Rechazar(rechazo,gestor,"QA").Exitoso,"rechazo se puede reintentar con exito");
        Check(!new BLL_Reserva().Rechazar(rechazo,gestor,"QA").Exitoso,"rechazo repetido no se vuelve a aplicar");
        int resena=Aceptar(840,30);Check(Pagar(resena).Exitoso,"prepara reserva para calificar");
        Sql("UPDATE dbo.Reserva SET estadoReserva=N'Finalizada' WHERE idReserva="+resena);
        var b=new BLL_Calificacion();
        FalloAuditoria();try{
            Check(!b.CalificarEspacio(resena,cliente,5,"QA experiencia correcta").Exitoso,"calificacion informa fallo de auditoria");
            Check(!b.CalificarSolicitante(resena,gestor,5,"QA experiencia correcta").Exitoso,"calificacion del gestor informa fallo de auditoria");
            Check(N("SELECT COUNT(*) FROM dbo.Calificacion WHERE idReserva="+resena)==0,"ninguna resena queda publicada tras el fallo");
        }finally{QuitarFallo();}
        Check(!b.CalificarEspacio(resena,gestor,5,"QA experiencia correcta").Exitoso,"calificacion respeta autoria");
        var puerta=new ManualResetEventSlim(false);
        var tareas=Enumerable.Range(0,2).Select(i=>Task.Run(()=>{puerta.Wait();return new BLL_Calificacion().CalificarEspacio(resena,cliente,5,"QA experiencia correcta");})).ToArray();
        puerta.Set();Task.WaitAll(tareas);
        Check(tareas.Count(t=>t.Result.Exitoso)==1,"dos calificaciones simultaneas producen un solo exito");
        Check(b.CalificarSolicitante(resena,gestor,4,"QA experiencia del gestor").Exitoso,"gestor puede calificar normalmente");
        Check(N("SELECT COUNT(*) FROM dbo.Calificacion WHERE idReserva="+resena)==2,"se conservan las dos calificaciones permitidas");
    }
    static byte[] Imagen(ImageFormat formato){using(var b=new Bitmap(40,30))using(var m=new MemoryStream()){using(var g=Graphics.FromImage(b))g.Clear(Color.BurlyWood);b.Save(m,formato);return m.ToArray();}}
    static void Imagenes()
    {
        string carpeta=Path.Combine(Path.GetTempPath(),"StageUp_QA_Imagenes_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(carpeta);
        Func<string,string> map=r=>r.EndsWith("/")?carpeta:Path.Combine(carpeta,Path.GetFileName(r));
        Func<byte[],string,string,int,string> guardar=(datos,nombre,mime,maximo)=>{string error;using(var m=new MemoryStream(datos))return ImagenSubida.Guardar(m,nombre,datos.Length,mime,"~/prueba/",map,out error,maximo);};
        try{
            byte[] png=Imagen(ImageFormat.Png),jpg=Imagen(ImageFormat.Jpeg);
            string guardada=guardar(png,"foto.png","image/png",5*1024*1024);
            Check(guardada!=null && File.Exists(map(guardada)),"foto PNG real se guarda");
            using(var imagen=Image.FromFile(map(guardada)))Check(imagen.Width==40 && imagen.Height==30,"foto guardada se puede abrir y conserva dimensiones pequenas");
            Check(guardar(jpg,"foto.jpg","image/jpeg",5*1024*1024)!=null,"foto JPG real se guarda");
            int cantidad=Directory.GetFiles(carpeta).Length;
            Check(guardar(new byte[]{1,2,3,4},"foto.jpg","image/jpeg",5*1024*1024)==null,"archivo renombrado no pasa como foto");
            Check(guardar(png,"foto.jpg","image/jpeg",5*1024*1024)==null,"extension incompatible se rechaza");
            Check(guardar(png,"foto.png","text/plain",5*1024*1024)==null,"MIME incompatible se rechaza");
            Check(guardar(png.Take(8).ToArray(),"foto.png","image/png",5*1024*1024)==null,"cabecera PNG sin imagen se rechaza");
            Check(Directory.GetFiles(carpeta).Length==cantidad,"archivos invalidos no se guardan");
            byte[] grande=new byte[4*1024*1024];Buffer.BlockCopy(jpg,0,grande,0,jpg.Length);
            Check(guardar(grande,"foto.jpg","image/jpeg",5*1024*1024)!=null,"perfil conserva limite de 5 MB y acepta imagen de 4 MB");
            Check(guardar(grande,"foto.jpg","image/jpeg",ValidadorImagen.MaximoBytes)==null,"otras imagenes conservan limite de 3 MB");
            byte[] exceso=new byte[5*1024*1024+1];Check(guardar(exceso,"foto.jpg","image/jpeg",5*1024*1024)==null,"perfil rechaza archivos mayores de 5 MB");
            string error;using(var m=new MemoryStream(exceso))Check(ImagenSubida.Guardar(m,"foto.jpg",1,"image/jpeg","~/prueba/",map,out error,5*1024*1024)==null,"limite controla bytes reales aunque se declare un tamano menor");
        }finally{foreach(string archivo in Directory.GetFiles(carpeta))File.Delete(archivo);Directory.Delete(carpeta);}
    }
    class Peticion : SimpleWorkerRequest
    {
        string verbo;
        public Peticion(string consulta,string metodo,TextWriter salida):base("DisponibilidadEspacio.aspx",consulta,salida){verbo=metodo;}
        public override string GetHttpVerbName(){return verbo;}
    }
    static void Reintento()
    {
        AppDomain.CurrentDomain.SetData(".appPath",Path.GetTempPath());AppDomain.CurrentDomain.SetData(".appVPath","/StageUp");
        var campos=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        object runtime=typeof(HttpRuntime).GetField("_theRuntime",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
        Type virtualPath=typeof(HttpRuntime).Assembly.GetType("System.Web.VirtualPath");
        object ruta=virtualPath.GetMethod("Create",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(string)},null).Invoke(null,new object[]{"/StageUp/"});
        typeof(HttpRuntime).GetField("_appDomainAppVPath",campos).SetValue(runtime,ruta);
        typeof(HttpRuntime).GetField("_appDomainAppPath",campos).SetValue(runtime,Path.GetTempPath());
        foreach(string verbo in new[]{"GET","POST"}){
            var salida=new StringWriter();var contexto=new HttpContext(new Peticion("id=7&texto=ensayo%20coral&pagina=2",verbo,salida));HttpContext.Current=contexto;
            try{RespuestaErrorSegura.Escribir(contexto,new ErrorCargaListadoException(new Exception("QA")));contexto.Response.Flush();string html=salida.ToString();
                Check(html.Contains("href=\"/StageUp/DisponibilidadEspacio.aspx?id=7&amp;texto=ensayo%20coral&amp;pagina=2\""),"reintento conserva id y filtros: "+verbo);
                Check(!html.Contains("<form") && !html.Contains("__doPostBack"),"reintento navega por enlace sin repetir POST: "+verbo);
            }finally{HttpContext.Current=null;}
        }
    }
    static int Main(string[] args)
    {
        bool habilitado=false;
        try{
            if(args.Length==1 && args[0]=="interfaz"){Imagenes();Reintento();Console.WriteLine(checks+" checks passed.");return 0;}
            if(!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_"))throw new Exception("Solo copia QA");
            if(ConfigurationManager.AppSettings["SmtpHost"]!="127.0.0.1" || ConfigurationManager.AppSettings["SmtpPuerto"]!="1")throw new Exception("Solo correo local inactivo");
            habilitado=true;Fixture();Parametros();Atomicidad();Imagenes();Reintento();Console.WriteLine(checks+" checks passed.");return 0;
        }catch(Exception ex){Console.WriteLine(ex);return 1;}
        finally{if(habilitado){QuitarFallo();RestaurarProcedimiento();foreach(var par in originales)Valor(par.Key,par.Value);}}
    }
}
