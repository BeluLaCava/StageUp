# StageUp

StageUp es una plataforma web de intermediación para espacios artísticos.

## Entorno tecnológico

- Windows
- Visual Studio 2022
- ASP.NET Web Forms sobre .NET Framework
- C#
- SQL Server local
- HTML
- CSS
- JavaScript
- SQL

XML debe utilizarse solamente cuando sea necesario por la tecnología, por ejemplo en Web.config.

No utilizar ASP.NET Core, MVC, Razor Pages, Blazor, React, Angular, Node.js u otros frameworks salvo autorización expresa.

No instalar paquetes NuGet ni dependencias externas sin autorización.

## Arquitectura obligatoria

UI -> BLL -> MPP -> DAL -> SQL Server

También existirán:

- BE
- Seguridad
- Servicios

### UI

Responsable de la interfaz Web Forms.

No debe acceder directamente a SQL Server.
No debe ejecutar consultas SQL.
No debe contener reglas de negocio que correspondan a BLL.

### BE

Contiene las entidades de negocio compartidas entre las capas.

### BLL

Contiene reglas, validaciones y lógica de negocio.

### MPP

Responsable del mapeo entre entidades de negocio y persistencia.

### DAL

Responsable exclusivamente del acceso a datos y comunicación con SQL Server.

No debe contener reglas de negocio.

### Seguridad

Responsable de autenticación, autorización, permisos, sesiones y manejo seguro de credenciales.

### Servicios

Responsable de correo electrónico, notificaciones e integraciones externas.

## Forma de trabajo

- Trabajar de manera incremental.
- No desarrollar funcionalidades que no hayan sido solicitadas.
- No adelantarse a futuras tareas.
- No inventar requisitos.
- No cambiar la arquitectura sin autorización.
- Antes de realizar cambios importantes, indicar qué archivos se modificarán.
- Si existe una decisión técnica no definida, preguntar antes de asumirla.
- Priorizar código claro y mantenible.
- Mantener compatibilidad con Visual Studio 2022 y .NET Framework.
- No crear código innecesario.
- Realizar cambios pequeños y verificables.

## Documentación

Existe documentación funcional y técnica de StageUp con requisitos, casos de uso, reglas de negocio, arquitectura, diagramas, modelo de datos, diccionario de datos y casos de prueba.

Esa documentación será considerada la fuente de verdad del proyecto.

También puede existir un proyecto anterior utilizado únicamente como referencia de implementación.

Si existe una diferencia entre el proyecto anterior y la documentación actual de StageUp, prevalece siempre la documentación actual de StageUp.
