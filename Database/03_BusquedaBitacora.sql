IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID('dbo.sp_RegistroActividad_Buscar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistroActividad_Buscar;
GO
CREATE PROCEDURE dbo.sp_RegistroActividad_Buscar
    @idUsuarioExternoResponsable    INT             = NULL,
    @fechaDesde                     DATETIME        = NULL,
    @fechaHasta                     DATETIME        = NULL,
    @tipoOperacion                  NVARCHAR(200)   = NULL,
    @tipoEntidadAfectada            NVARCHAR(200)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.idRegistroActividad,
        r.idUsuarioExternoResponsable,
        r.idUsuarioInternoResponsable,
        r.tipoOperacion,
        r.tipoEntidadAfectada,
        r.idEntidadAfectada,
        r.descripcionOperacion,
        r.fechaOperacion,
        r.origenOperacion,
        ISNULL(ue.nombre + ' ' + ue.apellido, ui.nombre + ' ' + ui.apellido) AS nombreResponsable,
        ISNULL(ue.correoElectronico, ui.correoElectronico) AS correoResponsable
    FROM dbo.RegistroActividad r
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = r.idUsuarioExternoResponsable
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = r.idUsuarioInternoResponsable
    WHERE (@idUsuarioExternoResponsable IS NULL OR r.idUsuarioExternoResponsable = @idUsuarioExternoResponsable)
      AND (@fechaDesde IS NULL OR r.fechaOperacion >= @fechaDesde)
      AND (@fechaHasta IS NULL OR r.fechaOperacion < DATEADD(DAY, 1, @fechaHasta))
      AND (@tipoOperacion IS NULL OR r.tipoOperacion = @tipoOperacion)
      AND (@tipoEntidadAfectada IS NULL OR r.tipoEntidadAfectada = @tipoEntidadAfectada)
    ORDER BY r.fechaOperacion DESC;
END
GO
