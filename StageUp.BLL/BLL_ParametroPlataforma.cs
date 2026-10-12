using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using StageUp.BE.Entidades;
using StageUp.DAL;
using StageUp.MPP;
using StageUp.Servicios;

namespace StageUp.BLL
{
    // CU-002-004 Configurar parámetros administrativos de la plataforma
    // (scripts 49, 53 y 65), editables desde Interno/ParametrosPlataforma.aspx
    // (permiso CONFIGURAR_PARAMETROS).
    //
    // Los parámetros se agrupan en: comisión y pagos, política de
    // cancelación, estados administrativos, datos de facturación de Artera y
    // correos administrativos. Cada cambio queda en el historial (valor
    // anterior, valor nuevo, responsable, fecha y vigencia) y en la bitácora.
    // Un cambio con vigencia futura queda programado y empieza a regir solo.
    // Si la base no responde se usan los valores por defecto, para que una
    // reserva nunca falle por no poder leer un parámetro.
    public class BLL_ParametroPlataforma
    {
        public const string ComisionPlataformaPorcentaje = "ComisionPlataformaPorcentaje";
        public const string HorasLimitePago = "HorasLimitePago";
        // Política de cancelación del CU-001-005 (A14 a A16), script 53.
        public const string DiasCancelacionSinCargo = "DiasCancelacionSinCargo";
        public const string DiasCancelacionCargoParcial = "DiasCancelacionCargoParcial";
        public const string PorcentajeCargoParcial = "PorcentajeCargoParcial";
        public const string PorcentajeCargoTotal = "PorcentajeCargoTotal";
        // Estados administrativos (texto con el que se muestra cada estado).
        public const string EstadoPagoAprobado = "EstadoPagoAprobado";
        public const string EstadoPagoPendiente = "EstadoPagoPendiente";
        public const string EstadoPagoEsperando = "EstadoPagoEsperando";
        public const string EstadoPagoRechazado = "EstadoPagoRechazado";
        public const string EstadoPagoAnulado = "EstadoPagoAnulado";
        public const string EstadoPagoVencido = "EstadoPagoVencido";
        public const string EstadoPagoObservado = "EstadoPagoObservado";
        public const string EstadoPagoNoAsociado = "EstadoPagoNoAsociado";
        public const string EstadoFacturaEmitida = "EstadoFacturaEmitida";
        public const string EstadoFacturaPendiente = "EstadoFacturaPendiente";
        public const string EstadoFacturaAnulada = "EstadoFacturaAnulada";
        public const string EstadoOperacionPagado = "EstadoOperacionPagado";
        public const string EstadoOperacionCobrado = "EstadoOperacionCobrado";
        public const string EstadoOperacionDevuelto = "EstadoOperacionDevuelto";
        public const string EstadoOperacionPendiente = "EstadoOperacionPendiente";
        // Datos generales de facturación de Artera.
        public const string FacturacionRazonSocial = "FacturacionRazonSocial";
        public const string FacturacionCuit = "FacturacionCuit";
        public const string FacturacionCondicionIva = "FacturacionCondicionIva";
        public const string FacturacionDomicilio = "FacturacionDomicilio";
        public const string FacturacionPuntoVenta = "FacturacionPuntoVenta";
        public const string FacturacionLeyenda = "FacturacionLeyenda";
        // Correos administrativos.
        public const string CorreosAdministrativosActivos = "CorreosAdministrativosActivos";
        public const string CorreosAdministrativosRemitente = "CorreosAdministrativosRemitente";
        public const string CorreosAdministrativosResponderA = "CorreosAdministrativosResponderA";
        public const string CorreosAdministrativosCopia = "CorreosAdministrativosCopia";

        // Grupos del paso 4.
        public const string GrupoComision = "Comision";
        public const string GrupoCancelacion = "Cancelacion";
        public const string GrupoEstados = "Estados";
        public const string GrupoFacturacion = "Facturacion";
        public const string GrupoCorreos = "Correos";

        public const string ValorSi = "Si";
        public const string ValorNo = "No";

        public enum TipoParametro { Numero, Texto, Correo, SiNo, Cuit }

        public class GrupoParametros
        {
            public string Codigo { get; set; }
            public string Titulo { get; set; }
            public string Descripcion { get; set; }
            public string CodigoAlternativo { get; set; }
        }

        private class DefinicionParametro
        {
            public string Grupo;
            public string Subgrupo;
            public string Nombre;
            public TipoParametro Tipo;
            public string Unidad = string.Empty;
            public decimal Minimo;
            public decimal Maximo;
            public bool SoloEnteros;
            public decimal PorDefecto;
            public string PorDefectoTexto;
            public bool Obligatorio = true;
            public int LongitudMaxima = 200;
        }

        private static readonly List<GrupoParametros> Grupos = new List<GrupoParametros>
        {
            new GrupoParametros { Codigo = GrupoComision, CodigoAlternativo = "A10", Titulo = "Comisión de StageUp y pagos",
                Descripcion = "Porcentaje que StageUp retiene de cada reserva pagada y plazo para pagar una reserva aceptada." },
            new GrupoParametros { Codigo = GrupoCancelacion, CodigoAlternativo = "A11", Titulo = "Política de cancelación",
                Descripcion = "Plazos y cargos de la cancelación de una reserva aceptada y pagada." },
            new GrupoParametros { Codigo = GrupoEstados, CodigoAlternativo = "A12", Titulo = "Estados administrativos de pagos y facturas",
                Descripcion = "Texto con el que se muestra cada estado de pagos, facturas y operaciones económicas." },
            new GrupoParametros { Codigo = GrupoFacturacion, CodigoAlternativo = "A13", Titulo = "Datos generales de facturación de Artera",
                Descripcion = "Datos del emisor que figuran en las facturas simuladas." },
            new GrupoParametros { Codigo = GrupoCorreos, CodigoAlternativo = "A14", Titulo = "Correos administrativos",
                Descripcion = "Correos de pagos, vencimientos de pago y notas de crédito o débito." }
        };

        private static readonly List<KeyValuePair<string, DefinicionParametro>> ListaDefiniciones = new List<KeyValuePair<string, DefinicionParametro>>
        {
            Def(ComisionPlataformaPorcentaje, new DefinicionParametro { Grupo = GrupoComision, Nombre = "Comisión de StageUp por reserva", Tipo = TipoParametro.Numero, Unidad = "%", Minimo = 0, Maximo = 50, PorDefecto = 10 }),
            Def(HorasLimitePago, new DefinicionParametro { Grupo = GrupoComision, Nombre = "Plazo para pagar una reserva aceptada", Tipo = TipoParametro.Numero, Unidad = "horas", Minimo = 1, Maximo = 168, SoloEnteros = true, PorDefecto = 48 }),
            Def(DiasCancelacionSinCargo, new DefinicionParametro { Grupo = GrupoCancelacion, Nombre = "Anticipación para cancelar sin cargo", Tipo = TipoParametro.Numero, Unidad = "días", Minimo = 1, Maximo = 365, SoloEnteros = true, PorDefecto = 14 }),
            Def(DiasCancelacionCargoParcial, new DefinicionParametro { Grupo = GrupoCancelacion, Nombre = "Anticipación mínima para el cargo parcial", Tipo = TipoParametro.Numero, Unidad = "días", Minimo = 0, Maximo = 364, SoloEnteros = true, PorDefecto = 7 }),
            Def(PorcentajeCargoParcial, new DefinicionParametro { Grupo = GrupoCancelacion, Nombre = "Cargo parcial por cancelación", Tipo = TipoParametro.Numero, Unidad = "%", Minimo = 0, Maximo = 100, PorDefecto = 50 }),
            Def(PorcentajeCargoTotal, new DefinicionParametro { Grupo = GrupoCancelacion, Nombre = "Cargo total por cancelación", Tipo = TipoParametro.Numero, Unidad = "%", Minimo = 0, Maximo = 100, PorDefecto = 100 }),

            Estado(EstadoPagoAprobado, "Pagos", "Pago confirmado", "Confirmado"),
            Estado(EstadoPagoPendiente, "Pagos", "Pago pendiente (informado por el medio)", "Pendiente (informado por el medio de pago)"),
            Estado(EstadoPagoEsperando, "Pagos", "Reserva esperando el pago", "Pendiente (esperando el pago)"),
            Estado(EstadoPagoRechazado, "Pagos", "Pago fallido", "Fallido"),
            Estado(EstadoPagoAnulado, "Pagos", "Pago anulado", "Cancelado"),
            Estado(EstadoPagoVencido, "Pagos", "Plazo de pago vencido", "Cancelado (venció el plazo de pago)"),
            Estado(EstadoPagoObservado, "Pagos", "Aviso observado", "En revisión (observado)"),
            Estado(EstadoPagoNoAsociado, "Pagos", "Aviso sin reserva", "En revisión (sin reserva asociada)"),
            Estado(EstadoFacturaEmitida, "Facturas", "Factura emitida", "Emitida"),
            Estado(EstadoFacturaPendiente, "Facturas", "Factura pendiente", "Factura pendiente"),
            Estado(EstadoFacturaAnulada, "Facturas", "Factura o comprobante anulado", "Anulada"),
            Estado(EstadoOperacionPagado, "Operaciones", "Operación pagada", "Pagado"),
            Estado(EstadoOperacionCobrado, "Operaciones", "Cargo cobrado", "Cobrado"),
            Estado(EstadoOperacionDevuelto, "Operaciones", "Importe devuelto", "Devuelto al solicitante"),
            Estado(EstadoOperacionPendiente, "Operaciones", "Operación pendiente", "Pendiente"),

            Def(FacturacionRazonSocial, new DefinicionParametro { Grupo = GrupoFacturacion, Nombre = "Razón social", Tipo = TipoParametro.Texto, PorDefectoTexto = "Artera – StageUp", LongitudMaxima = 120 }),
            Def(FacturacionCuit, new DefinicionParametro { Grupo = GrupoFacturacion, Nombre = "CUIT", Tipo = TipoParametro.Cuit, PorDefectoTexto = "30-00000000-0", LongitudMaxima = 13 }),
            Def(FacturacionCondicionIva, new DefinicionParametro { Grupo = GrupoFacturacion, Nombre = "Condición frente al IVA", Tipo = TipoParametro.Texto, PorDefectoTexto = "IVA Responsable Inscripto", LongitudMaxima = 60 }),
            Def(FacturacionDomicilio, new DefinicionParametro { Grupo = GrupoFacturacion, Nombre = "Domicilio comercial", Tipo = TipoParametro.Texto, PorDefectoTexto = "Ciudad Autónoma de Buenos Aires", LongitudMaxima = 150 }),
            Def(FacturacionPuntoVenta, new DefinicionParametro { Grupo = GrupoFacturacion, Nombre = "Punto de venta", Tipo = TipoParametro.Numero, Minimo = 1, Maximo = 99999, SoloEnteros = true, PorDefecto = 1 }),
            Def(FacturacionLeyenda, new DefinicionParametro { Grupo = GrupoFacturacion, Nombre = "Leyenda al pie de la factura", Tipo = TipoParametro.Texto, PorDefectoTexto = "Comprobante simulado emitido por StageUp. No tiene validez fiscal.", LongitudMaxima = 300, Obligatorio = false }),

            Def(CorreosAdministrativosActivos, new DefinicionParametro { Grupo = GrupoCorreos, Nombre = "Enviar correos administrativos", Tipo = TipoParametro.SiNo, PorDefectoTexto = ValorSi }),
            Def(CorreosAdministrativosRemitente, new DefinicionParametro { Grupo = GrupoCorreos, Nombre = "Nombre del remitente", Tipo = TipoParametro.Texto, PorDefectoTexto = "StageUp Administración", LongitudMaxima = 80 }),
            Def(CorreosAdministrativosResponderA, new DefinicionParametro { Grupo = GrupoCorreos, Nombre = "Responder a", Tipo = TipoParametro.Correo, PorDefectoTexto = string.Empty, LongitudMaxima = 150, Obligatorio = false }),
            Def(CorreosAdministrativosCopia, new DefinicionParametro { Grupo = GrupoCorreos, Nombre = "Copia oculta a administración", Tipo = TipoParametro.Correo, PorDefectoTexto = string.Empty, LongitudMaxima = 150, Obligatorio = false })
        };

        private static readonly Dictionary<string, DefinicionParametro> Definiciones = ListaDefiniciones.ToDictionary(d => d.Key, d => d.Value);

        private const string TipoEntidadBitacora = "ParametroPlataforma";
        private const int MaximoDiasVigencia = 365;
        private static readonly Regex FormatoCuit = new Regex(@"^\d{2}-\d{8}-\d$");

        private readonly MPP_ParametroPlataforma _mpp = new MPP_ParametroPlataforma();
        private readonly BLL_Bitacora _bitacora = new BLL_Bitacora();

        // Ítem 36: los parámetros se leen en cada reserva, cancelación y pago
        // (y en Mis reservas, una vez por fila). Se guardan en memoria unos
        // minutos (o hasta que empiece a regir un cambio programado) y se
        // descartan cuando el administrador los modifica.
        private const int MinutosCache = 5;
        private static readonly object _lockCache = new object();
        private static List<ParametroPlataforma> _cache;
        private static DateTime _vencimientoCache = DateTime.MinValue;

        private static KeyValuePair<string, DefinicionParametro> Def(string clave, DefinicionParametro definicion)
        {
            return new KeyValuePair<string, DefinicionParametro>(clave, definicion);
        }

        private static KeyValuePair<string, DefinicionParametro> Estado(string clave, string subgrupo, string nombre, string porDefecto)
        {
            return Def(clave, new DefinicionParametro
            {
                Grupo = GrupoEstados, Subgrupo = subgrupo, Nombre = nombre, Tipo = TipoParametro.Texto, PorDefectoTexto = porDefecto, LongitudMaxima = 60
            });
        }

        private List<ParametroPlataforma> ListarConCache()
        {
            lock (_lockCache)
            {
                if (_cache != null && DateTime.Now < _vencimientoCache)
                {
                    return _cache;
                }
            }

            List<ParametroPlataforma> parametros = _mpp.Listar();
            DateTime vencimiento = DateTime.Now.AddMinutes(MinutosCache);
            DateTime? proximaVigencia = parametros.Where(p => p.FechaVigenciaProgramada.HasValue).Select(p => p.FechaVigenciaProgramada).Min();
            if (proximaVigencia.HasValue && proximaVigencia.Value < vencimiento)
            {
                vencimiento = proximaVigencia.Value;
            }

            lock (_lockCache)
            {
                _cache = parametros;
                _vencimientoCache = vencimiento;
            }

            return parametros;
        }

        public static void InvalidarCache()
        {
            lock (_lockCache)
            {
                _cache = null;
            }
        }

        // ---- Definiciones ---------------------------------------------------

        public static List<GrupoParametros> ObtenerGrupos()
        {
            return Grupos.ToList();
        }

        public static string ObtenerNombre(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Nombre : clave;
        }

        public static string ObtenerUnidad(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Unidad : string.Empty;
        }

        public static string ObtenerGrupo(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Grupo : null;
        }

        public static string ObtenerSubgrupo(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Subgrupo : null;
        }

        public static TipoParametro ObtenerTipo(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) ? definicion.Tipo : TipoParametro.Texto;
        }

        public static int ObtenerLongitudMaxima(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion)
                ? (definicion.Tipo == TipoParametro.Numero ? 10 : definicion.LongitudMaxima) : 200;
        }

        public static bool EsObligatorio(string clave)
        {
            DefinicionParametro definicion;
            return Definiciones.TryGetValue(clave ?? string.Empty, out definicion) && definicion.Obligatorio;
        }

        // Valor como se muestra en la consulta (A2).
        public static string FormatearValor(string clave, string valor)
        {
            DefinicionParametro definicion;
            if (!Definiciones.TryGetValue(clave ?? string.Empty, out definicion))
            {
                return valor;
            }

            if (definicion.Tipo == TipoParametro.SiNo)
            {
                return valor == ValorNo ? "No" : "Sí";
            }

            if (string.IsNullOrWhiteSpace(valor))
            {
                return "Sin definir";
            }

            decimal numero;
            if (definicion.Tipo == TipoParametro.Numero &&
                decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out numero))
            {
                valor = numero.ToString("0.##", CultureInfo.GetCultureInfo("es-AR"));
            }

            return definicion.Unidad.Length == 0 ? valor : valor + " " + definicion.Unidad;
        }

        // ---- Consulta -------------------------------------------------------

        // Pasos 3 a 5: parámetros vigentes en el orden de los grupos.
        public List<ParametroPlataforma> Listar()
        {
            try
            {
                Dictionary<string, ParametroPlataforma> guardados = _mpp.Listar().ToDictionary(p => p.Clave);
                List<ParametroPlataforma> lista = new List<ParametroPlataforma>();
                foreach (KeyValuePair<string, DefinicionParametro> definicion in ListaDefiniciones)
                {
                    ParametroPlataforma parametro;
                    if (guardados.TryGetValue(definicion.Key, out parametro))
                    {
                        lista.Add(parametro);
                    }
                }
                return lista;
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<ParametroPlataforma>();
            }
        }

        public List<CambioParametroPlataforma> ListarCambios(string clave)
        {
            try
            {
                return _mpp.ListarCambios(Definiciones.ContainsKey(clave ?? string.Empty) ? clave : null);
            }
            catch (ErrorAccesoDatosException)
            {
                return new List<CambioParametroPlataforma>();
            }
        }

        public decimal ObtenerDecimal(string clave)
        {
            DefinicionParametro definicion = Definiciones[clave];
            try
            {
                ParametroPlataforma parametro = ListarConCache().FirstOrDefault(p => p.Clave == clave);
                decimal valor;
                if (parametro != null &&
                    decimal.TryParse(parametro.Valor, NumberStyles.Number, CultureInfo.InvariantCulture, out valor) &&
                    valor >= definicion.Minimo && valor <= definicion.Maximo)
                {
                    return valor;
                }
            }
            catch (ErrorAccesoDatosException)
            {
            }

            return definicion.PorDefecto;
        }

        public int ObtenerEntero(string clave)
        {
            return (int)Math.Round(ObtenerDecimal(clave));
        }

        public string ObtenerTexto(string clave)
        {
            DefinicionParametro definicion = Definiciones[clave];
            try
            {
                ParametroPlataforma parametro = ListarConCache().FirstOrDefault(p => p.Clave == clave);
                if (parametro != null && (!definicion.Obligatorio || !string.IsNullOrWhiteSpace(parametro.Valor)))
                {
                    return parametro.Valor ?? string.Empty;
                }
            }
            catch (ErrorAccesoDatosException)
            {
            }

            return definicion.PorDefectoTexto ?? string.Empty;
        }

        // Paso 14: texto configurado para un estado administrativo.
        public static string TextoEstado(string clave)
        {
            return new BLL_ParametroPlataforma().ObtenerTexto(clave);
        }

        // A6 paso 5: datos de facturación vigentes (o los que regían en la
        // fecha de una factura ya emitida).
        public DatosFacturacion ObtenerDatosFacturacion(DateTime? fechaFactura)
        {
            Dictionary<string, string> valores = null;
            if (fechaFactura.HasValue)
            {
                try
                {
                    valores = _mpp.ObtenerValoresAl(fechaFactura.Value);
                }
                catch (ErrorAccesoDatosException)
                {
                    valores = null;
                }
            }

            Func<string, string> valor = clave =>
            {
                string texto;
                return valores != null && valores.TryGetValue(clave, out texto) && (!Definiciones[clave].Obligatorio || !string.IsNullOrWhiteSpace(texto))
                    ? texto
                    : ObtenerTexto(clave);
            };

            int puntoVenta;
            if (!int.TryParse(valor(FacturacionPuntoVenta), NumberStyles.Integer, CultureInfo.InvariantCulture, out puntoVenta) || puntoVenta < 1)
            {
                puntoVenta = 1;
            }

            DatosFacturacion datos = new DatosFacturacion
            {
                RazonSocial = valor(FacturacionRazonSocial),
                Cuit = valor(FacturacionCuit),
                CondicionIva = valor(FacturacionCondicionIva),
                Domicilio = valor(FacturacionDomicilio),
                PuntoVenta = puntoVenta,
                Leyenda = valor(FacturacionLeyenda)
            };
            datos.DatosFiscales = "CUIT " + datos.Cuit + " · " + datos.CondicionIva + " · " + datos.Domicilio +
                                  " · Punto de venta " + puntoVenta.ToString("00000", CultureInfo.InvariantCulture);
            return datos;
        }

        // A7 paso 5: configuración para los próximos correos administrativos.
        public ConfiguracionCorreoAdministrativo ObtenerConfiguracionCorreoAdministrativo()
        {
            return new ConfiguracionCorreoAdministrativo
            {
                Activos = ObtenerTexto(CorreosAdministrativosActivos) != ValorNo,
                NombreRemitente = ObtenerTexto(CorreosAdministrativosRemitente),
                ResponderA = ObtenerTexto(CorreosAdministrativosResponderA),
                CopiaOculta = ObtenerTexto(CorreosAdministrativosCopia)
            };
        }

        // ---- Edición --------------------------------------------------------

        public ResultadoOperacion Actualizar(string clave, string valorIngresado, int idUsuarioInternoResponsable)
        {
            return GuardarCambios(new Dictionary<string, string> { { clave ?? string.Empty, valorIngresado } }, null, idUsuarioInternoResponsable);
        }

        // Pasos 10 a 15. Primero valida cada valor (A9 a A14) y la coherencia
        // de la política de cancelación con los valores nuevos; solo si todo
        // está bien guarda los cambios juntos (A15 si falla). Sin vigencia (o
        // con la fecha de hoy) rigen en el momento; con una fecha futura
        // quedan programados desde las 00:00 de ese día.
        public ResultadoOperacion GuardarCambios(IDictionary<string, string> valoresIngresados, DateTime? vigenciaDesde, int idUsuarioInternoResponsable)
        {
            if (valoresIngresados == null || valoresIngresados.Count == 0)
            {
                return ResultadoOperacion.Ok(null);
            }

            DateTime? vigencia = null;
            if (vigenciaDesde.HasValue)
            {
                DateTime dia = vigenciaDesde.Value.Date;
                if (dia < DateTime.Today)
                {
                    return ResultadoOperacion.Error("La vigencia no puede ser anterior a hoy.", "A9");
                }

                if (dia > DateTime.Today.AddDays(MaximoDiasVigencia))
                {
                    return ResultadoOperacion.Error("La vigencia puede programarse hasta un año desde hoy.", "A9");
                }

                vigencia = dia > DateTime.Today ? dia : (DateTime?)null;
            }

            Dictionary<string, string> nuevos = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> errores = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, string> par in valoresIngresados)
            {
                string normalizado;
                string codigo;
                string error = ValidarValor(par.Key, par.Value, out normalizado, out codigo);
                if (error != null)
                {
                    errores.Add(new KeyValuePair<string, string>(codigo, error));
                }
                else
                {
                    nuevos[par.Key] = normalizado;
                }
            }

            if (errores.Count == 0)
            {
                Func<string, decimal> numero = clave =>
                {
                    decimal valor;
                    return nuevos.ContainsKey(clave) && decimal.TryParse(nuevos[clave], NumberStyles.Number, CultureInfo.InvariantCulture, out valor)
                        ? valor
                        : ObtenerDecimal(clave);
                };

                string errorCancelacion = ValidarConsistenciaCancelacion(numero);
                if (errorCancelacion != null)
                {
                    errores.Add(new KeyValuePair<string, string>("A11", errorCancelacion));
                }

                foreach (string errorEstado in ValidarEstadosDuplicados(clave => nuevos.ContainsKey(clave) ? nuevos[clave] : ObtenerTexto(clave)))
                {
                    errores.Add(new KeyValuePair<string, string>("A12", errorEstado));
                }
            }

            if (errores.Count > 0)
            {
                return ResultadoOperacion.Error(string.Join(" ", errores.Select(e => e.Value)), errores[0].Key);
            }

            List<CambioParametroPlataforma> registrados;
            try
            {
                registrados = _mpp.GuardarCambios(nuevos, vigencia, idUsuarioInternoResponsable);
            }
            catch (ErrorAccesoDatosException)
            {
                return ResultadoOperacion.Error("No fue posible guardar la configuración en este momento. No se aplicó ningún cambio; probá de nuevo.", "A15");
            }

            if (registrados == null)
            {
                return ResultadoOperacion.Error("No fue posible guardar la configuración: falta algún parámetro en la base. Revisá que estén aplicados los scripts.", "A15");
            }

            InvalidarCache();

            // Paso 13: bitácora (CU-001-013) además del historial de cambios.
            foreach (CambioParametroPlataforma cambio in registrados)
            {
                _bitacora.RegistrarInterno(
                    idUsuarioInternoResponsable, "MODIFICACION", TipoEntidadBitacora, cambio.IdCambio,
                    ObtenerNombre(cambio.Clave) + ": " + FormatearValor(cambio.Clave, cambio.ValorAnterior) + " → " +
                    FormatearValor(cambio.Clave, cambio.ValorNuevo) + ". " + DescribirVigencia(cambio));
            }

            if (registrados.Count == 0)
            {
                return ResultadoOperacion.Ok(null);
            }

            return ResultadoOperacion.Ok(vigencia.HasValue
                ? "Los parámetros administrativos se actualizaron: los cambios rigen desde el " +
                  vigencia.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " para las operaciones nuevas."
                : "Los parámetros administrativos se actualizaron correctamente. Los cambios rigen desde ahora para las operaciones nuevas.");
        }

        public static string DescribirVigencia(CambioParametroPlataforma cambio)
        {
            if (cambio.Estado == "Programado")
            {
                return "Programado: rige desde el " + cambio.FechaVigencia.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + ".";
            }

            if (cambio.Estado == "Reemplazado")
            {
                return "Reemplazado por un cambio posterior antes de entrar en vigencia.";
            }

            return "Rige desde el " + (cambio.FechaAplicacion ?? cambio.FechaVigencia).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + ".";
        }

        private static string ValidarValor(string clave, string valorIngresado, out string normalizado, out string codigo)
        {
            normalizado = null;
            DefinicionParametro definicion;
            if (!Definiciones.TryGetValue(clave ?? string.Empty, out definicion))
            {
                codigo = "A9";
                return "El parámetro indicado no existe.";
            }

            codigo = Grupos.First(g => g.Codigo == definicion.Grupo).CodigoAlternativo;
            string texto = (valorIngresado ?? string.Empty).Trim();
            if (texto.Any(char.IsControl))
            {
                return definicion.Nombre + ": tiene caracteres no permitidos.";
            }

            // A9: obligatorios.
            if (texto.Length == 0 && definicion.Obligatorio && definicion.Tipo != TipoParametro.SiNo)
            {
                codigo = "A9";
                return "Completá " + definicion.Nombre.ToLowerInvariant() + ".";
            }

            switch (definicion.Tipo)
            {
                case TipoParametro.Numero:
                    decimal numero;
                    if (!decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out numero))
                    {
                        return definicion.Nombre + ": ingresá un número.";
                    }

                    if (definicion.SoloEnteros && numero != Math.Truncate(numero))
                    {
                        return definicion.Nombre + ": ingresá un número entero.";
                    }

                    if (numero < definicion.Minimo || numero > definicion.Maximo)
                    {
                        return definicion.Nombre + ": tiene que estar entre " + definicion.Minimo.ToString("0.##", CultureInfo.InvariantCulture) +
                               " y " + definicion.Maximo.ToString("0.##", CultureInfo.InvariantCulture) +
                               (definicion.Unidad.Length == 0 ? "." : " " + definicion.Unidad + ".");
                    }

                    normalizado = decimal.Round(numero, 2).ToString("0.##", CultureInfo.InvariantCulture);
                    return null;

                case TipoParametro.SiNo:
                    normalizado = texto == ValorNo ? ValorNo : ValorSi;
                    return null;

                case TipoParametro.Cuit:
                    string digitos = new string(texto.Where(char.IsDigit).ToArray());
                    string cuit = digitos.Length == 11 ? digitos.Substring(0, 2) + "-" + digitos.Substring(2, 8) + "-" + digitos.Substring(10) : texto;
                    if (!FormatoCuit.IsMatch(cuit))
                    {
                        return definicion.Nombre + ": ingresá los 11 dígitos con el formato 00-00000000-0.";
                    }

                    normalizado = cuit;
                    return null;

                case TipoParametro.Correo:
                    if (texto.Length > 0 && !EsCorreoValido(texto))
                    {
                        return definicion.Nombre + ": ingresá una dirección de correo válida o dejalo vacío.";
                    }
                    break;
            }

            if (texto.Length > definicion.LongitudMaxima)
            {
                return definicion.Nombre + ": puede tener hasta " + definicion.LongitudMaxima + " caracteres.";
            }

            normalizado = texto;
            return null;
        }

        private static bool EsCorreoValido(string correo)
        {
            if (correo.Length > 150 || correo.Contains(" ") || correo.Contains(",") || correo.Contains(";"))
            {
                return false;
            }

            try
            {
                MailAddress direccion = new MailAddress(correo);
                return direccion.Address == correo && correo.IndexOf('.', correo.IndexOf('@')) > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // A11: el plazo del cargo parcial tiene que ser menor que el plazo sin
        // cargo, y el cargo parcial no puede superar al total.
        private static string ValidarConsistenciaCancelacion(Func<string, decimal> valor)
        {
            decimal diasSinCargo = valor(DiasCancelacionSinCargo);
            decimal diasParcial = valor(DiasCancelacionCargoParcial);
            if (diasParcial >= diasSinCargo)
            {
                return ObtenerNombre(DiasCancelacionCargoParcial) + " (" + diasParcial.ToString("0", CultureInfo.InvariantCulture) +
                    " días) tiene que ser menor que " + ObtenerNombre(DiasCancelacionSinCargo).ToLowerInvariant() +
                    " (" + diasSinCargo.ToString("0", CultureInfo.InvariantCulture) + " días).";
            }

            decimal parcial = valor(PorcentajeCargoParcial);
            decimal total = valor(PorcentajeCargoTotal);
            if (parcial > total)
            {
                return ObtenerNombre(PorcentajeCargoParcial) + " (" + parcial.ToString("0.##", CultureInfo.InvariantCulture) +
                    " %) no puede superar el " + ObtenerNombre(PorcentajeCargoTotal).ToLowerInvariant() +
                    " (" + total.ToString("0.##", CultureInfo.InvariantCulture) + " %).";
            }

            return null;
        }

        // A12: dentro de pagos, facturas u operaciones no puede haber dos
        // estados que se muestren igual.
        private static IEnumerable<string> ValidarEstadosDuplicados(Func<string, string> valor)
        {
            foreach (IGrouping<string, KeyValuePair<string, DefinicionParametro>> subgrupo in
                     ListaDefiniciones.Where(d => d.Value.Grupo == GrupoEstados).GroupBy(d => d.Value.Subgrupo))
            {
                foreach (IGrouping<string, KeyValuePair<string, DefinicionParametro>> repetidos in subgrupo
                    .GroupBy(d => (valor(d.Key) ?? string.Empty).Trim().ToLowerInvariant())
                    .Where(g => g.Count() > 1))
                {
                    yield return "Estados de " + subgrupo.Key.ToLowerInvariant() + ": \"" + valor(repetidos.First().Key) + "\" está repetido en " +
                                 string.Join(" y ", repetidos.Select(r => r.Value.Nombre.ToLowerInvariant())) + ".";
                }
            }
        }
    }
}
