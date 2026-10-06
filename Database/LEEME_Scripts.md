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
