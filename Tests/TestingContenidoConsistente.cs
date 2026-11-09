using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.MPP;

class TestingContenidoConsistente
{
 static string cs=ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
 static int admin, checks;
 static string categoria;
 static object Sql(string s,params object[] p){using(var c=new SqlConnection(cs))using(var q=new SqlCommand(s,c)){for(int i=0;i<p.Length;i++)q.Parameters.AddWithValue("@p"+i,p[i]);c.Open();return q.ExecuteScalar();}}
 static int N(string s,params object[] p){return Convert.ToInt32(Sql(s,p));}
 static string S(string s,params object[] p){return Convert.ToString(Sql(s,p));}
 static void Need(bool ok,string texto){if(!ok)throw new Exception(texto);}
 static void Check(bool ok,string texto){Need(ok,"FAIL: "+texto);checks++;Console.WriteLine("PASS: "+texto);}
 static string Token(){return (Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N")).ToUpperInvariant();}
 static SuscriptorNewsletter Suscriptor(string prefijo){
  var s=new SuscriptorNewsletter{CorreoElectronico=prefijo+Guid.NewGuid().ToString("N")+"@example.test",Nombre="QA",TokenPreferencias=Token(),VencimientoConfirmacion=DateTime.Now.AddDays(1),Origen="Formulario"};
  var m=new MPP_SuscriptorNewsletter();string hash=Token();s.IdSuscriptorNewsletter=m.Insertar(s,hash);Need(m.Confirmar(s,hash),"Confirmacion fixture");s.Categorias.Add(categoria);m.GuardarCategorias(s);return s;
 }
 static int CrearNovedad(bool publicada){var r=new BLL_Novedad().Registrar("QA noticia "+Guid.NewGuid().ToString("N"),"Resumen QA para pruebas","Contenido de prueba del newsletter y las novedades.",categoria,null,DateTime.Now.AddMinutes(-1),null,publicada,admin);Need(r.Exitoso,r.Mensaje);return r.Valor;}
 static void Newsletter(){
  Sql("UPDATE dbo.SuscriptorNewsletter SET estado=N'Baja',fechaBaja=GETDATE()");
  var primero=Suscriptor("aaa");var segundo=Suscriptor("zzz");int novedad=CrearNovedad(true);
  var servidor=new TcpListener(IPAddress.Loopback,0);servidor.Start();int puerto=((IPEndPoint)servidor.LocalEndpoint).Port;
  var config=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);config.AppSettings.Settings["SmtpHost"].Value="127.0.0.1";config.AppSettings.Settings["SmtpPuerto"].Value=puerto.ToString();config.AppSettings.Settings["SmtpUsarSsl"].Value="false";config.Save(ConfigurationSaveMode.Modified);ConfigurationManager.RefreshSection("appSettings");
  int recibidos=0;bool recibioDadoDeBaja=false;bool detener=false;
  var receptor=Task.Factory.StartNew(()=>{
   while(!detener){
    TcpClient cliente;
    try{cliente=servidor.AcceptTcpClient();}catch(SocketException){if(detener)return;throw;}catch(ObjectDisposedException){if(detener)return;throw;}
    using(cliente)using(var flujo=cliente.GetStream())using(var lector=new StreamReader(flujo,Encoding.ASCII))using(var escritor=new StreamWriter(flujo,Encoding.ASCII){AutoFlush=true,NewLine="\r\n"}){
     flujo.ReadTimeout=15000;escritor.WriteLine("220 localhost QA");string destinatario="",linea;
     while((linea=lector.ReadLine())!=null){
      if(linea.StartsWith("EHLO")||linea.StartsWith("HELO")){escritor.WriteLine("250 localhost");}
      else if(linea.StartsWith("MAIL FROM")){escritor.WriteLine("250 OK");}
      else if(linea.StartsWith("RCPT TO")){Need(linea.Contains("@example.test"),"Solo destinatarios ficticios");destinatario=linea;escritor.WriteLine("250 OK");}
      else if(linea=="DATA"){
       escritor.WriteLine("354 End with dot");while((linea=lector.ReadLine())!=null&&linea!="."){}
       recibidos++;
       if(recibidos==1){Need(destinatario.Contains(primero.CorreoElectronico),"Orden fixture");var baja=new BLL_Newsletter().DarDeBaja(segundo.TokenPreferencias);Need(baja.Exitoso,baja.Mensaje);Console.WriteLine("BAJA_DURANTE_ENVIO: exitoso="+baja.Exitoso+"; segundo estado="+S("SELECT estado FROM dbo.SuscriptorNewsletter WHERE idSuscriptorNewsletter=@p0",segundo.IdSuscriptorNewsletter));}
       if(destinatario.Contains(segundo.CorreoElectronico))recibioDadoDeBaja=true;
       escritor.WriteLine("250 Accepted QA");
      }else if(linea=="QUIT"){escritor.WriteLine("221 Bye");break;}else{escritor.WriteLine("250 OK");}
     }
    }
   }
  },TaskCreationOptions.LongRunning);
  try{
   var r=new BLL_Novedad().EnviarNewsletter(novedad,"Activos","http://localhost/Novedades.aspx",admin,"http://localhost/");
   Check(r.Exitoso&&recibidos==1&&!recibioDadoDeBaja,"envio real contra SMTP local omite baja ocurrida durante el lote");
   var e=new MPP_EnvioNewsletter().Listar(novedad,1).Single();
   Check(e.CantidadEnviados==1&&e.CantidadOmitidos==1&&e.Estado=="Completo","resultado del envio conserva contadores de enviado y omitido");
   Check(new MPP_EnvioNewsletter().ListarDestinatarios(e.IdEnvioNewsletter).Single(d=>d.IdSuscriptorNewsletter==segundo.IdSuscriptorNewsletter).Estado=="Omitido","baja pendiente queda registrada como omitida");
   Check(!new BLL_Novedad().ReintentarNewsletter(novedad,"http://localhost/Novedades.aspx",admin,"http://localhost/").Exitoso,"reintento no reenvia exitosos ni omitidos");
   Check(!new BLL_Novedad().EnviarNewsletter(novedad,"Activos","http://localhost/Novedades.aspx",admin,"http://localhost/").Exitoso,"doble envio terminado sigue rechazado");
  }finally{detener=true;servidor.Stop();receptor.Wait();}
  Check(N("SELECT COUNT(*) FROM dbo.fn_Newsletter_Destinatarios(N'Activos',@p0) WHERE idSuscriptorNewsletter=@p1",categoria,segundo.IdSuscriptorNewsletter)==0,"baja previa tambien queda excluida");
 }
 static void Fallo(){Sql("CREATE OR ALTER TRIGGER dbo.QA_ContenidoAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END");}
 static void Limpiar(){Sql("IF OBJECT_ID('dbo.QA_ContenidoAudit','TR') IS NOT NULL DROP TRIGGER dbo.QA_ContenidoAudit");}
 static ResultadoOperacion[] Paralelo(params Func<ResultadoOperacion>[] acciones){using(var gate=new ManualResetEventSlim(false)){var tareas=acciones.Select(f=>Task.Factory.StartNew(()=>{gate.Wait();return f();},TaskCreationOptions.LongRunning)).ToArray();gate.Set();Task.WaitAll(tareas);return tareas.Select(t=>t.Result).ToArray();}}
 static void Destinatarios(){
  Sql("UPDATE dbo.SuscriptorNewsletter SET estado=N'Baja',fechaBaja=GETDATE()");
  var s=Suscriptor("qa");var m=new MPP_EnvioNewsletter();int noticia=CrearNovedad(true),id=m.Iniciar(new Novedad{IdNovedad=noticia},"Activos",admin);var d=m.ListarPorEnviar(id).Single();
  Check(m.MarcarEnviando(id,d)&&!m.MarcarEnviando(id,d),"destinatario activo se toma una sola vez");m.RegistrarResultado(id,d,false);m.Finalizar(id);
  Check(m.TomarParaReintento(new Novedad{IdNovedad=noticia}).HasValue,"envio fallido se puede retomar");
  d=m.ListarPorEnviar(id).Single();Need(new BLL_Newsletter().DarDeBaja(s.TokenPreferencias).Exitoso,"Baja");
  Check(!m.MarcarEnviando(id,d)&&m.ListarDestinatarios(id).Single().Estado=="Omitido","reintento tambien omite una baja posterior a cargar la lista");
  Check(m.Finalizar(id).Estado=="SinDestinatarios","lote enteramente omitido finaliza sin destinatarios");
  var activo=Suscriptor("qaactivo");int n2=CrearNovedad(true),e2=m.Iniciar(new Novedad{IdNovedad=n2},"Activos",admin);var d2=m.ListarPorEnviar(e2).Single();
  var resultados=Paralelo(()=>m.MarcarEnviando(e2,d2)?ResultadoOperacion.Ok():ResultadoOperacion.Error("ocupado"),()=>m.MarcarEnviando(e2,d2)?ResultadoOperacion.Ok():ResultadoOperacion.Error("ocupado"));
  Check(resultados.Count(r=>r.Exitoso)==1,"dos tomas simultaneas no duplican destinatario");m.RegistrarResultado(e2,d2,true);
  Check(!m.MarcarEnviando(e2,d2),"destinatario enviado no se vuelve a tomar");m.Finalizar(e2);
 }
 static void Novedades(){
  var b=new BLL_Novedad();string titulo="QA "+Guid.NewGuid().ToString("N");
  Fallo();try{Check(!b.Registrar(titulo,"Resumen","Contenido QA",categoria,null,null,null,false,admin).Exitoso&&N("SELECT COUNT(*) FROM dbo.Novedad WHERE titulo=@p0",titulo)==0,"alta fallida no deja novedad guardada");}finally{Limpiar();}
  int id=CrearNovedad(false);var anterior=b.ObtenerPorId(id);
  Fallo();try{
   Check(!b.Publicar(id,admin).Exitoso&&!b.ObtenerPorId(id).Publicado,"publicacion fallida conserva borrador");
   Check(!b.Modificar(id,"Cambio QA",anterior.Resumen,anterior.Contenido,categoria,null,anterior.FechaPublicacion,null,false,admin).Exitoso&&b.ObtenerPorId(id).Titulo==anterior.Titulo,"edicion fallida conserva datos anteriores");
   Check(!b.DarDeBaja(id,admin).Exitoso&&b.ObtenerPorId(id).Activo,"baja fallida conserva novedad activa");
  }finally{Limpiar();}
  Check(b.Publicar(id,admin).Exitoso&&b.ListarPublicadas().Any(n=>n.IdNovedad==id),"publicacion normal aparece al publico");
  Fallo();try{Check(!b.VolverABorrador(id,admin).Exitoso&&b.ObtenerPorId(id).Publicado,"volver a borrador con fallo conserva publicacion");}finally{Limpiar();}
  Check(b.VolverABorrador(id,admin).Exitoso&&!b.ListarPublicadas().Any(n=>n.IdNovedad==id),"volver a borrador normal oculta novedad");
  Check(b.DarDeBaja(id,admin).Exitoso&&b.ObtenerPorId(id).Estado==Novedad.EstadoDadaDeBaja,"baja normal conserva registro historico");
  Fallo();try{Check(!b.Restaurar(id,admin).Exitoso&&b.ObtenerPorId(id).Estado==Novedad.EstadoDadaDeBaja,"restauracion fallida conserva baja");}finally{Limpiar();}
  Check(b.Restaurar(id,admin).Exitoso&&b.ObtenerPorId(id).Estado==Novedad.EstadoBorrador,"restauracion normal vuelve a borrador");
  Check(Paralelo(()=>b.Publicar(id,admin),()=>b.Publicar(id,admin)).Count(r=>r.Exitoso)==1,"publicacion simultanea ocurre una sola vez");
  var programada=b.Registrar("Programada QA","Resumen","Contenido",categoria,null,DateTime.Now.AddDays(1),DateTime.Now.AddDays(2),true,admin);Need(programada.Exitoso,programada.Mensaje);
  Check(b.ObtenerPorId(programada.Valor).Estado==Novedad.EstadoProgramada&&!b.ListarPublicadas().Any(n=>n.IdNovedad==programada.Valor),"vigencia futura conserva publicacion programada");
  Check(!b.EnviarNewsletter(programada.Valor,"Activos","http://localhost/",admin).Exitoso,"programada no envia newsletter antes de su vigencia");
  Check(!b.Registrar("Vencida QA","Resumen","Contenido",categoria,null,DateTime.Now.AddDays(-2),DateTime.Now.AddDays(-1),true,admin).Exitoso,"fecha vencida sigue rechazada al publicar");
 }
 static Faq DatosFaq(){return new Faq{Pregunta="Pregunta QA "+Guid.NewGuid().ToString("N"),Respuesta="Respuesta de prueba suficientemente larga para el contenido de ayuda.",IdCategoriaAyuda=N("SELECT TOP 1 idCategoriaAyuda FROM dbo.CategoriaAyuda WHERE activa=1"),Estado="Borrador",Orden=500,EsBasica=true};}
 static void Faqs(){
  var b=new BLL_Faq();var datos=DatosFaq();
  Fallo();try{var r=b.Registrar(datos,admin);Check(!r.Exitoso&&r.CodigoAlternativo=="A14"&&N("SELECT COUNT(*) FROM dbo.Faq WHERE pregunta=@p0",datos.Pregunta)==0,"alta FAQ fallida revierte datos y devuelve error controlado");}finally{Limpiar();}
  var alta=b.Registrar(datos,admin);Need(alta.Exitoso,alta.Mensaje);int id=alta.Valor;
  Check(!b.Registrar(datos,admin).Exitoso,"FAQ duplicada conserva validacion");
  var editar=b.ObtenerPorId(id);editar.Respuesta="Respuesta modificada y suficientemente detallada para probar la edicion.";
  Fallo();try{
   var pub=b.Publicar(id,admin);Check(!pub.Exitoso&&pub.CodigoAlternativo=="A14"&&b.ObtenerPorId(id).Estado=="Borrador","publicar FAQ con fallo no lanza excepcion ni publica");
   Check(!b.Modificar(editar,admin).Exitoso&&b.ObtenerPorId(id).Respuesta==datos.Respuesta,"edicion FAQ fallida conserva respuesta");
  }finally{Limpiar();}
  Check(b.Modificar(editar,admin).Exitoso&&b.ObtenerPorId(id).Respuesta==editar.Respuesta,"edicion FAQ normal guarda datos");
  Check(Paralelo(()=>b.Publicar(id,admin),()=>b.Publicar(id,admin)).Count(r=>r.Exitoso)==1,"doble publicacion FAQ tiene un solo exito");
  Check(b.ObtenerPreguntasBasicas().Valor.Any(f=>f.IdFaq==id),"FAQ basica publicada aparece en ayuda");
  Fallo();try{Check(!b.Desactivar(id,admin).Exitoso&&b.ObtenerPorId(id).Estado=="Activo","desactivar FAQ con fallo conserva publicacion");}finally{Limpiar();}
  Check(b.Desactivar(id,admin).Exitoso&&!b.ObtenerPreguntasBasicas().Valor.Any(f=>f.IdFaq==id),"desactivar FAQ normal oculta sin borrar");
  Check(b.Publicar(id,admin).Exitoso,"FAQ desactivada puede volver a publicarse");
  string consulta="consulta qa "+Guid.NewGuid().ToString("N");var m=new MPP_Faq();m.RegistrarConsultaAsistente(consulta,consulta,null,null,null);
  Fallo();try{Check(!b.DescartarConsultaSinRespuesta(consulta,admin).Exitoso&&N("SELECT COUNT(*) FROM dbo.ConsultaAsistente WHERE textoNormalizado=@p0 AND atendida=0",consulta)==1,"descartar consulta con fallo conserva pendiente");}finally{Limpiar();}
  Check(b.DescartarConsultaSinRespuesta(consulta,admin).Exitoso&&N("SELECT COUNT(*) FROM dbo.ConsultaAsistente WHERE textoNormalizado=@p0 AND atendida=1",consulta)==1,"descartar consulta normal registra cambio");
  string origen="origen qa "+Guid.NewGuid().ToString("N");m.RegistrarConsultaAsistente(origen,origen,null,null,null);var desdeConsulta=DatosFaq();
  Sql("CREATE OR ALTER TRIGGER dbo.QA_ConsultaContenido ON dbo.ConsultaAsistente AFTER UPDATE AS BEGIN THROW 51000,'Fallo QA consulta',1; END");
  try{Check(!b.Registrar(desdeConsulta,admin,origen).Exitoso&&N("SELECT COUNT(*) FROM dbo.Faq WHERE pregunta=@p0",desdeConsulta.Pregunta)==0&&N("SELECT COUNT(*) FROM dbo.ConsultaAsistente WHERE textoNormalizado=@p0 AND atendida=0",origen)==1,"FAQ desde consulta se guarda completa o revierte todo");}finally{Sql("DROP TRIGGER dbo.QA_ConsultaContenido");}
  Check(b.Registrar(desdeConsulta,admin,origen).Exitoso&&N("SELECT COUNT(*) FROM dbo.ConsultaAsistente WHERE textoNormalizado=@p0 AND atendida=1",origen)==1,"reintento FAQ desde consulta conserva flujo");
 }
 static int Main(){Need(new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_",StringComparison.Ordinal),"Solo QA");try{admin=N("SELECT TOP 1 idUsuarioInterno FROM dbo.UsuarioInterno WHERE activo=1 AND estadoCuenta=N'Activa'");categoria=S("SELECT TOP 1 codigo FROM dbo.CategoriaNovedad WHERE activa=1 ORDER BY codigo");Newsletter();Destinatarios();Novedades();Faqs();Console.WriteLine("TOTAL: "+checks+" comprobaciones correctas");return 0;}catch(Exception ex){Console.WriteLine(ex);return 1;}finally{Limpiar();}}
}
