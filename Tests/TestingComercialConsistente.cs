using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;

class TestingComercialConsistente
{
    static readonly string Cadena = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int admin, pruebas;
    static object Sql(string sql, params object[] valores)
    {
        using (var c = new SqlConnection(Cadena))
        using (var q = new SqlCommand(sql, c))
        {
            for (int i = 0; i < valores.Length; i++) q.Parameters.AddWithValue("@p" + i, valores[i]);
            c.Open(); return q.ExecuteScalar();
        }
    }
    static int N(string sql, params object[] valores) { return Convert.ToInt32(Sql(sql, valores)); }
    static string S(string sql, params object[] valores) { return Convert.ToString(Sql(sql, valores)); }
    static void Check(bool bien, string nombre) { if (!bien) throw new Exception(nombre); pruebas++; Console.WriteLine("OK " + nombre); }
    static void Preparado(ResultadoOperacion r) { if (!r.Exitoso) throw new Exception("Preparacion: " + r.Mensaje); }
    static string Id() { return Guid.NewGuid().ToString("N"); }
    static string Cat(string tipo) { return new BLL_CatalogoComercial().Listar(tipo).First().Codigo; }
    static ContactoComercial Contacto()
    {
        return new ContactoComercial { NombreContacto = "QA " + Id(), TipoEntidad = Cat(ItemCatalogoComercial.TipoEntidad), MedioContacto = "Correo", CorreoElectronico = Id() + "@example.test", IdOrigenUsuario = N("SELECT TOP 1 idOrigenUsuario FROM OrigenUsuario WHERE activo=1"), Observaciones = "Contacto ficticio de prueba aislada", EstadoContacto = ContactoComercial.EstadoPendiente };
    }
    static ContenidoPromocional Pieza()
    {
        return new ContenidoPromocional { Titulo = "QA " + Id(), TipoContenido = Cat(ItemCatalogoComercial.TipoContenido), CanalPublicacion = Cat(ItemCatalogoComercial.CanalUso), EstadoContenido = ContenidoPromocional.EstadoEnRevision, UrlContenido = "https://example.test/pieza" };
    }
    static int CrearPieza()
    {
        string anterior; var r = new BLL_ContenidoPromocional().Guardar(Pieza(), admin, out anterior); Preparado(r); return r.Valor;
    }
    static void ConFallo(Action probar)
    {
        int antes = N("SELECT COUNT(*) FROM RegistroActividad");
        Sql("CREATE TRIGGER QA_ComercialAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA de auditoria',1; END");
        try { probar(); Check(N("SELECT COUNT(*) FROM RegistroActividad") == antes, "El fallo no deja auditoria parcial"); }
        finally { Sql("DROP TRIGGER QA_ComercialAudit"); }
    }
    static void Contactos()
    {
        var bll = new BLL_ContactoComercial(); var c = Contacto();
        ConFallo(() => {
            var r = bll.Guardar(c, admin);
            Check(!r.Exitoso && r.CodigoAlternativo == "A19" && N("SELECT COUNT(*) FROM ContactoComercial WHERE correoElectronico=@p0", c.CorreoElectronico) == 0, "Alta de contacto y bitacora se revierten juntos");
        });
        var alta = bll.Guardar(c, admin); Preparado(alta); c.IdContactoComercial = alta.Valor;
        Check(N("SELECT COUNT(*) FROM RegistroActividad WHERE tipoEntidadAfectada='ContactoComercial' AND idEntidadAfectada=@p0", alta.Valor) == 1, "Alta normal registra una auditoria");
        c.NombreContacto = "Nombre editado QA"; c.EstadoContacto = ContactoComercial.EstadoContactado;
        ConFallo(() => {
            var r = bll.Guardar(c, admin);
            Check(!r.Exitoso && bll.Obtener(c.IdContactoComercial).EstadoContacto == ContactoComercial.EstadoPendiente && bll.Obtener(c.IdContactoComercial).NombreContacto != c.NombreContacto && bll.ListarSeguimientos(c.IdContactoComercial).Count == 0, "Edicion revierte datos, cambio de estado e historial");
        });
        Preparado(bll.Guardar(c, admin));
        Check(bll.Obtener(c.IdContactoComercial).NombreContacto == c.NombreContacto && bll.ListarSeguimientos(c.IdContactoComercial).Count == 1, "Edicion normal conserva historial");
        var seguimiento = new SeguimientoContacto { IdContactoComercial = c.IdContactoComercial, TipoSeguimiento = Cat(ItemCatalogoComercial.TipoInteraccion), Descripcion = "Seguimiento ficticio de prueba", EstadoResultante = ContactoComercial.EstadoInteresado };
        ConFallo(() => {
            var r = bll.RegistrarSeguimiento(seguimiento, DateTime.Today.ToString("yyyy-MM-dd"), null, admin);
            Check(!r.Exitoso && bll.Obtener(c.IdContactoComercial).EstadoContacto == ContactoComercial.EstadoContactado && bll.ListarSeguimientos(c.IdContactoComercial).Count == 1, "Seguimiento fallido no deja estado ni historial parciales");
        });
        Preparado(bll.RegistrarSeguimiento(seguimiento, DateTime.Today.ToString("yyyy-MM-dd"), null, admin));
        Check(bll.Obtener(c.IdContactoComercial).EstadoContacto == ContactoComercial.EstadoInteresado && bll.ListarSeguimientos(c.IdContactoComercial).Count == 2, "Seguimiento normal y cambio de estado");
        Check(!bll.Guardar(null, admin).Exitoso && !bll.RegistrarSeguimiento(null, null, null, admin).Exitoso, "Validaciones de datos incompletos conservadas");
        var dup = Contacto(); dup.CorreoElectronico = c.CorreoElectronico;
        Check(bll.Guardar(dup, admin).CodigoAlternativo.StartsWith("A13:"), "Duplicado secuencial mantiene aviso y referencia");
        for (int tipo = 0; tipo < 3; tipo++)
        {
            var datos = Enumerable.Range(0, 6).Select(i => Contacto()).ToArray();
            string clave = Id(); string telefono = "119" + DateTime.UtcNow.Ticks.ToString().Substring(10) + tipo;
            foreach (var dato in datos)
            {
                if (tipo == 0) dato.CorreoElectronico = clave + "@example.test";
                if (tipo == 1) dato.Telefono = telefono;
                if (tipo == 2) { dato.NombreContacto = "QA " + clave; dato.EntidadRepresentada = "Entidad " + clave; }
            }
            var inicio = new ManualResetEventSlim(false);
            var tareas = datos.Select(dato => Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_ContactoComercial().Guardar(dato, admin); })).ToArray();
            inicio.Set(); Task.WaitAll(tareas); inicio.Dispose();
            Check(tareas.Count(t => t.Result.Exitoso) == 1 && tareas.Count(t => !t.Result.Exitoso && t.Result.CodigoAlternativo.StartsWith("A13:")) == 5, "Seis altas simultaneas: un contacto, cinco duplicados; criterio " + tipo);
        }
    }
    static void Contenido()
    {
        var bll = new BLL_ContenidoPromocional(); var pieza = Pieza();
        ConFallo(() => {
            string anterior; var r = bll.Guardar(pieza, admin, out anterior);
            Check(!r.Exitoso && r.CodigoAlternativo == "A20" && anterior == null && N("SELECT COUNT(*) FROM ContenidoPromocional WHERE titulo=@p0", pieza.Titulo) == 0, "Alta de pieza y auditoria atomicas");
        });
        string ruta; var alta = bll.Guardar(pieza, admin, out ruta); Preparado(alta);
        pieza.IdContenidoPromocional = alta.Valor;
        Check(N("SELECT COUNT(*) FROM RegistroActividad WHERE tipoEntidadAfectada='ContenidoPromocional' AND idEntidadAfectada=@p0", alta.Valor) == 1, "Alta normal de contenido conserva auditoria");
        string original = pieza.Titulo; pieza.Titulo = "Edicion QA " + Id();
        ConFallo(() => { string anterior; var r = bll.Guardar(pieza, admin, out anterior); Check(!r.Exitoso && bll.Obtener(alta.Valor).Titulo == original && anterior == null, "Edicion fallida conserva la pieza anterior"); });
        Preparado(bll.Guardar(pieza, admin, out ruta));
        Check(bll.Obtener(alta.Valor).Titulo == pieza.Titulo, "Edicion normal del contenido");
        ConFallo(() => { var r = bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoActivo, admin, false); Check(!r.Exitoso && bll.Obtener(alta.Valor).EstadoContenido == ContenidoPromocional.EstadoEnRevision, "Cambio de estado se revierte cuando falla auditoria"); });
        Preparado(bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoActivo, admin, false));
        ConFallo(() => { var r = bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoDadoDeBaja, admin, false); Check(!r.Exitoso && bll.Obtener(alta.Valor).Activo, "Baja fallida conserva contenido activo"); });
        Preparado(bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoDadoDeBaja, admin, false));
        Check(bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoActivo, admin, false).CodigoAlternativo == "A9", "Reactivacion sin permiso rechazada");
        Check(!bll.Guardar(pieza, admin, out ruta).Exitoso, "Edicion de pieza dada de baja rechazada");
        ConFallo(() => { var r = bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoActivo, admin, true); Check(!r.Exitoso && !bll.Obtener(alta.Valor).Activo, "Reactivacion fallida conserva la baja"); });
        Preparado(bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoActivo, admin, true));
        Check(bll.Obtener(alta.Valor).Activo && !bll.Obtener(alta.Valor).FechaBaja.HasValue, "Reactivacion autorizada funciona");
        Check(!bll.CambiarEstado(alta.Valor, ContenidoPromocional.EstadoActivo, admin, true).Exitoso && !bll.CambiarEstado(alta.Valor, "Invalido", admin, true).Exitoso, "Validaciones de estado conservadas");
        for (int i = 0; i < 12; i++)
        {
            int id = CrearPieza(); var inicio = new ManualResetEventSlim(false);
            var cambio = Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_ContenidoPromocional().CambiarEstado(id, ContenidoPromocional.EstadoActivo, admin, false); });
            var baja = Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_ContenidoPromocional().CambiarEstado(id, ContenidoPromocional.EstadoDadoDeBaja, admin, false); });
            inicio.Set(); Task.WaitAll(cambio, baja); inicio.Dispose();
            Check(baja.Result.Exitoso && !bll.Obtener(id).Activo && (cambio.Result.Exitoso || cambio.Result.CodigoAlternativo == "A9"), "Baja y cambio simultaneos no reactivan sin permiso " + i);
        }
        int editarId = CrearPieza(); var editar = bll.Obtener(editarId); editar.Titulo = "Edicion concurrente QA";
        var barrera = new ManualResetEventSlim(false);
        var edicion = Task.Factory.StartNew(() => { barrera.Wait(); string anterior; return new BLL_ContenidoPromocional().Guardar(editar, admin, out anterior); });
        var bajaEdicion = Task.Factory.StartNew(() => { barrera.Wait(); return new BLL_ContenidoPromocional().CambiarEstado(editarId, ContenidoPromocional.EstadoDadoDeBaja, admin, false); });
        barrera.Set(); Task.WaitAll(edicion, bajaEdicion); barrera.Dispose();
        Check(bajaEdicion.Result.Exitoso && !bll.Obtener(editarId).Activo, "Editar simultaneamente tampoco revierte una baja");
        var archivo = Pieza(); archivo.RutaArchivo = "qa/" + Id() + ".pdf"; archivo.NombreArchivoOriginal = "original.pdf"; archivo.TipoMimeArchivo = "application/pdf"; archivo.TamanioArchivo = 100;
        var guardado = bll.Guardar(archivo, admin, out ruta); Preparado(guardado); archivo.IdContenidoPromocional = guardado.Valor;
        string rutaVieja = archivo.RutaArchivo; archivo.RutaArchivo = "qa/" + Id() + ".pdf"; archivo.NombreArchivoOriginal = "nuevo.pdf";
        ConFallo(() => { string anterior; var r = bll.Guardar(archivo, admin, out anterior); Check(!r.Exitoso && anterior == null && bll.Obtener(guardado.Valor).RutaArchivo == rutaVieja, "Reemplazo fallido conserva archivo anterior y no solicita borrarlo"); });
        Preparado(bll.Guardar(archivo, admin, out ruta));
        Check(ruta == rutaVieja && bll.Obtener(guardado.Valor).RutaArchivo == archivo.RutaArchivo, "Reemplazo exitoso devuelve exclusivamente archivo anterior");
        archivo = bll.Obtener(guardado.Valor); archivo.QuitarArchivo = true; archivo.RutaArchivo = null;
        ConFallo(() => { string anterior; var r = bll.Guardar(archivo, admin, out anterior); Check(!r.Exitoso && anterior == null && bll.Obtener(guardado.Valor).TieneArchivo, "Quita fallida conserva archivo"); });
        Preparado(bll.Guardar(archivo, admin, out ruta));
        Check(ruta != null && !bll.Obtener(guardado.Valor).TieneArchivo, "Quita exitosa preserva enlace y entrega ruta a limpiar");
    }
    static int Main()
    {
        try
        {
            if (!new SqlConnectionStringBuilder(Cadena).InitialCatalog.StartsWith("StageUp_Beta2_QA_", StringComparison.Ordinal)) throw new Exception("Solo bases QA");
            ThreadPool.SetMinThreads(16, 16);
            admin = N("SELECT TOP 1 idUsuarioInterno FROM UsuarioInterno WHERE estadoCuenta=N'Activa'");
            Contactos(); Contenido(); Console.WriteLine("TOTAL " + pruebas + " comprobaciones correctas"); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
