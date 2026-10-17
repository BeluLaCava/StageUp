IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ===========================================================================
-- Procedimientos: alianzas estratégicas (CU-004-003)
-- ===========================================================================
IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Listar;
GO
-- @vista: EnGestion (en evaluación, activas y pausadas), Historicas
-- (finalizadas y dadas de baja) o Todas. Vigencia: alianzas cuyo período se
-- cruza con [@desde, @hasta] (sin fecha de fin = abierta).
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_Listar
    @vista            NVARCHAR(12) = N'EnGestion',
    @estado           NVARCHAR(20) = NULL,
    @tipoInstitucion  NVARCHAR(40) = NULL,
    @idResponsable    INT = NULL,
    @desde            DATE = NULL,
    @hasta            DATE = NULL,
    @texto            NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.idAlianzaEstrategica, a.nombreEntidad, a.tipoInstitucion, a.datosContacto, a.idUsuarioInternoResponsable,
           ui.nombre + N' ' + ui.apellido AS nombreResponsable,
           a.objetivoComercial, a.alcance, a.descripcionAcuerdo, a.beneficiosAsociados, a.fechaInicio, a.fechaFin,
           a.estadoAlianza, a.observaciones, a.fechaAlta, a.fechaUltimaModificacion, a.fechaBaja, a.activa,
           (SELECT COUNT(*) FROM dbo.ContactoComercial ct WHERE ct.idAlianzaEstrategica = a.idAlianzaEstrategica AND ct.activo = 1) AS cantidadContactos,
           (SELECT COUNT(*) FROM dbo.CampaniaAlianza ca WHERE ca.idAlianzaEstrategica = a.idAlianzaEstrategica) AS cantidadCampanias,
           (SELECT COUNT(*) FROM dbo.ContenidoPromocional cp WHERE cp.idAlianzaEstrategica = a.idAlianzaEstrategica AND cp.activo = 1) AS cantidadContenidos,
           (SELECT COUNT(*) FROM dbo.OrigenUsuario o WHERE o.idAlianzaEstrategica = a.idAlianzaEstrategica) AS cantidadOrigenes
    FROM dbo.AlianzaEstrategica a
    INNER JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = a.idUsuarioInternoResponsable
    WHERE (@vista = N'Todas'
           OR (@vista = N'EnGestion' AND a.estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada'))
           OR (@vista = N'Historicas' AND a.estadoAlianza IN (N'Finalizada', N'DadaDeBaja')))
      AND (@estado IS NULL OR a.estadoAlianza = @estado)
      AND (@tipoInstitucion IS NULL OR a.tipoInstitucion = @tipoInstitucion)
      AND (@idResponsable IS NULL OR a.idUsuarioInternoResponsable = @idResponsable)
      AND (@desde IS NULL OR a.fechaFin IS NULL OR a.fechaFin >= @desde)
      AND (@hasta IS NULL OR a.fechaInicio <= @hasta)
      AND (@texto IS NULL OR a.nombreEntidad LIKE N'%' + @texto + N'%' OR a.objetivoComercial LIKE N'%' + @texto + N'%')
    ORDER BY CASE a.estadoAlianza WHEN N'Activa' THEN 1 WHEN N'EnEvaluacion' THEN 2 WHEN N'Pausada' THEN 3 WHEN N'Finalizada' THEN 4 ELSE 5 END,
             a.nombreEntidad;
END
GO

IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Contar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Contar;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_Contar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS total,
           SUM(CASE WHEN estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada') THEN 1 ELSE 0 END) AS enGestion,
           SUM(CASE WHEN estadoAlianza = N'Activa' THEN 1 ELSE 0 END) AS activas,
           SUM(CASE WHEN estadoAlianza = N'EnEvaluacion' THEN 1 ELSE 0 END) AS enEvaluacion,
           SUM(CASE WHEN estadoAlianza IN (N'Activa', N'Pausada') AND fechaFin < CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS vencidas,
           SUM(CASE WHEN estadoAlianza IN (N'Finalizada', N'DadaDeBaja') THEN 1 ELSE 0 END) AS historicas
    FROM dbo.AlianzaEstrategica;
END
GO

IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Obtener;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_Obtener
    @idAlianzaEstrategica INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.idAlianzaEstrategica, a.nombreEntidad, a.tipoInstitucion, a.datosContacto, a.idUsuarioInternoResponsable,
           ui.nombre + N' ' + ui.apellido AS nombreResponsable,
           a.objetivoComercial, a.alcance, a.descripcionAcuerdo, a.beneficiosAsociados, a.fechaInicio, a.fechaFin,
           a.estadoAlianza, a.observaciones, a.fechaAlta, a.fechaUltimaModificacion, a.fechaBaja, a.activa,
           (SELECT COUNT(*) FROM dbo.ContactoComercial ct WHERE ct.idAlianzaEstrategica = a.idAlianzaEstrategica AND ct.activo = 1) AS cantidadContactos,
           (SELECT COUNT(*) FROM dbo.CampaniaAlianza ca WHERE ca.idAlianzaEstrategica = a.idAlianzaEstrategica) AS cantidadCampanias,
           (SELECT COUNT(*) FROM dbo.ContenidoPromocional cp WHERE cp.idAlianzaEstrategica = a.idAlianzaEstrategica AND cp.activo = 1) AS cantidadContenidos,
           (SELECT COUNT(*) FROM dbo.OrigenUsuario o WHERE o.idAlianzaEstrategica = a.idAlianzaEstrategica) AS cantidadOrigenes
    FROM dbo.AlianzaEstrategica a
    INNER JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = a.idUsuarioInternoResponsable
    WHERE a.idAlianzaEstrategica = @idAlianzaEstrategica;
END
GO

IF OBJECT_ID('dbo.sp_AlianzaEstrategica_ListarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_ListarVinculos;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_ListarVinculos
    @idAlianzaEstrategica INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'Contacto' AS tipo, ct.idContactoComercial AS id,
           ct.nombreContacto + ISNULL(N' ' + ct.apellidoContacto, N'') AS nombre,
           ct.estadoContacto AS estado, CAST(NULL AS INT) AS cantidad
    FROM dbo.ContactoComercial ct
    WHERE ct.idAlianzaEstrategica = @idAlianzaEstrategica AND ct.activo = 1
    UNION ALL
    SELECT N'Campania', c.idCampaniaComercial, c.nombre,
           CASE WHEN c.activa = 1 THEN c.estadoCampania ELSE N'DadaDeBaja' END, NULL
    FROM dbo.CampaniaAlianza ca
    INNER JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = ca.idCampaniaComercial
    WHERE ca.idAlianzaEstrategica = @idAlianzaEstrategica
    UNION ALL
    SELECT N'Contenido', cp.idContenidoPromocional, cp.titulo, cp.estadoContenido, NULL
    FROM dbo.ContenidoPromocional cp
    WHERE cp.idAlianzaEstrategica = @idAlianzaEstrategica AND cp.activo = 1
    UNION ALL
    SELECT N'Origen', o.idOrigenUsuario, o.nombreOrigen, CASE WHEN o.activo = 1 THEN N'Activo' ELSE N'Inactivo' END,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u WHERE u.idOrigenUsuario = o.idOrigenUsuario)
    FROM dbo.OrigenUsuario o
    WHERE o.idAlianzaEstrategica = @idAlianzaEstrategica
    ORDER BY 1, 3;
END
GO

IF OBJECT_ID('dbo.sp_AlianzaEstrategica_BuscarDuplicado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_BuscarDuplicado;
GO
-- A22: misma entidad, todavía en gestión, con un período que se cruza o el
-- mismo objetivo comercial.
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_BuscarDuplicado
    @idAlianzaExcluida  INT = NULL,
    @nombreEntidad      NVARCHAR(200),
    @fechaInicio        DATE,
    @fechaFin           DATE = NULL,
    @objetivoComercial  NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 a.idAlianzaEstrategica, a.nombreEntidad, a.estadoAlianza
    FROM dbo.AlianzaEstrategica a
    WHERE a.estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada')
      AND (@idAlianzaExcluida IS NULL OR a.idAlianzaEstrategica <> @idAlianzaExcluida)
      AND LTRIM(RTRIM(a.nombreEntidad)) = LTRIM(RTRIM(@nombreEntidad))
      AND ((a.fechaInicio <= ISNULL(@fechaFin, '99991231') AND ISNULL(a.fechaFin, '99991231') >= @fechaInicio)
           OR LTRIM(RTRIM(a.objetivoComercial)) = LTRIM(RTRIM(@objetivoComercial)))
    ORDER BY a.idAlianzaEstrategica;
END
GO

IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Guardar;
GO
-- Alta (@idAlianzaEstrategica NULL) o modificación. Un cambio de estado
-- queda en el seguimiento con su observación (A8). THROW 51320 si la alianza
-- no existe; 51321 si el responsable no es un usuario interno activo.
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_Guardar
    @idAlianzaEstrategica         INT = NULL,
    @nombreEntidad                NVARCHAR(200),
    @tipoInstitucion              NVARCHAR(40),
    @datosContacto                NVARCHAR(500),
    @idUsuarioInternoResponsable  INT,
    @objetivoComercial            NVARCHAR(1000),
    @alcance                      NVARCHAR(1000),
    @descripcionAcuerdo           NVARCHAR(2000),
    @beneficiosAsociados          NVARCHAR(2000),
    @fechaInicio                  DATE,
    @fechaFin                     DATE = NULL,
    @estadoAlianza                NVARCHAR(20),
    @observaciones                NVARCHAR(2000) = NULL,
    @observacionCambio            NVARCHAR(2000) = NULL,
    @idUsuarioInterno             INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estadoAnterior NVARCHAR(20);

    IF @idAlianzaEstrategica IS NOT NULL
    BEGIN
        SELECT @estadoAnterior = estadoAlianza FROM dbo.AlianzaEstrategica WHERE idAlianzaEstrategica = @idAlianzaEstrategica;
        IF @estadoAnterior IS NULL
            THROW 51320, 'La alianza no existe.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE idUsuarioInterno = @idUsuarioInternoResponsable AND activo = 1)
        THROW 51321, 'El responsable no es un usuario interno activo.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @idAlianzaEstrategica IS NULL
        BEGIN
            INSERT INTO dbo.AlianzaEstrategica
                (nombreEntidad, tipoInstitucion, datosContacto, idUsuarioInternoResponsable, objetivoComercial, alcance,
                 descripcionAcuerdo, beneficiosAsociados, fechaInicio, fechaFin, estadoAlianza, observaciones,
                 fechaBaja, activa)
            VALUES
                (@nombreEntidad, @tipoInstitucion, @datosContacto, @idUsuarioInternoResponsable, @objetivoComercial, @alcance,
                 @descripcionAcuerdo, @beneficiosAsociados, @fechaInicio, @fechaFin, @estadoAlianza, @observaciones,
                 CASE WHEN @estadoAlianza = N'DadaDeBaja' THEN GETDATE() END,
                 CASE WHEN @estadoAlianza = N'DadaDeBaja' THEN 0 ELSE 1 END);

            SET @idAlianzaEstrategica = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.AlianzaEstrategica
            SET nombreEntidad = @nombreEntidad,
                tipoInstitucion = @tipoInstitucion,
                datosContacto = @datosContacto,
                idUsuarioInternoResponsable = @idUsuarioInternoResponsable,
                objetivoComercial = @objetivoComercial,
                alcance = @alcance,
                descripcionAcuerdo = @descripcionAcuerdo,
                beneficiosAsociados = @beneficiosAsociados,
                fechaInicio = @fechaInicio,
                fechaFin = @fechaFin,
                estadoAlianza = @estadoAlianza,
                observaciones = @observaciones,
                fechaBaja = CASE WHEN @estadoAlianza = N'DadaDeBaja' THEN ISNULL(fechaBaja, GETDATE()) ELSE NULL END,
                activa = CASE WHEN @estadoAlianza = N'DadaDeBaja' THEN 0 ELSE 1 END,
                fechaUltimaModificacion = GETDATE()
            WHERE idAlianzaEstrategica = @idAlianzaEstrategica;

            IF @estadoAnterior <> @estadoAlianza
            BEGIN
                INSERT INTO dbo.SeguimientoAlianza
                    (idAlianzaEstrategica, tipoRegistro, fechaInteraccion, observacion, estadoAnterior, estadoResultante, idUsuarioInternoRegistro)
                VALUES
                    (@idAlianzaEstrategica, N'CambioEstado', CAST(GETDATE() AS DATE),
                     ISNULL(NULLIF(LTRIM(RTRIM(@observacionCambio)), N''), N'Cambio de estado desde la edición de la alianza.'),
                     @estadoAnterior, @estadoAlianza, @idUsuarioInterno);
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @idAlianzaEstrategica;
END
GO

-- Vincula la alianza con contactos, campañas, contenido promocional y
-- orígenes (listas separadas por coma). THROW 51322 si algo no está disponible.
IF OBJECT_ID('dbo.sp_AlianzaEstrategica_GuardarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_GuardarVinculos;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_GuardarVinculos
    @idAlianzaEstrategica  INT,
    @idsContactos          NVARCHAR(MAX) = NULL,
    @idsCampanias          NVARCHAR(MAX) = NULL,
    @idsContenidos         NVARCHAR(MAX) = NULL,
    @idsOrigenes           NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @contactos TABLE (id INT PRIMARY KEY);
    DECLARE @campanias TABLE (id INT PRIMARY KEY);
    DECLARE @contenidos TABLE (id INT PRIMARY KEY);
    DECLARE @origenes TABLE (id INT PRIMARY KEY);

    INSERT INTO @contactos SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsContactos, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @campanias SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsCampanias, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @contenidos SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsContenidos, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @origenes SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsOrigenes, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;

    IF EXISTS (SELECT 1 FROM @contactos l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ContactoComercial ct WHERE ct.idContactoComercial = l.id
                                 AND (ct.activo = 1 OR ct.idAlianzaEstrategica = @idAlianzaEstrategica)))
       OR EXISTS (SELECT 1 FROM @campanias l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.CampaniaComercial c WHERE c.idCampaniaComercial = l.id
                                 AND ((c.activa = 1 AND c.estadoCampania <> N'Cancelada')
                                      OR EXISTS (SELECT 1 FROM dbo.CampaniaAlianza ca WHERE ca.idAlianzaEstrategica = @idAlianzaEstrategica
                                                 AND ca.idCampaniaComercial = c.idCampaniaComercial))))
       OR EXISTS (SELECT 1 FROM @contenidos l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ContenidoPromocional cp WHERE cp.idContenidoPromocional = l.id
                                 AND ((cp.activo = 1 AND cp.estadoContenido <> N'DadoDeBaja') OR cp.idAlianzaEstrategica = @idAlianzaEstrategica)))
       OR EXISTS (SELECT 1 FROM @origenes l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.OrigenUsuario o WHERE o.idOrigenUsuario = l.id
                                 AND (o.activo = 1 OR o.idAlianzaEstrategica = @idAlianzaEstrategica)))
        THROW 51322, 'Un vínculo comercial no está disponible.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.ContactoComercial SET idAlianzaEstrategica = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica AND idContactoComercial NOT IN (SELECT id FROM @contactos);
        UPDATE ct SET ct.idAlianzaEstrategica = @idAlianzaEstrategica, ct.fechaUltimaModificacion = GETDATE()
        FROM dbo.ContactoComercial ct INNER JOIN @contactos l ON l.id = ct.idContactoComercial
        WHERE ISNULL(ct.idAlianzaEstrategica, 0) <> @idAlianzaEstrategica;

        DELETE FROM dbo.CampaniaAlianza
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica AND idCampaniaComercial NOT IN (SELECT id FROM @campanias);
        INSERT INTO dbo.CampaniaAlianza (idCampaniaComercial, idAlianzaEstrategica)
        SELECT l.id, @idAlianzaEstrategica FROM @campanias l
        WHERE NOT EXISTS (SELECT 1 FROM dbo.CampaniaAlianza ca WHERE ca.idAlianzaEstrategica = @idAlianzaEstrategica AND ca.idCampaniaComercial = l.id);

        UPDATE dbo.ContenidoPromocional SET idAlianzaEstrategica = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica AND idContenidoPromocional NOT IN (SELECT id FROM @contenidos);
        UPDATE cp SET cp.idAlianzaEstrategica = @idAlianzaEstrategica, cp.fechaUltimaModificacion = GETDATE()
        FROM dbo.ContenidoPromocional cp INNER JOIN @contenidos l ON l.id = cp.idContenidoPromocional
        WHERE ISNULL(cp.idAlianzaEstrategica, 0) <> @idAlianzaEstrategica;

        UPDATE dbo.OrigenUsuario SET idAlianzaEstrategica = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idAlianzaEstrategica = @idAlianzaEstrategica AND idOrigenUsuario NOT IN (SELECT id FROM @origenes);
        UPDATE o SET o.idAlianzaEstrategica = @idAlianzaEstrategica, o.fechaUltimaModificacion = GETDATE()
        FROM dbo.OrigenUsuario o INNER JOIN @origenes l ON l.id = o.idOrigenUsuario
        WHERE ISNULL(o.idAlianzaEstrategica, 0) <> @idAlianzaEstrategica;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- A7: seguimiento (y A8 si se indica el estado resultante).
IF OBJECT_ID('dbo.sp_SeguimientoAlianza_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SeguimientoAlianza_Insertar;
GO
CREATE PROCEDURE dbo.sp_SeguimientoAlianza_Insertar
    @idAlianzaEstrategica      INT,
    @fechaInteraccion          DATE,
    @tipoInteraccion           NVARCHAR(40),
    @observacion               NVARCHAR(2000),
    @avance                    NVARCHAR(1000) = NULL,
    @accionPendiente           NVARCHAR(1000) = NULL,
    @idUsuarioInternoAsignado  INT = NULL,
    @estadoResultante          NVARCHAR(20) = NULL,
    @idUsuarioInterno          INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estadoAnterior NVARCHAR(20);
    SELECT @estadoAnterior = estadoAlianza FROM dbo.AlianzaEstrategica WHERE idAlianzaEstrategica = @idAlianzaEstrategica;
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

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT ISNULL(@estadoResultante, @estadoAnterior) AS estadoActual;
END
GO

IF OBJECT_ID('dbo.sp_SeguimientoAlianza_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SeguimientoAlianza_Listar;
GO
CREATE PROCEDURE dbo.sp_SeguimientoAlianza_Listar
    @idAlianzaEstrategica INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.idSeguimientoAlianza, s.idAlianzaEstrategica, s.tipoRegistro, s.fechaInteraccion, s.tipoInteraccion,
           s.observacion, s.avance, s.accionPendiente, s.idUsuarioInternoAsignado,
           ua.nombre + N' ' + ua.apellido AS nombreAsignado,
           s.estadoAnterior, s.estadoResultante, s.idUsuarioInternoRegistro,
           ur.nombre + N' ' + ur.apellido AS nombreRegistro, s.fechaRegistro
    FROM dbo.SeguimientoAlianza s
    LEFT JOIN dbo.UsuarioInterno ua ON ua.idUsuarioInterno = s.idUsuarioInternoAsignado
    LEFT JOIN dbo.UsuarioInterno ur ON ur.idUsuarioInterno = s.idUsuarioInternoRegistro
    WHERE s.idAlianzaEstrategica = @idAlianzaEstrategica
    ORDER BY s.fechaRegistro DESC, s.idSeguimientoAlianza DESC;
END
GO

-- A9 (y reactivación de una finalizada o dada de baja con @reactivar = 1):
-- guarda el período anterior en AlianzaVigencia. Los datos opcionales en
-- NULL no se cambian.
IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Renovar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Renovar;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_Renovar
    @idAlianzaEstrategica         INT,
    @fechaInicio                  DATE,
    @fechaFin                     DATE = NULL,
    @alcance                      NVARCHAR(1000) = NULL,
    @beneficiosAsociados          NVARCHAR(2000) = NULL,
    @idUsuarioInternoResponsable  INT = NULL,
    @observacion                  NVARCHAR(1000) = NULL,
    @reactivar                    BIT = 0,
    @idUsuarioInterno             INT
AS
BEGIN
    SET NOCOUNT ON;

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

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_AlianzaEstrategica_ListarVigencias', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_ListarVigencias;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_ListarVigencias
    @idAlianzaEstrategica INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT v.idAlianzaVigencia, v.fechaInicioAnterior, v.fechaFinAnterior, v.fechaInicioNueva, v.fechaFinNueva,
           v.observacion, ui.nombre + N' ' + ui.apellido AS nombreUsuario, v.fechaRegistro
    FROM dbo.AlianzaVigencia v
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = v.idUsuarioInterno
    WHERE v.idAlianzaEstrategica = @idAlianzaEstrategica
    ORDER BY v.fechaRegistro DESC;
END
GO

-- A10 (Finalizada) y A11 (DadaDeBaja). THROW 51324 si el estado actual no
-- lo permite.
IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Cerrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Cerrar;
GO
CREATE PROCEDURE dbo.sp_AlianzaEstrategica_Cerrar
    @idAlianzaEstrategica  INT,
    @estadoNuevo           NVARCHAR(20),
    @observacion           NVARCHAR(2000) = NULL,
    @idUsuarioInterno      INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estadoAnterior NVARCHAR(20);
    SELECT @estadoAnterior = estadoAlianza FROM dbo.AlianzaEstrategica WHERE idAlianzaEstrategica = @idAlianzaEstrategica;
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

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ===========================================================================
-- Procedimientos: contenido promocional (CU-004-005)
-- ===========================================================================
IF OBJECT_ID('dbo.sp_ContenidoPromocional_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContenidoPromocional_Listar;
GO
-- @vista: Vigentes (sin los dados de baja), Bajas o Todos.
CREATE PROCEDURE dbo.sp_ContenidoPromocional_Listar
    @vista                NVARCHAR(10) = N'Vigentes',
    @estado               NVARCHAR(20) = NULL,
    @tipoContenido        NVARCHAR(40) = NULL,
    @canalPublicacion     NVARCHAR(40) = NULL,
    @idCampaniaComercial  INT = NULL,
    @texto                NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT cp.idContenidoPromocional, cp.titulo, cp.descripcion, cp.tipoContenido, cp.canalPublicacion,
           cp.idCampaniaComercial, c.nombre AS nombreCampania, cp.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           cp.estadoContenido, cp.urlContenido, cp.rutaArchivo, cp.nombreArchivoOriginal, cp.tipoMimeArchivo, cp.tamanioArchivo,
           cp.idUsuarioInternoResponsable, ui.nombre + N' ' + ui.apellido AS nombreResponsable,
           cp.fechaAlta, cp.fechaUltimaModificacion, cp.fechaBaja, cp.activo
    FROM dbo.ContenidoPromocional cp
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = cp.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = cp.idAlianzaEstrategica
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = cp.idUsuarioInternoResponsable
    WHERE (@vista = N'Todos' OR (@vista = N'Vigentes' AND cp.activo = 1) OR (@vista = N'Bajas' AND cp.activo = 0))
      AND (@estado IS NULL OR cp.estadoContenido = @estado)
      AND (@tipoContenido IS NULL OR cp.tipoContenido = @tipoContenido)
      AND (@canalPublicacion IS NULL OR cp.canalPublicacion = @canalPublicacion)
      AND (@idCampaniaComercial IS NULL OR cp.idCampaniaComercial = @idCampaniaComercial)
      AND (@texto IS NULL OR cp.titulo LIKE N'%' + @texto + N'%' OR cp.descripcion LIKE N'%' + @texto + N'%')
    ORDER BY cp.activo DESC,
             CASE cp.estadoContenido WHEN N'Activo' THEN 1 WHEN N'EnRevision' THEN 2 WHEN N'Desactualizado' THEN 3 ELSE 4 END,
             cp.fechaAlta DESC;
END
GO

IF OBJECT_ID('dbo.sp_ContenidoPromocional_Contar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContenidoPromocional_Contar;
GO
CREATE PROCEDURE dbo.sp_ContenidoPromocional_Contar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS total,
           SUM(CASE WHEN estadoContenido = N'Activo' THEN 1 ELSE 0 END) AS activos,
           SUM(CASE WHEN estadoContenido = N'EnRevision' THEN 1 ELSE 0 END) AS enRevision,
           SUM(CASE WHEN estadoContenido = N'Desactualizado' THEN 1 ELSE 0 END) AS desactualizados,
           SUM(CASE WHEN estadoContenido = N'DadoDeBaja' THEN 1 ELSE 0 END) AS bajas
    FROM dbo.ContenidoPromocional;
END
GO

IF OBJECT_ID('dbo.sp_ContenidoPromocional_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContenidoPromocional_Obtener;
GO
CREATE PROCEDURE dbo.sp_ContenidoPromocional_Obtener
    @idContenidoPromocional INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT cp.idContenidoPromocional, cp.titulo, cp.descripcion, cp.tipoContenido, cp.canalPublicacion,
           cp.idCampaniaComercial, c.nombre AS nombreCampania, cp.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           cp.estadoContenido, cp.urlContenido, cp.rutaArchivo, cp.nombreArchivoOriginal, cp.tipoMimeArchivo, cp.tamanioArchivo,
           cp.idUsuarioInternoResponsable, ui.nombre + N' ' + ui.apellido AS nombreResponsable,
           cp.fechaAlta, cp.fechaUltimaModificacion, cp.fechaBaja, cp.activo
    FROM dbo.ContenidoPromocional cp
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = cp.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = cp.idAlianzaEstrategica
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = cp.idUsuarioInternoResponsable
    WHERE cp.idContenidoPromocional = @idContenidoPromocional;
END
GO

IF OBJECT_ID('dbo.sp_ContenidoPromocional_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContenidoPromocional_Guardar;
GO
-- Alta (@idContenidoPromocional NULL) o modificación. El archivo solo se
-- reemplaza si @rutaArchivo viene informado; @quitarArchivo = 1 lo saca.
-- THROW 51330 contenido inexistente o dado de baja; 51331 campaña o alianza
-- no disponible; 51332 sin archivo ni enlace.
CREATE PROCEDURE dbo.sp_ContenidoPromocional_Guardar
    @idContenidoPromocional  INT = NULL,
    @titulo                  NVARCHAR(200),
    @descripcion             NVARCHAR(2000) = NULL,
    @tipoContenido           NVARCHAR(40),
    @canalPublicacion        NVARCHAR(40),
    @idCampaniaComercial     INT = NULL,
    @idAlianzaEstrategica    INT = NULL,
    @estadoContenido         NVARCHAR(20),
    @urlContenido            NVARCHAR(1000) = NULL,
    @rutaArchivo             NVARCHAR(260) = NULL,
    @nombreArchivoOriginal   NVARCHAR(260) = NULL,
    @tipoMimeArchivo         NVARCHAR(100) = NULL,
    @tamanioArchivo          INT = NULL,
    @quitarArchivo           BIT = 0,
    @idUsuarioInterno        INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @campaniaAnterior INT, @alianzaAnterior INT, @rutaAnterior NVARCHAR(260), @existe BIT = 0;

    IF @idContenidoPromocional IS NOT NULL
    BEGIN
        SELECT @existe = 1, @campaniaAnterior = idCampaniaComercial, @alianzaAnterior = idAlianzaEstrategica, @rutaAnterior = rutaArchivo
        FROM dbo.ContenidoPromocional
        WHERE idContenidoPromocional = @idContenidoPromocional AND activo = 1;

        IF @existe = 0
            THROW 51330, 'El contenido no existe o está dado de baja.', 1;
    END

    IF @idCampaniaComercial IS NOT NULL AND ISNULL(@campaniaAnterior, 0) <> @idCampaniaComercial
       AND NOT EXISTS (SELECT 1 FROM dbo.CampaniaComercial WHERE idCampaniaComercial = @idCampaniaComercial
                       AND activa = 1 AND estadoCampania <> N'Cancelada')
        THROW 51331, 'La campaña no está disponible.', 1;

    IF @idAlianzaEstrategica IS NOT NULL AND ISNULL(@alianzaAnterior, 0) <> @idAlianzaEstrategica
       AND NOT EXISTS (SELECT 1 FROM dbo.AlianzaEstrategica WHERE idAlianzaEstrategica = @idAlianzaEstrategica
                       AND activa = 1 AND estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada'))
        THROW 51331, 'La alianza no está disponible.', 1;

    DECLARE @rutaFinal NVARCHAR(260) = CASE WHEN @rutaArchivo IS NOT NULL THEN @rutaArchivo
                                            WHEN @quitarArchivo = 1 THEN NULL
                                            ELSE @rutaAnterior END;
    IF @rutaFinal IS NULL AND NULLIF(LTRIM(RTRIM(@urlContenido)), N'') IS NULL
        THROW 51332, 'El contenido necesita un archivo o un enlace.', 1;

    IF @idContenidoPromocional IS NULL
    BEGIN
        INSERT INTO dbo.ContenidoPromocional
            (titulo, descripcion, tipoContenido, canalPublicacion, idCampaniaComercial, idAlianzaEstrategica, estadoContenido,
             urlContenido, rutaArchivo, nombreArchivoOriginal, tipoMimeArchivo, tamanioArchivo, idUsuarioInternoResponsable)
        VALUES
            (@titulo, @descripcion, @tipoContenido, @canalPublicacion, @idCampaniaComercial, @idAlianzaEstrategica, @estadoContenido,
             @urlContenido, @rutaArchivo, @nombreArchivoOriginal, @tipoMimeArchivo, @tamanioArchivo, @idUsuarioInterno);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idContenidoPromocional, CAST(NULL AS NVARCHAR(260)) AS rutaArchivoAnterior;
        RETURN;
    END

    UPDATE dbo.ContenidoPromocional
    SET titulo = @titulo,
        descripcion = @descripcion,
        tipoContenido = @tipoContenido,
        canalPublicacion = @canalPublicacion,
        idCampaniaComercial = @idCampaniaComercial,
        idAlianzaEstrategica = @idAlianzaEstrategica,
        estadoContenido = @estadoContenido,
        urlContenido = @urlContenido,
        rutaArchivo = @rutaFinal,
        nombreArchivoOriginal = CASE WHEN @rutaArchivo IS NOT NULL THEN @nombreArchivoOriginal WHEN @quitarArchivo = 1 THEN NULL ELSE nombreArchivoOriginal END,
        tipoMimeArchivo = CASE WHEN @rutaArchivo IS NOT NULL THEN @tipoMimeArchivo WHEN @quitarArchivo = 1 THEN NULL ELSE tipoMimeArchivo END,
        tamanioArchivo = CASE WHEN @rutaArchivo IS NOT NULL THEN @tamanioArchivo WHEN @quitarArchivo = 1 THEN NULL ELSE tamanioArchivo END,
        fechaUltimaModificacion = GETDATE()
    WHERE idContenidoPromocional = @idContenidoPromocional;

    -- Devuelve el archivo anterior si se reemplazó o se quitó (para borrarlo).
    SELECT @idContenidoPromocional AS idContenidoPromocional,
           CASE WHEN ISNULL(@rutaFinal, N'') <> ISNULL(@rutaAnterior, N'') THEN @rutaAnterior END AS rutaArchivoAnterior;
END
GO

-- A7, A8 y reactivación de un contenido dado de baja.
IF OBJECT_ID('dbo.sp_ContenidoPromocional_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContenidoPromocional_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_ContenidoPromocional_CambiarEstado
    @idContenidoPromocional  INT,
    @estadoContenido         NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ContenidoPromocional
    SET estadoContenido = @estadoContenido,
        activo = CASE WHEN @estadoContenido = N'DadoDeBaja' THEN 0 ELSE 1 END,
        fechaBaja = CASE WHEN @estadoContenido = N'DadoDeBaja' THEN GETDATE() ELSE NULL END,
        fechaUltimaModificacion = GETDATE()
    WHERE idContenidoPromocional = @idContenidoPromocional
      AND estadoContenido <> @estadoContenido;

    SELECT @@ROWCOUNT;
END
GO

-- ===========================================================================
-- Procedimientos: métricas comerciales y origen de usuarios (CU-004-004)
-- Filtros comunes: período (@desde/@hasta sobre la fecha de cada hecho),
-- campaña, canal de origen ('NoInformado' = usuarios sin origen) y tipo de
-- usuario ('Solicitante' o 'Gestor').
-- ===========================================================================
IF OBJECT_ID('dbo.sp_MetricasComerciales_Resumen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MetricasComerciales_Resumen;
GO
CREATE PROCEDURE dbo.sp_MetricasComerciales_Resumen
    @desde                DATE = NULL,
    @hasta                DATE = NULL,
    @idCampaniaComercial  INT = NULL,
    @canalOrigen          NVARCHAR(40) = NULL,
    @tipoUsuario          NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hastaExclusivo DATETIME = CASE WHEN @hasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@hasta AS DATETIME)) END;

    ;WITH Usuarios AS
    (
        SELECT u.idUsuarioExterno, u.fechaAlta, u.perfilUsuario, o.canalOrigen, o.idCampaniaComercial
        FROM dbo.UsuarioExterno u
        LEFT JOIN dbo.OrigenUsuario o ON o.idOrigenUsuario = u.idOrigenUsuario
        WHERE (@idCampaniaComercial IS NULL OR o.idCampaniaComercial = @idCampaniaComercial)
          AND (@canalOrigen IS NULL OR (@canalOrigen = N'NoInformado' AND u.idOrigenUsuario IS NULL) OR o.canalOrigen = @canalOrigen)
          AND (@tipoUsuario IS NULL
               OR (@tipoUsuario = N'Gestor' AND u.perfilUsuario = N'GestorEspacios')
               OR (@tipoUsuario = N'Solicitante' AND u.perfilUsuario <> N'GestorEspacios'))
    )
    SELECT
        (SELECT COUNT(*) FROM Usuarios WHERE (@desde IS NULL OR fechaAlta >= @desde) AND (@hastaExclusivo IS NULL OR fechaAlta < @hastaExclusivo)) AS nuevosUsuarios,
        (SELECT COUNT(*) FROM Usuarios WHERE canalOrigen IS NOT NULL
             AND (@desde IS NULL OR fechaAlta >= @desde) AND (@hastaExclusivo IS NULL OR fechaAlta < @hastaExclusivo)) AS usuariosConOrigen,
        (SELECT COUNT(*) FROM dbo.EspacioArtistico e
             INNER JOIN Usuarios g ON g.idUsuarioExterno = e.idUsuarioGestor
             WHERE (@desde IS NULL OR e.fechaAlta >= @desde) AND (@hastaExclusivo IS NULL OR e.fechaAlta < @hastaExclusivo)) AS espaciosIncorporados,
        (SELECT COUNT(*) FROM dbo.CampaniaComercial c
             WHERE c.activa = 1 AND c.estadoCampania = N'Activa'
               AND (@idCampaniaComercial IS NULL OR c.idCampaniaComercial = @idCampaniaComercial)
               AND (@desde IS NULL OR c.fechaFin >= @desde) AND (@hasta IS NULL OR c.fechaInicio <= @hasta)) AS campaniasActivas,
        (SELECT COUNT(*) FROM dbo.ContactoComercial ct
             INNER JOIN dbo.OrigenUsuario oc ON oc.idOrigenUsuario = ct.idOrigenUsuario
             LEFT JOIN dbo.UsuarioExterno uc ON uc.idUsuarioExterno = ct.idUsuarioExterno
             WHERE ct.activo = 1 AND ct.estadoContacto = N'Incorporado'
               AND (@idCampaniaComercial IS NULL OR ct.idCampaniaComercial = @idCampaniaComercial)
               AND (@canalOrigen IS NULL OR oc.canalOrigen = @canalOrigen)
               AND (@tipoUsuario IS NULL OR uc.idUsuarioExterno IS NULL
                    OR (@tipoUsuario = N'Gestor' AND uc.perfilUsuario = N'GestorEspacios')
                    OR (@tipoUsuario = N'Solicitante' AND uc.perfilUsuario <> N'GestorEspacios'))
               AND (@desde IS NULL OR ct.fechaIncorporacion >= @desde) AND (@hastaExclusivo IS NULL OR ct.fechaIncorporacion < @hastaExclusivo)) AS contactosConvertidos,
        (SELECT COUNT(*) FROM dbo.UsuarioExterno) AS totalUsuarios,
        (SELECT COUNT(*) FROM dbo.CampaniaComercial) AS totalCampanias,
        (SELECT COUNT(*) FROM dbo.ContactoComercial) AS totalContactos;
END
GO

IF OBJECT_ID('dbo.sp_MetricasComerciales_PorOrigen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MetricasComerciales_PorOrigen;
GO
-- Usuarios registrados por origen (fila con idOrigenUsuario NULL = origen
-- no informado), con cuántos son gestores.
CREATE PROCEDURE dbo.sp_MetricasComerciales_PorOrigen
    @desde                DATE = NULL,
    @hasta                DATE = NULL,
    @idCampaniaComercial  INT = NULL,
    @canalOrigen          NVARCHAR(40) = NULL,
    @tipoUsuario          NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hastaExclusivo DATETIME = CASE WHEN @hasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@hasta AS DATETIME)) END;

    SELECT o.idOrigenUsuario, ISNULL(o.canalOrigen, N'NoInformado') AS canalOrigen, o.nombreOrigen,
           o.idCampaniaComercial, c.nombre AS nombreCampania, o.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           COUNT(*) AS cantidadUsuarios,
           SUM(CASE WHEN u.perfilUsuario = N'GestorEspacios' THEN 1 ELSE 0 END) AS cantidadGestores
    FROM dbo.UsuarioExterno u
    LEFT JOIN dbo.OrigenUsuario o ON o.idOrigenUsuario = u.idOrigenUsuario
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = o.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = o.idAlianzaEstrategica
    WHERE (@desde IS NULL OR u.fechaAlta >= @desde)
      AND (@hastaExclusivo IS NULL OR u.fechaAlta < @hastaExclusivo)
      AND (@idCampaniaComercial IS NULL OR o.idCampaniaComercial = @idCampaniaComercial)
      AND (@canalOrigen IS NULL OR (@canalOrigen = N'NoInformado' AND u.idOrigenUsuario IS NULL) OR o.canalOrigen = @canalOrigen)
      AND (@tipoUsuario IS NULL
           OR (@tipoUsuario = N'Gestor' AND u.perfilUsuario = N'GestorEspacios')
           OR (@tipoUsuario = N'Solicitante' AND u.perfilUsuario <> N'GestorEspacios'))
    GROUP BY o.idOrigenUsuario, o.canalOrigen, o.nombreOrigen, o.idCampaniaComercial, c.nombre, o.idAlianzaEstrategica, a.nombreEntidad
    ORDER BY COUNT(*) DESC, o.nombreOrigen;
END
GO

-- ===========================================================================
-- Permisos y menú (alianzas, métricas, contenido promocional y reactivación)
-- ===========================================================================
IF OBJECT_ID('tempdb..#RegistrarPermisoComercial') IS NOT NULL DROP PROCEDURE #RegistrarPermisoComercial;
GO
CREATE PROCEDURE #RegistrarPermisoComercial
    @codigo         NVARCHAR(100),
    @nombre         NVARCHAR(200),
    @descripcion    NVARCHAR(500),
    @url            NVARCHAR(300),
    @accion         NVARCHAR(50),
    @orden          INT,
    @agregarAlMenu  BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @idPermiso INT =
        (
            SELECT TOP 1 idPermisoInterno FROM dbo.PermisoInterno WHERE codigoPermiso = @codigo ORDER BY idPermisoInterno
        );

        IF @idPermiso IS NULL
        BEGIN
            INSERT INTO dbo.PermisoInterno
                (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
            VALUES
                (@codigo, @nombre, @descripcion, N'Comercialización', @accion, N'Activo', @url, 1);

            SET @idPermiso = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.PermisoInterno
            SET nombrePermiso = @nombre, descripcion = @descripcion, modulo = N'Comercialización', accion = @accion,
                estadoPermiso = N'Activo', urlAsociada = @url, activo = 1, fechaUltimaModificacion = GETDATE()
            WHERE idPermisoInterno = @idPermiso;
        END

        DECLARE @idRolAdministrador INT =
        (
            SELECT TOP 1 idRolInterno FROM dbo.RolInterno WHERE nombreRol = N'Administrador' AND activo = 1 ORDER BY idRolInterno
        );

        IF @idRolAdministrador IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.RolInternoPermiso WHERE idRolInterno = @idRolAdministrador AND idPermisoInterno = @idPermiso)
        BEGIN
            INSERT INTO dbo.RolInternoPermiso (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
            VALUES (@idRolAdministrador, @idPermiso, GETDATE(), 1);
        END
        ELSE IF @idRolAdministrador IS NOT NULL
        BEGIN
            UPDATE dbo.RolInternoPermiso SET activo = 1
            WHERE idRolInterno = @idRolAdministrador AND idPermisoInterno = @idPermiso;
        END

        DECLARE @idComponente INT = NULL;

        IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
        BEGIN
            DECLARE @idGrupoComercial INT =
            (
                SELECT TOP 1 idComponentePermiso FROM dbo.ComponentePermiso
                WHERE tipoComponente = N'Grupo' AND idComponentePadre IS NULL AND nombre = N'Comercialización'
                ORDER BY idComponentePermiso
            );

            IF @idGrupoComercial IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (NULL, N'Grupo', NULL, N'Comercialización',
                     N'Campañas, contactos, alianzas, métricas y contenido promocional.', NULL, 400, 1);

                SET @idGrupoComercial = CAST(SCOPE_IDENTITY() AS INT);
            END

            SET @idComponente =
            (
                SELECT TOP 1 idComponentePermiso FROM dbo.ComponentePermiso WHERE codigoPermiso = @codigo ORDER BY idComponentePermiso
            );

            IF @idComponente IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (@idGrupoComercial, N'Permiso', @codigo, @nombre, @descripcion, @url, @orden, 1);

                SET @idComponente = CAST(SCOPE_IDENTITY() AS INT);
            END
            ELSE
            BEGIN
                UPDATE dbo.ComponentePermiso
                SET idComponentePadre = @idGrupoComercial, tipoComponente = N'Permiso', nombre = @nombre,
                    descripcion = @descripcion, urlAsociada = @url, activo = 1, fechaUltimaModificacion = GETDATE()
                WHERE idComponentePermiso = @idComponente;
            END

            IF @idRolAdministrador IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.RolInternoComponentePermiso
                               WHERE idRolInterno = @idRolAdministrador AND idComponentePermiso = @idComponente)
                BEGIN
                    INSERT INTO dbo.RolInternoComponentePermiso (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                    VALUES (@idRolAdministrador, @idComponente, GETDATE(), 1);
                END
                ELSE
                BEGIN
                    UPDATE dbo.RolInternoComponentePermiso SET activo = 1
                    WHERE idRolInterno = @idRolAdministrador AND idComponentePermiso = @idComponente;
                END
            END
        END

        IF @agregarAlMenu = 1
           AND OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
           AND @idComponente IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponente)
        BEGIN
            -- Se ubica justo después de la última opción de Comercialización.
            DECLARE @ordenMenu INT =
            (
                SELECT MAX(orden) FROM dbo.OpcionMenu WHERE modulo = N'Comercialización'
            );

            IF @ordenMenu IS NULL
            BEGIN
                SET @ordenMenu = (SELECT ISNULL(MAX(orden), 0) + 10 FROM dbo.OpcionMenu);
            END
            ELSE
            BEGIN
                UPDATE dbo.OpcionMenu SET orden = orden + 1 WHERE orden > @ordenMenu;
                SET @ordenMenu = @ordenMenu + 1;
            END

            INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
            VALUES (@nombre, @descripcion, @url, N'Comercialización', @ordenMenu, @idComponente, 1);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        DECLARE @mensajeError NVARCHAR(4000);
        SET @mensajeError = ERROR_MESSAGE();
        RAISERROR(N'%s', 16, 1, @mensajeError);
    END CATCH
END
GO

EXEC #RegistrarPermisoComercial
    @codigo = N'GESTIONAR_ALIANZAS_ESTRATEGICAS',
    @nombre = N'Alianzas estratégicas',
    @descripcion = N'Registrar, consultar, modificar, renovar, finalizar y dar de baja alianzas estratégicas y su seguimiento.',
    @url = N'~/Interno/Comercializacion/AlianzasEstrategicas.aspx',
    @accion = N'Modificar',
    @orden = 430;
GO

EXEC #RegistrarPermisoComercial
    @codigo = N'CONSULTAR_METRICAS_COMERCIALES',
    @nombre = N'Métricas comerciales',
    @descripcion = N'Consultar métricas comerciales y el origen de los usuarios.',
    @url = N'~/Interno/Comercializacion/MetricasComerciales.aspx',
    @accion = N'Consultar',
    @orden = 440;
GO

EXEC #RegistrarPermisoComercial
    @codigo = N'GESTIONAR_CONTENIDO_PROMOCIONAL',
    @nombre = N'Contenido promocional',
    @descripcion = N'Registrar, consultar, modificar, cambiar de estado y dar de baja piezas promocionales.',
    @url = N'~/Interno/Comercializacion/ContenidoPromocional.aspx',
    @accion = N'Modificar',
    @orden = 450;
GO

EXEC #RegistrarPermisoComercial
    @codigo = N'REACTIVAR_REGISTROS_COMERCIALES',
    @nombre = N'Reactivar alianzas y contenido dados de baja',
    @descripcion = N'Volver a poner en gestión una alianza finalizada o dada de baja, o un contenido promocional dado de baja.',
    @url = N'~/Interno/Comercializacion/AlianzasEstrategicas.aspx',
    @accion = N'Modificar',
    @orden = 460,
    @agregarAlMenu = 0;
GO

DROP PROCEDURE #RegistrarPermisoComercial;
GO
