-- =====================================================================
-- 03_BusquedaBitacora.sql
--
-- Agrega la consulta de la bitácora (CU-001-013 Consultar registros de
-- actividad) sobre la tabla RegistroActividad ya creada en
-- 01_EsquemaSeguridadYRegistro.sql. No crea tablas nuevas.
--
-- Nota de alcance: el CU completo define como actor a un "usuario interno
-- autorizado" con permisos específicos. Como la autenticación y los
-- permisos de usuarios internos (RolInterno/PermisoInterno/UsuarioInterno)
-- todavía no están implementados en la aplicación (son parte de una etapa
-- posterior), esta consulta se expone por ahora a cualquier usuario
-- externo autenticado, igual que se hizo con "Mis espacios" en el ABMC de
-- EspacioArtistico. Cuando se implemente el login/permisos de usuarios
-- internos, hay que migrar el control de acceso de esta pantalla a
-- validar el permiso correspondiente en vez de solo la sesión.

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
