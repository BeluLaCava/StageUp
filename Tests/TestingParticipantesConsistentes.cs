using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.MPP;
using StageUp.Seguridad;

class TestingParticipantesConsistentes
{
    static string cs = ConfigurationManager.ConnectionStrings["StageUpConnectionString"].ConnectionString;
    static int gestor, espacio, siguiente = 70000000, dia = 30, checks;
    static BLL_Participante participantes = new BLL_Participante();
    static BLL_Actividad actividades = new BLL_Actividad();

    static object Sql(string s, params object[] p)
    {
        using (var c = new SqlConnection(cs)) using (var q = new SqlCommand(s, c))
        {
            for (int i = 0; i < p.Length; i++) q.Parameters.AddWithValue("@p" + i, p[i] ?? DBNull.Value);
            c.Open(); return q.ExecuteScalar();
        }
    }
    static int N(string s, params object[] p) { return Convert.ToInt32(Sql(s, p)); }
    static void Check(bool ok, string mensaje) { if (!ok) throw new Exception("FAIL: " + mensaje); checks++; Console.WriteLine("PASS: " + mensaje); }
    static Participante Persona() { return new Participante { Nombre = "QA", Apellido = "Participantes", Dni = (++siguiente).ToString() }; }
    static int Alta()
    {
        var r = participantes.Guardar(Persona(), gestor);
        if (!r.Exitoso) throw new Exception(r.Mensaje);
        return r.Valor;
    }
    static int Actividad(int cupo)
    {
        var r = actividades.Guardar(new Actividad { IdEspacioArtistico = espacio, Nombre = "QA participantes", ModoRecurrencia = "Fecha", Fecha = DateTime.Today.AddDays(++dia).ToString("yyyy-MM-dd"), MinutoDesde = 600, MinutoHasta = 660, CupoMaximo = cupo }, gestor);
        if (!r.Exitoso) throw new Exception(r.Mensaje);
        return r.Valor;
    }
    static Actividad ObtenerActividad(int id) { return new MPP_Actividad().ObtenerPorId(new Actividad { IdActividad = id }); }
    static int Cuenta(int id) { return N("SELECT COUNT(*) FROM dbo.ActividadParticipante ap JOIN dbo.Participante p ON p.idParticipante=ap.idParticipante WHERE ap.idActividad=@p0 AND ap.activa=1 AND p.activo=1", id); }
    static int Auditoria(string entidad, int id) { return N("SELECT COUNT(*) FROM dbo.RegistroActividad WHERE tipoEntidadAfectada=@p0 AND idEntidadAfectada=@p1", entidad, id); }
    static void Fallo() { Sql("CREATE OR ALTER TRIGGER dbo.QA_ParticipantesConsistentesAudit ON dbo.RegistroActividad AFTER INSERT AS BEGIN THROW 51000,'Fallo QA auditoria',1; END"); }
    static void QuitarFallo() { Sql("IF OBJECT_ID('dbo.QA_ParticipantesConsistentesAudit','TR') IS NOT NULL DROP TRIGGER dbo.QA_ParticipantesConsistentesAudit"); }
    static ResultadoOperacion[] Paralelo(params Func<ResultadoOperacion>[] acciones)
    {
        using (var puerta = new ManualResetEventSlim(false))
        {
            var tareas = acciones.Select(f => Task.Factory.StartNew(() => { puerta.Wait(); return f(); }, TaskCreationOptions.LongRunning)).ToArray();
            puerta.Set(); Task.WaitAll(tareas); return tareas.Select(t => t.Result).ToArray();
        }
    }
    static void Preparar()
    {
        var u = new UsuarioExterno { Nombre = "QA", Apellido = "Participantes", CorreoElectronico = Guid.NewGuid().ToString("N") + "@example.test", EstadoCuenta = "Activa", PerfilUsuario = "GestorEspacios", AceptaTerminos = true, AceptaPoliticaPrivacidad = true, FechaAceptacionTerminos = DateTime.Now };
        ProtectorDeCredenciales.ProtegerPassword(u, "QaPrueba2026"); gestor = new MPP_UsuarioExterno().Insertar(u);
        espacio = new MPP_EspacioArtistico().GuardarFicha(new EspacioArtistico { IdUsuarioGestor = gestor, NombreEspacio = "QA participantes", Descripcion = "QA", TipoEspacio = "Sala", Ficha = new FichaEspacio { Provincia = "Buenos Aires", Ciudad = "CABA", Direccion = "QA 123", CapacidadMaxima = 20, PrecioHora = 1000m, Moneda = "ARS", TipoPiso = "Madera", Disponibilidad = Enumerable.Range(1, 7).Select(d => new FranjaEspacio { DiaSemana = d, MinutoDesde = 0, MinutoHasta = 1440 }).ToList() } });
    }
    static void GuardadoParticipantes()
    {
        var nueva = Persona();
        Fallo(); try
        {
            Check(!participantes.Guardar(nueva, gestor).Exitoso && N("SELECT COUNT(*) FROM dbo.Participante WHERE idUsuarioGestor=@p0 AND dni=@p1", gestor, nueva.Dni) == 0, "alta fallida no deja participante guardado");
        } finally { QuitarFallo(); }
        var alta = participantes.Guardar(nueva, gestor); int id = alta.Valor;
        Check(alta.Exitoso && Auditoria("Participante", id) == 1, "reintento del alta guarda participante y auditoria");
        Check(!participantes.Guardar(nueva, gestor).Exitoso, "se conserva control de DNI duplicado");
        var edicion = participantes.ObtenerParaEditar(id, gestor); edicion.Nombre = "Nombre nuevo";
        Fallo(); try
        {
            Check(!participantes.Guardar(edicion, gestor).Exitoso && Convert.ToString(Sql("SELECT nombre FROM dbo.Participante WHERE idParticipante=@p0", id)) == "QA" && Auditoria("Participante", id) == 1, "edicion fallida conserva datos y auditoria anterior");
            Check(!participantes.DarDeBaja(id, gestor).Exitoso && Convert.ToBoolean(Sql("SELECT activo FROM dbo.Participante WHERE idParticipante=@p0", id)), "baja fallida conserva participante activo");
        } finally { QuitarFallo(); }
        Check(participantes.Guardar(edicion, gestor).Exitoso && Auditoria("Participante", id) == 2, "edicion normal y auditoria");
        Check(!participantes.Guardar(edicion, int.MaxValue).Exitoso && !participantes.DarDeBaja(id, int.MaxValue).Exitoso, "edicion y baja exigen propietario");
        Check(participantes.DarDeBaja(id, gestor).Exitoso && Auditoria("Participante", id) == 3, "baja normal y auditoria");
        Check(!participantes.Guardar(edicion, gestor).Exitoso && !participantes.DarDeBaja(id, gestor).Exitoso, "participante inactivo no admite edicion ni doble baja");
        var correo1 = Persona(); correo1.Correo = Guid.NewGuid().ToString("N") + "@example.test";
        Check(participantes.Guardar(correo1, gestor).Exitoso, "alta con correo valido");
        var correo2 = Persona(); correo2.Correo = correo1.Correo;
        Check(!participantes.Guardar(correo2, gestor).Exitoso, "se conserva control de correo duplicado");
        var invalida = Persona(); invalida.Dni = "abc";
        Check(!participantes.Guardar(invalida, gestor).Exitoso && !participantes.Guardar(null, gestor).Exitoso, "se conservan validaciones de datos");
    }
    static void Asociaciones()
    {
        int a = Actividad(1), p = Alta(), otro = Alta(), prev = Auditoria("Actividad", a);
        Fallo(); try
        {
            Check(!actividades.AsociarParticipante(a, p, gestor).Exitoso && Cuenta(a) == 0 && Auditoria("Actividad", a) == prev, "asociacion fallida revierte vinculo y auditoria");
        } finally { QuitarFallo(); }
        Check(actividades.AsociarParticipante(a, p, gestor).Exitoso && Cuenta(a) == 1 && Auditoria("Actividad", a) == prev + 1, "asociacion normal guarda vinculo y auditoria");
        Check(!actividades.AsociarParticipante(a, otro, gestor).Exitoso, "cupo completo rechaza nueva asociacion");
        Check(!actividades.AsociarParticipante(a, p, gestor).Exitoso, "no duplica una asociacion existente");
        Check(!actividades.AsociarParticipante(a, otro, int.MaxValue).Exitoso && !actividades.DesasociarParticipante(a, p, int.MaxValue).Exitoso, "vincular y desvincular exigen propietario");
        Fallo(); try
        {
            Check(!actividades.DesasociarParticipante(a, p, gestor).Exitoso && Cuenta(a) == 1 && Auditoria("Actividad", a) == prev + 1, "desvinculacion fallida conserva vinculo y auditoria");
        } finally { QuitarFallo(); }
        Check(actividades.DesasociarParticipante(a, p, gestor).Exitoso && Cuenta(a) == 0, "desvinculacion normal libera cupo");
        Check(actividades.AsociarParticipante(a, p, gestor).Exitoso && N("SELECT COUNT(*) FROM dbo.ActividadParticipante WHERE idActividad=@p0 AND idParticipante=@p1", a, p) == 1, "reasociacion conserva una sola fila historica");
        Check(participantes.DarDeBaja(p, gestor).Exitoso && Cuenta(a) == 0, "baja de participante conserva liberacion de cupo");
        Check(!actividades.AsociarParticipante(a, p, gestor).Exitoso && actividades.AsociarParticipante(a, otro, gestor).Exitoso, "inactivo no puede asociarse y otro participante ocupa el lugar");
        Check(actividades.DarDeBaja(a, gestor).Exitoso && !actividades.AsociarParticipante(a, Alta(), gestor).Exitoso, "actividad dada de baja no admite altas");
    }
    static void Concurrencia()
    {
        for (int ronda = 0; ronda < 4; ronda++)
        {
            int a = Actividad(1); int[] ids = Enumerable.Range(0, 6).Select(x => Alta()).ToArray();
            var resultados = Paralelo(ids.Select(id => (Func<ResultadoOperacion>)(() => new BLL_Actividad().AsociarParticipante(a, id, gestor))).ToArray());
            Check(resultados.Count(r => r.Exitoso) == 1 && Cuenta(a) == 1, "seis altas simultaneas respetan cupo uno, ronda " + ronda);
        }
        int duplicada = Actividad(2), p = Alta();
        var doble = Paralelo(() => actividades.AsociarParticipante(duplicada, p, gestor), () => actividades.AsociarParticipante(duplicada, p, gestor));
        Check(doble.Count(r => r.Exitoso) == 1 && Cuenta(duplicada) == 1, "alta simultanea repetida no duplica vinculo ni exito");
        for (int ronda = 0; ronda < 4; ronda++)
        {
            int a = Actividad(2), primero = Alta(), segundo = Alta();
            if (!actividades.AsociarParticipante(a, primero, gestor).Exitoso) throw new Exception("Preparacion cupo");
            var cambio = ObtenerActividad(a); cambio.CupoMaximo = 1;
            var resultados = Paralelo(() => actividades.Guardar(cambio, gestor), () => actividades.AsociarParticipante(a, segundo, gestor));
            Check(resultados.Count(r => r.Exitoso) == 1 && Cuenta(a) <= ObtenerActividad(a).CupoMaximo, "reducir cupo y asociar simultaneamente mantienen consistencia, ronda " + ronda);
        }
        int completa = Actividad(2), original = Alta(), nuevo = Alta(), tercero = Alta();
        actividades.AsociarParticipante(completa, original, gestor);
        var editada = ObtenerActividad(completa); editada.CupoMaximo = 1;
        Paralelo(() => actividades.Guardar(editada, gestor, new List<int> { nuevo }), () => actividades.AsociarParticipante(completa, tercero, gestor));
        Check(Cuenta(completa) == 1 && ObtenerActividad(completa).CupoMaximo == 1, "edicion con lista de participantes coordina altas independientes");
        int baja = Alta(), destino = Actividad(1);
        Paralelo(() => participantes.DarDeBaja(baja, gestor), () => actividades.AsociarParticipante(destino, baja, gestor));
        Check(!Convert.ToBoolean(Sql("SELECT activo FROM dbo.Participante WHERE idParticipante=@p0", baja)) && Cuenta(destino) == 0, "baja y asociacion simultaneas no dejan participante activo en la actividad");
        var repetida = Persona();
        var altas = Paralelo(() => participantes.Guardar(new Participante { Nombre = repetida.Nombre, Apellido = repetida.Apellido, Dni = repetida.Dni }, gestor), () => participantes.Guardar(new Participante { Nombre = repetida.Nombre, Apellido = repetida.Apellido, Dni = repetida.Dni }, gestor));
        Check(altas.Count(r => r.Exitoso) == 1 && N("SELECT COUNT(*) FROM dbo.Participante WHERE idUsuarioGestor=@p0 AND dni=@p1", gestor, repetida.Dni) == 1, "altas simultaneas del participante conservan unicidad");
    }
    static int Main()
    {
        if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("StageUp_Beta2_QA_", StringComparison.Ordinal)) throw new InvalidOperationException("Solo copia QA");
        try
        {
            Preparar(); GuardadoParticipantes(); Asociaciones(); Concurrencia();
            Console.WriteLine("TOTAL: " + checks + " comprobaciones correctas"); return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
        finally { QuitarFallo(); }
    }
}
