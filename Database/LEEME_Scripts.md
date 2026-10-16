# Scripts de base de datos de StageUp

## Cómo se instala y se actualiza la base

El único flujo normal, tanto para armar la base desde cero como para actualizar una base existente, es:

```powershell
cd Database
.\EjecutarTodosLosScripts.ps1                      # instancia por defecto .\SQLEXPRESS
.\EjecutarTodosLosScripts.ps1 -ServerInstance "PC\SQLEXPRESS01"
```

El script recorre los archivos numerados (`NN_*.sql`) en orden y ejecuta **solo los que todavía no figuran** en la tabla de control `dbo._ScriptsEjecutados`. Un script ya aplicado nunca se vuelve a ejecutar, así que es seguro correrlo después de cada `git pull`.

Si la base se armó a mano antes de existir la tabla de control, la primera vez hay que correr `.\EjecutarTodosLosScripts.ps1 -MarcarComoAplicados` (no ejecuta nada, solo registra lo que ya está aplicado).

**No ejecutar los scripts numerados a mano sobre una base con datos.**

## Tipos de script

### Scripts iniciales destructivos (contienen `DROP TABLE`)

| Script | Tablas que recrea |
|---|---|
| `01_EsquemaSeguridadYRegistro.sql` | UsuarioExterno, CodigoActivacion, CodigoRecuperacion, AreaInterna, RolInterno, PermisoInterno, RolInternoPermiso, UsuarioInterno, RegistroActividad |
| `02_EspacioArtistico.sql` | EspacioArtistico |
| `06_Reservas.sql` | Reserva |
| `09_FichaEspacio.sql` | FichaEspacio, FichaEspacioEquipamiento, FranjaEspacio |
| `13_EspacioFoto.sql` | EspacioFoto |
| `26_ActividadesYNotificaciones.sql` | Notificacion, ActividadParticipante, Participante, ActividadDiaSemana, Actividad |

Sirven para armar la base desde cero. Cada uno tiene un encabezado de advertencia y una **protección**: si alguna de las tablas que borraría ya tiene filas, el script se detiene con un error y no ejecuta nada (`SET NOEXEC ON`). En una base nueva esas tablas no existen o están vacías, así que la instalación sigue normalmente.

### Scripts incrementales (migraciones no destructivas)

Todos los demás numerados: agregan columnas, tablas nuevas (con `IF OBJECT_ID(...) IS NULL`), índices, permisos, traducciones o recrean stored procedures. Son seguros sobre una base con datos. Los últimos agregados:

| Script | Qué hace |
|---|---|
| `32_DescontarBloqueosEnBusqueda.sql` | Parche para bases viejas: la búsqueda del catálogo descuenta franjas bloqueadas por actividades (el 15 ya lo trae). |
| `33_LimpiarTraduccionesLoginInterno.sql` | Limpieza para bases viejas de las traducciones de la pantalla de login interno retirada (el 17 ya no las inserta). |
| `39_EncuestasBorradorYEliminacion.sql` | Una encuesta en Borrador no muestra resultados; eliminación controlada de borradores. |
| `40_SoporteReservaAsociada.sql` | Tickets de soporte asociados a una reserva propia (validada) y contador de tickets abiertos. |
| `41_FichaEspacioSinBorrarBloqueosDeActividad.sql` | Guardar la ficha de un espacio ya no borra la ficha ni los bloqueos de actividades. |
| `42_AvisosPorCorreoReservas.sql` | Marca de "aviso de finalización enviado" para el mail de reserva finalizada, y datos de importe en el listado de recordatorios. |
| `43_HistorialYSeguimientoReservas.sql` | El historial de Mis reservas incluye el nombre del gestor del espacio. |
| `44_DatosDemoRanking.sql` | Datos de demostración: reseñas de ejemplo para el ranking de espacios mejor valorados. |
| `45_MenuDinamicoAbmc.sql` | Menú del panel interno administrable (tabla `OpcionMenu`, permiso `GESTIONAR_MENU`), cargado con las opciones que ya existían. |
| `46_BackupRestore.sql` | Backup y restauración desde el panel (permiso `GESTIONAR_BACKUP`). Crea `sp_StageUp_RestaurarBackup` en **master** (una base no se puede restaurar a sí misma). |
| `47_BusquedaGlobal.sql` | Búsqueda de toda la plataforma: pública (`Buscar.aspx`) e interna según permisos (`Interno/BusquedaInterna.aspx`, permiso `BUSCAR_EN_PLATAFORMA`). |
| `48_ReportesYDashboard.sql` | Reportes con gráficos y tablero de indicadores (`Interno/Reportes.aspx`, permiso `VER_REPORTES`): ingresos por día/semana/mes/año, por zona, reservas por estado y participación en encuestas. |
| `49_PagosNotasYCuentaCorriente.sql` | Pagos de reservas (tarjeta con pasarela simulada, saldo a favor o ambos), notas de crédito y débito, cuenta corriente de clientes y gestores y parámetros de la plataforma. Permisos `GESTIONAR_PAGOS` y `CONFIGURAR_PARAMETROS`. Las reservas aceptadas pasan a esperar el pago. |
| `50_PagoSinTitularYMenuInterno.sql` | Correcciones de la revisión: el pago ya no guarda el titular de la tarjeta (se elimina `Pago.titularTarjeta` y se recrean los SP de pagos) y el menú dinámico solo acepta páginas `~/Interno/*.aspx` (nuevo CHECK de `OpcionMenu.url`). |
| `51_RendimientoYBitacora.sql` | Rendimiento (ítem 36): índices para bitácora, reservas con pago pendiente o por finalizar y notificaciones; búsqueda de bitácora con tope de 500 filas y filtro por responsable (interno, externo o sistema); detalle de un registro; reputación de solicitantes y conteo de solicitudes pendientes en una sola consulta. |
| `52_BajaLogicaCuentaExterna.sql` | CU-001-003: baja lógica de la cuenta desde Mi perfil (`sp_UsuarioExterno_BajaLogica`). Valida reservas pendientes o aceptadas (propias y de sus espacios) y saldo en cuenta corriente; si no hay nada pendiente pasa la cuenta a Inactiva, pausa sus espacios publicados y conserva el historial. |
| `53_PoliticaCancelacionYDetalleReserva.sql` | CU-001-005: política de cancelación del documento (14 días o más sin cargo, de 7 a 14 días 50 %, menos de 7 días 100 %), con parámetros nuevos que reemplazan a los de 24 h / 10 %; el cargo de una reserva pagada se reparte entre el gestor y StageUp; SP para la pantalla Detalle de reserva y rechazo que avisa si la solicitud ya fue procesada; texto de Términos y condiciones. |
| `54_SolicitudHabilitacionGestor.sql` | CU-001-007 (A1 a A6): solicitud de habilitación como gestor con formulario (responsable, contacto, datos administrativos y del espacio), estados Pendiente de revisión / Aprobada / Rechazada, motivo de rechazo e historial (`SolicitudHabilitacionGestor`). Migra las cuentas que ya estaban pendientes. |
| `55_FichaEspacioMedidasCondiciones.sql` | CU-001-007: la ficha del espacio suma medidas (superficie y altura), condiciones de uso y reglas de uso; precios de referencia para "Sugerir valores". Los espacios ya cargados completan estos datos al editarlos. |
| `56_DisponibilidadEspacio.sql` | CU-001-008: disponibilidad del espacio de a una franja (alta, modificación y eliminación de franjas manuales), bloqueo manual con motivo, listados con id y motivo; la ficha del espacio ya no reemplaza la disponibilidad al editar. Corrige la búsqueda por fecha: un bloqueo parcial de una fecha ya no cierra todo el día. |
| `57_ActividadesInternasDetalle.sql` | CU-001-009: listado de actividades del gestor con filtro de estado (activas, dadas de baja, todas) y espacio; días y participantes de todas las actividades en una consulta cada uno (sin una consulta por actividad, ítem 36); el detalle de la actividad trae el nombre del espacio. |
| `58_ParticipantesActividades.sql` | CU-001-010: el participante suma correo y teléfono (opcionales) y fechas de modificación y baja; DNI y correo únicos solo entre los participantes activos del gestor; la asociación con una actividad pasa a ser lógica (desvincular conserva el historial); búsqueda de participantes por estado y texto; detección de duplicados. |
| `59_NotificacionesCentro.sql` | CU-001-011: las notificaciones suman título (las existentes lo toman según su tipo); listado por estado (todas, no leídas, leídas) para el centro de notificaciones; obtener una notificación solo si es del usuario (para abrirla y validar su destino). |
| `60_UsuariosInternosYRoles.sql` | CU-001-012: no puede haber dos roles activos con el mismo nombre (índice filtrado); listado de roles con área, estado, cantidad de usuarios activos y de permisos; alta y modificación del rol junto con sus permisos en una sola transacción (rechaza un rol sin permisos o con permisos inexistentes o inactivos). |
| `61_BajaCuentaExternaYNewsletter.sql` | CU-001-003: la baja lógica del usuario externo también deja `activo = 0` (y corrige las cuentas ya dadas de baja); el newsletter nunca incluye cuentas dadas de baja, tampoco con el criterio "Todos". |
| `62_PagosReservasControl.sql` | CU-002-001: el pago guarda lo que informa el medio de pago (identificador, medio, estado informado, fecha, origen y observación) y suma los estados Pendiente, Anulado, Observado y NoAsociado; factura simulada (FC) de cada pago de reserva aprobado (también para los anteriores); listado y detalle de "Pagos de reservas" (incluye reservas esperando el pago y vencidas); registro de avisos del medio de pago; permiso y opción de menú CONTROLAR_PAGOS_RESERVAS. |
| `63_TransaccionesEconomicas.sql` | CU-002-002: tabla TransaccionEconomica (reserva abonada y cargo por cancelación, con comisión de StageUp, importe del gestor, porcentaje de cargo, estado de pago, referencia al medio de pago y estado de registro), carga de las transacciones de pagos y cargos anteriores, registro sin duplicar, listado con filtros, detalle y permiso/menú CONSULTAR_TRANSACCIONES. |
| `64_HistorialFinanciero.sql` | CU-002-003: vista vw_HistorialFinanciero (transacciones económicas, pagos sin confirmar y pagos de deuda, reservas aceptadas sin pagar, liquidaciones a gestores y notas de crédito/débito manuales), listado con filtros por período, estado de pago, tipo de operación, gestor y texto, gestores para el filtro, detalle con comprobantes y movimientos de cuenta corriente, y permiso/menú CONSULTAR_HISTORIAL_FINANCIERO. |
| `65_ParametrosAdministrativos.sql` | CU-002-004: parámetros administrativos agrupados (estados de pagos, facturas y operaciones, datos de facturación de Artera y correos administrativos, además de comisión y política de cancelación), historial de cambios con valor anterior, valor nuevo, responsable y vigencia (ParametroPlataformaCambio), cambios programados que rigen solos desde su fecha, guardado de todos los cambios en una transacción, valores vigentes en una fecha (facturas viejas) y nombre del permiso/menú "Parámetros administrativos". |
| `66_BilleteraYClavesCifrado.sql` | Ítems 8, 9 y 10 del profesor: tabla ClaveCifrado (pares RSA versionados, clave privada guardada cifrada con la clave simétrica, huella, estado Activa/Retirada, responsable y motivo) y TarjetaGuardada (billetera: alias, marca, últimos 4, vencimiento, preferida, huella HMAC del número y número + titular cifrados con AES-256 por tarjeta envuelta con RSA; el código de seguridad no se guarda), SP de alta/listado/alias/preferida/baja/uso, rotación (reenvolver claves de las tarjetas) y descripción del permiso GESTIONAR_ENCRIPTACION. |
| `67_ContenidoAyudaYAsistente.sql` | CU-003-004 y CU-003-001: tabla CategoriaAyuda (10 categorías temáticas), Faq con categoría, estado Borrador/Activo/Inactivo (activo queda sincronizado), uso en el asistente y palabras clave; contenido inicial por categoría; tabla ConsultaAsistente (consultas escritas al asistente, para detectar las que no tuvieron respuesta); SP de listado con filtros, alta, modificación, cambio de estado, categorías y consultas sin respuesta; permiso/menú "Contenido de ayuda". |
| `68_SolicitudesSoporte.sql` | CU-003-002, CU-003-003 e ítem 12 del profesor: solicitudes de soporte con motivo, funcionalidad vinculada (reserva, pago, espacio, cuenta, gestor u otra), descripción, información adicional y origen (soporte, centro de ayuda, asistente o reserva); estados Pendiente, En proceso, Resuelta y Cerrada (convierte los anteriores); tabla TicketActualizacion con el historial (alta, cambios de estado con responsable, fecha, estado anterior y nuevo, asignaciones); marcas de lectura de cada lado para leído / no leído; vista vw_TicketSoporte; SP de registro, listado interno con filtros y paginado, resumen, respuesta (con "marcar como resuelta"), cambio de estado, asignación, mensajes del usuario, marcar leído y marcas para la actualización periódica; menú "Solicitudes de soporte" y permiso REABRIR_SOLICITUD_SOPORTE. |
| `69_EstadosSoporteEnReportesYBaja.sql` | Recrea `sp_Reporte_Indicadores` y `sp_UsuarioExterno_BajaLogica` con los estados nuevos de las solicitudes de soporte (Pendiente y En proceso cuentan como abiertas), para las bases que ya tenían aplicados los scripts 49 y 61. |
| `70_AsistenteGuiado.sql` | CU-003-001 (corrección de María): el asistente pasa a ser un árbol de decisión con contenido propio, separado de las preguntas frecuentes. Tablas NodoAsistente (tema → subtema → pregunta → respuesta, con palabras clave para consultas escritas) y AccionNodoAsistente (ir a una pantalla, ver otra respuesta o registrar una solicitud de soporte), con el contenido inicial (reservas, pagos y cuenta corriente, espacios y gestores, soporte técnico, no encuentro lo que necesito). Las preguntas frecuentes dejan de usarse en el asistente salvo que se marquen; ConsultaAsistente registra el paso que respondió. |
| `71_PreguntasEspacio.sql` | Ítem 1 del profesor: preguntas y respuestas públicas sobre los espacios. Tabla PreguntaEspacio (pregunta, autor, respuesta del gestor, estados Pendiente, Respondida, Oculta y Eliminada, motivo y fecha de baja), vista vw_PreguntaEspacio y SP de alta (solo espacios publicados y que no sean del propio usuario), listado público y conteo, pendientes de un usuario, listado y conteo para el gestor, respuesta (solo el gestor dueño y si sigue pendiente) y baja lógica. |
| `72_AsistenteEstiloChat.sql` | CU-003-001 (video de referencia de María): el asistente se muestra como un chat con menú de opciones. NodoAsistente suma descripción corta e ícono de cada opción y el texto del botón que abre el menú de cada paso ("Menú principal", "Opciones de reservas"...), con el contenido inicial; el inicio pasa a preguntar "¿Qué te gustaría hacer hoy?" (el saludo con el nombre lo arma el asistente). |
| `73_MensajesReserva.sql` | Mensajería privada por reserva entre el solicitante y el gestor del espacio. Tabla MensajeReserva (emisor, texto, fecha de envío y fecha de lectura) y SP de alta (solo participantes y con la reserva no rechazada ni cancelada), conversación de una reserva, marcar como leídos y mensajes sin leer por reserva y por papel (solicitante o gestor). |

Desde el script 45, el menú del panel interno sale de `dbo.OpcionMenu`: si un script futuro agrega un permiso con pantalla propia, tiene que insertar también su opción de menú (o darla de alta desde Gestión del menú).

Cuando se corrige un script ya publicado (por ejemplo 15, 36 o 38), la corrección se hace **en el script original** (para que una instalación nueva quede bien) **y en un script incremental nuevo** (para que una base que ya tenía aplicado el original también la reciba). Ejecutar ambos deja el mismo resultado.

### Datos de demostración

`05_DatosDePrueba.sql`, `25_DatosDemoCatalogoEspacios.sql`, `31_ReservaDemoFinalizada.sql` y `44_DatosDemoRanking.sql` cargan datos de ejemplo para la demo.

## Scripts que NO forman parte del flujo normal

- **`Eliminacion_creacion_bd.sql`**: recuperación manual de una base rota. Borra todas las tablas y recrea la base. No es un script de instalación y no se entrega como parte del flujo normal. Tiene rutas físicas de `.mdf`/`.ldf` que hay que ajustar a cada instancia local, y arranca frenado (`SET NOEXEC ON`) para que no se ejecute entero por accidente. `EjecutarTodosLosScripts.ps1` nunca lo toma porque su nombre no empieza con número.
- **`RepararTildes.ps1`**: utilitario puntual para reparar datos cargados con una codificación incorrecta en bases viejas.
- **`Volumen_CargarDatos.sql`** y **`Volumen_LimpiarDatos.sql`**: carga y borrado de datos de volumen para medir el rendimiento (ítem 36). Ver la sección siguiente.

## Pruebas de rendimiento con volumen (ítem 36)

`Volumen_CargarDatos.sql` agrega a la base 200 gestores, 3.000 clientes, 1.000 espacios, 60.000 reservas (con pagos, movimientos de cuenta corriente y calificaciones), 1.000 actividades, 3.000 tickets, 150.000 notificaciones y 300.000 registros de bitácora. Las cantidades se cambian en las variables del principio del script. Todo queda marcado (correos `volumen.*@stageup.test`, espacios `[Volumen] ...`, bitácora con origen `CargaVolumen`) y `Volumen_LimpiarDatos.sql` lo borra sin tocar los datos reales.

Pasos:

1. Hacer un backup de la base (Interno > Backup y restauración, o SSMS).
2. Tener aplicados todos los scripts numerados (incluido el 51): `.\EjecutarTodosLosScripts.ps1`.
3. Cargar el volumen (tarda entre uno y tres minutos):
   ```powershell
   cd Database
   sqlcmd -S .\SQLEXPRESS -d StageUp -E -f 65001 -b -i Volumen_CargarDatos.sql
   ```
4. Para medir como en producción, en `StageUp.UI/Web.config` poner `<compilation debug="false" ...>` (volver a `true` al terminar) y compilar en **Release**.
5. Medir las pantallas con más datos: catálogo y búsqueda (`Explorar/ResultadosBusqueda.aspx`), Mis reservas y Solicitudes recibidas con un usuario de volumen (por ejemplo `volumen.cliente1@stageup.test` o `volumen.gestor1@stageup.test`, con la misma contraseña que los usuarios demo), Mi cuenta corriente, Interno > Registros de actividad (sin filtros y filtrando por responsable), Interno > Reportes y Interno > Gestión de soporte. El tiempo de cada página se ve en las herramientas de desarrollo del navegador (F12 > Red > columna Tiempo del documento).
6. Para ver el costo de una consulta puntual en SSMS:
   ```sql
   SET STATISTICS IO ON;
   SET STATISTICS TIME ON;
   EXEC dbo.sp_RegistroActividad_Buscar @maximo = 500;
   ```
   La pestaña Mensajes muestra lecturas lógicas y milisegundos; "Incluir plan de ejecución real" (Ctrl+M) muestra qué índices usa.
7. Al terminar, borrar el volumen:
   ```powershell
   sqlcmd -S .\SQLEXPRESS -d StageUp -E -f 65001 -b -i Volumen_LimpiarDatos.sql
   ```

## Backup y restauración

Desde **Interno > Backup y restauración** (rol Administrador) se genera un backup completo de la base en la carpeta de backups de la instancia de SQL Server, y se puede restaurar desde cualquier backup del historial. Para restaurar hay que escribir RESTAURAR; antes de restaurar se genera automáticamente un backup del estado actual, y todo queda en la bitácora. El usuario de Windows con el que corre la aplicación necesita permisos de backup/restore en la instancia (en desarrollo local normalmente es sysadmin).

Alternativa manual (SSMS): clic derecho sobre la base > Tareas > Copia de seguridad / Restaurar.

## Configuración privada

Los secretos (SMTP, reCAPTCHA, claves de encriptación) viven en `StageUp.UI/AppSettings.private.config`, que está en `.gitignore` y no se versiona. Para armarlo, copiar `StageUp.UI/AppSettings.private.config.example` y completar los valores.
