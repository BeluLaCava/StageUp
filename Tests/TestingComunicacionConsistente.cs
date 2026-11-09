using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;

class TestingComunicacionConsistente
{
    static readonly string Cadena = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int admin, espacio, usuario, gestor, reserva, emisor, receptor, ajeno, pruebas;
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
    static void Check(bool ok, string texto) { if (!ok) throw new Exception(texto); pruebas++; Console.WriteLine("OK " + texto); }
    static void Preparado(ResultadoOperacion r) { if (!r.Exitoso) throw new Exception("Preparacion: " + r.Mensaje); }
    static void ConFallo(Action accion)
    {
        int auditorias = N("SELECT COUNT(*) FROM RegistroActividad");
        int avisos = N("SELECT COUNT(*) FROM Notificacion");
        Sql("CREATE TRIGGER QA_ComunicacionAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END");
        try
        {
            accion();
            Check(N("SELECT COUNT(*) FROM RegistroActividad") == auditorias && N("SELECT COUNT(*) FROM Notificacion") == avisos, "Fallo no deja auditorias ni avisos parciales");
        }
        finally { Sql("DROP TRIGGER QA_ComunicacionAudit"); }
    }
    static void ConDemora(string procedimiento, Action accion)
    {
        string original = S("SELECT OBJECT_DEFINITION(OBJECT_ID(@p0))", procedimiento);
        string alterar = System.Text.RegularExpressions.Regex.Replace(original, @"\bCREATE\s+PROCEDURE\b", "ALTER PROCEDURE", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        Sql(alterar.Replace("SET NOCOUNT ON;", "SET NOCOUNT ON; WAITFOR DELAY '00:00:00.250';"));
        try { accion(); } finally { Sql(alterar); }
    }
    static ResultadoOperacion<int>[] Simultaneos(int cantidad, Func<int, ResultadoOperacion<int>> accion)
    {
        using (var inicio = new ManualResetEventSlim(false))
        {
            var tareas = Enumerable.Range(0, cantidad).Select(i => Task.Factory.StartNew(() => { inicio.Wait(); return accion(i); })).ToArray();
            inicio.Set(); Task.WaitAll(tareas); return tareas.Select(t => t.Result).ToArray();
        }
    }
    static string EstadoPregunta(int id) { return S("SELECT estado FROM PreguntaEspacio WHERE idPreguntaEspacio=@p0", id); }
    static void LimpiarPreguntas() { Sql("UPDATE PreguntaEspacio SET estado='Eliminada' WHERE idEspacioArtistico=@p0 AND idUsuarioExterno=@p1", espacio, usuario); }
    static void Preguntas()
    {
        var b = new BLL_PreguntaEspacio(); LimpiarPreguntas();
        string texto = "Consulta QA sobre las luces del espacio";
        ConFallo(() => { var r = b.Preguntar(espacio, usuario, texto); Check(!r.Exitoso && N("SELECT COUNT(*) FROM PreguntaEspacio WHERE idEspacioArtistico=@p0 AND idUsuarioExterno=@p1 AND estado='Pendiente'", espacio, usuario) == 0, "Pregunta fallida no queda publicada"); });
        int avisos = N("SELECT COUNT(*) FROM Notificacion");
        var alta = b.Preguntar(espacio, usuario, texto); Preparado(alta);
        Check(EstadoPregunta(alta.Valor) == "Pendiente" && N("SELECT COUNT(*) FROM Notificacion") == avisos + 1, "Pregunta normal se publica y avisa al gestor");
        Check(!b.Preguntar(espacio, null, texto).Exitoso && !b.Preguntar(espacio, gestor, texto).Exitoso, "Sesion y prohibicion de preguntarse a si mismo conservadas");
        Check(!b.Preguntar(espacio, usuario, "Escribir a prueba@example.test").Exitoso && !b.Preguntar(espacio, usuario, "corta").Exitoso, "Validaciones de pregunta conservadas");
        Check(!b.Responder(alta.Valor, usuario, "Respuesta no autorizada").Exitoso && !b.Retirar(alta.Valor, gestor).Exitoso && !b.Ocultar(alta.Valor, usuario, "Motivo no autorizado").Exitoso, "Autor y propietario conservan sus permisos");
        ConFallo(() => { var r = b.Responder(alta.Valor, gestor, "Tenemos luces disponibles"); Check(!r.Exitoso && EstadoPregunta(alta.Valor) == "Pendiente" && S("SELECT respuesta FROM PreguntaEspacio WHERE idPreguntaEspacio=@p0", alta.Valor) == "", "Respuesta fallida no deja texto ni cambia estado"); });
        avisos = N("SELECT COUNT(*) FROM Notificacion"); Preparado(b.Responder(alta.Valor, gestor, "Tenemos luces disponibles"));
        Check(EstadoPregunta(alta.Valor) == "Respondida" && N("SELECT COUNT(*) FROM Notificacion") == avisos + 1, "Respuesta normal y aviso al autor");
        Check(!b.Responder(alta.Valor, gestor, "Otra respuesta").Exitoso && !b.Retirar(alta.Valor, usuario).Exitoso, "Respuesta unica y retiro solo de pendientes");
        ConFallo(() => { var r = b.Ocultar(alta.Valor, gestor, "Motivo ficticio para prueba"); Check(!r.Exitoso && EstadoPregunta(alta.Valor) == "Respondida", "Ocultamiento fallido conserva publicacion"); });
        Preparado(b.Ocultar(alta.Valor, gestor, "Motivo ficticio para prueba"));
        Check(!b.ListarPublicas(espacio, true).Valor.Preguntas.Any(p => p.IdPreguntaEspacio == alta.Valor), "Pregunta oculta no aparece en listado publico");
        var retirar = b.Preguntar(espacio, usuario, "Consulta QA sobre el horario de apertura"); Preparado(retirar);
        ConFallo(() => { Check(!b.Retirar(retirar.Valor, usuario).Exitoso && EstadoPregunta(retirar.Valor) == "Pendiente", "Retiro fallido conserva pregunta"); });
        Preparado(b.Retirar(retirar.Valor, usuario)); Check(EstadoPregunta(retirar.Valor) == "Eliminada", "Retiro normal mantiene baja logica");
        ConDemora("dbo.sp_PreguntaEspacio_Insertar", () => {
            LimpiarPreguntas(); int antes = N("SELECT COUNT(*) FROM Notificacion");
            var iguales = Simultaneos(6, i => new BLL_PreguntaEspacio().Preguntar(espacio, usuario, "Consulta QA simultanea sobre el piano"));
            Check(iguales.Count(r => r.Exitoso) == 1 && N("SELECT COUNT(*) FROM Notificacion") == antes + 1, "Seis preguntas iguales producen una publicacion y un aviso");
            Check(!b.Preguntar(espacio, usuario, "CONSULTA QA SIMULTÁNEA SOBRE EL PIANO").Exitoso, "Deteccion normalizada de preguntas duplicadas conservada");
            LimpiarPreguntas();
            var distintas = Simultaneos(6, i => new BLL_PreguntaEspacio().Preguntar(espacio, usuario, "Consulta QA sobre el instrumento " + (char)('A' + i)));
            Check(distintas.Count(r => r.Exitoso) == 3 && N("SELECT COUNT(*) FROM PreguntaEspacio WHERE idEspacioArtistico=@p0 AND idUsuarioExterno=@p1 AND estado='Pendiente'", espacio, usuario) == 3, "Seis preguntas distintas respetan el limite de tres");
            int id = distintas.First(r => r.Exitoso).Valor; Preparado(b.Responder(id, gestor, "Si esta disponible"));
            Check(b.Preguntar(espacio, usuario, "Consulta QA luego de recibir respuesta").Exitoso, "Responder libera un lugar para preguntar");
        });
        LimpiarPreguntas(); var carrera = b.Preguntar(espacio, usuario, "Consulta QA para retiro simultaneo"); Preparado(carrera);
        using (var inicio = new ManualResetEventSlim(false))
        {
            var respuesta = Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_PreguntaEspacio().Responder(carrera.Valor, gestor, "Respuesta simultanea"); });
            var retiro = Task.Factory.StartNew(() => { inicio.Wait(); return new BLL_PreguntaEspacio().Retirar(carrera.Valor, usuario); });
            inicio.Set(); Task.WaitAll(respuesta, retiro);
            Check(respuesta.Result.Exitoso != retiro.Result.Exitoso, "Responder y retirar simultaneamente solo permite un resultado");
        }
    }
    static void Mensajes()
    {
        var b = new BLL_MensajeReserva(); string texto = "QA mensaje " + Id();
        ConFallo(() => { var r = b.Enviar(reserva, emisor, texto); Check(!r.Exitoso && N("SELECT COUNT(*) FROM MensajeReserva WHERE idReserva=@p0 AND texto=@p1", reserva, texto) == 0, "Mensaje fallido no se guarda ni bloquea reintento"); });
        int avisos = N("SELECT COUNT(*) FROM Notificacion"); var alta = b.Enviar(reserva, emisor, texto); Preparado(alta);
        Check(N("SELECT COUNT(*) FROM Notificacion") == avisos + 1, "Mensaje normal avisa al destinatario");
        Check(!b.Enviar(reserva, ajeno, texto).Exitoso && !b.Enviar(reserva, emisor, " ").Exitoso && !b.Enviar(reserva, emisor, new string('a', 1001)).Exitoso, "Acceso y limites de mensajes conservados");
        var detalle = new BLL_Reserva().ObtenerDetalle(reserva, receptor); Preparado(detalle);
        var listados = b.Listar(detalle.Valor, receptor); Preparado(listados);
        Check(listados.Valor.Any(m => m.IdMensajeReserva == alta.Valor) && !b.Listar(detalle.Valor, ajeno).Exitoso, "Conversacion privada visible solo a participantes");
        var posterior = b.Enviar(reserva, emisor, "Mensaje posterior " + Id()); Preparado(posterior);
        b.MarcarLeidos(detalle.Valor, receptor, new[] { alta.Valor });
        Check(N("SELECT COUNT(*) FROM MensajeReserva WHERE idMensajeReserva=@p0 AND fechaLectura IS NOT NULL", alta.Valor) == 1 && N("SELECT COUNT(*) FROM MensajeReserva WHERE idMensajeReserva=@p0 AND fechaLectura IS NULL", posterior.Valor) == 1, "Lectura mantiene mensajes posteriores como no leidos");
        ConDemora("dbo.sp_MensajeReserva_Insertar", () => {
            string mismo = "Mensaje simultaneo " + Id(); int antes = N("SELECT COUNT(*) FROM Notificacion");
            var resultados = Simultaneos(6, i => new BLL_MensajeReserva().Enviar(reserva, emisor, mismo));
            Check(resultados.Count(r => r.Exitoso) == 1 && N("SELECT COUNT(*) FROM MensajeReserva WHERE idReserva=@p0 AND texto=@p1", reserva, mismo) == 1 && N("SELECT COUNT(*) FROM Notificacion") == antes + 1, "Seis envios iguales guardan un mensaje y un aviso");
            Check(!b.Enviar(reserva, emisor, mismo).Exitoso, "Duplicado posterior sigue rechazado");
            var distintos = Simultaneos(4, i => new BLL_MensajeReserva().Enviar(reserva, emisor, "Mensaje diferente " + Id()));
            Check(distintos.All(r => r.Exitoso), "Mensajes diferentes simultaneos se conservan");
        });
        Check(b.Enviar(reserva, receptor, "Respuesta del gestor " + Id()).Exitoso, "El otro participante tambien puede responder");
    }
    static void Categorias()
    {
        var b = new BLL_CategoriaNovedad(); string nombre = "QA categoria " + Id();
        BLL_CategoriaNovedad.ListarTodas();
        ConFallo(() => { Check(!b.Registrar(nombre, 10, admin).Exitoso && N("SELECT COUNT(*) FROM CategoriaNovedad WHERE nombre=@p0", nombre) == 0, "Alta de categoria fallida no persiste"); });
        Preparado(b.Registrar(nombre, 10, admin)); string codigo = S("SELECT codigo FROM CategoriaNovedad WHERE nombre=@p0", nombre);
        Check(BLL_CategoriaNovedad.ListarActivas().Any(c => c.Codigo == codigo), "Alta normal aparece en cache publica");
        string nuevo = "QA nombre " + Id();
        ConFallo(() => { Check(!b.Modificar(codigo, nuevo, 20, admin).Exitoso && BLL_CategoriaNovedad.ObtenerEtiqueta(codigo) == nombre && S("SELECT nombre FROM CategoriaNovedad WHERE codigo=@p0", codigo) == nombre, "Edicion fallida conserva nombre en base y cache"); });
        Preparado(b.Modificar(codigo, nuevo, 20, admin)); Check(BLL_CategoriaNovedad.ObtenerEtiqueta(codigo) == nuevo, "Edicion confirmada actualiza cache");
        ConFallo(() => { Check(!b.CambiarEstado(codigo, false, admin).Exitoso && BLL_CategoriaNovedad.EsActiva(codigo) && N("SELECT COUNT(*) FROM CategoriaNovedad WHERE codigo=@p0 AND activa=1", codigo) == 1, "Baja fallida conserva categoria activa"); });
        Preparado(b.CambiarEstado(codigo, false, admin)); Check(!BLL_CategoriaNovedad.EsActiva(codigo), "Baja normal retira categoria de opciones activas");
        Preparado(b.CambiarEstado(codigo, true, admin)); Check(BLL_CategoriaNovedad.EsActiva(codigo), "Reactivacion normal actualiza cache");
        Check(!b.Registrar(nuevo, 1, admin).Exitoso && !b.Modificar(codigo, nuevo, -1, admin).Exitoso, "Validaciones de categoria conservadas");
    }
    static void Origenes()
    {
        var b = new BLL_OrigenUsuario(); var dato = new OrigenUsuario { NombreOrigen = "QA origen", Codigo = "qa-" + Id(), CanalOrigen = OrigenUsuario.CanalContactoDirecto, Activo = true, VisibleEnRegistro = true };
        ConFallo(() => { Check(!b.Guardar(dato, admin).Exitoso && N("SELECT COUNT(*) FROM OrigenUsuario WHERE codigo=@p0", dato.Codigo) == 0, "Origen fallido no persiste"); });
        var alta = b.Guardar(dato, admin); Preparado(alta); dato.IdOrigenUsuario = alta.Valor;
        Check(b.Obtener(alta.Valor).Codigo == dato.Codigo, "Alta normal de origen");
        dato.NombreOrigen = "QA origen editado";
        ConFallo(() => { Check(!b.Guardar(dato, admin).Exitoso && b.Obtener(alta.Valor).NombreOrigen != dato.NombreOrigen, "Edicion fallida conserva origen anterior"); });
        Preparado(b.Guardar(dato, admin)); Check(b.Obtener(alta.Valor).NombreOrigen == dato.NombreOrigen, "Edicion normal de origen");
        ConFallo(() => { Check(!b.CambiarActivo(alta.Valor, false, admin).Exitoso && b.Obtener(alta.Valor).Activo, "Baja fallida conserva origen activo"); });
        Preparado(b.CambiarActivo(alta.Valor, false, admin)); Check(!b.Obtener(alta.Valor).Activo, "Baja logica de origen conserva registro");
        Preparado(b.CambiarActivo(alta.Valor, true, admin)); Check(b.Obtener(alta.Valor).Activo, "Reactivacion de origen");
        Check(!b.Guardar(null, admin).Exitoso, "Validacion de origen conservada");
    }
    static string OrdenMenu() { return string.Join("|", new BLL_OpcionMenu().Listar().Select(o => o.IdOpcionMenu + ":" + o.Orden)); }
    static void Menu()
    {
        var b = new BLL_OpcionMenu(); var dato = new OpcionMenu { Texto = "QA menu " + Id(), Modulo = "QA", Url = "~/Interno/QATemporal.aspx", IdComponentePermiso = b.ListarPermisosDisponibles().First().IdComponentePermiso, Activo = true };
        ConFallo(() => { Check(!b.Registrar(dato, admin).Exitoso && N("SELECT COUNT(*) FROM OpcionMenu WHERE texto=@p0", dato.Texto) == 0, "Alta de menu fallida no persiste"); });
        var alta = b.Registrar(dato, admin); Preparado(alta); dato = b.ObtenerPorId(alta.Valor); string original = dato.Texto; dato.Texto = "QA menu editado " + Id();
        ConFallo(() => { Check(!b.Modificar(dato, admin).Exitoso && b.ObtenerPorId(alta.Valor).Texto == original, "Edicion fallida no cambia opcion de menu"); });
        Preparado(b.Modificar(dato, admin)); Check(b.ObtenerPorId(alta.Valor).Texto == dato.Texto, "Edicion normal de menu");
        ConFallo(() => { Check(!b.CambiarEstado(alta.Valor, false, admin).Exitoso && b.ObtenerPorId(alta.Valor).Activo, "Baja fallida de menu no oculta opcion"); });
        Preparado(b.CambiarEstado(alta.Valor, false, admin)); Check(!b.ObtenerPorId(alta.Valor).Activo, "Baja normal de menu");
        Preparado(b.CambiarEstado(alta.Valor, true, admin)); Check(b.ObtenerPorId(alta.Valor).Activo, "Reactivacion normal de menu");
        string orden = OrdenMenu();
        ConFallo(() => { Check(!b.Mover(alta.Valor, true, admin).Exitoso && OrdenMenu() == orden, "Movimiento fallido revierte ambas posiciones"); });
        Preparado(b.Mover(alta.Valor, true, admin)); Check(OrdenMenu() != orden, "Movimiento normal actualiza ambas posiciones");
        var protegida = b.Listar().First(o => o.CodigoPermiso == "GESTIONAR_MENU");
        Check(!b.CambiarEstado(protegida.IdOpcionMenu, false, admin).Exitoso && b.ObtenerPorId(protegida.IdOpcionMenu).Activo, "Proteccion de Gestion del menu conservada");
        dato.Url = "https://example.test"; Check(!b.Modificar(dato, admin).Exitoso, "Menu sigue rechazando URLs externas");
    }
    static int Main()
    {
        try
        {
            if (!new SqlConnectionStringBuilder(Cadena).InitialCatalog.StartsWith("StageUp_Beta2_QA_", StringComparison.Ordinal)) throw new Exception("Solo bases QA");
            ThreadPool.SetMinThreads(16, 16);
            admin = N("SELECT TOP 1 idUsuarioInterno FROM UsuarioInterno WHERE estadoCuenta='Activa'");
            espacio = N("SELECT TOP 1 idEspacioArtistico FROM EspacioArtistico WHERE publicado=1 AND activo=1");
            gestor = N("SELECT idUsuarioGestor FROM EspacioArtistico WHERE idEspacioArtistico=@p0", espacio);
            usuario = N("SELECT TOP 1 idUsuarioExterno FROM UsuarioExterno WHERE activo=1 AND estadoCuenta='Activa' AND idUsuarioExterno<>@p0", gestor);
            reserva = N("SELECT TOP 1 idReserva FROM Reserva WHERE estadoReserva='Finalizada'");
            emisor = N("SELECT idUsuarioExternoSolicitante FROM Reserva WHERE idReserva=@p0", reserva);
            receptor = N("SELECT e.idUsuarioGestor FROM Reserva r JOIN EspacioArtistico e ON e.idEspacioArtistico=r.idEspacioArtistico WHERE r.idReserva=@p0", reserva);
            ajeno = N("SELECT TOP 1 idUsuarioExterno FROM UsuarioExterno WHERE activo=1 AND idUsuarioExterno NOT IN (@p0,@p1)", emisor, receptor);
            if (espacio <= 0 || usuario <= 0 || reserva <= 0 || ajeno <= 0) throw new Exception("Faltan fixtures QA");
            Preguntas(); Mensajes(); Categorias(); Origenes(); Menu();
            Console.WriteLine("TOTAL " + pruebas + " comprobaciones correctas"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
