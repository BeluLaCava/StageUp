IF OBJECT_ID('dbo.Faq', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Faq
    (
        idFaq           INT IDENTITY(1,1)  NOT NULL PRIMARY KEY,
        pregunta        NVARCHAR(300)       NOT NULL,
        respuesta       NVARCHAR(MAX)       NOT NULL,
        orden           INT                 NOT NULL,
        activo          BIT                 NOT NULL DEFAULT 1
    );
END
GO

IF OBJECT_ID('dbo.sp_Faq_ListarActivas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_ListarActivas;
GO
CREATE PROCEDURE dbo.sp_Faq_ListarActivas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idFaq, pregunta, respuesta, orden, activo
    FROM dbo.Faq
    WHERE activo = 1
    ORDER BY orden ASC;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Faq)
BEGIN
    INSERT INTO dbo.Faq (pregunta, respuesta, orden, activo) VALUES
    (N'¿Qué es StageUp?', N'StageUp es una plataforma de intermediación digital que conecta a personas que necesitan espacios para actividades artísticas con gestores que desean ofrecerlos temporalmente.', 1, 1),
    (N'¿Cómo puedo buscar un espacio?', N'La exploración pública permite buscar por palabras clave y combinar filtros generales con características artísticas del espacio.', 2, 1),
    (N'¿Cómo funcionan las reservas?', N'Un usuario autenticado podrá seleccionar un día y una franja disponible para enviar una solicitud. El gestor correspondiente podrá aceptarla o rechazarla.', 3, 1),
    (N'¿Qué significa ser gestor?', N'Es un usuario habilitado para publicar y administrar espacios artísticos, configurar su disponibilidad y gestionar las solicitudes recibidas.', 4, 1),
    (N'¿Para qué sirve una cuenta?', N'La cuenta permitirá acceder a funciones que requieren identificación, como solicitar reservas, gestionar operaciones y registrar solicitudes de soporte.', 5, 1);
END
GO


SELECT session_id, blocking_session_id, wait_type, wait_time, status, command
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;