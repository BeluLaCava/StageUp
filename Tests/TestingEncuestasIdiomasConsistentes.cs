using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BE.Interfaces;
using StageUp.BLL;
using StageUp.MPP;

class TestingEncuestasIdiomasConsistentes
{
    static string cs = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int admin, checks;
    static BLL_Encuesta encuestas = new BLL_Encuesta();
    static BLL_Idioma idiomas = new BLL_Idioma();
    static BLL_Traduccion traducciones = new BLL_Traduccion();
    static object Sql(string s, params object[] p)
    {
        using (var c = new SqlConnection(cs)) using (var q = new SqlCommand(s, c))
        {
            for (int i = 0; i < p.Length; i++) q.Parameters.AddWithValue("@p" + i, p[i]);
            c.Open(); return q.ExecuteScalar();
        }
    }
    static int N(string s, params object[] p) { return Convert.ToInt32(Sql(s, p)); }
    static string S(string s, params object[] p) { return Convert.ToString(Sql(s, p)); }
    static void Check(bool ok, string texto) { if (!ok) throw new Exception("FAIL: " + texto); checks++; Console.WriteLine("PASS: " + texto); }
    static void Need(ResultadoOperacion r) { if (!r.Exitoso) throw new Exception(r.Mensaje); }
    static int Encuesta() { var r = encuestas.Registrar("QA " + Guid.NewGuid().ToString("N"), "QA", DateTime.Now.AddMinutes(-5), DateTime.Now.AddDays(7), "Todos", admin); Need(r); return r.Valor; }
    static int Pregunta(int id) { var r = encuestas.AgregarPregunta(id, "Pregunta QA", new List<string> { "Si", "No" }, admin); Need(r); return r.Valor; }
    static int Preguntas(int id) { return N("SELECT COUNT(*) FROM dbo.PreguntaEncuesta WHERE idEncuesta=@p0", id); }
    static int Opciones(int id) { return N("SELECT COUNT(*) FROM dbo.OpcionPregunta o JOIN dbo.PreguntaEncuesta p ON p.idPreguntaEncuesta=o.idPreguntaEncuesta WHERE p.idEncuesta=@p0", id); }
    static string Estado(int id) { return S("SELECT estado FROM dbo.Encuesta WHERE idEncuesta=@p0", id); }
    static int Audit(string entidad, int id) { return N("SELECT COUNT(*) FROM dbo.RegistroActividad WHERE tipoEntidadAfectada=@p0 AND idEntidadAfectada=@p1", entidad, id); }
    static void Fallo() { Sql("CREATE OR ALTER TRIGGER dbo.QA_EncuestasIdiomasAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END"); }
    static void LimpiarFallo() { Sql("IF OBJECT_ID('dbo.QA_EncuestasIdiomasAudit','TR') IS NOT NULL DROP TRIGGER dbo.QA_EncuestasIdiomasAudit; IF OBJECT_ID('dbo.QA_EncuestasIdiomasOpcion','TR') IS NOT NULL DROP TRIGGER dbo.QA_EncuestasIdiomasOpcion;"); }
    static ResultadoOperacion[] Paralelo(params Func<ResultadoOperacion>[] acciones)
    {
        using (var gate = new ManualResetEventSlim(false))
        {
            var tareas = acciones.Select(f => Task.Factory.StartNew(() => { gate.Wait(); return f(); }, TaskCreationOptions.LongRunning)).ToArray();
            gate.Set(); Task.WaitAll(tareas); return tareas.Select(t => t.Result).ToArray();
        }
    }
    static void EncuestasAtomicas()
    {
        string nombre = "QA " + Guid.NewGuid().ToString("N");
        Fallo(); try
        {
            Check(!encuestas.Registrar(nombre, "QA", DateTime.Now, DateTime.Now.AddDays(2), "Todos", admin).Exitoso && N("SELECT COUNT(*) FROM dbo.Encuesta WHERE titulo=@p0", nombre) == 0, "alta fallida no deja encuesta");
        } finally { LimpiarFallo(); }
        int e = Encuesta(); string titulo = encuestas.ObtenerPorId(e).Titulo;
        Check(!encuestas.Publicar(e, admin).Exitoso, "borrador vacio no publica");
        Check(!encuestas.AgregarPregunta(e, "Pregunta", new List<string> { "Una" }, admin).Exitoso, "pregunta conserva minimo de opciones");
        Sql("CREATE OR ALTER TRIGGER dbo.QA_EncuestasIdiomasOpcion ON dbo.OpcionPregunta AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE orden=2) THROW 51000,'Fallo QA segunda opcion',1; END");
        try
        {
            Check(!encuestas.AgregarPregunta(e, "Interrumpida", new List<string> { "A", "B" }, admin).Exitoso && Preguntas(e) == 0 && Opciones(e) == 0, "fallo en segunda opcion revierte pregunta y todas las opciones");
        } finally { LimpiarFallo(); }
        Fallo(); try
        {
            Check(!encuestas.AgregarPregunta(e, "Interrumpida", new List<string> { "A", "B" }, admin).Exitoso && Preguntas(e) == 0, "fallo de auditoria revierte pregunta completa");
            Check(!encuestas.Modificar(e, "Modificada QA", "QA", DateTime.Now, DateTime.Now.AddDays(4), "Todos", admin).Exitoso && encuestas.ObtenerPorId(e).Titulo == titulo, "edicion fallida mantiene datos originales");
        } finally { LimpiarFallo(); }
        int p = Pregunta(e), auditoria = Audit("Encuesta", e);
        Check(Preguntas(e) == 1 && Opciones(e) == 2, "reintento guarda pregunta completa");
        Fallo(); try
        {
            Check(!encuestas.EliminarPregunta(p, e, admin).Exitoso && Preguntas(e) == 1 && Opciones(e) == 2, "eliminar pregunta con fallo conserva pregunta y opciones");
            Check(!encuestas.Publicar(e, admin).Exitoso && Estado(e) == "Borrador", "publicacion fallida conserva borrador");
            Check(!encuestas.EliminarBorrador(e, admin).Exitoso && Estado(e) == "Borrador" && Preguntas(e) == 1 && Opciones(e) == 2, "eliminar borrador con fallo conserva encuesta completa");
            Check(Audit("Encuesta", e) == auditoria, "fallos no dejan auditoria parcial");
        } finally { LimpiarFallo(); }
        Check(encuestas.Publicar(e, admin).Exitoso && Estado(e) == "Activa", "publicacion normal tras reintento");
        Check(!encuestas.EliminarPregunta(p, e, admin).Exitoso && !encuestas.EliminarBorrador(e, admin).Exitoso, "publicada no permite borrar preguntas ni eliminar encuesta");
        Check(!encuestas.Modificar(e, "Otra", "QA", DateTime.Now, DateTime.Now.AddDays(4), "Todos", admin).Exitoso && !encuestas.AgregarPregunta(e, "Otra", new List<string> { "A", "B" }, admin).Exitoso, "publicada no permite edicion ni nuevas preguntas");
        Fallo(); try { Check(!encuestas.Cerrar(e, admin).Exitoso && Estado(e) == "Activa", "cierre fallido conserva activa"); } finally { LimpiarFallo(); }
        Check(encuestas.Cerrar(e, admin).Exitoso && Estado(e) == "Cerrada" && !encuestas.Publicar(e, admin).Exitoso, "cierre normal no permite republicar");
        int borrar = Encuesta(); int pb = Pregunta(borrar);
        Check(encuestas.EliminarPregunta(pb, borrar, admin).Exitoso && Preguntas(borrar) == 0 && Opciones(borrar) == 0, "eliminacion normal de pregunta y opciones");
        Pregunta(borrar);
        Check(encuestas.EliminarBorrador(borrar, admin).Exitoso && encuestas.ObtenerPorId(borrar) == null, "eliminacion normal de borrador");
    }
    static void EncuestasConcurrentes()
    {
        for (int i = 0; i < 20; i++)
        {
            int e = Encuesta(), p = Pregunta(e);
            var r = Paralelo(() => encuestas.Publicar(e, admin), () => encuestas.EliminarPregunta(p, e, admin));
            Check(r.Count(x => x.Exitoso) == 1 && !(Estado(e) == "Activa" && Preguntas(e) == 0), "publicar y eliminar ultima pregunta, ronda " + i);
        }
        int doble = Encuesta(); Pregunta(doble); int previo = Audit("Encuesta", doble);
        var dos = Paralelo(() => encuestas.Publicar(doble, admin), () => encuestas.Publicar(doble, admin));
        Check(dos.Count(r => r.Exitoso) == 1 && Audit("Encuesta", doble) == previo + 1, "doble publicacion registra una sola operacion");
        int agregar = Encuesta(); Pregunta(agregar);
        var nueva = Paralelo(() => encuestas.Publicar(agregar, admin), () => encuestas.AgregarPregunta(agregar, "Adicional", new List<string> { "A", "B" }, admin));
        Check(nueva[0].Exitoso && Preguntas(agregar) == (nueva[1].Exitoso ? 2 : 1) && Opciones(agregar) == Preguntas(agregar) * 2, "agregar y publicar conserva todas las opciones o rechaza la edicion");
        int eliminar = Encuesta(); Pregunta(eliminar);
        var borrado = Paralelo(() => encuestas.Publicar(eliminar, admin), () => encuestas.EliminarBorrador(eliminar, admin));
        Check(borrado.Count(r => r.Exitoso) == 1 && (Estado(eliminar) == "Activa" || encuestas.ObtenerPorId(eliminar) == null), "publicar y eliminar borrador tienen un solo resultado definitivo");
    }
    static void Responder()
    {
        int e = Encuesta(); Pregunta(e); Need(encuestas.Publicar(e, admin));
        int usuario = N("SELECT TOP 1 idUsuarioExterno FROM dbo.UsuarioExterno WHERE activo=1 AND estadoCuenta=N'Activa'");
        string perfil = S("SELECT perfilUsuario FROM dbo.UsuarioExterno WHERE idUsuarioExterno=@p0", usuario);
        var encuestado = BLL_Encuesta.EncuestadoConSesion(usuario, perfil, BLL_Encuesta.GenerarIdentificadorNavegador());
        var formulario = encuestas.ObtenerParaResponder(e, encuestado);
        Check(formulario.Exitoso, "usuario puede abrir encuesta publicada");
        var elegidas = formulario.Valor.Preguntas.ToDictionary(p => p.IdPreguntaEncuesta, p => p.Opciones[0].IdOpcionPregunta);
        Check(!encuestas.RegistrarRespuesta(e, encuestado, new Dictionary<int, int>(), null).Exitoso, "respuesta incompleta sigue rechazada");
        Check(encuestas.RegistrarRespuesta(e, encuestado, elegidas, null).Exitoso, "respuesta valida conserva flujo y auditoria");
        Check(!encuestas.RegistrarRespuesta(e, encuestado, elegidas, null).Exitoso, "no permite respuesta duplicada");
        Check(encuestas.ObtenerResultadosParaUsuario(e, encuestado).Exitoso, "resultados disponibles al responder");
        Need(encuestas.Cerrar(e, admin));
        Check(!encuestas.ObtenerParaResponder(e, encuestado).Exitoso, "encuesta cerrada no acepta respuestas");
    }
    class Observador : IObservadorIdioma
    {
        public IList<Traduccion> Datos;
        public void ActualizarIdioma(Idioma idioma, IList<Traduccion> datos) { Datos = datos; }
    }
    static IList<Traduccion> Diccionario(int id)
    {
        var b = new BLL_Multidioma(); var o = new Observador(); b.RegistrarObservador(o); b.NotificarObservadores(new Idioma { IdIdioma = id }); return o.Datos;
    }
    static string Texto(int id, string clave) { return Diccionario(id).Single(t => t.ClaveEtiqueta == clave).TextoTraducido; }
    static void IdiomasAtomicos()
    {
        int original = N("SELECT TOP 1 idIdioma FROM dbo.Idioma WHERE activo=1 AND esPredeterminado=1");
        string codigo = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(c => c.Name).First(c => c.Length <= 20 && N("SELECT COUNT(*) FROM dbo.Idioma WHERE codigoIdioma=@p0", c) == 0);
        Fallo(); try { Check(!idiomas.Registrar("QA idioma", codigo, false, admin).Exitoso && N("SELECT COUNT(*) FROM dbo.Idioma WHERE codigoIdioma=@p0", codigo) == 0, "alta de idioma con fallo no persiste"); } finally { LimpiarFallo(); }
        var alta = idiomas.Registrar("QA idioma", codigo, false, admin); Need(alta); int id = alta.Valor;
        Check(!idiomas.Registrar("QA repetido", codigo, false, admin).Exitoso, "idioma duplicado sigue rechazado");
        Check(!idiomas.Registrar("QA", "codigo-invalido-xyz", false, admin).Exitoso, "codigo de cultura invalido sigue rechazado");
        Fallo(); try
        {
            Check(!idiomas.Modificar(id, "Cambiado", codigo, true, admin).Exitoso && S("SELECT nombreIdioma FROM dbo.Idioma WHERE idIdioma=@p0", id) == "QA idioma" && N("SELECT idIdioma FROM dbo.Idioma WHERE activo=1 AND esPredeterminado=1") == original, "edicion fallida conserva nombre y predeterminado anterior");
            Check(!idiomas.DarDeBaja(id, admin).Exitoso && N("SELECT COUNT(*) FROM dbo.Idioma WHERE idIdioma=@p0 AND activo=1", id) == 1, "baja fallida conserva idioma activo");
        } finally { LimpiarFallo(); }
        Check(idiomas.Modificar(id, "Cambiado", codigo, true, admin).Exitoso && N("SELECT COUNT(*) FROM dbo.Idioma WHERE activo=1 AND esPredeterminado=1") == 1, "cambio normal mantiene un unico predeterminado");
        Check(!idiomas.DarDeBaja(id, admin).Exitoso, "no se puede dar de baja el predeterminado");
        int etiqueta = N("SELECT TOP 1 idEtiquetaTraduccion FROM dbo.EtiquetaTraduccion WHERE activo=1");
        string clave = S("SELECT claveEtiqueta FROM dbo.EtiquetaTraduccion WHERE idEtiquetaTraduccion=@p0", etiqueta);
        Need(traducciones.Guardar(id, etiqueta, "Texto original QA", admin));
        var cache = Diccionario(id);
        Fallo(); try
        {
            Check(!traducciones.Guardar(id, etiqueta, "Texto fallido QA", admin).Exitoso && S("SELECT textoTraducido FROM dbo.Traduccion WHERE idIdioma=@p0 AND idEtiquetaTraduccion=@p1", id, etiqueta) == "Texto original QA", "edicion de traduccion fallida conserva texto persistido");
            Check(Object.ReferenceEquals(cache, Diccionario(id)) && Texto(id, clave) == "Texto original QA", "edicion fallida conserva cache previa");
            Check(!traducciones.Eliminar(id, etiqueta, admin).Exitoso && Texto(id, clave) == "Texto original QA" && N("SELECT COUNT(*) FROM dbo.Traduccion WHERE idIdioma=@p0 AND idEtiquetaTraduccion=@p1", id, etiqueta) == 1, "eliminacion fallida conserva traduccion y cache");
        } finally { LimpiarFallo(); }
        Check(traducciones.Guardar(id, etiqueta, "Texto actualizado QA", admin).Exitoso && Texto(id, clave) == "Texto actualizado QA", "edicion exitosa renueva cache");
        Check(traducciones.Eliminar(id, etiqueta, admin).Exitoso && Texto(id, clave) == S("SELECT textoPredeterminado FROM dbo.EtiquetaTraduccion WHERE idEtiquetaTraduccion=@p0", etiqueta), "eliminacion exitosa recupera texto predeterminado");
        CacheConcurrente(id, etiqueta, clave);
        var anterior = new MPP_Idioma().ObtenerPorId(new Idioma { IdIdioma = original });
        Need(idiomas.Modificar(original, anterior.NombreIdioma, anterior.CodigoIdioma, true, admin));
        Check(idiomas.DarDeBaja(id, admin).Exitoso && !traducciones.Guardar(id, etiqueta, "No permitido", admin).Exitoso, "baja normal impide editar traducciones del idioma inactivo");
    }
    static void CacheConcurrente(int id, int etiqueta, string clave)
    {
        Need(traducciones.Guardar(id, etiqueta, "Antes QA", admin));
        string original = S("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_Traduccion_ListarDiccionario'))");
        Sql("CREATE TABLE dbo.QA_CacheIdiomaSenal (id INT PRIMARY KEY)");
        try
        {
            Sql("CREATE OR ALTER PROCEDURE dbo.sp_Traduccion_ListarDiccionario @idIdioma INT AS BEGIN SET NOCOUNT ON; SELECT e.claveEtiqueta,e.textoPredeterminado,ISNULL(t.textoTraducido,e.textoPredeterminado) AS textoTraducido,e.modulo INTO #datos FROM dbo.EtiquetaTraduccion e LEFT JOIN dbo.Traduccion t ON t.idEtiquetaTraduccion=e.idEtiquetaTraduccion AND t.idIdioma=@idIdioma WHERE e.activo=1; IF @idIdioma=" + id + " AND NOT EXISTS(SELECT 1 FROM dbo.QA_CacheIdiomaSenal) BEGIN INSERT INTO dbo.QA_CacheIdiomaSenal VALUES(1); WAITFOR DELAY '00:00:02'; END; SELECT * FROM #datos; END");
            BLL_Multidioma.InvalidarCache(id);
            var lectura = Task.Factory.StartNew(() => Texto(id, clave), TaskCreationOptions.LongRunning);
            try
            {
                var limite = DateTime.UtcNow.AddSeconds(10);
                while (N("SELECT COUNT(*) FROM dbo.QA_CacheIdiomaSenal") == 0 && DateTime.UtcNow < limite) Thread.Sleep(20);
                Check(N("SELECT COUNT(*) FROM dbo.QA_CacheIdiomaSenal") == 1, "lectura concurrente preparada en QA");
                Need(traducciones.Guardar(id, etiqueta, "Despues QA", admin));
                Check(lectura.Result == "Despues QA" && Texto(id, clave) == "Despues QA", "lectura anterior al guardado no repuebla cache con texto viejo");
            } finally { lectura.Wait(); }
        }
        finally
        {
            Sql(Regex.Replace(original, @"(?i)CREATE\s+(?:OR\s+ALTER\s+)?PROCEDURE", "ALTER PROCEDURE"));
            Sql("DROP TABLE dbo.QA_CacheIdiomaSenal"); BLL_Multidioma.InvalidarCache(id);
        }
    }
    static int Main()
    {
        if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_", StringComparison.Ordinal)) throw new InvalidOperationException("Solo QA");
        try
        {
            admin = N("SELECT TOP 1 idUsuarioInterno FROM dbo.UsuarioInterno WHERE activo=1 AND estadoCuenta=N'Activa'");
            EncuestasAtomicas(); EncuestasConcurrentes(); Responder(); IdiomasAtomicos();
            Console.WriteLine("TOTAL: " + checks + " comprobaciones correctas"); return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
        finally { LimpiarFallo(); }
    }
}
