IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ===========================================================================
-- Vínculos históricos: también se listan los contactos y las piezas de
-- contenido dados de baja que siguen asociados, para que el formulario los
-- muestre marcados y no se pierdan al editar otros datos.
-- ===========================================================================
IF OBJECT_ID('dbo.sp_CampaniaComercial_ListarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_ListarVinculos;
GO
CREATE PROCEDURE dbo.sp_CampaniaComercial_ListarVinculos
    @idCampaniaComercial INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'Contacto' AS tipo, ct.idContactoComercial AS id,
           ct.nombreContacto + ISNULL(N' ' + ct.apellidoContacto, N'') AS nombre,
           ct.estadoContacto AS estado, CAST(NULL AS INT) AS cantidad
    FROM dbo.ContactoComercial ct
    WHERE ct.idCampaniaComercial = @idCampaniaComercial
    UNION ALL
    SELECT N'Alianza', a.idAlianzaEstrategica, a.nombreEntidad, a.estadoAlianza, NULL
    FROM dbo.CampaniaAlianza ca
    INNER JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = ca.idAlianzaEstrategica
    WHERE ca.idCampaniaComercial = @idCampaniaComercial
    UNION ALL
    SELECT N'Contenido', cp.idContenidoPromocional, cp.titulo,
           CASE WHEN cp.activo = 1 THEN cp.estadoContenido ELSE N'DadoDeBaja' END, NULL
    FROM dbo.ContenidoPromocional cp
    WHERE cp.idCampaniaComercial = @idCampaniaComercial
    UNION ALL
    SELECT N'Origen', o.idOrigenUsuario, o.nombreOrigen, CASE WHEN o.activo = 1 THEN N'Activo' ELSE N'Inactivo' END,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u WHERE u.idOrigenUsuario = o.idOrigenUsuario)
    FROM dbo.OrigenUsuario o
    WHERE o.idCampaniaComercial = @idCampaniaComercial
    ORDER BY 1, 3;
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
    WHERE ct.idAlianzaEstrategica = @idAlianzaEstrategica
    UNION ALL
    SELECT N'Campania', c.idCampaniaComercial, c.nombre,
           CASE WHEN c.activa = 1 THEN c.estadoCampania ELSE N'DadaDeBaja' END, NULL
    FROM dbo.CampaniaAlianza ca
    INNER JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = ca.idCampaniaComercial
    WHERE ca.idAlianzaEstrategica = @idAlianzaEstrategica
    UNION ALL
    SELECT N'Contenido', cp.idContenidoPromocional, cp.titulo,
           CASE WHEN cp.activo = 1 THEN cp.estadoContenido ELSE N'DadoDeBaja' END, NULL
    FROM dbo.ContenidoPromocional cp
    WHERE cp.idAlianzaEstrategica = @idAlianzaEstrategica
    UNION ALL
    SELECT N'Origen', o.idOrigenUsuario, o.nombreOrigen, CASE WHEN o.activo = 1 THEN N'Activo' ELSE N'Inactivo' END,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u WHERE u.idOrigenUsuario = o.idOrigenUsuario)
    FROM dbo.OrigenUsuario o
    WHERE o.idAlianzaEstrategica = @idAlianzaEstrategica
    ORDER BY 1, 3;
END
GO

-- ===========================================================================
-- Campaña: datos, público objetivo y vínculos en una sola transacción.
-- Se guarda todo o nada. THROW 51303 si la campaña no existe o está dada de
-- baja; 51302 si un vínculo nuevo no está disponible (lo que ya estaba
-- vinculado se puede conservar aunque haya cambiado de estado).
-- ===========================================================================
IF OBJECT_ID('dbo.sp_CampaniaComercial_GuardarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_GuardarVinculos;
GO
IF OBJECT_ID('dbo.sp_CampaniaComercial_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_Guardar;
GO
CREATE PROCEDURE dbo.sp_CampaniaComercial_Guardar
    @idCampaniaComercial          INT = NULL,
    @nombre                       NVARCHAR(150),
    @descripcion                  NVARCHAR(2000),
    @canalDifusion                NVARCHAR(40),
    @fechaInicio                  DATE,
    @fechaFin                     DATE,
    @estadoCampania               NVARCHAR(20),
    @publicoObjetivo              NVARCHAR(1000),
    @idUsuarioInternoResponsable  INT,
    @idsContactos                 NVARCHAR(MAX) = NULL,
    @idsAlianzas                  NVARCHAR(MAX) = NULL,
    @idsContenidos                NVARCHAR(MAX) = NULL,
    @idsOrigenes                  NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @idCampaniaComercial IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.CampaniaComercial WHERE idCampaniaComercial = @idCampaniaComercial AND activa = 1)
        THROW 51303, 'La campaña no existe o está dada de baja.', 1;

    DECLARE @contactos TABLE (id INT PRIMARY KEY);
    DECLARE @alianzas TABLE (id INT PRIMARY KEY);
    DECLARE @contenidos TABLE (id INT PRIMARY KEY);
    DECLARE @origenes TABLE (id INT PRIMARY KEY);

    INSERT INTO @contactos SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsContactos, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @alianzas SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsAlianzas, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @contenidos SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsContenidos, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @origenes SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsOrigenes, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;

    IF EXISTS (SELECT 1 FROM @contactos l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ContactoComercial ct WHERE ct.idContactoComercial = l.id
                                 AND (ct.activo = 1 OR ct.idCampaniaComercial = @idCampaniaComercial)))
       OR EXISTS (SELECT 1 FROM @alianzas l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.AlianzaEstrategica a WHERE a.idAlianzaEstrategica = l.id
                                 AND ((a.activa = 1 AND a.estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada'))
                                      OR EXISTS (SELECT 1 FROM dbo.CampaniaAlianza ca WHERE ca.idCampaniaComercial = @idCampaniaComercial
                                                 AND ca.idAlianzaEstrategica = a.idAlianzaEstrategica))))
       OR EXISTS (SELECT 1 FROM @contenidos l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ContenidoPromocional cp WHERE cp.idContenidoPromocional = l.id
                                 AND ((cp.activo = 1 AND cp.estadoContenido <> N'DadoDeBaja') OR cp.idCampaniaComercial = @idCampaniaComercial)))
       OR EXISTS (SELECT 1 FROM @origenes l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.OrigenUsuario o WHERE o.idOrigenUsuario = l.id
                                 AND (o.activo = 1 OR o.idCampaniaComercial = @idCampaniaComercial)))
        THROW 51302, 'Un vínculo comercial no está disponible.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @idCampaniaComercial IS NULL
        BEGIN
            INSERT INTO dbo.CampaniaComercial
                (nombre, descripcion, canalDifusion, fechaInicio, fechaFin, estadoCampania, idUsuarioInternoResponsable)
            VALUES
                (@nombre, @descripcion, @canalDifusion, @fechaInicio, @fechaFin, @estadoCampania, @idUsuarioInternoResponsable);

            SET @idCampaniaComercial = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.CampaniaComercial
            SET nombre = @nombre,
                descripcion = @descripcion,
                canalDifusion = @canalDifusion,
                fechaInicio = @fechaInicio,
                fechaFin = @fechaFin,
                estadoCampania = @estadoCampania,
                fechaUltimaModificacion = GETDATE()
            WHERE idCampaniaComercial = @idCampaniaComercial;
        END

        DELETE FROM dbo.CampaniaPublicoObjetivo WHERE idCampaniaComercial = @idCampaniaComercial;
        INSERT INTO dbo.CampaniaPublicoObjetivo (idCampaniaComercial, codigoSegmento)
        SELECT DISTINCT @idCampaniaComercial, LTRIM(RTRIM(value))
        FROM STRING_SPLIT(ISNULL(@publicoObjetivo, N''), N',')
        WHERE LEN(LTRIM(RTRIM(value))) > 0;

        UPDATE dbo.ContactoComercial SET idCampaniaComercial = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial AND idContactoComercial NOT IN (SELECT id FROM @contactos);
        UPDATE ct SET ct.idCampaniaComercial = @idCampaniaComercial, ct.fechaUltimaModificacion = GETDATE()
        FROM dbo.ContactoComercial ct INNER JOIN @contactos l ON l.id = ct.idContactoComercial
        WHERE ISNULL(ct.idCampaniaComercial, 0) <> @idCampaniaComercial;

        DELETE FROM dbo.CampaniaAlianza
        WHERE idCampaniaComercial = @idCampaniaComercial AND idAlianzaEstrategica NOT IN (SELECT id FROM @alianzas);
        INSERT INTO dbo.CampaniaAlianza (idCampaniaComercial, idAlianzaEstrategica)
        SELECT @idCampaniaComercial, l.id FROM @alianzas l
        WHERE NOT EXISTS (SELECT 1 FROM dbo.CampaniaAlianza ca WHERE ca.idCampaniaComercial = @idCampaniaComercial AND ca.idAlianzaEstrategica = l.id);

        UPDATE dbo.ContenidoPromocional SET idCampaniaComercial = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial AND idContenidoPromocional NOT IN (SELECT id FROM @contenidos);
        UPDATE cp SET cp.idCampaniaComercial = @idCampaniaComercial, cp.fechaUltimaModificacion = GETDATE()
        FROM dbo.ContenidoPromocional cp INNER JOIN @contenidos l ON l.id = cp.idContenidoPromocional
        WHERE ISNULL(cp.idCampaniaComercial, 0) <> @idCampaniaComercial;

        UPDATE dbo.OrigenUsuario SET idCampaniaComercial = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial AND idOrigenUsuario NOT IN (SELECT id FROM @origenes);
        UPDATE o SET o.idCampaniaComercial = @idCampaniaComercial, o.fechaUltimaModificacion = GETDATE()
        FROM dbo.OrigenUsuario o INNER JOIN @origenes l ON l.id = o.idOrigenUsuario
        WHERE ISNULL(o.idCampaniaComercial, 0) <> @idCampaniaComercial;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @idCampaniaComercial;
END
GO

-- ===========================================================================
-- Alianza: datos, seguimiento del cambio de estado y vínculos en una sola
-- transacción. THROW 51320 si no existe; 51321 si el responsable no es un
-- usuario interno activo; 51322 si un vínculo nuevo no está disponible.
-- ===========================================================================
IF OBJECT_ID('dbo.sp_AlianzaEstrategica_GuardarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_GuardarVinculos;
GO
IF OBJECT_ID('dbo.sp_AlianzaEstrategica_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AlianzaEstrategica_Guardar;
GO
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
    @idUsuarioInterno             INT,
    @idsContactos                 NVARCHAR(MAX) = NULL,
    @idsCampanias                 NVARCHAR(MAX) = NULL,
    @idsContenidos                NVARCHAR(MAX) = NULL,
    @idsOrigenes                  NVARCHAR(MAX) = NULL
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

    SELECT @idAlianzaEstrategica;
END
GO

-- ===========================================================================
-- Métricas comerciales (CU-004-004).
--   * Contactos convertidos: solo los incorporados con una cuenta de
--     StageUp asociada; el tipo de usuario se aplica al perfil de esa
--     cuenta. Los incorporados sin cuenta se informan aparte.
--   * Campañas: sin período, las campañas en curso marcadas como activas;
--     con período, "campañas con vigencia en el período": vigencia que se
--     cruza con el período y estado actual Activa o Finalizada, sin baja
--     anterior al comienzo del período. Es un cálculo por vigencia y estado
--     actual: como no se guarda el historial de estados, una campaña hoy
--     cancelada o planificada no se cuenta aunque haya estado activa en ese
--     período. Este indicador usa el período y la campaña; no depende del
--     canal de origen ni del tipo de usuario.
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
    DECLARE @conPeriodo BIT = CASE WHEN @desde IS NULL AND @hasta IS NULL THEN 0 ELSE 1 END;

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
    ),
    Incorporados AS
    (
        SELECT ct.idContactoComercial, ct.idUsuarioExterno, uc.perfilUsuario
        FROM dbo.ContactoComercial ct
        INNER JOIN dbo.OrigenUsuario oc ON oc.idOrigenUsuario = ct.idOrigenUsuario
        LEFT JOIN dbo.UsuarioExterno uc ON uc.idUsuarioExterno = ct.idUsuarioExterno
        WHERE ct.activo = 1 AND ct.estadoContacto = N'Incorporado'
          AND (@idCampaniaComercial IS NULL OR ct.idCampaniaComercial = @idCampaniaComercial)
          AND (@canalOrigen IS NULL OR oc.canalOrigen = @canalOrigen)
          AND (@desde IS NULL OR ct.fechaIncorporacion >= @desde)
          AND (@hastaExclusivo IS NULL OR ct.fechaIncorporacion < @hastaExclusivo)
    )
    SELECT
        (SELECT COUNT(*) FROM Usuarios WHERE (@desde IS NULL OR fechaAlta >= @desde) AND (@hastaExclusivo IS NULL OR fechaAlta < @hastaExclusivo)) AS nuevosUsuarios,
        (SELECT COUNT(*) FROM Usuarios WHERE canalOrigen IS NOT NULL
             AND (@desde IS NULL OR fechaAlta >= @desde) AND (@hastaExclusivo IS NULL OR fechaAlta < @hastaExclusivo)) AS usuariosConOrigen,
        (SELECT COUNT(*) FROM dbo.EspacioArtistico e
             INNER JOIN Usuarios g ON g.idUsuarioExterno = e.idUsuarioGestor
             WHERE (@desde IS NULL OR e.fechaAlta >= @desde) AND (@hastaExclusivo IS NULL OR e.fechaAlta < @hastaExclusivo)) AS espaciosIncorporados,
        (SELECT COUNT(*) FROM dbo.CampaniaComercial c
             WHERE (@idCampaniaComercial IS NULL OR c.idCampaniaComercial = @idCampaniaComercial)
               AND ((@conPeriodo = 0 AND c.activa = 1 AND c.estadoCampania = N'Activa')
                    OR (@conPeriodo = 1
                        AND c.estadoCampania IN (N'Activa', N'Finalizada')
                        AND (@desde IS NULL OR c.fechaFin >= @desde)
                        AND (@hasta IS NULL OR c.fechaInicio <= @hasta)
                        AND (c.fechaBaja IS NULL OR @desde IS NULL OR c.fechaBaja >= @desde)))) AS campaniasActivas,
        (SELECT COUNT(*) FROM Incorporados i
             WHERE i.idUsuarioExterno IS NOT NULL AND i.perfilUsuario IS NOT NULL
               AND (@tipoUsuario IS NULL
                    OR (@tipoUsuario = N'Gestor' AND i.perfilUsuario = N'GestorEspacios')
                    OR (@tipoUsuario = N'Solicitante' AND i.perfilUsuario <> N'GestorEspacios'))) AS contactosConvertidos,
        (SELECT COUNT(*) FROM Incorporados i WHERE i.idUsuarioExterno IS NULL OR i.perfilUsuario IS NULL) AS incorporadosSinCuenta,
        (SELECT COUNT(*) FROM dbo.UsuarioExterno) AS totalUsuarios,
        (SELECT COUNT(*) FROM dbo.CampaniaComercial) AS totalCampanias,
        (SELECT COUNT(*) FROM dbo.ContactoComercial) AS totalContactos;
END
GO
