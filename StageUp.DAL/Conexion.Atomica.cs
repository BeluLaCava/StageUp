using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace StageUp.DAL
{
    public sealed partial class Conexion
    {

        public const string RecursoAgenda = "StageUp:Agenda";
        [ThreadStatic] private static SqlConnection _conexionAtomica;
        [ThreadStatic] private static SqlTransaction _transaccionAtomica;
        [ThreadStatic] private static List<Action> _accionesConfirmadas;
        [ThreadStatic] private static bool _abortarAtomica;

        public void DespuesDeConfirmar(Action accion)
        {
            if (_conexionAtomica == null) accion();
            else _accionesConfirmadas.Add(accion);
        }

        public T EjecutarAtomico<T>(string recurso, Func<T> operacion, Func<T, bool> confirmar)
        {
            if (_conexionAtomica != null)
            {
                try
                {
                    BloquearRecurso(recurso);
                    T interno = operacion();
                    if (!confirmar(interno)) _abortarAtomica = true;
                    return interno;
                }
                catch { _abortarAtomica = true; throw; }
            }
            T resultado;
            List<Action> acciones = null;
            try
            {
                using (SqlConnection conexion = ObtenerConexion())
                {
                    conexion.Open();
                    using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        _conexionAtomica = conexion;
                        _transaccionAtomica = transaccion;
                        _accionesConfirmadas = new List<Action>();
                        _abortarAtomica = false;
                        try
                        {
                            BloquearRecurso(recurso);
                            resultado = operacion();
                            if (confirmar(resultado) && !_abortarAtomica)
                            {
                                transaccion.Commit();
                                acciones = _accionesConfirmadas;
                            }
                            else
                            {
                                if (transaccion.Connection != null) transaccion.Rollback();
                                if (confirmar(resultado)) throw new InvalidOperationException("Una operación relacionada no pudo completarse.");
                            }
                        }
                        catch
                        {
                            if (transaccion.Connection != null) transaccion.Rollback();
                            throw;
                        }
                        finally
                        {
                            _conexionAtomica = null;
                            _transaccionAtomica = null;
                            _accionesConfirmadas = null;
                            _abortarAtomica = false;
                        }
                    }
                }
            }
            catch (ErrorAccesoDatosException) { throw; }
            catch (Exception ex)
            {
                throw new ErrorAccesoDatosException("UnidadDeTrabajo", "No se pudo completar la operación. Los cambios no se guardaron; intentá nuevamente.", ex);
            }

            if (acciones != null)
                foreach (Action accion in acciones)
                    try { accion(); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceError("Acción posterior a confirmación: " + ex.GetType().Name); }
            return resultado;
        }

        private static void BloquearRecurso(string recurso)
        {
            if (string.IsNullOrEmpty(recurso)) return;
            using (SqlCommand cmd = new SqlCommand("sys.sp_getapplock", _conexionAtomica, _transaccionAtomica))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Resource", recurso);
                cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
                cmd.Parameters.AddWithValue("@LockOwner", "Transaction");
                cmd.Parameters.AddWithValue("@LockTimeout", 10000);
                SqlParameter retorno = cmd.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                retorno.Direction = ParameterDirection.ReturnValue;
                cmd.ExecuteNonQuery();
                if (Convert.ToInt32(retorno.Value) < 0)
                    throw new InvalidOperationException("Otra operación está actualizando los mismos datos. Intentá nuevamente.");
            }
        }
    }
}
