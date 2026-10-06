IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Actividad', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/26_ActividadesYNotificaciones.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Actividad_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Actividad_ObtenerPorId
    @idActividad INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.idActividad, a.idEspacioArtistico, a.nombre, a.tipo, a.modoRecurrencia, a.fecha, a.semanaDelMes,
           a.diaSemanaMensual, a.minutoDesde, a.minutoHasta, a.cupoMaximo, a.participantesEstimados, a.notas,
           a.activa, a.fechaCreacion, a.fechaUltimaModificacion,
           e.nombreEspacio, e.activo AS espacioActivo
    FROM dbo.Actividad a
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE a.idActividad = @idActividad;
END
GO

IF OBJECT_ID('dbo.sp_Actividad_ListarPorUsuarioGestorV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Actividad_ListarPorUsuarioGestorV2;
GO
CREATE PROCEDURE dbo.sp_Actividad_ListarPorUsuarioGestorV2
    @idUsuarioGestor    INT,
    @estado             NVARCHAR(20) = N'Activas',   -- Activas | Inactivas | Todas
    @idEspacioArtistico INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.idActividad, a.idEspacioArtistico, a.nombre, a.tipo, a.modoRecurrencia, a.fecha, a.semanaDelMes,
           a.diaSemanaMensual, a.minutoDesde, a.minutoHasta, a.cupoMaximo, a.participantesEstimados, a.notas,
           a.activa, a.fechaCreacion, a.fechaUltimaModificacion, e.nombreEspacio, e.activo AS espacioActivo
    FROM dbo.Actividad a
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1
      AND (@idEspacioArtistico IS NULL OR a.idEspacioArtistico = @idEspacioArtistico)
      AND (@estado = N'Todas'
           OR (@estado = N'Activas' AND a.activa = 1)
           OR (@estado = N'Inactivas' AND a.activa = 0))
    ORDER BY a.activa DESC, e.nombreEspacio, a.nombre;
END
GO

IF OBJECT_ID('dbo.sp_ActividadDiaSemana_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadDiaSemana_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_ActividadDiaSemana_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.idActividad, d.diaSemana
    FROM dbo.ActividadDiaSemana d
    INNER JOIN dbo.Actividad a ON a.idActividad = d.idActividad
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor;
END
GO

IF OBJECT_ID('dbo.sp_ActividadParticipante_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ActividadParticipante_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_ActividadParticipante_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ap.idActividad, p.idParticipante, p.nombre, p.apellido, p.dni, p.notas, p.activo, ap.fechaAsociacion
    FROM dbo.ActividadParticipante ap
    INNER JOIN dbo.Participante p ON p.idParticipante = ap.idParticipante
    INNER JOIN dbo.Actividad a ON a.idActividad = ap.idActividad
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = a.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND p.activo = 1;
END
GO
