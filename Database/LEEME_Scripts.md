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

Desde el script 45, el menú del panel interno sale de `dbo.OpcionMenu`: si un script futuro agrega un permiso con pantalla propia, tiene que insertar también su opción de menú (o darla de alta desde Gestión del menú).

Cuando se corrige un script ya publicado (por ejemplo 15, 36 o 38), la corrección se hace **en el script original** (para que una instalación nueva quede bien) **y en un script incremental nuevo** (para que una base que ya tenía aplicado el original también la reciba). Ejecutar ambos deja el mismo resultado.

### Datos de demostración

`05_DatosDePrueba.sql`, `25_DatosDemoCatalogoEspacios.sql`, `31_ReservaDemoFinalizada.sql` y `44_DatosDemoRanking.sql` cargan datos de ejemplo para la demo.

## Scripts que NO forman parte del flujo normal

- **`Eliminacion_creacion_bd.sql`**: recuperación manual de una base rota. Borra todas las tablas y recrea la base. No es un script de instalación y no se entrega como parte del flujo normal. Tiene rutas físicas de `.mdf`/`.ldf` que hay que ajustar a cada instancia local, y arranca frenado (`SET NOEXEC ON`) para que no se ejecute entero por accidente. `EjecutarTodosLosScripts.ps1` nunca lo toma porque su nombre no empieza con número.
- **`RepararTildes.ps1`**: utilitario puntual para reparar datos cargados con una codificación incorrecta en bases viejas.

## Backup y restauración

Desde **Interno > Backup y restauración** (rol Administrador) se genera un backup completo de la base en la carpeta de backups de la instancia de SQL Server, y se puede restaurar desde cualquier backup del historial. Para restaurar hay que escribir RESTAURAR; antes de restaurar se genera automáticamente un backup del estado actual, y todo queda en la bitácora. El usuario de Windows con el que corre la aplicación necesita permisos de backup/restore en la instancia (en desarrollo local normalmente es sysadmin).

Alternativa manual (SSMS): clic derecho sobre la base > Tareas > Copia de seguridad / Restaurar.

## Configuración privada

Los secretos (SMTP, reCAPTCHA, claves de encriptación) viven en `StageUp.UI/AppSettings.private.config`, que está en `.gitignore` y no se versiona. Para armarlo, copiar `StageUp.UI/AppSettings.private.config.example` y completar los valores.
