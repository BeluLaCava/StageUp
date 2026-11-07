using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

class TestingBusqueda91
{
    static string Connection;
    static int Passed, Failed, Ties, PreviousTruncations;
    static readonly string[] Flags={"UsuariosExternos","UsuariosInternos","Espacios","Reservas","Tickets","Novedades","Faq","Encuestas","RegistrosActividad"};
    static Dictionary<string,object> Args(bool all,params object[] pairs)
    {
        var d=new Dictionary<string,object>();
        if(all) foreach(var flag in Flags)d["@incluir"+flag]=true;
        for(int i=0;i<pairs.Length;i+=2)d[(string)pairs[i]]=pairs[i+1];
        return d;
    }
    static DataTable Run(string proc, Dictionary<string,object> args)
    {
        using(var c=new SqlConnection(Connection))
        using(var cmd=new SqlCommand(proc,c))
        {
            cmd.CommandType=CommandType.StoredProcedure;cmd.CommandTimeout=120;
            foreach(var pair in args)
            {
                if(pair.Value==null)cmd.Parameters.Add(pair.Key,SqlDbType.Int).Value=DBNull.Value;
                else cmd.Parameters.AddWithValue(pair.Key,pair.Value);
            }
            var table=new DataTable();using(var da=new SqlDataAdapter(cmd))da.Fill(table);return table;
        }
    }
    static bool Eq(object a,object b){return Object.Equals(a,b);}
    static void Compare(string label,bool search,Dictionary<string,object> args)
    {
        try
        {
            string name=search?"sp_Busqueda_Interna":"sp_RegistroActividad_Buscar";
            DataTable old;
            try { old=Run(name+"_QA_Antes91",args); }
            catch(SqlException e)
            {
                if(!search || (e.Number!=2628 && e.Number!=8152))throw;

                PreviousTruncations++;
                old=Run(name+"_QA_DetalleCompleto",args);
            }
            var current=Run(name,args);
            if(old.Columns.Count!=current.Columns.Count||old.Rows.Count!=current.Rows.Count)throw new Exception("Difieren columnas o cantidad de filas: "+old.Rows.Count+" / "+current.Rows.Count);
            foreach(DataColumn col in old.Columns)if(!current.Columns.Contains(col.ColumnName)||current.Columns[col.ColumnName].DataType!=col.DataType)throw new Exception("Difiere el esquema: "+col.ColumnName);
            for(int i=0;i<old.Rows.Count;i++)
            {
                var a=old.Rows[i];var b=current.Rows[i];
                bool same=a.ItemArray.SequenceEqual(b.ItemArray);
                if(same)continue;

                bool tie=search && Eq(a["tipo"],b["tipo"]) && Eq(a["titulo"],b["titulo"]) && Eq(a["fecha"],b["fecha"]) && Eq(a["posicion"],b["posicion"]) && !Eq(a["idReferencia"],b["idReferencia"]);
                if(!tie)throw new Exception("Difiere el resultado en fila "+i);
                Ties++;
            }
            Passed++;Console.WriteLine("PASS "+label+" ("+current.Rows.Count+" filas)");
        }
        catch(Exception e){Failed++;Console.WriteLine("FAIL "+label+": "+e.Message);}
    }
    static int Main(string[] args)
    {
        if(args.Length!=2 || !System.Text.RegularExpressions.Regex.IsMatch(args[1],@"^StageUp_Beta2_QA_[A-Za-z0-9_]+$")){Console.Error.WriteLine("Requiere servidor y nombre de copia QA.");return 2;}
        Connection=new SqlConnectionStringBuilder{DataSource=args[0],InitialCatalog=args[1],IntegratedSecurity=true,TrustServerCertificate=true}.ConnectionString;
        var orders=new[]{"Relevancia","Recientes","Antiguos","Alfabetico"};
        foreach(var order in orders)
        {
            foreach(var flag in Flags)Compare("tipo "+flag+" / "+order,true,Args(false,"@incluir"+flag,true,"@orden",order,"@maximo",25));
            foreach(var max in new[]{10,25,50,100})Compare("todos / "+order+" / "+max,true,Args(true,"@texto","Volumen","@orden",order,"@maximo",max));
        }
        foreach(var q in new[]{"LOGIN","Reserva","O'Connor","%","_","[Volumen]","sin coincidencias xyz",""})Compare("texto "+q,true,Args(true,"@texto",q));
        var filters=new object[][]{
            new object[]{"@estado","Finalizada"},new object[]{"@ubicacion","Buenos","@estado","Publicado"},
            new object[]{"@categoriaNovedad","Comunidad"},new object[]{"@perfil","ExternoSolicitante"},
            new object[]{"@rol","Administrador"},new object[]{"@responsable","Volumen"},
            new object[]{"@nombre","StageUp"},new object[]{"@correo","stageup.test"},
            new object[]{"@operacion","LOGIN","@entidadAfectada","Reserva"},new object[]{"@idRegistro",1},
            new object[]{"@fechaDesde",DateTime.Today.AddDays(-30),"@fechaHasta",DateTime.Today},
            new object[]{"@fechaDesde",DateTime.Today.AddDays(40),"@fechaHasta",DateTime.Today.AddDays(50)},
            new object[]{"@ubicacion","Buenos","@categoriaNovedad","Comunidad"},
            new object[]{"@maximo",0},new object[]{"@maximo",null}
        };
        for(int i=0;i<filters.Length;i++)Compare("filtros combinados "+i,true,Args(true,filters[i]));
        Compare("sin tipos autorizados",true,Args(false,"@texto","Volumen"));
        Compare("bitacora completa",false,Args(false));
        foreach(var max in new[]{1,100,500,0,501})Compare("bitacora limite "+max,false,Args(false,"@maximo",max));
        foreach(var type in new[]{"Interno","Externo","Sistema"})Compare("bitacora responsable "+type,false,Args(false,"@tipoResponsable",type));
        foreach(var q in new[]{"Volumen","O'Connor","%","_","[","nadie-qa"})Compare("bitacora texto "+q,false,Args(false,"@textoResponsable",q));
        Compare("bitacora filtros multiples",false,Args(false,"@tipoOperacion","ALTA","@tipoEntidadAfectada","Reserva","@fechaDesde",DateTime.Today.AddDays(-180),"@fechaHasta",DateTime.Today));
        Compare("bitacora usuario externo",false,Args(false,"@idUsuarioExternoResponsable",1));
        Compare("bitacora usuario interno",false,Args(false,"@idUsuarioInternoResponsable",1));
        Console.WriteLine("RESULTADO: "+Passed+" correctas, "+Failed+" fallidas. Filas con empate previo resuelto por ID: "+Ties+". Casos que antes fallaban por longitud: "+PreviousTruncations);
        return Failed==0?0:1;
    }
}
