<p align="center">
  <img src="StageUp.UI/Content/Images/stageup-logo.png" alt="StageUp" width="320">
</p>

# StageUp

StageUp es una plataforma web para publicar, buscar y reservar espacios destinados a actividades artísticas, culturales y de producción. Conecta a personas que necesitan un lugar con gestores que administran salas, estudios, teatros y otros espacios, y centraliza el proceso completo desde la búsqueda hasta el pago y la calificación.

El proyecto fue desarrollado con ASP.NET Web Forms y .NET Framework 4.8, siguiendo una arquitectura en capas y utilizando SQL Server para la persistencia.

## Funcionalidades principales

- Registro, activación, recuperación y administración de cuentas.
- Perfiles de solicitante, gestor de espacios y usuarios internos.
- Publicación de espacios con ficha, fotografías, equipamiento, condiciones, precios y disponibilidad.
- Búsqueda simple y avanzada de espacios.
- Solicitudes de reserva, aceptación, seguimiento y cancelación.
- Mensajería privada entre solicitante y gestor.
- Preguntas y respuestas públicas sobre cada espacio.
- Pagos simulados, comprobantes, saldo a favor y cuentas corrientes.
- Billetera de tarjetas con información sensible cifrada.
- Calificaciones y reputación de espacios, gestores y solicitantes.
- Novedades, newsletter por categorías y encuestas públicas.
- Centro de ayuda, asistente de soporte y helpdesk con tickets.
- Notificaciones y registro de actividad.
- Panel interno con roles y permisos configurables.
- Gestión comercial de campañas, contactos, alianzas y contenido promocional.
- Reportes, métricas, parámetros administrativos, backup y restauración.
- Interfaz multidioma.

## Arquitectura

La solución respeta el siguiente flujo:

```text
StageUp.UI → StageUp.BLL → StageUp.MPP → StageUp.DAL → SQL Server
```

- `StageUp.UI`: interfaz ASP.NET Web Forms.
- `StageUp.BE`: entidades y enumeraciones del dominio.
- `StageUp.BLL`: reglas, validaciones y procesos de negocio.
- `StageUp.MPP`: mapeo entre entidades y persistencia.
- `StageUp.DAL`: conexión y ejecución de operaciones en SQL Server.
- `StageUp.Seguridad`: autenticación, autorización, cifrado y protección de credenciales.
- `StageUp.Servicios`: correo electrónico, reCAPTCHA, pasarela de pago simulada e integraciones externas.

La UI no accede directamente a SQL Server. Las consultas y procedimientos se ejecutan exclusivamente a través de las capas BLL, MPP y DAL.

## Tecnologías

- Visual Studio 2022.
- .NET Framework 4.8.
- ASP.NET Web Forms.
- C#.
- ADO.NET y SQL Server.
- HTML, CSS y JavaScript.
- Windows PowerShell y `sqlcmd` para las migraciones.
- Google reCAPTCHA v2.
- SMTP para correos transaccionales y newsletter.

## Estructura del repositorio

```text
StageUp/
├── Database/             Scripts, migraciones y datos de demostración
├── docs/                 Documentación funcional y técnica de referencia
├── StageUp.BE/           Entidades de negocio
├── StageUp.BLL/          Lógica de negocio
├── StageUp.DAL/          Acceso a datos
├── StageUp.MPP/          Mapeadores de persistencia
├── StageUp.Seguridad/    Seguridad y cifrado
├── StageUp.Servicios/    Servicios e integraciones
├── StageUp.UI/           Aplicación Web Forms
└── StageUp.sln           Solución de Visual Studio
```

## Requisitos

- Windows 10 u 11.
- Visual Studio 2022 con la carga de trabajo **Desarrollo de ASP.NET y web**.
- .NET Framework 4.8 Developer Pack.
- SQL Server o SQL Server Express.
- SQL Server Management Studio, recomendado para administrar la base.
- `sqlcmd` disponible desde PowerShell.
- Windows PowerShell 5.1 para los cargadores de datos de demostración.

## Instalación y ejecución

### 1. Obtener el repositorio

```powershell
git clone https://github.com/BeluLaCava/StageUp.git
cd StageUp
```

Si el repositorio ya está descargado, actualizar la rama correspondiente antes de continuar.

### 2. Crear o actualizar la base de datos

Desde PowerShell:

```powershell
cd Database
.\EjecutarTodosLosScripts.ps1
```

El comando anterior utiliza de manera predeterminada la instancia `.\SQLEXPRESS` y la base `StageUp`. Para indicar otra instancia:

```powershell
.\EjecutarTodosLosScripts.ps1 -ServerInstance "EQUIPO\SQLEXPRESS01"
```

El ejecutor crea la base si no existe y registra cada migración en `dbo._ScriptsEjecutados`. Puede volver a ejecutarse después de un `git pull`: solamente aplicará los scripts pendientes y respetará su orden.

No se deben ejecutar manualmente los scripts numerados sobre una base que ya contiene información. Las instrucciones detalladas y las alternativas para bases existentes están en [Database/LEEME_Scripts.md](Database/LEEME_Scripts.md).

### 3. Configurar la conexión

Editar `StageUp.UI/Web.config` para que `StageUpConnectionString` apunte a la instancia local. Ejemplo para SQL Server Express:

```xml
<connectionStrings>
  <add name="StageUpConnectionString"
       connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=StageUp;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### 4. Crear la configuración privada

Copiar el archivo de ejemplo:

```powershell
Copy-Item StageUp.UI/AppSettings.private.config.example StageUp.UI/AppSettings.private.config
```

Completar en el nuevo archivo únicamente los valores necesarios para el entorno local:

- Credenciales SMTP y remitente.
- Claves de reCAPTCHA.
- Clave simétrica AES.
- Claves asimétricas RSA.
- Configuración opcional del asistente externo.

`AppSettings.private.config` está excluido por `.gitignore`. No se deben publicar contraseñas, claves privadas ni credenciales reales.

Si la base ya contiene información cifrada, debe utilizarse la misma clave AES con la que se generaron esos datos. Después de modificar la configuración privada hay que detener la aplicación y volver a iniciarla.

### 5. Compilar y ejecutar

1. Abrir `StageUp.sln` en Visual Studio 2022.
2. Seleccionar **Compilar → Recompilar solución**.
3. Establecer `StageUp.UI` como proyecto de inicio si fuera necesario.
4. Ejecutar con IIS Express.

## Datos de demostración

Las migraciones incluyen datos preparados para recorrer los módulos principales. Después de compilar la solución en configuración Debug se pueden completar las tarjetas y los usuarios de demostración mediante:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Database\87_CargarTarjetasDemo.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Database\87_PrepararUsuariosDemo.ps1
```

Ambos cargadores son idempotentes: pueden volver a ejecutarse sin duplicar los registros que administran. Utilizan la misma lógica de negocio y seguridad que la aplicación.

## Seguridad

- Las contraseñas se almacenan mediante hash seguro.
- Los datos sensibles se cifran antes de persistirse.
- La billetera utiliza cifrado híbrido AES y RSA.
- El código de seguridad de una tarjeta nunca se guarda.
- Los permisos internos se validan tanto en la navegación como al acceder directamente a una página.
- Las operaciones relevantes quedan registradas en la bitácora.
- Las credenciales privadas permanecen fuera del repositorio.

## Documentación adicional

- [Guía de scripts y migraciones](Database/LEEME_Scripts.md).
- Documentación funcional y técnica disponible en [`docs/referencia`](docs/referencia).

## Estado del proyecto

StageUp es un proyecto académico en desarrollo. El repositorio contiene la solución completa, las migraciones necesarias para reproducir la base y datos de demostración para verificar sus principales circuitos de negocio.
