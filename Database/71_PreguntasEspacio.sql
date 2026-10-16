IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO


IF OBJECT_ID(N'dbo.PreguntaEspacio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PreguntaEspacio
    (
        idPreguntaEspacio         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreguntaEspacio PRIMARY KEY,
        idEspacioArtistico        INT            NOT NULL,
        idUsuarioExterno          INT            NOT NULL,
        pregunta                  NVARCHAR(500)  NOT NULL,
        fechaPregunta             DATETIME       NOT NULL CONSTRAINT DF_PreguntaEspacio_fechaPregunta DEFAULT (GETDATE()),
        respuesta                 NVARCHAR(1000) NULL,
        fechaRespuesta            DATETIME       NULL,
        idUsuarioGestorRespuesta  INT            NULL,
        estado                    NVARCHAR(12)   NOT NULL CONSTRAINT DF_PreguntaEspacio_estado DEFAULT (N'Pendiente'),
        motivoOcultamiento        NVARCHAR(300)  NULL,
        fechaBaja                 DATETIME       NULL,
        idUsuarioBaja             INT            NULL,
        CONSTRAINT FK_PreguntaEspacio_Espacio FOREIGN KEY (idEspacioArtistico) REFERENCES dbo.EspacioArtistico (idEspacioArtistico),
        CONSTRAINT FK_PreguntaEspacio_Autor FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_PreguntaEspacio_Gestor FOREIGN KEY (idUsuarioGestorRespuesta) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_PreguntaEspacio_UsuarioBaja FOREIGN KEY (idUsuarioBaja) REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT CK_PreguntaEspacio_estado CHECK (estado IN (N'Pendiente', N'Respondida', N'Oculta', N'Eliminada'))
    );

    CREATE INDEX IX_PreguntaEspacio_Espacio ON dbo.PreguntaEspacio (idEspacioArtistico, estado, fechaPregunta DESC);
    CREATE INDEX IX_PreguntaEspacio_Autor ON dbo.PreguntaEspacio (idUsuarioExterno, estado);
END
GO

-- Columnas comunes de los listados.
IF OBJECT_ID(N'dbo.vw_PreguntaEspacio', N'V') IS NOT NULL DROP VIEW dbo.vw_PreguntaEspacio;
GO
CREATE VIEW dbo.vw_PreguntaEspacio
AS
SELECT
    p.idPreguntaEspacio, p.idEspacioArtistico, p.idUsuarioExterno, p.pregunta, p.fechaPregunta,
    p.respuesta, p.fechaRespuesta, p.idUsuarioGestorRespuesta, p.estado, p.motivoOcultamiento, p.fechaBaja,
    e.nombreEspacio, e.idUsuarioGestor AS idUsuarioGestorEspacio,
    u.nombre AS nombreAutor, u.apellido AS apellidoAutor
FROM dbo.PreguntaEspacio p
INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = p.idEspacioArtistico
INNER JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = p.idUsuarioExterno;
GO

-- Alta. Segunda barrera (la primera es BLL_PreguntaEspacio): el espacio
-- tiene que estar publicado y activo, y el gestor no pregunta en el suyo.
IF OBJECT_ID('dbo.sp_PreguntaEspacio_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_Insertar;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_Insertar
    @idEspacioArtistico  INT,
    @idUsuarioExterno    INT,
    @pregunta            NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.EspacioArtistico
                   WHERE idEspacioArtistico = @idEspacioArtistico AND publicado = 1 AND activo = 1
                     AND idUsuarioGestor <> @idUsuarioExterno)
        THROW 51040, 'El espacio no admite preguntas de este usuario.', 1;

    INSERT INTO dbo.PreguntaEspacio (idEspacioArtistico, idUsuarioExterno, pregunta, fechaPregunta, estado)
    VALUES (@idEspacioArtistico, @idUsuarioExterno, @pregunta, GETDATE(), N'Pendiente');

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idPreguntaEspacio;
END
GO

IF OBJECT_ID('dbo.sp_PreguntaEspacio_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_ObtenerPorId
    @idPreguntaEspacio INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_PreguntaEspacio WHERE idPreguntaEspacio = @idPreguntaEspacio;
END
GO

-- Detalle público del espacio: pendientes y respondidas, las más nuevas primero.
IF OBJECT_ID('dbo.sp_PreguntaEspacio_ListarPublicas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_ListarPublicas;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_ListarPublicas
    @idEspacioArtistico  INT,
    @cantidad            INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (ISNULL(@cantidad, 1000)) *
    FROM dbo.vw_PreguntaEspacio
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND estado IN (N'Pendiente', N'Respondida')
    ORDER BY fechaPregunta DESC, idPreguntaEspacio DESC;
END
GO

IF OBJECT_ID('dbo.sp_PreguntaEspacio_ContarPublicas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_ContarPublicas;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_ContarPublicas
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad
    FROM dbo.PreguntaEspacio
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND estado IN (N'Pendiente', N'Respondida');
END
GO

-- Preguntas sin responder de un usuario en un espacio (límite anti-abuso).
IF OBJECT_ID('dbo.sp_PreguntaEspacio_ListarPendientesDeUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_ListarPendientesDeUsuario;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_ListarPendientesDeUsuario
    @idEspacioArtistico  INT,
    @idUsuarioExterno    INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.vw_PreguntaEspacio
    WHERE idEspacioArtistico = @idEspacioArtistico
      AND idUsuarioExterno = @idUsuarioExterno
      AND estado = N'Pendiente';
END
GO

-- Pantalla del gestor: preguntas de sus espacios (no se muestran las que
-- retiró quien preguntó).
IF OBJECT_ID('dbo.sp_PreguntaEspacio_ListarPorGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_ListarPorGestor;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_ListarPorGestor
    @idUsuarioGestor     INT,
    @estado              NVARCHAR(12) = NULL,
    @idEspacioArtistico  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.vw_PreguntaEspacio
    WHERE idUsuarioGestorEspacio = @idUsuarioGestor
      AND estado <> N'Eliminada'
      AND (@estado IS NULL OR estado = @estado)
      AND (@idEspacioArtistico IS NULL OR idEspacioArtistico = @idEspacioArtistico)
    ORDER BY CASE WHEN estado = N'Pendiente' THEN 0 ELSE 1 END, fechaPregunta DESC, idPreguntaEspacio DESC;
END
GO

IF OBJECT_ID('dbo.sp_PreguntaEspacio_ContarPendientesGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_ContarPendientesGestor;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_ContarPendientesGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS cantidad
    FROM dbo.PreguntaEspacio p
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = p.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND p.estado = N'Pendiente';
END
GO

-- Respuesta: solo el gestor dueño del espacio y solo si sigue pendiente.
IF OBJECT_ID('dbo.sp_PreguntaEspacio_Responder', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_Responder;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_Responder
    @idPreguntaEspacio  INT,
    @idUsuarioGestor    INT,
    @respuesta          NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE p
    SET p.respuesta = @respuesta,
        p.fechaRespuesta = GETDATE(),
        p.idUsuarioGestorRespuesta = @idUsuarioGestor,
        p.estado = N'Respondida'
    FROM dbo.PreguntaEspacio p
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = p.idEspacioArtistico
    WHERE p.idPreguntaEspacio = @idPreguntaEspacio
      AND p.estado = N'Pendiente'
      AND e.idUsuarioGestor = @idUsuarioGestor;

    SELECT @@ROWCOUNT AS filas;
END
GO

-- Baja lógica: "Oculta" (moderación del gestor, con motivo) o "Eliminada"
-- (quien preguntó la retira). Se cambia solo si el estado sigue siendo el
-- que vio el usuario.
IF OBJECT_ID('dbo.sp_PreguntaEspacio_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PreguntaEspacio_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_PreguntaEspacio_DarDeBaja
    @idPreguntaEspacio  INT,
    @estadoAnterior     NVARCHAR(12),
    @estadoNuevo        NVARCHAR(12),
    @idUsuarioBaja      INT,
    @motivo             NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PreguntaEspacio
    SET estado = @estadoNuevo,
        motivoOcultamiento = @motivo,
        fechaBaja = GETDATE(),
        idUsuarioBaja = @idUsuarioBaja
    WHERE idPreguntaEspacio = @idPreguntaEspacio
      AND estado = @estadoAnterior
      AND @estadoNuevo IN (N'Oculta', N'Eliminada');

    SELECT @@ROWCOUNT AS filas;
END
GO
