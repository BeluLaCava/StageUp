SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CampaniaComercial_DarDeBaja
    @idCampaniaComercial INT,
    @idUsuarioInterno INT,
    @descripcionBitacora NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @filas INT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.CampaniaComercial
        SET activa = 0,
            fechaBaja = GETDATE(),
            fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial
          AND activa = 1;

        SET @filas = @@ROWCOUNT;

        IF @filas > 0
            EXEC dbo.sp_RegistroActividad_Insertar
                @idUsuarioInternoResponsable = @idUsuarioInterno,
                @tipoOperacion = N'BAJA',
                @tipoEntidadAfectada = N'CampaniaComercial',
                @idEntidadAfectada = @idCampaniaComercial,
                @descripcionOperacion = @descripcionBitacora,
                @origenOperacion = N'StageUp.UI';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @filas;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SeguimientoAlianza_Insertar
    @idAlianzaEstrategica INT,
    @fechaInteraccion DATE,
    @tipoInteraccion NVARCHAR(40),
    @observacion NVARCHAR(2000),
    @avance NVARCHAR(1000) = NULL,
    @accionPendiente NVARCHAR(1000) = NULL,
    @idUsuarioInternoAsignado INT = NULL,
    @estadoResultante NVARCHAR(20) = NULL,
    @idUsuarioInterno INT,
    @descripcionBitacora NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @estadoAnterior NVARCHAR(20);
    SELECT @estadoAnterior = estadoAlianza
    FROM dbo.AlianzaEstrategica
    WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

    IF @estadoAnterior IS NULL
        THROW 51320, 'La alianza no existe.', 1;

    IF @estadoResultante = @estadoAnterior
        SET @estadoResultante = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.SeguimientoAlianza
            (idAlianzaEstrategica, tipoRegistro, fechaInteraccion, tipoInteraccion, observacion, avance, accionPendiente,
             idUsuarioInternoAsignado, estadoAnterior, estadoResultante, idUsuarioInternoRegistro)
        VALUES
            (@idAlianzaEstrategica, N'Seguimiento', @fechaInteraccion, @tipoInteraccion, @observacion, @avance, @accionPendiente,
             @idUsuarioInternoAsignado, CASE WHEN @estadoResultante IS NULL THEN NULL ELSE @estadoAnterior END,
             @estadoResultante, @idUsuarioInterno);

        UPDATE dbo.AlianzaEstrategica
        SET estadoAlianza = ISNULL(@estadoResultante, estadoAlianza),
            activa = CASE WHEN ISNULL(@estadoResultante, estadoAlianza) = N'DadaDeBaja' THEN 0 ELSE 1 END,
            fechaBaja = CASE WHEN ISNULL(@estadoResultante, estadoAlianza) = N'DadaDeBaja' THEN ISNULL(fechaBaja, GETDATE()) ELSE NULL END,
            fechaUltimaModificacion = GETDATE()
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

        EXEC dbo.sp_RegistroActividad_Insertar
            @idUsuarioInternoResponsable = @idUsuarioInterno,
            @tipoOperacion = N'SEGUIMIENTO',
            @tipoEntidadAfectada = N'AlianzaEstrategica',
            @idEntidadAfectada = @idAlianzaEstrategica,
            @descripcionOperacion = @descripcionBitacora,
            @origenOperacion = N'StageUp.UI';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT ISNULL(@estadoResultante, @estadoAnterior) AS estadoActual;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AlianzaEstrategica_Renovar
    @idAlianzaEstrategica INT,
    @fechaInicio DATE,
    @fechaFin DATE = NULL,
    @alcance NVARCHAR(1000) = NULL,
    @beneficiosAsociados NVARCHAR(2000) = NULL,
    @idUsuarioInternoResponsable INT = NULL,
    @observacion NVARCHAR(1000) = NULL,
    @reactivar BIT = 0,
    @idUsuarioInterno INT,
    @operacionBitacora NVARCHAR(50),
    @descripcionBitacora NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @estadoAnterior NVARCHAR(20), @inicioAnterior DATE, @finAnterior DATE;
    SELECT @estadoAnterior = estadoAlianza, @inicioAnterior = fechaInicio, @finAnterior = fechaFin
    FROM dbo.AlianzaEstrategica
    WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

    IF @estadoAnterior IS NULL
        THROW 51320, 'La alianza no existe.', 1;

    IF @estadoAnterior IN (N'Finalizada', N'DadaDeBaja') AND @reactivar = 0
        THROW 51323, 'La alianza no está vigente.', 1;

    IF @idUsuarioInternoResponsable IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE idUsuarioInterno = @idUsuarioInternoResponsable AND activo = 1)
        THROW 51321, 'El responsable no es un usuario interno activo.', 1;

    DECLARE @estadoNuevo NVARCHAR(20) =
        CASE WHEN @estadoAnterior IN (N'Finalizada', N'DadaDeBaja', N'EnEvaluacion') THEN N'Activa' ELSE @estadoAnterior END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.AlianzaVigencia
            (idAlianzaEstrategica, fechaInicioAnterior, fechaFinAnterior, fechaInicioNueva, fechaFinNueva, observacion, idUsuarioInterno)
        VALUES
            (@idAlianzaEstrategica, @inicioAnterior, @finAnterior, @fechaInicio, @fechaFin, @observacion, @idUsuarioInterno);

        UPDATE dbo.AlianzaEstrategica
        SET fechaInicio = @fechaInicio,
            fechaFin = @fechaFin,
            alcance = ISNULL(@alcance, alcance),
            beneficiosAsociados = ISNULL(@beneficiosAsociados, beneficiosAsociados),
            idUsuarioInternoResponsable = ISNULL(@idUsuarioInternoResponsable, idUsuarioInternoResponsable),
            estadoAlianza = @estadoNuevo,
            activa = 1,
            fechaBaja = NULL,
            fechaUltimaModificacion = GETDATE()
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

        INSERT INTO dbo.SeguimientoAlianza
            (idAlianzaEstrategica, tipoRegistro, fechaInteraccion, observacion, idUsuarioInternoAsignado,
             estadoAnterior, estadoResultante, idUsuarioInternoRegistro)
        VALUES
            (@idAlianzaEstrategica, CASE WHEN @reactivar = 1 AND @estadoAnterior IN (N'Finalizada', N'DadaDeBaja') THEN N'Reactivacion' ELSE N'Renovacion' END,
             CAST(GETDATE() AS DATE),
             N'Nueva vigencia: ' + CONVERT(NVARCHAR(10), @fechaInicio, 103) + N' al ' + ISNULL(CONVERT(NVARCHAR(10), @fechaFin, 103), N'sin fecha de fin') +
                 N' (antes ' + CONVERT(NVARCHAR(10), @inicioAnterior, 103) + N' al ' + ISNULL(CONVERT(NVARCHAR(10), @finAnterior, 103), N'sin fecha de fin') + N').' +
                 ISNULL(N' ' + NULLIF(LTRIM(RTRIM(@observacion)), N''), N''),
             @idUsuarioInternoResponsable,
             CASE WHEN @estadoNuevo <> @estadoAnterior THEN @estadoAnterior END,
             CASE WHEN @estadoNuevo <> @estadoAnterior THEN @estadoNuevo END,
             @idUsuarioInterno);

        EXEC dbo.sp_RegistroActividad_Insertar
            @idUsuarioInternoResponsable = @idUsuarioInterno,
            @tipoOperacion = @operacionBitacora,
            @tipoEntidadAfectada = N'AlianzaEstrategica',
            @idEntidadAfectada = @idAlianzaEstrategica,
            @descripcionOperacion = @descripcionBitacora,
            @origenOperacion = N'StageUp.UI';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AlianzaEstrategica_Cerrar
    @idAlianzaEstrategica INT,
    @estadoNuevo NVARCHAR(20),
    @observacion NVARCHAR(2000) = NULL,
    @idUsuarioInterno INT,
    @operacionBitacora NVARCHAR(50),
    @descripcionBitacora NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @estadoAnterior NVARCHAR(20);
    SELECT @estadoAnterior = estadoAlianza
    FROM dbo.AlianzaEstrategica
    WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

    IF @estadoAnterior IS NULL
        THROW 51320, 'La alianza no existe.', 1;

    IF (@estadoNuevo = N'Finalizada' AND @estadoAnterior NOT IN (N'Activa', N'Pausada'))
       OR (@estadoNuevo = N'DadaDeBaja' AND @estadoAnterior = N'DadaDeBaja')
       OR @estadoNuevo NOT IN (N'Finalizada', N'DadaDeBaja')
        THROW 51324, 'El estado actual de la alianza no lo permite.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.AlianzaEstrategica
        SET estadoAlianza = @estadoNuevo,
            activa = CASE WHEN @estadoNuevo = N'DadaDeBaja' THEN 0 ELSE 1 END,
            fechaBaja = CASE WHEN @estadoNuevo = N'DadaDeBaja' THEN GETDATE() ELSE fechaBaja END,
            fechaUltimaModificacion = GETDATE()
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

        INSERT INTO dbo.SeguimientoAlianza
            (idAlianzaEstrategica, tipoRegistro, fechaInteraccion, observacion, estadoAnterior, estadoResultante, idUsuarioInternoRegistro)
        VALUES
            (@idAlianzaEstrategica, CASE WHEN @estadoNuevo = N'Finalizada' THEN N'Finalizacion' ELSE N'Baja' END, CAST(GETDATE() AS DATE),
             ISNULL(NULLIF(LTRIM(RTRIM(@observacion)), N''),
                    CASE WHEN @estadoNuevo = N'Finalizada' THEN N'Alianza finalizada.' ELSE N'Alianza dada de baja.' END),
             @estadoAnterior, @estadoNuevo, @idUsuarioInterno);

        EXEC dbo.sp_RegistroActividad_Insertar
            @idUsuarioInternoResponsable = @idUsuarioInterno,
            @tipoOperacion = @operacionBitacora,
            @tipoEntidadAfectada = N'AlianzaEstrategica',
            @idEntidadAfectada = @idAlianzaEstrategica,
            @descripcionOperacion = @descripcionBitacora,
            @origenOperacion = N'StageUp.UI';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
