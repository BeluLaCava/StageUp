IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.EnvioNewsletter', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/85_NewsletterCorrecciones.sql.', 1;
END
GO

-- ===========================================================================
-- Ayuda (CU-003-001): la búsqueda pública devuelve las mismas preguntas
-- frecuentes que se pueden consultar (publicadas y de una categoría activa).
-- Cada resultado abre su pregunta y respuesta en el centro de ayuda.
-- ===========================================================================
IF OBJECT_ID('dbo.sp_Busqueda_Publica', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Busqueda_Publica;
GO
CREATE PROCEDURE dbo.sp_Busqueda_Publica
    @texto              NVARCHAR(300) = NULL,
    @incluirEspacios    BIT = 1,
    @incluirNovedades   BIT = 1,
    @incluirFaq         BIT = 1,
    @fechaDesde         DATETIME = NULL,
    @fechaHasta         DATETIME = NULL,
    @ubicacion          NVARCHAR(100) = NULL,
    @capacidadMinima    INT = NULL,
    @precioMaximo       DECIMAL(18, 2) = NULL,
    @categoriaNovedad   NVARCHAR(100) = NULL,
    @orden              NVARCHAR(20) = N'Relevancia',
    @maximo             INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @patron NVARCHAR(310) = N'%' + ISNULL(@texto, N'') + N'%';
    DECLARE @patronUbicacion NVARCHAR(110) = N'%' + ISNULL(@ubicacion, N'') + N'%';

    CREATE TABLE #resultados
    (
        tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(400), detalle NVARCHAR(1000),
        fecha DATETIME NULL, prioridad INT
    );

    -- Con capacidad, precio o ubicación se buscan solo espacios (son
    -- criterios que no tienen sentido en novedades ni en la ayuda).
    DECLARE @soloEspacios BIT = CASE WHEN @capacidadMinima IS NOT NULL OR @precioMaximo IS NOT NULL OR @ubicacion IS NOT NULL THEN 1 ELSE 0 END;

    IF @incluirEspacios = 1
        INSERT INTO #resultados
        SELECT N'Espacio', e.idEspacioArtistico, e.nombreEspacio,
               e.tipoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N'')
                 + ISNULL(N' · Hasta ' + CAST(f.capacidadMaxima AS NVARCHAR(10)) + N' personas', N'')
                 + ISNULL(N' · ' + ISNULL(f.moneda, N'ARS') + N' ' + CONVERT(NVARCHAR(30), f.precioHora) + N' por hora', N'')
                 + ISNULL(N' · ' + LEFT(e.descripcion, 160), N''),
               e.fechaPublicacion,
               CASE WHEN @texto IS NOT NULL AND e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.EspacioArtistico e
        LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE e.activo = 1 AND e.publicado = 1
          AND (@texto IS NULL OR e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron
               OR f.ciudad LIKE @patron OR f.provincia LIKE @patron)
          AND (@ubicacion IS NULL OR f.ciudad LIKE @patronUbicacion OR f.provincia LIKE @patronUbicacion OR f.direccion LIKE @patronUbicacion)
          AND (@capacidadMinima IS NULL OR f.capacidadMaxima >= @capacidadMinima)
          AND (@precioMaximo IS NULL OR f.precioHora <= @precioMaximo)
          AND (@fechaDesde IS NULL OR e.fechaPublicacion >= @fechaDesde)
          AND (@fechaHasta IS NULL OR e.fechaPublicacion < DATEADD(DAY, 1, @fechaHasta));

    IF @incluirNovedades = 1 AND @soloEspacios = 0
        INSERT INTO #resultados
        SELECT N'Novedad', n.idNovedad, n.titulo, n.categoria + N' · ' + n.resumen,
               ISNULL(n.fechaPublicacion, n.fechaAlta),
               CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.vw_Novedad n
        WHERE n.estado = N'Vigente'
          AND (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron)
          AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad)
          AND (@fechaDesde IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) >= @fechaDesde)
          AND (@fechaHasta IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) < DATEADD(DAY, 1, @fechaHasta));

    IF @incluirFaq = 1 AND @soloEspacios = 0 AND @categoriaNovedad IS NULL
        INSERT INTO #resultados
        SELECT N'FAQ', q.idFaq, q.pregunta, LEFT(q.respuesta, 300), NULL,
               CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.Faq q
        INNER JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = q.idCategoriaAyuda AND c.activa = 1
        WHERE q.estado = N'Activo'
          AND (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron);

    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion
    FROM
    (
        SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY
                   CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC,
                   CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC,
                   CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC,
                   r.fecha DESC, r.titulo ASC) AS posicion
        FROM #resultados r
    ) ordenados
    WHERE posicion <= @maximo
    ORDER BY tipo, posicion;
END
GO

-- ===========================================================================
-- Encuestas (ítem 3): una respuesta por navegador también al cambiar de
-- cuenta, y auditoría en la misma transacción que la respuesta.
-- ===========================================================================
-- "Ya respondida" (la misma regla para mostrar, listar y guardar): por la
-- cuenta, o desde ese navegador con cualquier cuenta o sin sesión.
IF OBJECT_ID('dbo.sp_RespuestaEncuesta_ExisteDeUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_ExisteDeUsuario;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_ExisteDeUsuario
    @idEncuesta             INT,
    @idUsuarioExterno       INT = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.RespuestaEncuesta r
        WHERE r.idEncuesta = @idEncuesta
          AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
               OR (@identificadorAnonimo IS NOT NULL
                   AND (r.identificadorAnonimo = @identificadorAnonimo OR r.identificadorNavegador = @identificadorAnonimo)))
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS existeRespuesta;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ListarPendientesParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarPendientesParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarPendientesParaUsuario
    @idUsuarioExterno       INT = NULL,
    @perfilUsuario          NVARCHAR(30) = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.estado = N'Activa'
      AND e.fechaInicio <= GETDATE()
      AND e.fechaVencimiento >= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR (@idUsuarioExterno IS NOT NULL AND e.publicoObjetivo = @perfilUsuario))
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.RespuestaEncuesta r
          WHERE r.idEncuesta = e.idEncuesta
            AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
                 OR (@identificadorAnonimo IS NOT NULL
                     AND (r.identificadorAnonimo = @identificadorAnonimo OR r.identificadorNavegador = @identificadorAnonimo)))
      )
    ORDER BY e.fechaVencimiento ASC;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ListarConResultadosParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario
    @idUsuarioExterno       INT = NULL,
    @perfilUsuario          NVARCHAR(30) = NULL,
    @identificadorAnonimo   CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.estado IN (N'Activa', N'Cerrada')
      AND e.fechaInicio <= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR (@idUsuarioExterno IS NOT NULL AND e.publicoObjetivo = @perfilUsuario))
      AND
      (
          e.estado = N'Cerrada'
          OR e.fechaVencimiento < GETDATE()
          OR EXISTS
          (
              SELECT 1 FROM dbo.RespuestaEncuesta r
              WHERE r.idEncuesta = e.idEncuesta
                AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
                     OR (@identificadorAnonimo IS NOT NULL
                         AND (r.identificadorAnonimo = @identificadorAnonimo OR r.identificadorNavegador = @identificadorAnonimo)))
          )
      )
    ORDER BY e.fechaVencimiento DESC;
END
GO

-- ===========================================================================
-- Registro completo de una respuesta: cabecera y todas las opciones elegidas
-- en una sola transacción (todo o nada).
--   @opciones: pares "idPregunta:idOpcion" separados por coma, uno por cada
--   pregunta de la encuesta.
--   @identificadorNavegador: hash del navegador, obligatorio con o sin sesión
--   (sin sesión se guarda como identificadorAnonimo).
--   @descripcionBitacora: la auditoría se registra dentro de la misma
--   transacción, así no puede quedar una respuesta sin su registro.
-- THROW 51060 sin identificador del navegador; 51061 la encuesta no admite
-- la respuesta; 51062 ya respondida (por la cuenta o desde ese navegador,
-- con cualquier cuenta o sin sesión); 51063 las opciones no corresponden a
-- las preguntas de la encuesta.
-- ===========================================================================
IF OBJECT_ID('dbo.sp_RespuestaEncuesta_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RespuestaEncuesta_Registrar;
GO
CREATE PROCEDURE dbo.sp_RespuestaEncuesta_Registrar
    @idEncuesta               INT,
    @idUsuarioExterno         INT = NULL,
    @identificadorNavegador   CHAR(64) = NULL,
    @opciones                 NVARCHAR(MAX),
    @descripcionBitacora      NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @identificadorNavegador IS NULL
        THROW 51060, 'La respuesta no tiene identificador del navegador.', 1;

    DECLARE @elegidas TABLE (idPregunta INT NULL, idOpcion INT NULL);
    INSERT INTO @elegidas (idPregunta, idOpcion)
    SELECT TRY_CAST(LEFT(value, NULLIF(CHARINDEX(N':', value), 0) - 1) AS INT),
           TRY_CAST(SUBSTRING(value, NULLIF(CHARINDEX(N':', value), 0) + 1, 20) AS INT)
    FROM STRING_SPLIT(ISNULL(@opciones, N''), N',')
    WHERE LEN(value) > 0;

    -- Una opción por pregunta, todas las preguntas, y cada opción de su pregunta.
    IF (SELECT COUNT(*) FROM @elegidas) <> (SELECT COUNT(*) FROM dbo.PreguntaEncuesta WHERE idEncuesta = @idEncuesta)
       OR (SELECT COUNT(*) FROM @elegidas) = 0
       OR EXISTS (SELECT 1 FROM @elegidas WHERE idPregunta IS NULL OR idOpcion IS NULL)
       OR (SELECT COUNT(DISTINCT idPregunta) FROM @elegidas) <> (SELECT COUNT(*) FROM @elegidas)
       OR EXISTS (SELECT 1 FROM @elegidas l
                  WHERE NOT EXISTS (SELECT 1 FROM dbo.OpcionPregunta o
                                    INNER JOIN dbo.PreguntaEncuesta p ON p.idPreguntaEncuesta = o.idPreguntaEncuesta
                                    WHERE o.idOpcionPregunta = l.idOpcion AND p.idPreguntaEncuesta = l.idPregunta
                                      AND p.idEncuesta = @idEncuesta))
        THROW 51063, 'Las opciones no corresponden a las preguntas de la encuesta.', 1;

    DECLARE @idRespuestaEncuesta INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Encuesta
            WHERE idEncuesta = @idEncuesta
              AND estado = N'Activa'
              AND fechaInicio <= GETDATE()
              AND fechaVencimiento >= GETDATE()
              AND (@idUsuarioExterno IS NOT NULL OR publicoObjetivo = N'Todos')
        )
            THROW 51061, 'La encuesta no admite esta respuesta.', 1;

        -- Con UPDLOCK y HOLDLOCK, dos envíos simultáneos desde la misma cuenta
        -- o el mismo navegador no pasan los dos este control.
        IF EXISTS
        (
            SELECT 1 FROM dbo.RespuestaEncuesta r WITH (UPDLOCK, HOLDLOCK)
            WHERE r.idEncuesta = @idEncuesta
              AND ((@idUsuarioExterno IS NOT NULL AND r.idUsuarioExterno = @idUsuarioExterno)
                   OR r.identificadorAnonimo = @identificadorNavegador
                   OR r.identificadorNavegador = @identificadorNavegador)
        )
            THROW 51062, 'La encuesta ya fue respondida.', 1;

        INSERT INTO dbo.RespuestaEncuesta (idEncuesta, idUsuarioExterno, identificadorAnonimo, identificadorNavegador, fechaRespuesta)
        VALUES (@idEncuesta, @idUsuarioExterno,
                CASE WHEN @idUsuarioExterno IS NULL THEN @identificadorNavegador END,
                CASE WHEN @idUsuarioExterno IS NOT NULL THEN @identificadorNavegador END,
                GETDATE());

        SET @idRespuestaEncuesta = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO dbo.RespuestaEncuestaDetalle (idRespuestaEncuesta, idPreguntaEncuesta, idOpcionPregunta)
        SELECT @idRespuestaEncuesta, idPregunta, idOpcion FROM @elegidas;

        EXEC dbo.sp_RegistroActividad_Insertar
            @idUsuarioExternoResponsable = @idUsuarioExterno,
            @tipoOperacion = N'ASOCIACION',
            @tipoEntidadAfectada = N'Encuesta',
            @idEntidadAfectada = @idEncuesta,
            @descripcionOperacion = @descripcionBitacora,
            @origenOperacion = N'StageUp.UI';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @idRespuestaEncuesta AS idRespuestaEncuesta;
END
GO

-- ===========================================================================
-- Comercialización (CU-004-001 y CU-004-003): la auditoría de campañas y
-- alianzas se registra dentro de la transacción del guardado.
-- ===========================================================================
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
    @idsOrigenes                  NVARCHAR(MAX) = NULL,
    @idUsuarioInternoBitacora     INT = NULL,
    @descripcionBitacora          NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @operacionBitacora NVARCHAR(20) = CASE WHEN @idCampaniaComercial IS NULL THEN N'ALTA' ELSE N'MODIFICACION' END;

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

        -- Auditoría en la misma transacción: no queda un cambio guardado sin
        -- su registro, ni un registro de algo que no se guardó.
        IF @descripcionBitacora IS NOT NULL
            EXEC dbo.sp_RegistroActividad_Insertar
                @idUsuarioInternoResponsable = @idUsuarioInternoBitacora,
                @tipoOperacion = @operacionBitacora,
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

    SELECT @idCampaniaComercial;
END
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
    @idsOrigenes                  NVARCHAR(MAX) = NULL,
    @descripcionBitacora          NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @operacionBitacora NVARCHAR(20) = CASE WHEN @idAlianzaEstrategica IS NULL THEN N'ALTA' ELSE N'MODIFICACION' END;

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

        -- Auditoría en la misma transacción (ver campañas).
        IF @descripcionBitacora IS NOT NULL
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

    SELECT @idAlianzaEstrategica;
END
GO

-- ===========================================================================
-- Newsletter (ítem 13).
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- Suscripciones de origen Cuenta sin evidencia de consentimiento.
-- La versión anterior de sp_Newsletter_ListarDestinatarios las creaba
-- activas sin pedido de la persona (y el aviso "Ya estás suscripto" también
-- completaba fechaUltimoEnvioConfirmacion, así que esa fecha no prueba nada).
-- La evidencia real es la confirmación con el enlace del correo o la
-- reactivación desde el enlace de preferencias, que quedan en la bitácora.
-- Las que no la tienen pasan a "sin confirmar" (no reciben envíos) sin
-- borrarlas: se conserva el registro y su historial, y queda anotado en la
-- bitácora. La administración puede reenviarles la confirmación.
-- ---------------------------------------------------------------------------
DECLARE @sinConsentimiento TABLE (idSuscriptorNewsletter INT PRIMARY KEY);

UPDATE s
SET estado = N'Pendiente',
    hashTokenConfirmacion = NULL,
    vencimientoConfirmacion = NULL
OUTPUT inserted.idSuscriptorNewsletter INTO @sinConsentimiento
FROM dbo.SuscriptorNewsletter s
WHERE s.origen = N'Cuenta'
  AND s.estado = N'Activo'
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.RegistroActividad r
      WHERE r.tipoEntidadAfectada = N'SuscriptorNewsletter'
        AND r.idEntidadAfectada = s.idSuscriptorNewsletter
        AND r.descripcionOperacion IN (N'Suscripción al newsletter confirmada.',
                                       N'Nueva suscripción al newsletter desde el enlace de preferencias.')
  );

INSERT INTO dbo.RegistroActividad
    (idUsuarioExternoResponsable, idUsuarioInternoResponsable, tipoOperacion, tipoEntidadAfectada, idEntidadAfectada,
     descripcionOperacion, fechaOperacion, origenOperacion)
SELECT NULL, NULL, N'MODIFICACION', N'SuscriptorNewsletter', idSuscriptorNewsletter,
       N'Suscripción creada automáticamente y sin confirmación de la persona: pasa a pendiente de confirmación y deja de recibir envíos.',
       GETDATE(), N'Script 86'
FROM @sinConsentimiento;

DECLARE @cantidadSinConsentimiento INT = (SELECT COUNT(*) FROM @sinConsentimiento);
PRINT CONCAT(N'Suscripciones sin consentimiento verificable pasadas a pendientes: ', @cantidadSinConsentimiento);
GO

-- ---------------------------------------------------------------------------
-- Vinculación verificada con una cuenta (Mi perfil). La cuenta pide el
-- vínculo; el dueño del correo lo confirma con el enlace que recibe.
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.SuscriptorNewsletter', N'idUsuarioExternoVinculoPendiente') IS NULL
    ALTER TABLE dbo.SuscriptorNewsletter ADD
        idUsuarioExternoVinculoPendiente INT NULL,
        hashTokenVinculo                 CHAR(64) NULL,
        vencimientoVinculo               DATETIME NULL;
GO

IF OBJECT_ID(N'dbo.FK_SuscriptorNewsletter_UsuarioVinculo', N'F') IS NULL
    ALTER TABLE dbo.SuscriptorNewsletter ADD CONSTRAINT FK_SuscriptorNewsletter_UsuarioVinculo
        FOREIGN KEY (idUsuarioExternoVinculoPendiente) REFERENCES dbo.UsuarioExterno (idUsuarioExterno);
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_Obtener;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_Obtener
    @correoElectronico      NVARCHAR(254) = NULL,
    @tokenPreferencias      VARCHAR(64) = NULL,
    @hashTokenConfirmacion  CHAR(64) = NULL,
    @hashTokenVinculo       CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.idSuscriptorNewsletter, s.correoElectronico, s.nombre, s.idUsuarioExterno, s.estado, s.origen,
           s.vencimientoConfirmacion, s.fechaUltimoEnvioConfirmacion, s.tokenPreferencias, s.fechaAlta,
           s.fechaConfirmacion, s.fechaBaja, s.idUsuarioExternoVinculoPendiente, s.vencimientoVinculo,
           STUFF((SELECT N',' + c.categoria FROM dbo.SuscriptorNewsletterCategoria c
                  WHERE c.idSuscriptorNewsletter = s.idSuscriptorNewsletter ORDER BY c.categoria
                  FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(400)'), 1, 1, N'') AS categorias
    FROM dbo.SuscriptorNewsletter s
    WHERE (@correoElectronico IS NOT NULL AND s.correoElectronico = @correoElectronico)
       OR (@tokenPreferencias IS NOT NULL AND s.tokenPreferencias = @tokenPreferencias)
       OR (@hashTokenConfirmacion IS NOT NULL AND s.hashTokenConfirmacion = @hashTokenConfirmacion)
       OR (@hashTokenVinculo IS NOT NULL AND s.hashTokenVinculo = @hashTokenVinculo);
END
GO

IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_SolicitarVinculo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_SolicitarVinculo;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_SolicitarVinculo
    @idSuscriptorNewsletter  INT,
    @idUsuarioExterno        INT,
    @hashTokenVinculo        CHAR(64),
    @vencimientoVinculo      DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SuscriptorNewsletter
    SET idUsuarioExternoVinculoPendiente = @idUsuarioExterno,
        hashTokenVinculo = @hashTokenVinculo,
        vencimientoVinculo = @vencimientoVinculo,
        fechaUltimoEnvioConfirmacion = GETDATE()
    WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter AND estado = N'Activo';

    SELECT @@ROWCOUNT;
END
GO

-- Confirma el vínculo con el token vigente. La misma suscripción (no se
-- crea otra) queda asociada a la cuenta que lo pidió.
IF OBJECT_ID('dbo.sp_SuscriptorNewsletter_ConfirmarVinculo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SuscriptorNewsletter_ConfirmarVinculo;
GO
CREATE PROCEDURE dbo.sp_SuscriptorNewsletter_ConfirmarVinculo
    @idSuscriptorNewsletter  INT,
    @hashTokenVinculo        CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SuscriptorNewsletter
    SET idUsuarioExterno = idUsuarioExternoVinculoPendiente,
        idUsuarioExternoVinculoPendiente = NULL,
        hashTokenVinculo = NULL,
        vencimientoVinculo = NULL
    WHERE idSuscriptorNewsletter = @idSuscriptorNewsletter
      AND estado = N'Activo'
      AND hashTokenVinculo = @hashTokenVinculo
      AND vencimientoVinculo >= GETDATE()
      AND idUsuarioExternoVinculoPendiente IS NOT NULL;

    SELECT @@ROWCOUNT;
END
GO

-- ---------------------------------------------------------------------------
-- Envíos: resultado incierto y reintento cuando no hubo destinatarios.
--   * Antes de mandar cada correo el destinatario queda "Enviando". Si el
--     correo salió pero no se pudo registrar el resultado, queda así: es un
--     resultado incierto. No se reintenta solo (podría duplicarse); la
--     administración decide si reenviarle.
--   * Un envío sin destinatarios no marca la novedad como enviada: se puede
--     volver a intentar más adelante y el intento queda en el historial.
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.EnvioNewsletter', N'cantidadInciertos') IS NULL
    ALTER TABLE dbo.EnvioNewsletter ADD cantidadInciertos INT NOT NULL CONSTRAINT DF_EnvioNewsletter_inc DEFAULT (0);
GO

IF OBJECT_ID(N'dbo.CK_EnvioNewsletter_Estado', N'C') IS NOT NULL
    ALTER TABLE dbo.EnvioNewsletter DROP CONSTRAINT CK_EnvioNewsletter_Estado;
GO
ALTER TABLE dbo.EnvioNewsletter ADD CONSTRAINT CK_EnvioNewsletter_Estado
    CHECK (estado IN (N'EnCurso', N'Completo', N'Parcial', N'SinDestinatarios', N'Fallido', N'Incierto'));
GO

IF OBJECT_ID(N'dbo.CK_EnvioNewsletterDestinatario_Estado', N'C') IS NOT NULL
    ALTER TABLE dbo.EnvioNewsletterDestinatario DROP CONSTRAINT CK_EnvioNewsletterDestinatario_Estado;
GO
ALTER TABLE dbo.EnvioNewsletterDestinatario ADD CONSTRAINT CK_EnvioNewsletterDestinatario_Estado
    CHECK (estado IN (N'Pendiente', N'Enviando', N'Enviado', N'Fallido', N'Omitido'));
GO

-- Un único envío "real" por novedad; los intentos sin destinatarios no cuentan.
IF OBJECT_ID(N'dbo.UQ_EnvioNewsletter_Novedad', N'UQ') IS NOT NULL
    ALTER TABLE dbo.EnvioNewsletter DROP CONSTRAINT UQ_EnvioNewsletter_Novedad;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EnvioNewsletter_Novedad' AND object_id = OBJECT_ID(N'dbo.EnvioNewsletter'))
    CREATE UNIQUE INDEX UX_EnvioNewsletter_Novedad ON dbo.EnvioNewsletter (idNovedad) WHERE estado <> N'SinDestinatarios';
GO

-- Novedades marcadas como enviadas solo por un intento sin destinatarios.
UPDATE n
SET enviadaPorCorreo = 0,
    destinatarioNewsletter = NULL,
    fechaEnvioNewsletter = NULL
FROM dbo.Novedad n
WHERE n.enviadaPorCorreo = 1
  AND EXISTS (SELECT 1 FROM dbo.EnvioNewsletter e WHERE e.idNovedad = n.idNovedad AND e.estado = N'SinDestinatarios')
  AND NOT EXISTS (SELECT 1 FROM dbo.EnvioNewsletter e WHERE e.idNovedad = n.idNovedad AND e.estado <> N'SinDestinatarios');
GO

IF OBJECT_ID('dbo.sp_EnvioNewsletter_Iniciar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_Iniciar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_Iniciar
    @idNovedad         INT,
    @criterio          NVARCHAR(50),
    @idUsuarioInterno  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @categoria NVARCHAR(100), @enviada BIT;
    SELECT @categoria = categoria, @enviada = enviadaPorCorreo
    FROM dbo.Novedad WITH (UPDLOCK, HOLDLOCK)
    WHERE idNovedad = @idNovedad AND activo = 1;

    IF @categoria IS NULL
        THROW 51080, N'No se encontró la novedad seleccionada.', 1;

    IF @enviada = 1 OR EXISTS (SELECT 1 FROM dbo.EnvioNewsletter WITH (UPDLOCK, HOLDLOCK)
                               WHERE idNovedad = @idNovedad AND estado <> N'SinDestinatarios')
        THROW 51081, N'Esta novedad ya se envió por correo.', 1;

    INSERT INTO dbo.EnvioNewsletter (idNovedad, criterio, categoria, estado, idUsuarioInterno)
    VALUES (@idNovedad, @criterio, @categoria, N'EnCurso', @idUsuarioInterno);

    DECLARE @idEnvio INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.EnvioNewsletterDestinatario (idEnvioNewsletter, idSuscriptorNewsletter, correoElectronico, nombre)
    SELECT @idEnvio, d.idSuscriptorNewsletter, d.correoElectronico, d.nombre
    FROM dbo.fn_Newsletter_Destinatarios(@criterio, @categoria) d;

    DECLARE @cantidad INT = @@ROWCOUNT;
    UPDATE dbo.EnvioNewsletter SET cantidadDestinatarios = @cantidad WHERE idEnvioNewsletter = @idEnvio;

    COMMIT TRANSACTION;

    SELECT @idEnvio AS idEnvioNewsletter;
END
GO

-- Antes de mandar el correo. 0 si el destinatario ya no estaba pendiente.
IF OBJECT_ID('dbo.sp_EnvioNewsletter_MarcarEnviando', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_MarcarEnviando;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_MarcarEnviando
    @idEnvioNewsletter       INT,
    @idSuscriptorNewsletter  INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.EnvioNewsletterDestinatario
    SET estado = N'Enviando',
        fechaUltimoIntento = GETDATE()
    WHERE idEnvioNewsletter = @idEnvioNewsletter
      AND idSuscriptorNewsletter = @idSuscriptorNewsletter
      AND estado IN (N'Pendiente', N'Fallido');

    SELECT @@ROWCOUNT;
END
GO

IF OBJECT_ID('dbo.sp_EnvioNewsletter_RegistrarResultado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_RegistrarResultado;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_RegistrarResultado
    @idEnvioNewsletter       INT,
    @idSuscriptorNewsletter  INT,
    @enviado                 BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.EnvioNewsletterDestinatario
    SET estado = CASE WHEN @enviado = 1 THEN N'Enviado' ELSE N'Fallido' END,
        intentos = intentos + 1,
        fechaUltimoIntento = GETDATE(),
        fechaEnvio = CASE WHEN @enviado = 1 THEN GETDATE() ELSE fechaEnvio END
    WHERE idEnvioNewsletter = @idEnvioNewsletter
      AND idSuscriptorNewsletter = @idSuscriptorNewsletter
      AND estado IN (N'Enviando', N'Pendiente', N'Fallido');

    UPDATE dbo.EnvioNewsletter SET fechaUltimaActividad = GETDATE() WHERE idEnvioNewsletter = @idEnvioNewsletter;
END
GO

-- Cierra el envío:
--   Completo: todos lo recibieron. Parcial: algunos fallaron (se reintentan
--   solo esos). Fallido: no llegó a nadie. Incierto: no quedan fallidos pero
--   hay destinatarios con resultado incierto. SinDestinatarios: no había a
--   quién enviarlo (no marca la novedad como enviada).
IF OBJECT_ID('dbo.sp_EnvioNewsletter_Finalizar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_Finalizar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_Finalizar
    @idEnvioNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @total INT, @enviados INT, @fallidos INT, @omitidos INT, @inciertos INT;
    SELECT @total = COUNT(*),
           @enviados = ISNULL(SUM(CASE WHEN estado = N'Enviado' THEN 1 ELSE 0 END), 0),
           @fallidos = ISNULL(SUM(CASE WHEN estado IN (N'Fallido', N'Pendiente') THEN 1 ELSE 0 END), 0),
           @omitidos = ISNULL(SUM(CASE WHEN estado = N'Omitido' THEN 1 ELSE 0 END), 0),
           @inciertos = ISNULL(SUM(CASE WHEN estado = N'Enviando' THEN 1 ELSE 0 END), 0)
    FROM dbo.EnvioNewsletterDestinatario
    WHERE idEnvioNewsletter = @idEnvioNewsletter;

    DECLARE @estado NVARCHAR(20) =
        CASE WHEN @enviados = 0 AND @fallidos = 0 AND @inciertos = 0 THEN N'SinDestinatarios'
             WHEN @fallidos > 0 AND @enviados = 0 AND @inciertos = 0 THEN N'Fallido'
             WHEN @fallidos > 0 THEN N'Parcial'
             WHEN @inciertos > 0 THEN N'Incierto'
             ELSE N'Completo' END;

    BEGIN TRANSACTION;

    UPDATE dbo.EnvioNewsletter
    SET estado = @estado,
        cantidadDestinatarios = @total,
        cantidadEnviados = @enviados,
        cantidadFallidos = @fallidos,
        cantidadOmitidos = @omitidos,
        cantidadInciertos = @inciertos,
        fechaUltimaActividad = GETDATE(),
        fechaFin = GETDATE()
    WHERE idEnvioNewsletter = @idEnvioNewsletter;

    IF @estado NOT IN (N'Fallido', N'SinDestinatarios')
        UPDATE n
        SET enviadaPorCorreo = 1,
            destinatarioNewsletter = e.criterio,
            fechaEnvioNewsletter = ISNULL(n.fechaEnvioNewsletter, GETDATE()),
            fechaUltimaModificacion = GETDATE()
        FROM dbo.Novedad n
        JOIN dbo.EnvioNewsletter e ON e.idNovedad = n.idNovedad
        WHERE e.idEnvioNewsletter = @idEnvioNewsletter;

    COMMIT TRANSACTION;

    SELECT @estado AS estado, @total AS cantidadDestinatarios, @enviados AS cantidadEnviados,
           @fallidos AS cantidadFallidos, @omitidos AS cantidadOmitidos, @inciertos AS cantidadInciertos;
END
GO

-- La administración decide reenviar a los de resultado incierto: vuelven a
-- quedar como fallidos y el envío queda listo para "Reintentar fallidos".
-- Solo con el envío cerrado (o trabado hace más de 15 minutos).
IF OBJECT_ID('dbo.sp_EnvioNewsletter_ReabrirInciertos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_ReabrirInciertos;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_ReabrirInciertos
    @idEnvioNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.EnvioNewsletter WITH (UPDLOCK, HOLDLOCK)
                   WHERE idEnvioNewsletter = @idEnvioNewsletter
                     AND (estado IN (N'Incierto', N'Parcial', N'Fallido')
                          OR (estado = N'EnCurso' AND fechaUltimaActividad < DATEADD(MINUTE, -15, GETDATE()))))
    BEGIN
        COMMIT TRANSACTION;
        SELECT 0;
        RETURN;
    END

    UPDATE dbo.EnvioNewsletterDestinatario SET estado = N'Fallido'
    WHERE idEnvioNewsletter = @idEnvioNewsletter AND estado = N'Enviando';

    DECLARE @reabiertos INT = @@ROWCOUNT;

    UPDATE dbo.EnvioNewsletter
    SET estado = CASE WHEN estado = N'Incierto' THEN N'Parcial' ELSE estado END,
        cantidadFallidos = cantidadFallidos + @reabiertos,
        cantidadInciertos = 0
    WHERE idEnvioNewsletter = @idEnvioNewsletter AND @reabiertos > 0;

    COMMIT TRANSACTION;

    SELECT @reabiertos;
END
GO

IF OBJECT_ID('dbo.sp_EnvioNewsletter_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EnvioNewsletter_Listar;
GO
CREATE PROCEDURE dbo.sp_EnvioNewsletter_Listar
    @idNovedad  INT = NULL,
    @maximo     INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (CASE WHEN @maximo BETWEEN 1 AND 500 THEN @maximo ELSE 100 END)
           e.idEnvioNewsletter, e.idNovedad, n.titulo AS tituloNovedad, e.criterio, e.categoria, e.estado,
           e.cantidadDestinatarios, e.cantidadEnviados, e.cantidadFallidos, e.cantidadOmitidos, e.cantidadInciertos,
           e.fechaInicio, e.fechaUltimaActividad, e.fechaFin,
           LTRIM(RTRIM(ISNULL(ui.nombre, N'') + N' ' + ISNULL(ui.apellido, N''))) AS responsable
    FROM dbo.EnvioNewsletter e
    JOIN dbo.Novedad n ON n.idNovedad = e.idNovedad
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = e.idUsuarioInterno
    WHERE @idNovedad IS NULL OR e.idNovedad = @idNovedad
    ORDER BY e.fechaInicio DESC, e.idEnvioNewsletter DESC;
END
GO
