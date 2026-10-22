IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ===========================================================================
-- Más correcciones y datos de demostración.
--
-- Este script se completará de manera incremental con los datos necesarios
-- para la presentación. Cada bloque debe poder ejecutarse más de una vez sin
-- duplicar información ni sobrescribir datos cargados posteriormente desde la
-- aplicación.
--
-- Primera etapa:
--   1. Completar campos vacíos de los perfiles de Ana y Marcos.
--   2. Ampliar las galerías de los cinco espacios del Marcos de demostración.
-- ===========================================================================

IF OBJECT_ID(N'dbo.UsuarioExterno', N'U') IS NULL
   OR OBJECT_ID(N'dbo.EspacioArtistico', N'U') IS NULL
   OR OBJECT_ID(N'dbo.EspacioFoto', N'U') IS NULL
BEGIN
    THROW 51700, 'Primero deben ejecutarse los scripts anteriores de StageUp.', 1;
END
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @correoAna NVARCHAR(320) = N'ana.solicitante@stageup.test';
    DECLARE @correoMarcos NVARCHAR(320) = N'marcos.gestor@stageup.test';
    DECLARE @idAna INT;
    DECLARE @idMarcos INT;

    IF (SELECT COUNT(*) FROM dbo.UsuarioExterno WHERE correoElectronico = @correoAna) <> 1
        THROW 51701, 'No se encontró una única cuenta de demostración para Ana.', 1;

    IF (SELECT COUNT(*) FROM dbo.UsuarioExterno WHERE correoElectronico = @correoMarcos) <> 1
        THROW 51702, 'No se encontró una única cuenta de demostración para Marcos.', 1;

    SELECT @idAna = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = @correoAna;

    SELECT @idMarcos = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = @correoMarcos;

    -- Los valores se completan únicamente cuando el campo sigue vacío. Si la
    -- persona luego edita su perfil desde la web, una nueva ejecución de este
    -- script conserva esa información.
    UPDATE dbo.UsuarioExterno
    SET telefono = CASE
                       WHEN NULLIF(LTRIM(RTRIM(telefono)), N'') IS NULL
                           THEN N'+54 9 11 5555-0182'
                       ELSE telefono
                   END,
        fotoPerfilRuta = CASE
                             WHEN NULLIF(LTRIM(RTRIM(fotoPerfilRuta)), N'') IS NULL
                                 THEN N'~/Content/Uploads/Perfiles/ana-solicitante-demo.jpg'
                             ELSE fotoPerfilRuta
                         END,
        descripcionPerfil = CASE
                                WHEN NULLIF(LTRIM(RTRIM(descripcionPerfil)), N'') IS NULL
                                    THEN N'Productora cultural independiente y bailarina aficionada. Organiza ensayos, sesiones de contenido y encuentros creativos en Buenos Aires. Valora los espacios cuidados, la comunicación clara y el cumplimiento de los horarios.'
                                ELSE descripcionPerfil
                            END,
        fechaUltimaModificacion = GETDATE()
    WHERE idUsuarioExterno = @idAna
      AND
      (
          NULLIF(LTRIM(RTRIM(telefono)), N'') IS NULL
          OR NULLIF(LTRIM(RTRIM(fotoPerfilRuta)), N'') IS NULL
          OR NULLIF(LTRIM(RTRIM(descripcionPerfil)), N'') IS NULL
      );

    UPDATE dbo.UsuarioExterno
    SET telefono = CASE
                       WHEN NULLIF(LTRIM(RTRIM(telefono)), N'') IS NULL
                           THEN N'+54 9 11 5555-0147'
                       ELSE telefono
                   END,
        fotoPerfilRuta = CASE
                             WHEN NULLIF(LTRIM(RTRIM(fotoPerfilRuta)), N'') IS NULL
                                 THEN N'~/Content/Uploads/Perfiles/marcos-gestor-demo.jpg'
                             ELSE fotoPerfilRuta
                         END,
        descripcionPerfil = CASE
                                WHEN NULLIF(LTRIM(RTRIM(descripcionPerfil)), N'') IS NULL
                                    THEN N'Gestor cultural y músico aficionado de Buenos Aires. Coordina espacios para ensayos, grabaciones y producciones, y también utiliza StageUp para encontrar locaciones cuando participa en proyectos de otros equipos.'
                                ELSE descripcionPerfil
                            END,
        fechaUltimaModificacion = GETDATE()
    WHERE idUsuarioExterno = @idMarcos
      AND
      (
          NULLIF(LTRIM(RTRIM(telefono)), N'') IS NULL
          OR NULLIF(LTRIM(RTRIM(fotoPerfilRuta)), N'') IS NULL
          OR NULLIF(LTRIM(RTRIM(descripcionPerfil)), N'') IS NULL
      );

    DECLARE @idSalaPrincipal INT;
    DECLARE @idEstudioFotografico INT;
    DECLARE @idTeatroColon INT;
    DECLARE @idSalaEnsayo INT;
    DECLARE @idEstudioSonoro INT;

    SELECT @idSalaPrincipal = idEspacioArtistico
    FROM dbo.EspacioArtistico
    WHERE idUsuarioGestor = @idMarcos AND nombreEspacio = N'Sala Principal StageUp' AND activo = 1;

    SELECT @idEstudioFotografico = idEspacioArtistico
    FROM dbo.EspacioArtistico
    WHERE idUsuarioGestor = @idMarcos AND nombreEspacio = N'Estudio Fotográfico Norte' AND activo = 1;

    SELECT @idTeatroColon = idEspacioArtistico
    FROM dbo.EspacioArtistico
    WHERE idUsuarioGestor = @idMarcos AND nombreEspacio = N'Teatro Colón' AND activo = 1;

    SELECT @idSalaEnsayo = idEspacioArtistico
    FROM dbo.EspacioArtistico
    WHERE idUsuarioGestor = @idMarcos AND nombreEspacio = N'Sala Ensayo Palermo' AND activo = 1;

    SELECT @idEstudioSonoro = idEspacioArtistico
    FROM dbo.EspacioArtistico
    WHERE idUsuarioGestor = @idMarcos AND nombreEspacio = N'Estudio Sonoro Colegiales' AND activo = 1;

    IF @idSalaPrincipal IS NULL
       OR @idEstudioFotografico IS NULL
       OR @idTeatroColon IS NULL
       OR @idSalaEnsayo IS NULL
       OR @idEstudioSonoro IS NULL
    BEGIN
        THROW 51703, 'No se encontraron los cinco espacios activos del Marcos de demostración.', 1;
    END

    DECLARE @Fotos TABLE
    (
        idEspacioArtistico INT NOT NULL,
        rutaFoto NVARCHAR(600) NOT NULL,
        secuencia INT NOT NULL
    );

    INSERT INTO @Fotos (idEspacioArtistico, rutaFoto, secuencia)
    VALUES
        (@idSalaPrincipal, N'~/Content/Uploads/Espacios/sala-principal-stageup-vista-lateral.jpg', 1),
        (@idSalaPrincipal, N'~/Content/Uploads/Espacios/sala-principal-stageup-equipamiento.jpg', 2),
        (@idEstudioFotografico, N'~/Content/Uploads/Espacios/estudio-fotografico-norte-vista-general.jpg', 1),
        (@idEstudioFotografico, N'~/Content/Uploads/Espacios/estudio-fotografico-norte-equipamiento.jpg', 2),
        (@idTeatroColon, N'~/Content/Uploads/Espacios/teatro-colon-vista-escenario.jpg', 1),
        (@idTeatroColon, N'~/Content/Uploads/Espacios/teatro-colon-palcos.jpg', 2),
        (@idSalaEnsayo, N'~/Content/Uploads/Espacios/sala-ensayo-palermo-vista-general.jpg', 1),
        (@idSalaEnsayo, N'~/Content/Uploads/Espacios/sala-ensayo-palermo-equipamiento.jpg', 2),
        (@idEstudioSonoro, N'~/Content/Uploads/Espacios/estudio-sonoro-colegiales-vista-general.jpg', 1),
        (@idEstudioSonoro, N'~/Content/Uploads/Espacios/estudio-sonoro-colegiales-equipamiento.jpg', 2);

    ;WITH FotosPendientes AS
    (
        SELECT f.idEspacioArtistico,
               f.rutaFoto,
               ROW_NUMBER() OVER
               (
                   PARTITION BY f.idEspacioArtistico
                   ORDER BY f.secuencia
               ) AS numeroNuevaFoto
        FROM @Fotos f
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.EspacioFoto existente
            WHERE existente.idEspacioArtistico = f.idEspacioArtistico
              AND existente.rutaFoto = f.rutaFoto
        )
    )
    INSERT INTO dbo.EspacioFoto (idEspacioArtistico, rutaFoto, orden, esPrincipal)
    SELECT pendiente.idEspacioArtistico,
           pendiente.rutaFoto,
           ISNULL
           (
               (
                   SELECT MAX(existente.orden)
                   FROM dbo.EspacioFoto existente
                   WHERE existente.idEspacioArtistico = pendiente.idEspacioArtistico
               ),
               0
           ) + pendiente.numeroNuevaFoto,
           0
    FROM FotosPendientes pendiente;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- ===========================================================================
-- Quinta etapa:
--   9. Ordenar las novedades existentes y completar el catálogo público.
--  10. Preparar suscripciones con preferencias distintas para demostrar el
--      filtrado del newsletter, sin iniciar ningún envío de correo.
-- ===========================================================================

IF OBJECT_ID(N'dbo.Novedad', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CategoriaNovedad', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SuscriptorNewsletter', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SuscriptorNewsletterCategoria', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RegistroActividad', N'U') IS NULL
BEGIN
    THROW 51740, 'Primero deben ejecutarse los scripts de novedades y newsletter.', 1;
END
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ahoraEtapa5 DATETIME = GETDATE();
    DECLARE @idNovedadCatalogo INT;
    DECLARE @idNovedadConsejos INT;
    DECLARE @idNovedadComunidad INT;

    IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE codigo = N'Catalogo' AND activa = 1)
       OR NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE codigo = N'Consejos' AND activa = 1)
       OR NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE codigo = N'Comunidad' AND activa = 1)
       OR NOT EXISTS (SELECT 1 FROM dbo.CategoriaNovedad WHERE codigo = N'Institucional' AND activa = 1)
    BEGIN
        THROW 51741, 'No están disponibles todas las categorías requeridas para los datos de demostración.', 1;
    END

    -- La novedad que ya figuraba como enviada conserva el tema del catálogo.
    -- Solo se reemplaza el contenido inicial de demostración; una edición
    -- posterior hecha desde la aplicación no vuelve a ser sobrescrita.
    SELECT TOP (1) @idNovedadCatalogo = idNovedad
    FROM dbo.Novedad
    WHERE activo = 1
      AND titulo = N'Nuevos espacios para crear, ensayar y producir'
    ORDER BY idNovedad;

    IF @idNovedadCatalogo IS NULL
    BEGIN
        SELECT TOP (1) @idNovedadCatalogo = idNovedad
        FROM dbo.Novedad
        WHERE activo = 1
          AND titulo = N'Nuevos espacios'
          AND resumen = N'Nuevos espacios este fin de año!'
        ORDER BY enviadaPorCorreo DESC, idNovedad DESC;
    END

    -- La segunda copia, que no había sido enviada, se aprovecha para una guía
    -- diferente. Esto elimina el duplicado sin borrar el historial.
    SELECT TOP (1) @idNovedadConsejos = idNovedad
    FROM dbo.Novedad
    WHERE activo = 1
      AND titulo = N'Cómo preparar un ensayo sin perder tiempo'
    ORDER BY idNovedad;

    IF @idNovedadConsejos IS NULL
    BEGIN
        SELECT TOP (1) @idNovedadConsejos = idNovedad
        FROM dbo.Novedad
        WHERE activo = 1
          AND idNovedad <> ISNULL(@idNovedadCatalogo, -1)
          AND enviadaPorCorreo = 0
          AND titulo = N'Nuevos espacios'
          AND resumen = N'Nuevos espacios este fin de año!'
        ORDER BY idNovedad;
    END

    IF @idNovedadCatalogo IS NULL
    BEGIN
        INSERT INTO dbo.Novedad
            (titulo, resumen, contenido, categoria, urlImagen, publicado,
             fechaPublicacion, fechaVencimiento, fechaAlta, activo)
        VALUES
            (N'Nuevos espacios para crear, ensayar y producir',
             N'El catálogo de StageUp suma salas y estudios preparados para ensayos, sesiones, grabaciones y producciones de distintos formatos.',
             N'Encontrá nuevas alternativas para cada etapa de tu proyecto: salas de ensayo con equipamiento, estudios fotográficos con iluminación y espacios versátiles para producciones. Revisá la capacidad, los servicios incluidos, las fotografías y la reputación de cada propuesta antes de solicitar una fecha. También podés hacer preguntas públicas al gestor si necesitás confirmar un detalle.',
             N'Catalogo',
             N'~/Content/Uploads/Novedades/newsletter-nuevos-espacios-stageup.png',
             1, DATEADD(DAY, -7, @ahoraEtapa5), DATEADD(DAY, 120, @ahoraEtapa5),
             @ahoraEtapa5, 1);

        SET @idNovedadCatalogo = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Novedad
        SET titulo = N'Nuevos espacios para crear, ensayar y producir',
            resumen = N'El catálogo de StageUp suma salas y estudios preparados para ensayos, sesiones, grabaciones y producciones de distintos formatos.',
            contenido = N'Encontrá nuevas alternativas para cada etapa de tu proyecto: salas de ensayo con equipamiento, estudios fotográficos con iluminación y espacios versátiles para producciones. Revisá la capacidad, los servicios incluidos, las fotografías y la reputación de cada propuesta antes de solicitar una fecha. También podés hacer preguntas públicas al gestor si necesitás confirmar un detalle.',
            categoria = N'Catalogo',
            urlImagen = N'~/Content/Uploads/Novedades/newsletter-nuevos-espacios-stageup.png',
            publicado = 1,
            fechaPublicacion = ISNULL(fechaPublicacion, DATEADD(DAY, -7, @ahoraEtapa5)),
            fechaVencimiento = ISNULL(fechaVencimiento, DATEADD(DAY, 120, @ahoraEtapa5)),
            fechaUltimaModificacion = @ahoraEtapa5
        WHERE idNovedad = @idNovedadCatalogo
          AND titulo = N'Nuevos espacios'
          AND resumen = N'Nuevos espacios este fin de año!';
    END

    IF @idNovedadConsejos IS NULL
    BEGIN
        INSERT INTO dbo.Novedad
            (titulo, resumen, contenido, categoria, urlImagen, publicado,
             fechaPublicacion, fechaVencimiento, fechaAlta, activo)
        VALUES
            (N'Cómo preparar un ensayo sin perder tiempo',
             N'Una lista simple para organizar horarios, necesidades técnicas y dinámica de trabajo antes de llegar al espacio.',
             N'Antes de reservar, definí cuántas personas van a participar, qué equipamiento necesitás y cuánto tiempo real requiere el armado. Compartí con el gestor cualquier necesidad especial, confirmá el horario de ingreso y prepará una lista breve de elementos propios. El día del ensayo, respetar los tiempos acordados y dejar el espacio ordenado ayuda a que la experiencia sea buena para todo el equipo.',
             N'Consejos',
             N'~/Content/Uploads/Novedades/guia-preparar-ensayo-stageup.png',
             1, DATEADD(DAY, -3, @ahoraEtapa5), DATEADD(DAY, 120, @ahoraEtapa5),
             @ahoraEtapa5, 1);

        SET @idNovedadConsejos = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Novedad
        SET titulo = N'Cómo preparar un ensayo sin perder tiempo',
            resumen = N'Una lista simple para organizar horarios, necesidades técnicas y dinámica de trabajo antes de llegar al espacio.',
            contenido = N'Antes de reservar, definí cuántas personas van a participar, qué equipamiento necesitás y cuánto tiempo real requiere el armado. Compartí con el gestor cualquier necesidad especial, confirmá el horario de ingreso y prepará una lista breve de elementos propios. El día del ensayo, respetar los tiempos acordados y dejar el espacio ordenado ayuda a que la experiencia sea buena para todo el equipo.',
            categoria = N'Consejos',
            urlImagen = N'~/Content/Uploads/Novedades/guia-preparar-ensayo-stageup.png',
            publicado = 1,
            fechaPublicacion = DATEADD(DAY, -3, @ahoraEtapa5),
            fechaVencimiento = ISNULL(fechaVencimiento, DATEADD(DAY, 120, @ahoraEtapa5)),
            fechaUltimaModificacion = @ahoraEtapa5
        WHERE idNovedad = @idNovedadConsejos
          AND titulo = N'Nuevos espacios'
          AND resumen = N'Nuevos espacios este fin de año!'
          AND enviadaPorCorreo = 0;
    END

    SELECT TOP (1) @idNovedadComunidad = idNovedad
    FROM dbo.Novedad
    WHERE activo = 1
      AND titulo = N'Preguntá antes de reservar y aprovechá las respuestas de la comunidad'
    ORDER BY idNovedad;

    IF @idNovedadComunidad IS NULL
    BEGIN
        INSERT INTO dbo.Novedad
            (titulo, resumen, contenido, categoria, urlImagen, publicado,
             fechaPublicacion, fechaVencimiento, fechaAlta, activo)
        VALUES
            (N'Preguntá antes de reservar y aprovechá las respuestas de la comunidad',
             N'Ahora cada espacio reúne preguntas públicas y respuestas del gestor para resolver dudas concretas antes de enviar una solicitud.',
             N'¿El espacio tiene espejos, camarines o estacionamiento cercano? ¿Se puede llevar equipamiento propio? Desde el detalle de cada publicación podés dejar una pregunta. El gestor responde una vez y la información queda disponible para otras personas con la misma duda. Las consultas privadas sobre una reserva siguen dentro de la mensajería de esa reserva.',
             N'Comunidad',
             N'~/Content/Uploads/Novedades/preguntas-respuestas-espacios-stageup.png',
             1, DATEADD(DAY, -1, @ahoraEtapa5), DATEADD(DAY, 150, @ahoraEtapa5),
             @ahoraEtapa5, 1);

        SET @idNovedadComunidad = SCOPE_IDENTITY();
    END

    DECLARE @correoMariaNewsletter NVARCHAR(254) = N'polioly.oficial@gmail.com';
    DECLARE @correoDanteNewsletter NVARCHAR(254) = N'dantemajeras@gmail.com';
    DECLARE @idMariaNewsletter INT;
    DECLARE @idDanteNewsletter INT;
    DECLARE @idSuscriptorMaria INT;
    DECLARE @idSuscriptorDante INT;
    DECLARE @tokenMaria VARCHAR(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', N'StageUp-87-newsletter-maria'), 2));
    DECLARE @tokenDante VARCHAR(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', N'StageUp-87-newsletter-dante'), 2));
    DECLARE @suscriptoresNuevos TABLE
    (
        idSuscriptorNewsletter INT PRIMARY KEY,
        correoElectronico NVARCHAR(254) NOT NULL
    );

    IF (SELECT COUNT(*) FROM dbo.UsuarioExterno
        WHERE correoElectronico = @correoMariaNewsletter AND activo = 1 AND estadoCuenta = N'Activa') <> 1
    BEGIN
        THROW 51742, 'No se encontró una única cuenta activa para María López.', 1;
    END

    IF (SELECT COUNT(*) FROM dbo.UsuarioExterno
        WHERE correoElectronico = @correoDanteNewsletter AND activo = 1 AND estadoCuenta = N'Activa') <> 1
    BEGIN
        THROW 51743, 'No se encontró una única cuenta activa para Dante Majeras.', 1;
    END

    SELECT @idMariaNewsletter = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = @correoMariaNewsletter AND activo = 1 AND estadoCuenta = N'Activa';

    SELECT @idDanteNewsletter = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = @correoDanteNewsletter AND activo = 1 AND estadoCuenta = N'Activa';

    IF NOT EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletter WHERE correoElectronico = @correoMariaNewsletter)
    BEGIN
        INSERT INTO dbo.SuscriptorNewsletter
            (correoElectronico, nombre, idUsuarioExterno, estado, origen,
             hashTokenConfirmacion, vencimientoConfirmacion, fechaUltimoEnvioConfirmacion,
             tokenPreferencias, fechaAlta, fechaConfirmacion, fechaBaja)
        OUTPUT inserted.idSuscriptorNewsletter, inserted.correoElectronico
            INTO @suscriptoresNuevos (idSuscriptorNewsletter, correoElectronico)
        VALUES
            (@correoMariaNewsletter, N'María López', @idMariaNewsletter, N'Activo', N'Cuenta',
             NULL, NULL, NULL, @tokenMaria, @ahoraEtapa5, @ahoraEtapa5, NULL);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SuscriptorNewsletter WHERE correoElectronico = @correoDanteNewsletter)
    BEGIN
        INSERT INTO dbo.SuscriptorNewsletter
            (correoElectronico, nombre, idUsuarioExterno, estado, origen,
             hashTokenConfirmacion, vencimientoConfirmacion, fechaUltimoEnvioConfirmacion,
             tokenPreferencias, fechaAlta, fechaConfirmacion, fechaBaja)
        OUTPUT inserted.idSuscriptorNewsletter, inserted.correoElectronico
            INTO @suscriptoresNuevos (idSuscriptorNewsletter, correoElectronico)
        VALUES
            (@correoDanteNewsletter, N'Dante Majeras', @idDanteNewsletter, N'Activo', N'Cuenta',
             NULL, NULL, NULL, @tokenDante, @ahoraEtapa5, @ahoraEtapa5, NULL);
    END

    SELECT @idSuscriptorMaria = idSuscriptorNewsletter
    FROM dbo.SuscriptorNewsletter
    WHERE correoElectronico = @correoMariaNewsletter;

    SELECT @idSuscriptorDante = idSuscriptorNewsletter
    FROM dbo.SuscriptorNewsletter
    WHERE correoElectronico = @correoDanteNewsletter;

    IF EXISTS (SELECT 1 FROM @suscriptoresNuevos WHERE idSuscriptorNewsletter = @idSuscriptorMaria)
    BEGIN
        INSERT INTO dbo.SuscriptorNewsletterCategoria (idSuscriptorNewsletter, categoria)
        VALUES (@idSuscriptorMaria, N'Catalogo'),
               (@idSuscriptorMaria, N'Comunidad');

        INSERT INTO dbo.RegistroActividad
            (idUsuarioExternoResponsable, idUsuarioInternoResponsable, tipoOperacion,
             tipoEntidadAfectada, idEntidadAfectada, descripcionOperacion,
             fechaOperacion, origenOperacion)
        VALUES
            (@idMariaNewsletter, NULL, N'MODIFICACION', N'SuscriptorNewsletter',
             @idSuscriptorMaria, N'Suscripción al newsletter confirmada.',
             @ahoraEtapa5, N'Script 87 - Datos demo'),
            (@idMariaNewsletter, NULL, N'MODIFICACION', N'SuscriptorNewsletter',
             @idSuscriptorMaria, N'Preferencias iniciales: Nuevos espacios y Comunidad artística.',
             @ahoraEtapa5, N'Script 87 - Datos demo');
    END

    IF EXISTS (SELECT 1 FROM @suscriptoresNuevos WHERE idSuscriptorNewsletter = @idSuscriptorDante)
    BEGIN
        INSERT INTO dbo.SuscriptorNewsletterCategoria (idSuscriptorNewsletter, categoria)
        VALUES (@idSuscriptorDante, N'Consejos'),
               (@idSuscriptorDante, N'Institucional');

        INSERT INTO dbo.RegistroActividad
            (idUsuarioExternoResponsable, idUsuarioInternoResponsable, tipoOperacion,
             tipoEntidadAfectada, idEntidadAfectada, descripcionOperacion,
             fechaOperacion, origenOperacion)
        VALUES
            (@idDanteNewsletter, NULL, N'MODIFICACION', N'SuscriptorNewsletter',
             @idSuscriptorDante, N'Suscripción al newsletter confirmada.',
             @ahoraEtapa5, N'Script 87 - Datos demo'),
            (@idDanteNewsletter, NULL, N'MODIFICACION', N'SuscriptorNewsletter',
             @idSuscriptorDante, N'Preferencias iniciales: Institucional y Guías y consejos.',
             @ahoraEtapa5, N'Script 87 - Datos demo');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- ===========================================================================
-- Segunda etapa:
--   3. Reputaciones personales de Ana y Marcos como solicitantes.
--   4. Reservas históricas en otros espacios de Marcos y estados útiles para
--      explicar el circuito durante la demostración.
--
-- Escenarios que quedan intencionalmente distintos:
--   - Sala Principal: conserva sus cinco reseñas existentes.
--   - Estudio Fotográfico Norte y Sala Ensayo Palermo: experiencia finalizada
--     y calificada.
--   - Estudio Sonoro Colegiales: experiencia finalizada todavía sin calificar.
--   - Teatro Colón: espacio publicado todavía sin reservas ni reseñas.
--   - Estudio Fotográfico Norte: reserva futura aceptada y pagada.
--   - Marcos: una experiencia como solicitante y una calificación recibida.
--
-- El modelo vigente no califica personalmente al gestor: el solicitante
-- califica el espacio y el gestor califica al usuario solicitante. Por eso las
-- reputaciones personales de este bloque corresponden al papel Solicitante.
-- ===========================================================================

IF OBJECT_ID(N'dbo.sp_Reserva_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Reserva_AceptarSiDisponible', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Pago_RegistrarReserva', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_TransaccionEconomica_Registrar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Calificacion_Insertar', N'P') IS NULL
BEGIN
    THROW 51704, 'Primero deben ejecutarse los scripts de reservas, pagos, transacciones y calificaciones.', 1;
END
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idAnaEtapa2 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'ana.solicitante@stageup.test' AND activo = 1
    );
    DECLARE @idMarcosEtapa2 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'marcos.gestor@stageup.test' AND activo = 1
    );
    DECLARE @idClaraEtapa2 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'clara.espacios@stageup.test' AND activo = 1
    );

    IF @idAnaEtapa2 IS NULL OR @idMarcosEtapa2 IS NULL OR @idClaraEtapa2 IS NULL
        THROW 51705, 'No se encontraron las cuentas activas de Ana, Marcos y Clara requeridas por los datos demo.', 1;

    DECLARE @idSalaPrincipalEtapa2 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa2 AND nombreEspacio = N'Sala Principal StageUp' AND activo = 1
    );
    DECLARE @idEstudioFotograficoEtapa2 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa2 AND nombreEspacio = N'Estudio Fotográfico Norte' AND activo = 1
    );
    DECLARE @idSalaEnsayoEtapa2 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa2 AND nombreEspacio = N'Sala Ensayo Palermo' AND activo = 1
    );
    DECLARE @idEstudioSonoroEtapa2 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa2 AND nombreEspacio = N'Estudio Sonoro Colegiales' AND activo = 1
    );
    DECLARE @idEstudioClaraEtapa2 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idClaraEtapa2 AND nombreEspacio = N'Estudio Creativo Palermo' AND activo = 1
    );

    IF @idSalaPrincipalEtapa2 IS NULL
       OR @idEstudioFotograficoEtapa2 IS NULL
       OR @idSalaEnsayoEtapa2 IS NULL
       OR @idEstudioSonoroEtapa2 IS NULL
       OR @idEstudioClaraEtapa2 IS NULL
    BEGIN
        THROW 51706, 'No se encontraron todos los espacios requeridos por los escenarios demo.', 1;
    END

    DECLARE @porcentajeComisionDemo DECIMAL(5,2) = TRY_CONVERT
    (
        DECIMAL(5,2),
        (SELECT valor FROM dbo.ParametroPlataforma WHERE clave = N'ComisionPlataformaPorcentaje')
    );

    IF @porcentajeComisionDemo IS NULL
        THROW 51707, 'No se pudo obtener la comisión vigente de StageUp.', 1;

    DECLARE @fechaReservaPagada DATE = DATEADD
    (
        DAY,
        CASE WHEN CONVERT(TIME, GETDATE()) < CONVERT(TIME, N'19:00') THEN 0 ELSE 1 END,
        CONVERT(DATE, GETDATE())
    );

    DECLARE @ReservasDemo TABLE
    (
        numero INT IDENTITY(1,1) PRIMARY KEY,
        codigo NVARCHAR(40) NOT NULL UNIQUE,
        idEspacioArtistico INT NOT NULL,
        idSolicitante INT NOT NULL,
        fechaSolicitada DATE NOT NULL,
        minutoDesde SMALLINT NOT NULL,
        minutoHasta SMALLINT NOT NULL,
        comentario NVARCHAR(1000) NOT NULL,
        esHistorica BIT NOT NULL,
        fechaCreacion DATETIME NULL,
        fechaResolucion DATETIME NULL,
        fechaPago DATETIME NULL,
        fechaFinalizacion DATETIME NULL
    );

    INSERT INTO @ReservasDemo
        (codigo, idEspacioArtistico, idSolicitante, fechaSolicitada, minutoDesde, minutoHasta,
         comentario, esHistorica, fechaCreacion, fechaResolucion, fechaPago, fechaFinalizacion)
    VALUES
        (N'ANA-FOTO-HIST', @idEstudioFotograficoEtapa2, @idAnaEtapa2, '20260904', 600, 720,
         N'Sesión de retratos para el elenco de una producción independiente.', 1,
         '20260901 10:15', '20260901 13:20', '20260901 13:35', '20260904 12:05'),
        (N'ANA-ENSAYO-HIST', @idSalaEnsayoEtapa2, @idAnaEtapa2, '20260917', 1080, 1200,
         N'Ensayo general de movimiento y música antes de la muestra.', 1,
         '20260913 16:10', '20260913 18:40', '20260913 18:52', '20260917 20:05'),
        (N'ANA-SONORO-PENDIENTE-RESENA', @idEstudioSonoroEtapa2, @idAnaEtapa2, '20261002', 900, 1020,
         N'Grabación de voces y edición de una pieza breve para difusión.', 1,
         '20260928 11:25', '20260928 14:15', '20260928 14:26', '20261002 17:05'),
        (N'MARCOS-SOLICITANTE-HIST', @idEstudioClaraEtapa2, @idMarcosEtapa2, '20261003', 840, 960,
         N'Reunión creativa y prueba de cámara para un proyecto colaborativo.', 1,
         '20260929 09:40', '20260929 12:10', '20260929 12:18', '20261003 16:05'),
        (N'ANA-FOTO-PAGADA-FUTURA', @idEstudioFotograficoEtapa2, @idAnaEtapa2, @fechaReservaPagada, 1200, 1320,
         N'Sesión nocturna de fotografías promocionales con iluminación continua.', 0,
         NULL, NULL, NULL, NULL);

    DECLARE @ReservasProcesadas TABLE
    (
        codigo NVARCHAR(40) NOT NULL PRIMARY KEY,
        idReserva INT NOT NULL,
        idPago INT NULL,
        idComprobante INT NULL
    );

    DECLARE @numeroReservaDemo INT = 1;
    DECLARE @cantidadReservasDemo INT = (SELECT COUNT(*) FROM @ReservasDemo);
    DECLARE @codigoReservaDemo NVARCHAR(40);
    DECLARE @idEspacioReservaDemo INT;
    DECLARE @idSolicitanteReservaDemo INT;
    DECLARE @fechaSolicitadaDemo DATE;
    DECLARE @minutoDesdeDemo SMALLINT;
    DECLARE @minutoHastaDemo SMALLINT;
    DECLARE @comentarioReservaDemo NVARCHAR(1000);
    DECLARE @esHistoricaDemo BIT;
    DECLARE @fechaCreacionDemo DATETIME;
    DECLARE @fechaResolucionDemo DATETIME;
    DECLARE @fechaPagoDemo DATETIME;
    DECLARE @fechaFinalizacionDemo DATETIME;
    DECLARE @precioHoraDemo DECIMAL(18,2);
    DECLARE @monedaDemo NVARCHAR(3);
    DECLARE @importeDemo DECIMAL(18,2);
    DECLARE @idReservaDemo INT;
    DECLARE @idPagoDemo INT;
    DECLARE @idComprobanteDemo INT;
    DECLARE @limitePagoDemo DATETIME;
    DECLARE @codigoAutorizacionDemo NVARCHAR(100);
    DECLARE @fechaOperacionDemo DATETIME;
    DECLARE @importeComisionDemo DECIMAL(18,2);
    DECLARE @importeGestorDemo DECIMAL(18,2);
    DECLARE @referenciaPagoDemo NVARCHAR(200);

    DECLARE @NuevaReserva TABLE (idReserva INT NOT NULL);
    DECLARE @Aceptacion TABLE (seAcepto BIT NOT NULL);
    DECLARE @ResultadoPago TABLE
    (
        resultado NVARCHAR(30) NOT NULL,
        idPago INT NULL,
        idComprobante INT NULL
    );
    DECLARE @TransaccionRegistrada TABLE
    (
        idTransaccion INT NOT NULL,
        esNueva BIT NOT NULL
    );

    WHILE @numeroReservaDemo <= @cantidadReservasDemo
    BEGIN
        SELECT @codigoReservaDemo = NULL,
               @idEspacioReservaDemo = NULL,
               @idSolicitanteReservaDemo = NULL,
               @fechaSolicitadaDemo = NULL,
               @minutoDesdeDemo = NULL,
               @minutoHastaDemo = NULL,
               @comentarioReservaDemo = NULL,
               @esHistoricaDemo = NULL,
               @fechaCreacionDemo = NULL,
               @fechaResolucionDemo = NULL,
               @fechaPagoDemo = NULL,
               @fechaFinalizacionDemo = NULL,
               @precioHoraDemo = NULL,
               @monedaDemo = NULL,
               @importeDemo = NULL,
               @idReservaDemo = NULL,
               @idPagoDemo = NULL,
               @idComprobanteDemo = NULL,
               @limitePagoDemo = NULL,
               @codigoAutorizacionDemo = NULL,
               @fechaOperacionDemo = NULL,
               @importeComisionDemo = NULL,
               @importeGestorDemo = NULL,
               @referenciaPagoDemo = NULL;

        DELETE FROM @NuevaReserva;
        DELETE FROM @Aceptacion;
        DELETE FROM @ResultadoPago;
        DELETE FROM @TransaccionRegistrada;

        SELECT @codigoReservaDemo = codigo,
               @idEspacioReservaDemo = idEspacioArtistico,
               @idSolicitanteReservaDemo = idSolicitante,
               @fechaSolicitadaDemo = fechaSolicitada,
               @minutoDesdeDemo = minutoDesde,
               @minutoHastaDemo = minutoHasta,
               @comentarioReservaDemo = comentario,
               @esHistoricaDemo = esHistorica,
               @fechaCreacionDemo = fechaCreacion,
               @fechaResolucionDemo = fechaResolucion,
               @fechaPagoDemo = fechaPago,
               @fechaFinalizacionDemo = fechaFinalizacion
        FROM @ReservasDemo
        WHERE numero = @numeroReservaDemo;

        SELECT @precioHoraDemo = precioHora,
               @monedaDemo = ISNULL(moneda, N'ARS')
        FROM dbo.FichaEspacio
        WHERE idEspacioArtistico = @idEspacioReservaDemo;

        IF @precioHoraDemo IS NULL
            THROW 51708, 'Uno de los espacios demo no tiene precio por hora.', 1;

        SET @importeDemo = ROUND
        (
            @precioHoraDemo * CONVERT(DECIMAL(10,2), @minutoHastaDemo - @minutoDesdeDemo) / 60,
            2
        );

        SELECT @idReservaDemo = idReserva
        FROM dbo.Reserva
        WHERE idEspacioArtistico = @idEspacioReservaDemo
          AND idUsuarioExternoSolicitante = @idSolicitanteReservaDemo
          AND comentarioSolicitante = @comentarioReservaDemo;

        IF @idReservaDemo IS NULL
        BEGIN
            INSERT INTO @NuevaReserva (idReserva)
            EXEC dbo.sp_Reserva_Insertar
                @idEspacioArtistico = @idEspacioReservaDemo,
                @idUsuarioExternoSolicitante = @idSolicitanteReservaDemo,
                @fechaSolicitada = @fechaSolicitadaDemo,
                @comentarioSolicitante = @comentarioReservaDemo,
                @minutoDesde = @minutoDesdeDemo,
                @minutoHasta = @minutoHastaDemo,
                @precioHoraPactado = @precioHoraDemo,
                @moneda = @monedaDemo,
                @importeEstimado = @importeDemo;

            SELECT @idReservaDemo = idReserva FROM @NuevaReserva;

            SET @limitePagoDemo = CASE
                WHEN @esHistoricaDemo = 1 THEN DATEADD(HOUR, 2, GETDATE())
                ELSE DATEADD(MINUTE, @minutoDesdeDemo, CONVERT(DATETIME, @fechaSolicitadaDemo))
            END;

            INSERT INTO @Aceptacion (seAcepto)
            EXEC dbo.sp_Reserva_AceptarSiDisponible
                @idReserva = @idReservaDemo,
                @comentarioResolucion = N'Solicitud aceptada. El espacio quedó reservado para la actividad indicada.',
                @fechaLimitePago = @limitePagoDemo;

            IF NOT EXISTS (SELECT 1 FROM @Aceptacion WHERE seAcepto = 1)
                THROW 51709, 'No se pudo aceptar una de las reservas demo por disponibilidad.', 1;

            SET @codigoAutorizacionDemo = N'87' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @idReservaDemo), 6);
            SET @fechaOperacionDemo = CASE WHEN @esHistoricaDemo = 1 THEN @fechaPagoDemo ELSE GETDATE() END;

            INSERT INTO @ResultadoPago (resultado, idPago, idComprobante)
            EXEC dbo.sp_Pago_RegistrarReserva
                @idReserva = @idReservaDemo,
                @idUsuarioExterno = @idSolicitanteReservaDemo,
                @importeTarjeta = @importeDemo,
                @importeSaldo = 0,
                @marcaTarjeta = N'Visa',
                @ultimosDigitos = N'4242',
                @codigoAutorizacion = @codigoAutorizacionDemo,
                @porcentajeComisionPlataforma = @porcentajeComisionDemo,
                @origen = N'StageUp',
                @identificadorExterno = NULL,
                @medioInformado = N'Tarjeta de demostración',
                @fechaOperacion = @fechaOperacionDemo,
                @idPagoPendiente = NULL,
                @idUsuarioInternoRegistro = NULL;

            IF NOT EXISTS (SELECT 1 FROM @ResultadoPago WHERE resultado = N'OK')
                THROW 51710, 'No se pudo registrar el pago de una de las reservas demo.', 1;

            SELECT @idPagoDemo = idPago,
                   @idComprobanteDemo = idComprobante
            FROM @ResultadoPago
            WHERE resultado = N'OK';

            SET @importeComisionDemo = ROUND(@importeDemo * @porcentajeComisionDemo / 100, 2);
            SET @importeGestorDemo = @importeDemo - @importeComisionDemo;

            SELECT @referenciaPagoDemo = identificadorExterno
            FROM dbo.Pago
            WHERE idPago = @idPagoDemo;

            INSERT INTO @TransaccionRegistrada (idTransaccion, esNueva)
            EXEC dbo.sp_TransaccionEconomica_Registrar
                @tipoOperacion = N'ReservaAbonada',
                @idReserva = @idReservaDemo,
                @idPago = @idPagoDemo,
                @idComprobante = @idComprobanteDemo,
                @montoTotal = @importeDemo,
                @porcentajeComision = @porcentajeComisionDemo,
                @importeComision = @importeComisionDemo,
                @importeGestor = @importeGestorDemo,
                @porcentajeCargo = NULL,
                @estadoPago = N'Pagado',
                @referenciaPago = @referenciaPagoDemo,
                @estadoRegistro = N'Registrada',
                @observacion = N'Pago de reserva de demostración registrado por el script 87.';

            IF @esHistoricaDemo = 1
            BEGIN
                UPDATE dbo.Reserva
                SET estadoReserva = N'Finalizada',
                    fechaCreacion = @fechaCreacionDemo,
                    fechaResolucion = @fechaResolucionDemo,
                    fechaUltimaModificacion = @fechaFinalizacionDemo,
                    fechaLimitePago = DATEADD(HOUR, 48, @fechaResolucionDemo),
                    fechaPago = @fechaPagoDemo,
                    fechaFinalizacion = @fechaFinalizacionDemo,
                    avisoFinalizacionEnviado = 1
                WHERE idReserva = @idReservaDemo;

                UPDATE dbo.Pago
                SET fechaPago = @fechaPagoDemo,
                    fechaOperacion = @fechaPagoDemo
                WHERE idPago = @idPagoDemo;

                UPDATE dbo.MovimientoCuentaCorriente
                SET fechaMovimiento = @fechaPagoDemo
                WHERE idPago = @idPagoDemo;

                UPDATE dbo.Comprobante
                SET fechaEmision = @fechaPagoDemo
                WHERE idComprobante = @idComprobanteDemo;

                UPDATE dbo.TransaccionEconomica
                SET fechaGeneracion = @fechaPagoDemo,
                    fechaUltimaModificacion = @fechaPagoDemo
                WHERE idPago = @idPagoDemo;
            END
        END
        ELSE
        BEGIN
            SELECT TOP (1) @idPagoDemo = idPago
            FROM dbo.Pago
            WHERE idReserva = @idReservaDemo AND estado = N'Aprobado'
            ORDER BY idPago DESC;

            SELECT TOP (1) @idComprobanteDemo = idComprobante
            FROM dbo.Comprobante
            WHERE idReserva = @idReservaDemo AND tipo = N'FC'
            ORDER BY idComprobante DESC;
        END

        INSERT INTO @ReservasProcesadas (codigo, idReserva, idPago, idComprobante)
        VALUES (@codigoReservaDemo, @idReservaDemo, @idPagoDemo, @idComprobanteDemo);

        SET @numeroReservaDemo += 1;
    END

    -- Las primeras cuatro reservas de Sala Principal quedan completas desde
    -- la mirada del gestor. La quinta se conserva sin esta calificación para
    -- poder mostrar el llamado a la acción "Calificar al solicitante".
    DECLARE @CalificacionesDemo TABLE
    (
        numero INT IDENTITY(1,1) PRIMARY KEY,
        idReserva INT NOT NULL,
        idAutor INT NOT NULL,
        tipo NVARCHAR(30) NOT NULL,
        puntaje TINYINT NOT NULL,
        comentario NVARCHAR(1000) NOT NULL,
        fechaAlta DATETIME NOT NULL
    );

    INSERT INTO @CalificacionesDemo (idReserva, idAutor, tipo, puntaje, comentario, fechaAlta)
    SELECT r.idReserva, @idMarcosEtapa2, N'UsuarioSolicitante', 5,
           N'Ana coordinó todo con claridad, llegó puntual y dejó la sala en excelentes condiciones.',
           '20261006 20:30'
    FROM dbo.Reserva r
    WHERE r.idReserva = 1 AND r.idUsuarioExternoSolicitante = @idAnaEtapa2 AND r.idEspacioArtistico = @idSalaPrincipalEtapa2
    UNION ALL
    SELECT r.idReserva, @idMarcosEtapa2, N'UsuarioSolicitante', 5,
           N'Muy buena comunicación desde la solicitud. El grupo respetó los horarios y las pautas del espacio.',
           '20260830 11:10'
    FROM dbo.Reserva r
    WHERE r.idReserva = 2 AND r.idUsuarioExternoSolicitante = @idAnaEtapa2 AND r.idEspacioArtistico = @idSalaPrincipalEtapa2
    UNION ALL
    SELECT r.idReserva, @idMarcosEtapa2, N'UsuarioSolicitante', 5,
           N'Organización impecable y trato muy respetuoso. Entregaron el espacio ordenado al finalizar.',
           '20260907 10:45'
    FROM dbo.Reserva r
    WHERE r.idReserva = 3 AND r.idUsuarioExternoSolicitante = @idAnaEtapa2 AND r.idEspacioArtistico = @idSalaPrincipalEtapa2
    UNION ALL
    SELECT r.idReserva, @idMarcosEtapa2, N'UsuarioSolicitante', 4,
           N'La actividad se desarrolló según lo acordado y Ana avisó con tiempo cada necesidad técnica.',
           '20260915 09:20'
    FROM dbo.Reserva r
    WHERE r.idReserva = 4 AND r.idUsuarioExternoSolicitante = @idAnaEtapa2 AND r.idEspacioArtistico = @idSalaPrincipalEtapa2;

    IF (SELECT COUNT(*) FROM @CalificacionesDemo WHERE idReserva IN (1,2,3,4)) <> 4
        THROW 51711, 'No se encontraron las cuatro reservas históricas esperadas de Sala Principal.', 1;

    INSERT INTO @CalificacionesDemo (idReserva, idAutor, tipo, puntaje, comentario, fechaAlta)
    SELECT p.idReserva, @idAnaEtapa2, N'Espacio', 5,
           N'El estudio estaba impecable y los fondos y flashes permitieron trabajar sin demoras.',
           '20260905 10:20'
    FROM @ReservasProcesadas p WHERE p.codigo = N'ANA-FOTO-HIST'
    UNION ALL
    SELECT p.idReserva, @idMarcosEtapa2, N'UsuarioSolicitante', 5,
           N'Ana presentó un plan de trabajo claro, cuidó el equipamiento y respetó perfectamente el horario.',
           '20260905 10:25'
    FROM @ReservasProcesadas p WHERE p.codigo = N'ANA-FOTO-HIST'
    UNION ALL
    SELECT p.idReserva, @idAnaEtapa2, N'Espacio', 4,
           N'Muy buena acústica y equipamiento práctico para el ensayo. La sala resultó cómoda para todo el grupo.',
           '20260918 11:30'
    FROM @ReservasProcesadas p WHERE p.codigo = N'ANA-ENSAYO-HIST'
    UNION ALL
    SELECT p.idReserva, @idMarcosEtapa2, N'UsuarioSolicitante', 5,
           N'Excelente experiencia: comunicación fluida, puntualidad y mucho cuidado del espacio y los equipos.',
           '20260918 11:35'
    FROM @ReservasProcesadas p WHERE p.codigo = N'ANA-ENSAYO-HIST'
    UNION ALL
    SELECT p.idReserva, @idMarcosEtapa2, N'Espacio', 5,
           N'Un estudio luminoso y muy cómodo para reunir al equipo y realizar la prueba de cámara.',
           '20261004 12:15'
    FROM @ReservasProcesadas p WHERE p.codigo = N'MARCOS-SOLICITANTE-HIST'
    UNION ALL
    SELECT p.idReserva, @idClaraEtapa2, N'UsuarioSolicitante', 5,
           N'Marcos fue muy claro con lo que necesitaba, llegó a horario y entregó el estudio en perfectas condiciones.',
           '20261004 12:20'
    FROM @ReservasProcesadas p WHERE p.codigo = N'MARCOS-SOLICITANTE-HIST';

    DECLARE @numeroCalificacionDemo INT = 1;
    DECLARE @cantidadCalificacionesDemo INT = (SELECT COUNT(*) FROM @CalificacionesDemo);
    DECLARE @idReservaCalificacionDemo INT;
    DECLARE @idAutorCalificacionDemo INT;
    DECLARE @tipoCalificacionDemo NVARCHAR(30);
    DECLARE @puntajeCalificacionDemo TINYINT;
    DECLARE @comentarioCalificacionDemo NVARCHAR(1000);
    DECLARE @fechaAltaCalificacionDemo DATETIME;
    DECLARE @NuevaCalificacion TABLE (idCalificacion INT NOT NULL);

    WHILE @numeroCalificacionDemo <= @cantidadCalificacionesDemo
    BEGIN
        SELECT @idReservaCalificacionDemo = NULL,
               @idAutorCalificacionDemo = NULL,
               @tipoCalificacionDemo = NULL,
               @puntajeCalificacionDemo = NULL,
               @comentarioCalificacionDemo = NULL,
               @fechaAltaCalificacionDemo = NULL;

        DELETE FROM @NuevaCalificacion;

        SELECT @idReservaCalificacionDemo = idReserva,
               @idAutorCalificacionDemo = idAutor,
               @tipoCalificacionDemo = tipo,
               @puntajeCalificacionDemo = puntaje,
               @comentarioCalificacionDemo = comentario,
               @fechaAltaCalificacionDemo = fechaAlta
        FROM @CalificacionesDemo
        WHERE numero = @numeroCalificacionDemo;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Calificacion
            WHERE idReserva = @idReservaCalificacionDemo
              AND tipoCalificacion = @tipoCalificacionDemo
        )
        BEGIN
            INSERT INTO @NuevaCalificacion (idCalificacion)
            EXEC dbo.sp_Calificacion_Insertar
                @idReserva = @idReservaCalificacionDemo,
                @idUsuarioAutor = @idAutorCalificacionDemo,
                @tipoCalificacion = @tipoCalificacionDemo,
                @puntaje = @puntajeCalificacionDemo,
                @comentario = @comentarioCalificacionDemo;

            UPDATE dbo.Calificacion
            SET fechaAlta = @fechaAltaCalificacionDemo
            WHERE idCalificacion = (SELECT idCalificacion FROM @NuevaCalificacion);
        END

        SET @numeroCalificacionDemo += 1;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- ===========================================================================
-- Tercera etapa:
--   5. Preguntas públicas sobre los espacios de Marcos.
--   6. Conversación privada en la reserva futura de Ana.
--
-- Se conservan estados distintos para la demostración:
--   - ocho preguntas públicas distribuidas entre los cinco espacios;
--   - seis respondidas y dos pendientes en la bandeja del gestor;
--   - cinco mensajes privados en la reserva futura ya creada;
--   - los primeros cuatro mensajes están leídos y el último queda pendiente
--     de lectura para Marcos.
-- ===========================================================================

IF OBJECT_ID(N'dbo.sp_PreguntaEspacio_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_PreguntaEspacio_Responder', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_MensajeReserva_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Notificacion_Insertar', N'P') IS NULL
BEGIN
    THROW 51712, 'Primero deben ejecutarse los scripts de preguntas, mensajes y notificaciones.', 1;
END
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idAnaEtapa3 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'ana.solicitante@stageup.test' AND activo = 1
    );
    DECLARE @idMarcosEtapa3 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'marcos.gestor@stageup.test' AND activo = 1
    );
    DECLARE @idMariaEtapa3 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'polioly.oficial@gmail.com' AND activo = 1
    );
    DECLARE @idDanteEtapa3 INT =
    (
        SELECT idUsuarioExterno
        FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'dantemajeras@gmail.com' AND activo = 1
    );

    IF @idAnaEtapa3 IS NULL OR @idMarcosEtapa3 IS NULL
       OR @idMariaEtapa3 IS NULL OR @idDanteEtapa3 IS NULL
    BEGIN
        THROW 51713, 'No se encontraron las cuentas activas requeridas para las preguntas demo.', 1;
    END

    DECLARE @idSalaPrincipalEtapa3 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa3 AND nombreEspacio = N'Sala Principal StageUp' AND activo = 1
    );
    DECLARE @idEstudioFotograficoEtapa3 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa3 AND nombreEspacio = N'Estudio Fotográfico Norte' AND activo = 1
    );
    DECLARE @idSalaEnsayoEtapa3 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa3 AND nombreEspacio = N'Sala Ensayo Palermo' AND activo = 1
    );
    DECLARE @idEstudioSonoroEtapa3 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa3 AND nombreEspacio = N'Estudio Sonoro Colegiales' AND activo = 1
    );
    DECLARE @idTeatroEtapa3 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa3 AND nombreEspacio = N'Teatro Colón' AND activo = 1
    );

    IF @idSalaPrincipalEtapa3 IS NULL OR @idEstudioFotograficoEtapa3 IS NULL
       OR @idSalaEnsayoEtapa3 IS NULL OR @idEstudioSonoroEtapa3 IS NULL
       OR @idTeatroEtapa3 IS NULL
    BEGIN
        THROW 51714, 'No se encontraron todos los espacios de Marcos requeridos para las preguntas demo.', 1;
    END

    DECLARE @PreguntasDemo TABLE
    (
        numero INT IDENTITY(1,1) PRIMARY KEY,
        codigo NVARCHAR(40) NOT NULL UNIQUE,
        idEspacioArtistico INT NOT NULL,
        nombreEspacio NVARCHAR(200) NOT NULL,
        idAutor INT NOT NULL,
        pregunta NVARCHAR(500) NOT NULL,
        fechaPregunta DATETIME NOT NULL,
        respuesta NVARCHAR(1000) NULL,
        fechaRespuesta DATETIME NULL
    );

    INSERT INTO @PreguntasDemo
        (codigo, idEspacioArtistico, nombreEspacio, idAutor, pregunta, fechaPregunta, respuesta, fechaRespuesta)
    VALUES
        (N'SALA-ESTACIONAMIENTO', @idSalaPrincipalEtapa3, N'Sala Principal StageUp', @idAnaEtapa3,
         N'¿Hay estacionamiento cerca o algún lugar cómodo para descargar escenografía?', '20261008 10:15',
         N'Hay estacionamientos pagos a una cuadra sobre Paraná. Para descargar escenografía pueden detenerse unos minutos frente al acceso, coordinándolo previamente.', '20261008 12:40'),
        (N'SALA-CAMARINES', @idSalaPrincipalEtapa3, N'Sala Principal StageUp', @idMariaEtapa3,
         N'¿La sala cuenta con camarines y espacio para cambiarse antes de la función?', '20261009 16:25',
         N'Sí, contamos con dos camarines y un sector de apoyo para vestuario. Están incluidos en la reserva de la sala.', '20261009 18:05'),
        (N'FOTO-EQUIPO-PROPIO', @idEstudioFotograficoEtapa3, N'Estudio Fotográfico Norte', @idDanteEtapa3,
         N'¿Se puede utilizar equipo de iluminación propio además del que ofrece el estudio?', '20261011 09:20',
         N'Sí, pueden traer iluminación propia. Solo les pedimos avisar antes para organizar las conexiones y la distribución segura del equipo.', '20261011 11:10'),
        (N'FOTO-FONDOS-PENDIENTE', @idEstudioFotograficoEtapa3, N'Estudio Fotográfico Norte', @idAnaEtapa3,
         N'¿Qué colores de fondos están disponibles y se pueden solicitar antes de la sesión?', '20261020 14:35',
         NULL, NULL),
        (N'ENSAYO-ESPEJOS', @idSalaEnsayoEtapa3, N'Sala Ensayo Palermo', @idMariaEtapa3,
         N'¿La sala tiene espejos para trabajar coreografías y se pueden cubrir si no los usamos?', '20261013 17:45',
         N'Uno de los laterales tiene espejos de cuerpo entero. También contamos con cortinas para cubrirlos cuando el ensayo no los necesita.', '20261013 19:15'),
        (N'ENSAYO-UTILERIA-PENDIENTE', @idSalaEnsayoEtapa3, N'Sala Ensayo Palermo', @idDanteEtapa3,
         N'¿Se puede ingresar con utilería voluminosa y hay algún horario recomendado para descargarla?', '20261021 11:05',
         NULL, NULL),
        (N'SONORO-MICROFONOS', @idEstudioSonoroEtapa3, N'Estudio Sonoro Colegiales', @idAnaEtapa3,
         N'¿El precio incluye los micrófonos, auriculares y la interfaz de audio que figuran en la publicación?', '20261015 13:30',
         N'Sí, incluye la interfaz, los micrófonos condenser, auriculares y monitores. El uso está contemplado durante todo el horario reservado.', '20261015 15:05'),
        (N'TEATRO-ACCESIBILIDAD', @idTeatroEtapa3, N'Teatro Colón', @idDanteEtapa3,
         N'¿El ingreso es accesible para personas con movilidad reducida y hay ubicaciones adaptadas?', '20261017 10:40',
         N'Sí, hay un ingreso accesible y ubicaciones adaptadas. Al reservar, indicanos cuántas personas necesitan asistencia para coordinar el acceso.', '20261017 12:30');

    DECLARE @numeroPreguntaDemo INT = 1;
    DECLARE @cantidadPreguntasDemo INT = (SELECT COUNT(*) FROM @PreguntasDemo);
    DECLARE @idPreguntaDemo INT;
    DECLARE @idEspacioPreguntaDemo INT;
    DECLARE @nombreEspacioPreguntaDemo NVARCHAR(200);
    DECLARE @idAutorPreguntaDemo INT;
    DECLARE @textoPreguntaDemo NVARCHAR(500);
    DECLARE @fechaPreguntaDemo DATETIME;
    DECLARE @respuestaPreguntaDemo NVARCHAR(1000);
    DECLARE @fechaRespuestaPreguntaDemo DATETIME;
    DECLARE @estadoPreguntaDemo NVARCHAR(12);
    DECLARE @urlPreguntaDemo NVARCHAR(300);
    DECLARE @mensajeNotificacionDemo NVARCHAR(300);
    DECLARE @idNotificacionDemo INT;
    DECLARE @notificacionCreadaDemo BIT;
    DECLARE @NuevaPregunta TABLE (idPreguntaEspacio INT NOT NULL);
    DECLARE @RespuestaRegistrada TABLE (filas INT NOT NULL);

    WHILE @numeroPreguntaDemo <= @cantidadPreguntasDemo
    BEGIN
        SELECT @idPreguntaDemo = NULL,
               @idEspacioPreguntaDemo = NULL,
               @nombreEspacioPreguntaDemo = NULL,
               @idAutorPreguntaDemo = NULL,
               @textoPreguntaDemo = NULL,
               @fechaPreguntaDemo = NULL,
               @respuestaPreguntaDemo = NULL,
               @fechaRespuestaPreguntaDemo = NULL,
               @estadoPreguntaDemo = NULL,
               @urlPreguntaDemo = NULL,
               @mensajeNotificacionDemo = NULL,
               @idNotificacionDemo = NULL,
               @notificacionCreadaDemo = 0;

        DELETE FROM @NuevaPregunta;
        DELETE FROM @RespuestaRegistrada;

        SELECT @idEspacioPreguntaDemo = idEspacioArtistico,
               @nombreEspacioPreguntaDemo = nombreEspacio,
               @idAutorPreguntaDemo = idAutor,
               @textoPreguntaDemo = pregunta,
               @fechaPreguntaDemo = fechaPregunta,
               @respuestaPreguntaDemo = respuesta,
               @fechaRespuestaPreguntaDemo = fechaRespuesta
        FROM @PreguntasDemo
        WHERE numero = @numeroPreguntaDemo;

        SELECT @idPreguntaDemo = idPreguntaEspacio,
               @estadoPreguntaDemo = estado
        FROM dbo.PreguntaEspacio
        WHERE idEspacioArtistico = @idEspacioPreguntaDemo
          AND idUsuarioExterno = @idAutorPreguntaDemo
          AND pregunta = @textoPreguntaDemo;

        IF @idPreguntaDemo IS NULL
        BEGIN
            INSERT INTO @NuevaPregunta (idPreguntaEspacio)
            EXEC dbo.sp_PreguntaEspacio_Insertar
                @idEspacioArtistico = @idEspacioPreguntaDemo,
                @idUsuarioExterno = @idAutorPreguntaDemo,
                @pregunta = @textoPreguntaDemo;

            SELECT @idPreguntaDemo = idPreguntaEspacio FROM @NuevaPregunta;

            UPDATE dbo.PreguntaEspacio
            SET fechaPregunta = @fechaPreguntaDemo
            WHERE idPreguntaEspacio = @idPreguntaDemo;

            SET @estadoPreguntaDemo = N'Pendiente';
        END

        IF @respuestaPreguntaDemo IS NOT NULL AND @estadoPreguntaDemo = N'Pendiente'
        BEGIN
            INSERT INTO @RespuestaRegistrada (filas)
            EXEC dbo.sp_PreguntaEspacio_Responder
                @idPreguntaEspacio = @idPreguntaDemo,
                @idUsuarioGestor = @idMarcosEtapa3,
                @respuesta = @respuestaPreguntaDemo;

            IF NOT EXISTS (SELECT 1 FROM @RespuestaRegistrada WHERE filas = 1)
                THROW 51715, 'No se pudo responder una de las preguntas demo.', 1;

            UPDATE dbo.PreguntaEspacio
            SET fechaRespuesta = @fechaRespuestaPreguntaDemo
            WHERE idPreguntaEspacio = @idPreguntaDemo;

            SET @estadoPreguntaDemo = N'Respondida';
        END

        SET @urlPreguntaDemo = N'~/PreguntasRecibidas.aspx?espacio=' + CONVERT(NVARCHAR(20), @idEspacioPreguntaDemo);
        SET @mensajeNotificacionDemo = N'Te preguntaron sobre "' + @nombreEspacioPreguntaDemo + N'": "' + @textoPreguntaDemo + N'"';

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Notificacion
            WHERE idUsuarioExterno = @idMarcosEtapa3
              AND tipo = N'PreguntaEspacioRecibida'
              AND mensaje = @mensajeNotificacionDemo
              AND urlDestino = @urlPreguntaDemo
        )
        BEGIN
            EXEC dbo.sp_Notificacion_Insertar
                @idUsuarioExterno = @idMarcosEtapa3,
                @tipo = N'PreguntaEspacioRecibida',
                @mensaje = @mensajeNotificacionDemo,
                @urlDestino = @urlPreguntaDemo,
                @titulo = N'Nueva pregunta sobre tu espacio';

            SET @notificacionCreadaDemo = 1;
        END

        SELECT TOP (1) @idNotificacionDemo = idNotificacion
        FROM dbo.Notificacion
        WHERE idUsuarioExterno = @idMarcosEtapa3
          AND tipo = N'PreguntaEspacioRecibida'
          AND mensaje = @mensajeNotificacionDemo
          AND urlDestino = @urlPreguntaDemo
        ORDER BY idNotificacion DESC;

        IF @notificacionCreadaDemo = 1
        BEGIN
            UPDATE dbo.Notificacion
            SET fechaCreacion = @fechaPreguntaDemo,
                leida = CASE WHEN @respuestaPreguntaDemo IS NULL THEN 0 ELSE 1 END
            WHERE idNotificacion = @idNotificacionDemo;
        END

        IF @respuestaPreguntaDemo IS NOT NULL
        BEGIN
            SET @urlPreguntaDemo = N'~/Explorar/DetalleEspacio.aspx?id=' + CONVERT(NVARCHAR(20), @idEspacioPreguntaDemo)
                + N'&pregunta=' + CONVERT(NVARCHAR(20), @idPreguntaDemo);
            SET @mensajeNotificacionDemo = N'El gestor de "' + @nombreEspacioPreguntaDemo
                + N'" respondió tu pregunta: "' + @respuestaPreguntaDemo + N'"';
            SET @idNotificacionDemo = NULL;
            SET @notificacionCreadaDemo = 0;

            IF NOT EXISTS
            (
                SELECT 1 FROM dbo.Notificacion
                WHERE idUsuarioExterno = @idAutorPreguntaDemo
                  AND tipo = N'PreguntaEspacioRespondida'
                  AND mensaje = @mensajeNotificacionDemo
                  AND urlDestino = @urlPreguntaDemo
            )
            BEGIN
                EXEC dbo.sp_Notificacion_Insertar
                    @idUsuarioExterno = @idAutorPreguntaDemo,
                    @tipo = N'PreguntaEspacioRespondida',
                    @mensaje = @mensajeNotificacionDemo,
                    @urlDestino = @urlPreguntaDemo,
                    @titulo = N'Respondieron tu pregunta';

                SET @notificacionCreadaDemo = 1;
            END

            SELECT TOP (1) @idNotificacionDemo = idNotificacion
            FROM dbo.Notificacion
            WHERE idUsuarioExterno = @idAutorPreguntaDemo
              AND tipo = N'PreguntaEspacioRespondida'
              AND mensaje = @mensajeNotificacionDemo
              AND urlDestino = @urlPreguntaDemo
            ORDER BY idNotificacion DESC;

            IF @notificacionCreadaDemo = 1
            BEGIN
                UPDATE dbo.Notificacion
                SET fechaCreacion = @fechaRespuestaPreguntaDemo,
                    leida = 1
                WHERE idNotificacion = @idNotificacionDemo;
            END
        END

        SET @numeroPreguntaDemo += 1;
    END

    -- La conversación pertenece a la reserva futura creada en la segunda
    -- etapa. El comentario de la solicitud se conserva y no se mezcla con
    -- estos mensajes privados.
    DECLARE @idReservaMensajesDemo INT =
    (
        SELECT idReserva
        FROM dbo.Reserva
        WHERE idEspacioArtistico = @idEstudioFotograficoEtapa3
          AND idUsuarioExternoSolicitante = @idAnaEtapa3
          AND comentarioSolicitante = N'Sesión nocturna de fotografías promocionales con iluminación continua.'
          AND estadoReserva = N'Aceptada'
          AND estadoPago = N'Pagado'
    );

    IF @idReservaMensajesDemo IS NULL
        THROW 51716, 'No se encontró la reserva futura pagada requerida para la conversación demo.', 1;

    DECLARE @fechaBaseMensajesDemo DATETIME = DATEADD(MINUTE, -65, GETDATE());
    DECLARE @MensajesDemo TABLE
    (
        numero INT IDENTITY(1,1) PRIMARY KEY,
        idEmisor INT NOT NULL,
        texto NVARCHAR(1000) NOT NULL,
        fechaEnvio DATETIME NOT NULL,
        fechaLectura DATETIME NULL
    );

    INSERT INTO @MensajesDemo (idEmisor, texto, fechaEnvio, fechaLectura)
    VALUES
        (@idAnaEtapa3,
         N'Hola Marcos, ¿podemos ingresar quince minutos antes para preparar el vestuario y la iluminación?',
         DATEADD(MINUTE, 0, @fechaBaseMensajesDemo), DATEADD(MINUTE, 4, @fechaBaseMensajesDemo)),
        (@idMarcosEtapa3,
         N'Hola Ana. Sí, pueden ingresar desde las 19:45; voy a dejar el acceso coordinado.',
         DATEADD(MINUTE, 7, @fechaBaseMensajesDemo), DATEADD(MINUTE, 10, @fechaBaseMensajesDemo)),
        (@idAnaEtapa3,
         N'Perfecto. ¿La reserva incluye fondos, flashes y disparadores o tenemos que llevar algo?',
         DATEADD(MINUTE, 14, @fechaBaseMensajesDemo), DATEADD(MINUTE, 19, @fechaBaseMensajesDemo)),
        (@idMarcosEtapa3,
         N'Incluye fondos intercambiables, iluminación continua y flashes. Traigan únicamente los disparadores compatibles si prefieren usar los propios.',
         DATEADD(MINUTE, 23, @fechaBaseMensajesDemo), DATEADD(MINUTE, 28, @fechaBaseMensajesDemo)),
        (@idAnaEtapa3,
         N'Genial, entonces llegamos a las 19:45. Gracias por la información.',
         DATEADD(MINUTE, 34, @fechaBaseMensajesDemo), NULL);

    DECLARE @numeroMensajeDemo INT = 1;
    DECLARE @cantidadMensajesDemo INT = (SELECT COUNT(*) FROM @MensajesDemo);
    DECLARE @idMensajeDemo INT;
    DECLARE @idEmisorMensajeDemo INT;
    DECLARE @idDestinatarioMensajeDemo INT;
    DECLARE @textoMensajeDemo NVARCHAR(1000);
    DECLARE @fechaEnvioMensajeDemo DATETIME;
    DECLARE @fechaLecturaMensajeDemo DATETIME;
    DECLARE @nombreEmisorMensajeDemo NVARCHAR(200);
    DECLARE @mensajeCreadoDemo BIT;
    DECLARE @urlMensajeDemo NVARCHAR(300) = N'~/DetalleReserva.aspx?id=' + CONVERT(NVARCHAR(20), @idReservaMensajesDemo) + N'&ver=mensajes';
    DECLARE @NuevoMensaje TABLE (idMensajeReserva INT NOT NULL);

    WHILE @numeroMensajeDemo <= @cantidadMensajesDemo
    BEGIN
        SELECT @idMensajeDemo = NULL,
               @idEmisorMensajeDemo = NULL,
               @idDestinatarioMensajeDemo = NULL,
               @textoMensajeDemo = NULL,
               @fechaEnvioMensajeDemo = NULL,
               @fechaLecturaMensajeDemo = NULL,
               @nombreEmisorMensajeDemo = NULL,
               @mensajeNotificacionDemo = NULL,
               @idNotificacionDemo = NULL,
               @mensajeCreadoDemo = 0,
               @notificacionCreadaDemo = 0;

        DELETE FROM @NuevoMensaje;

        SELECT @idEmisorMensajeDemo = idEmisor,
               @textoMensajeDemo = texto,
               @fechaEnvioMensajeDemo = fechaEnvio,
               @fechaLecturaMensajeDemo = fechaLectura
        FROM @MensajesDemo
        WHERE numero = @numeroMensajeDemo;

        SELECT @idMensajeDemo = idMensajeReserva
        FROM dbo.MensajeReserva
        WHERE idReserva = @idReservaMensajesDemo
          AND idUsuarioExternoEmisor = @idEmisorMensajeDemo
          AND texto = @textoMensajeDemo;

        IF @idMensajeDemo IS NULL
        BEGIN
            INSERT INTO @NuevoMensaje (idMensajeReserva)
            EXEC dbo.sp_MensajeReserva_Insertar
                @idReserva = @idReservaMensajesDemo,
                @idUsuarioExternoEmisor = @idEmisorMensajeDemo,
                @texto = @textoMensajeDemo;

            SELECT @idMensajeDemo = idMensajeReserva FROM @NuevoMensaje;

            SET @mensajeCreadoDemo = 1;
        END

        IF @mensajeCreadoDemo = 1
        BEGIN
            UPDATE dbo.MensajeReserva
            SET fechaEnvio = @fechaEnvioMensajeDemo,
                fechaLectura = @fechaLecturaMensajeDemo
            WHERE idMensajeReserva = @idMensajeDemo;
        END

        IF @idEmisorMensajeDemo = @idAnaEtapa3
        BEGIN
            SET @idDestinatarioMensajeDemo = @idMarcosEtapa3;
            SET @nombreEmisorMensajeDemo = N'Ana Solicitante';
        END
        ELSE
        BEGIN
            SET @idDestinatarioMensajeDemo = @idAnaEtapa3;
            SET @nombreEmisorMensajeDemo = N'Marcos Gestor';
        END

        SET @mensajeNotificacionDemo = @nombreEmisorMensajeDemo + N' te escribió sobre la reserva N° '
            + CONVERT(NVARCHAR(20), @idReservaMensajesDemo) + N' en "Estudio Fotográfico Norte": "'
            + CASE WHEN LEN(@textoMensajeDemo) <= 100 THEN @textoMensajeDemo
                   ELSE LEFT(@textoMensajeDemo, 97) + N'...' END + N'"';

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Notificacion
            WHERE idUsuarioExterno = @idDestinatarioMensajeDemo
              AND tipo = N'MensajeReserva'
              AND mensaje = @mensajeNotificacionDemo
              AND urlDestino = @urlMensajeDemo
        )
        BEGIN
            EXEC dbo.sp_Notificacion_Insertar
                @idUsuarioExterno = @idDestinatarioMensajeDemo,
                @tipo = N'MensajeReserva',
                @mensaje = @mensajeNotificacionDemo,
                @urlDestino = @urlMensajeDemo,
                @titulo = N'Nuevo mensaje sobre una reserva';

            SET @notificacionCreadaDemo = 1;
        END

        SELECT TOP (1) @idNotificacionDemo = idNotificacion
        FROM dbo.Notificacion
        WHERE idUsuarioExterno = @idDestinatarioMensajeDemo
          AND tipo = N'MensajeReserva'
          AND mensaje = @mensajeNotificacionDemo
          AND urlDestino = @urlMensajeDemo
        ORDER BY idNotificacion DESC;

        IF @notificacionCreadaDemo = 1
        BEGIN
            UPDATE dbo.Notificacion
            SET fechaCreacion = @fechaEnvioMensajeDemo,
                leida = CASE WHEN @fechaLecturaMensajeDemo IS NULL THEN 0 ELSE 1 END
            WHERE idNotificacion = @idNotificacionDemo;
        END

        SET @numeroMensajeDemo += 1;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- ===========================================================================
-- Cuarta etapa:
--   7. Completar el seguimiento con pocas reservas futuras útiles.
--   8. Publicar dos encuestas generales vigentes con resultados de muestra.
--
-- El seguimiento queda representado con una sola reserva en cada estado:
--   - pendiente de aceptación en Sala Ensayo Palermo;
--   - aceptada y pendiente de pago en Estudio Sonoro Colegiales;
--   - aceptada y pagada en Estudio Fotográfico Norte (segunda etapa).
--
-- Las respuestas de muestra pertenecen a otras cuentas y a visitantes
-- anónimos. Ana y Marcos quedan sin responder para permitir la demostración
-- en vivo, tanto con sesión como sin ella.
-- ===========================================================================

IF OBJECT_ID(N'dbo.sp_Reserva_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Reserva_AceptarSiDisponible', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Encuesta_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_Encuesta_CambiarEstado', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_PreguntaEncuesta_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_OpcionPregunta_Insertar', N'P') IS NULL
   OR OBJECT_ID(N'dbo.sp_RespuestaEncuesta_Registrar', N'P') IS NULL
BEGIN
    THROW 51717, 'Primero deben ejecutarse los scripts de reservas y encuestas.', 1;
END
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @idAnaEtapa4 INT =
    (
        SELECT idUsuarioExterno FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'ana.solicitante@stageup.test' AND activo = 1
    );
    DECLARE @idMarcosEtapa4 INT =
    (
        SELECT idUsuarioExterno FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'marcos.gestor@stageup.test' AND activo = 1
    );
    DECLARE @idDanteEtapa4 INT =
    (
        SELECT idUsuarioExterno FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'dantemajeras@gmail.com' AND activo = 1
    );
    DECLARE @idMariaEtapa4 INT =
    (
        SELECT idUsuarioExterno FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'polioly.oficial@gmail.com' AND activo = 1
    );
    DECLARE @idClaraEtapa4 INT =
    (
        SELECT idUsuarioExterno FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'clara.espacios@stageup.test' AND activo = 1
    );
    DECLARE @idValeriaEtapa4 INT =
    (
        SELECT idUsuarioExterno FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'valeria.produccion@stageup.test' AND activo = 1
    );

    IF @idAnaEtapa4 IS NULL OR @idMarcosEtapa4 IS NULL OR @idDanteEtapa4 IS NULL
       OR @idMariaEtapa4 IS NULL OR @idClaraEtapa4 IS NULL OR @idValeriaEtapa4 IS NULL
    BEGIN
        THROW 51718, 'No se encontraron todas las cuentas requeridas por los datos demo.', 1;
    END

    DECLARE @idSalaEnsayoEtapa4 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa4 AND nombreEspacio = N'Sala Ensayo Palermo' AND activo = 1
    );
    DECLARE @idEstudioSonoroEtapa4 INT =
    (
        SELECT idEspacioArtistico FROM dbo.EspacioArtistico
        WHERE idUsuarioGestor = @idMarcosEtapa4 AND nombreEspacio = N'Estudio Sonoro Colegiales' AND activo = 1
    );

    IF @idSalaEnsayoEtapa4 IS NULL OR @idEstudioSonoroEtapa4 IS NULL
        THROW 51719, 'No se encontraron los espacios requeridos para las reservas futuras.', 1;

    DECLARE @fechaReservaPendienteDemo DATE;
    DECLARE @fechaReservaAceptadaDemo DATE;
    DECLARE @minutoDesdeEtapa4 SMALLINT = 840;
    DECLARE @minutoHastaEtapa4 SMALLINT = 960;

    -- 1900-01-01 fue lunes. Esta fórmula convierte cada fecha en 1=lunes,
    -- ..., 7=domingo sin depender de DATEFIRST ni del idioma del servidor.
    ;WITH ProximosDias AS
    (
        SELECT numero
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) dias(numero)
    )
    SELECT TOP (1) @fechaReservaPendienteDemo = DATEADD(DAY, dias.numero, CONVERT(DATE, GETDATE()))
    FROM ProximosDias dias
    WHERE EXISTS
    (
        SELECT 1 FROM dbo.FranjaEspacio franja
        WHERE franja.idEspacioArtistico = @idSalaEnsayoEtapa4
          AND franja.bloqueado = 0
          AND franja.fecha IS NULL
          AND franja.diaSemana = ((DATEDIFF(DAY, '19000101', DATEADD(DAY, dias.numero, CONVERT(DATE, GETDATE()))) % 7) + 1)
          AND @minutoDesdeEtapa4 >= franja.minutoDesde
          AND @minutoHastaEtapa4 <= franja.minutoHasta
    )
    ORDER BY dias.numero;

    ;WITH ProximosDias AS
    (
        SELECT numero
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) dias(numero)
    )
    SELECT TOP (1) @fechaReservaAceptadaDemo = DATEADD(DAY, dias.numero, CONVERT(DATE, GETDATE()))
    FROM ProximosDias dias
    WHERE DATEADD(DAY, dias.numero, CONVERT(DATE, GETDATE())) > @fechaReservaPendienteDemo
      AND EXISTS
      (
          SELECT 1 FROM dbo.FranjaEspacio franja
          WHERE franja.idEspacioArtistico = @idEstudioSonoroEtapa4
            AND franja.bloqueado = 0
            AND franja.fecha IS NULL
            AND franja.diaSemana = ((DATEDIFF(DAY, '19000101', DATEADD(DAY, dias.numero, CONVERT(DATE, GETDATE()))) % 7) + 1)
            AND @minutoDesdeEtapa4 >= franja.minutoDesde
            AND @minutoHastaEtapa4 <= franja.minutoHasta
      )
    ORDER BY dias.numero;

    IF @fechaReservaPendienteDemo IS NULL OR @fechaReservaAceptadaDemo IS NULL
        THROW 51720, 'No se encontraron próximos días disponibles para las reservas demo.', 1;

    DECLARE @precioSalaEnsayoEtapa4 DECIMAL(18,2);
    DECLARE @monedaSalaEnsayoEtapa4 NVARCHAR(3);
    DECLARE @precioEstudioSonoroEtapa4 DECIMAL(18,2);
    DECLARE @monedaEstudioSonoroEtapa4 NVARCHAR(3);
    DECLARE @importeSalaEnsayoEtapa4 DECIMAL(18,2);
    DECLARE @importeEstudioSonoroEtapa4 DECIMAL(18,2);
    DECLARE @fechaLimitePagoEtapa4 DATETIME = DATEADD(HOUR, 48, GETDATE());

    SELECT @precioSalaEnsayoEtapa4 = precioHora,
           @monedaSalaEnsayoEtapa4 = ISNULL(moneda, N'ARS')
    FROM dbo.FichaEspacio
    WHERE idEspacioArtistico = @idSalaEnsayoEtapa4;

    SELECT @precioEstudioSonoroEtapa4 = precioHora,
           @monedaEstudioSonoroEtapa4 = ISNULL(moneda, N'ARS')
    FROM dbo.FichaEspacio
    WHERE idEspacioArtistico = @idEstudioSonoroEtapa4;

    IF @precioSalaEnsayoEtapa4 IS NULL OR @precioEstudioSonoroEtapa4 IS NULL
        THROW 51721, 'Uno de los espacios futuros no tiene precio configurado.', 1;

    SET @importeSalaEnsayoEtapa4 = @precioSalaEnsayoEtapa4 * 2;
    SET @importeEstudioSonoroEtapa4 = @precioEstudioSonoroEtapa4 * 2;

    DECLARE @comentarioReservaPendienteDemo NVARCHAR(1000) =
        N'Ensayo de escenas y movimientos con utilería liviana para una muestra independiente.';
    DECLARE @comentarioReservaAceptadaDemo NVARCHAR(1000) =
        N'Grabación de una pieza de voz y música para el avance de una producción artística.';
    DECLARE @idReservaPendienteDemo INT;
    DECLARE @idReservaAceptadaDemo INT;
    DECLARE @reservaPendienteNueva BIT = 0;
    DECLARE @reservaAceptadaNueva BIT = 0;
    DECLARE @NuevaReservaEtapa4 TABLE (idReserva INT NOT NULL);
    DECLARE @AceptacionEtapa4 TABLE (seAcepto BIT NOT NULL);

    SELECT @idReservaPendienteDemo = idReserva
    FROM dbo.Reserva
    WHERE idEspacioArtistico = @idSalaEnsayoEtapa4
      AND idUsuarioExternoSolicitante = @idAnaEtapa4
      AND comentarioSolicitante = @comentarioReservaPendienteDemo;

    IF @idReservaPendienteDemo IS NULL
    BEGIN
        INSERT INTO @NuevaReservaEtapa4 (idReserva)
        EXEC dbo.sp_Reserva_Insertar
            @idEspacioArtistico = @idSalaEnsayoEtapa4,
            @idUsuarioExternoSolicitante = @idAnaEtapa4,
            @fechaSolicitada = @fechaReservaPendienteDemo,
            @comentarioSolicitante = @comentarioReservaPendienteDemo,
            @minutoDesde = @minutoDesdeEtapa4,
            @minutoHasta = @minutoHastaEtapa4,
            @precioHoraPactado = @precioSalaEnsayoEtapa4,
            @moneda = @monedaSalaEnsayoEtapa4,
            @importeEstimado = @importeSalaEnsayoEtapa4;

        SELECT @idReservaPendienteDemo = idReserva FROM @NuevaReservaEtapa4;
        SET @reservaPendienteNueva = 1;
    END

    DELETE FROM @NuevaReservaEtapa4;

    SELECT @idReservaAceptadaDemo = idReserva
    FROM dbo.Reserva
    WHERE idEspacioArtistico = @idEstudioSonoroEtapa4
      AND idUsuarioExternoSolicitante = @idAnaEtapa4
      AND comentarioSolicitante = @comentarioReservaAceptadaDemo;

    IF @idReservaAceptadaDemo IS NULL
    BEGIN
        INSERT INTO @NuevaReservaEtapa4 (idReserva)
        EXEC dbo.sp_Reserva_Insertar
            @idEspacioArtistico = @idEstudioSonoroEtapa4,
            @idUsuarioExternoSolicitante = @idAnaEtapa4,
            @fechaSolicitada = @fechaReservaAceptadaDemo,
            @comentarioSolicitante = @comentarioReservaAceptadaDemo,
            @minutoDesde = @minutoDesdeEtapa4,
            @minutoHasta = @minutoHastaEtapa4,
            @precioHoraPactado = @precioEstudioSonoroEtapa4,
            @moneda = @monedaEstudioSonoroEtapa4,
            @importeEstimado = @importeEstudioSonoroEtapa4;

        SELECT @idReservaAceptadaDemo = idReserva FROM @NuevaReservaEtapa4;

        INSERT INTO @AceptacionEtapa4 (seAcepto)
        EXEC dbo.sp_Reserva_AceptarSiDisponible
            @idReserva = @idReservaAceptadaDemo,
            @comentarioResolucion = N'Solicitud aceptada. El estudio quedó reservado a la espera del pago.',
            @fechaLimitePago = @fechaLimitePagoEtapa4;

        IF NOT EXISTS (SELECT 1 FROM @AceptacionEtapa4 WHERE seAcepto = 1)
            THROW 51722, 'No se pudo aceptar la reserva futura pendiente de pago.', 1;

        SET @reservaAceptadaNueva = 1;
    END

    IF @reservaPendienteNueva = 1
       AND NOT EXISTS
       (
           SELECT 1 FROM dbo.Notificacion
           WHERE idUsuarioExterno = @idMarcosEtapa4
             AND tipo = N'SolicitudReserva'
             AND urlDestino = N'~/DetalleReserva.aspx?id=' + CONVERT(NVARCHAR(20), @idReservaPendienteDemo)
       )
    BEGIN
        DECLARE @urlReservaPendienteDemo NVARCHAR(300) =
            N'~/DetalleReserva.aspx?id=' + CONVERT(NVARCHAR(20), @idReservaPendienteDemo);

        EXEC dbo.sp_Notificacion_Insertar
            @idUsuarioExterno = @idMarcosEtapa4,
            @tipo = N'SolicitudReserva',
            @mensaje = N'Recibiste una nueva solicitud de reserva para "Sala Ensayo Palermo".',
            @urlDestino = @urlReservaPendienteDemo,
            @titulo = N'Nueva solicitud de reserva';
    END

    IF @reservaAceptadaNueva = 1
       AND NOT EXISTS
       (
           SELECT 1 FROM dbo.Notificacion
           WHERE idUsuarioExterno = @idAnaEtapa4
             AND tipo = N'ReservaAceptada'
             AND urlDestino = N'~/DetalleReserva.aspx?id=' + CONVERT(NVARCHAR(20), @idReservaAceptadaDemo)
       )
    BEGIN
        DECLARE @urlReservaAceptadaDemo NVARCHAR(300) =
            N'~/DetalleReserva.aspx?id=' + CONVERT(NVARCHAR(20), @idReservaAceptadaDemo);

        EXEC dbo.sp_Notificacion_Insertar
            @idUsuarioExterno = @idAnaEtapa4,
            @tipo = N'ReservaAceptada',
            @mensaje = N'Tu reserva para "Estudio Sonoro Colegiales" fue aceptada y está pendiente de pago.',
            @urlDestino = @urlReservaAceptadaDemo,
            @titulo = N'Reserva aceptada';
    END

    -- -----------------------------------------------------------------------
    -- Encuestas generales vigentes.
    -- -----------------------------------------------------------------------
    DECLARE @EncuestasDemo TABLE
    (
        numero INT PRIMARY KEY,
        titulo NVARCHAR(200) NOT NULL,
        descripcion NVARCHAR(1000) NOT NULL
    );

    INSERT INTO @EncuestasDemo (numero, titulo, descripcion)
    VALUES
        (1, N'Tu experiencia en StageUp',
         N'Queremos conocer cómo buscás y elegís espacios para seguir mejorando la plataforma y priorizar las funciones que más necesitás.'),
        (2, N'Cómo planificás tu próxima producción',
         N'Contanos en qué etapa trabajás y qué información o canales te ayudan a organizar una actividad artística con mayor confianza.');

    DECLARE @PreguntasOpcionesDemo TABLE
    (
        numeroEncuesta INT NOT NULL,
        ordenPregunta INT NOT NULL,
        pregunta NVARCHAR(300) NOT NULL,
        ordenOpcion INT NOT NULL,
        opcion NVARCHAR(200) NOT NULL,
        PRIMARY KEY (numeroEncuesta, ordenPregunta, ordenOpcion)
    );

    INSERT INTO @PreguntasOpcionesDemo
        (numeroEncuesta, ordenPregunta, pregunta, ordenOpcion, opcion)
    VALUES
        (1, 1, N'¿Qué tipo de espacio buscás con mayor frecuencia?', 1, N'Salas de ensayo'),
        (1, 1, N'¿Qué tipo de espacio buscás con mayor frecuencia?', 2, N'Estudios fotográficos'),
        (1, 1, N'¿Qué tipo de espacio buscás con mayor frecuencia?', 3, N'Estudios de audio'),
        (1, 1, N'¿Qué tipo de espacio buscás con mayor frecuencia?', 4, N'Teatros o auditorios'),
        (1, 2, N'¿Qué factor considerás más importante al elegir?', 1, N'La ubicación'),
        (1, 2, N'¿Qué factor considerás más importante al elegir?', 2, N'El precio total'),
        (1, 2, N'¿Qué factor considerás más importante al elegir?', 3, N'El equipamiento incluido'),
        (1, 2, N'¿Qué factor considerás más importante al elegir?', 4, N'La reputación y las reseñas'),
        (1, 3, N'¿Cómo calificarías la facilidad para encontrar un espacio?', 1, N'Muy fácil'),
        (1, 3, N'¿Cómo calificarías la facilidad para encontrar un espacio?', 2, N'Fácil'),
        (1, 3, N'¿Cómo calificarías la facilidad para encontrar un espacio?', 3, N'Aceptable'),
        (1, 3, N'¿Cómo calificarías la facilidad para encontrar un espacio?', 4, N'Difícil'),
        (1, 4, N'¿Qué funcionalidad te gustaría encontrar próximamente?', 1, N'Favoritos y espacios guardados'),
        (1, 4, N'¿Qué funcionalidad te gustaría encontrar próximamente?', 2, N'Recomendaciones personalizadas'),
        (1, 4, N'¿Qué funcionalidad te gustaría encontrar próximamente?', 3, N'Promociones y beneficios'),
        (1, 4, N'¿Qué funcionalidad te gustaría encontrar próximamente?', 4, N'Calendario colaborativo'),
        (2, 1, N'¿En qué etapa se encuentra tu próximo proyecto?', 1, N'Idea y preproducción'),
        (2, 1, N'¿En qué etapa se encuentra tu próximo proyecto?', 2, N'Ensayos y pruebas'),
        (2, 1, N'¿En qué etapa se encuentra tu próximo proyecto?', 3, N'Producción o grabación'),
        (2, 1, N'¿En qué etapa se encuentra tu próximo proyecto?', 4, N'Función o presentación'),
        (2, 2, N'¿Qué información te ayuda más a decidir una reserva?', 1, N'Fotografías actualizadas'),
        (2, 2, N'¿Qué información te ayuda más a decidir una reserva?', 2, N'Equipamiento detallado'),
        (2, 2, N'¿Qué información te ayuda más a decidir una reserva?', 3, N'Disponibilidad y horarios'),
        (2, 2, N'¿Qué información te ayuda más a decidir una reserva?', 4, N'Reseñas verificadas'),
        (2, 3, N'¿Qué canal preferís para resolver dudas?', 1, N'Preguntas públicas del espacio'),
        (2, 3, N'¿Qué canal preferís para resolver dudas?', 2, N'Mensajería privada de la reserva'),
        (2, 3, N'¿Qué canal preferís para resolver dudas?', 3, N'Chat de soporte'),
        (2, 3, N'¿Qué canal preferís para resolver dudas?', 4, N'Correo electrónico');

    DECLARE @EncuestasProcesadas TABLE
    (
        numero INT PRIMARY KEY,
        idEncuesta INT NOT NULL
    );
    DECLARE @numeroEncuestaDemo INT = 1;
    DECLARE @idEncuestaDemo INT;
    DECLARE @tituloEncuestaDemo NVARCHAR(200);
    DECLARE @descripcionEncuestaDemo NVARCHAR(1000);
    DECLARE @encuestaNuevaDemo BIT;
    DECLARE @NuevaEncuestaDemo TABLE (idEncuesta INT NOT NULL);
    DECLARE @NuevaPreguntaEncuestaDemo TABLE (idPreguntaEncuesta INT NOT NULL);
    DECLARE @NuevaOpcionPreguntaDemo TABLE (idOpcionPregunta INT NOT NULL);
    DECLARE @fechaInicioEncuestaDemo DATETIME = DATEADD(DAY, -2, GETDATE());
    DECLARE @fechaVencimientoEncuestaDemo DATETIME = DATEADD(DAY, 45, GETDATE());

    WHILE @numeroEncuestaDemo <= 2
    BEGIN
        SELECT @idEncuestaDemo = NULL,
               @tituloEncuestaDemo = NULL,
               @descripcionEncuestaDemo = NULL,
               @encuestaNuevaDemo = 0;

        DELETE FROM @NuevaEncuestaDemo;
        DELETE FROM @NuevaPreguntaEncuestaDemo;
        DELETE FROM @NuevaOpcionPreguntaDemo;

        SELECT @tituloEncuestaDemo = titulo,
               @descripcionEncuestaDemo = descripcion
        FROM @EncuestasDemo
        WHERE numero = @numeroEncuestaDemo;

        IF (SELECT COUNT(*) FROM dbo.Encuesta WHERE titulo = @tituloEncuestaDemo) > 1
            THROW 51723, 'Existe más de una encuesta con uno de los títulos demo.', 1;

        SELECT @idEncuestaDemo = idEncuesta
        FROM dbo.Encuesta
        WHERE titulo = @tituloEncuestaDemo;

        IF @idEncuestaDemo IS NULL
        BEGIN
            INSERT INTO @NuevaEncuestaDemo (idEncuesta)
            EXEC dbo.sp_Encuesta_Insertar
                @titulo = @tituloEncuestaDemo,
                @descripcion = @descripcionEncuestaDemo,
                @fechaInicio = @fechaInicioEncuestaDemo,
                @fechaVencimiento = @fechaVencimientoEncuestaDemo,
                @publicoObjetivo = N'Todos';

            SELECT @idEncuestaDemo = idEncuesta FROM @NuevaEncuestaDemo;
            SET @encuestaNuevaDemo = 1;

            DECLARE @ordenPreguntaDemo INT = 1;
            DECLARE @cantidadPreguntasEncuestaDemo INT =
            (
                SELECT COUNT(DISTINCT ordenPregunta)
                FROM @PreguntasOpcionesDemo
                WHERE numeroEncuesta = @numeroEncuestaDemo
            );

            WHILE @ordenPreguntaDemo <= @cantidadPreguntasEncuestaDemo
            BEGIN
                DECLARE @idPreguntaEncuestaDemo INT;
                DECLARE @textoPreguntaEncuestaDemo NVARCHAR(300);

                DELETE FROM @NuevaPreguntaEncuestaDemo;

                SELECT TOP (1) @textoPreguntaEncuestaDemo = pregunta
                FROM @PreguntasOpcionesDemo
                WHERE numeroEncuesta = @numeroEncuestaDemo
                  AND ordenPregunta = @ordenPreguntaDemo;

                INSERT INTO @NuevaPreguntaEncuestaDemo (idPreguntaEncuesta)
                EXEC dbo.sp_PreguntaEncuesta_Insertar
                    @idEncuesta = @idEncuestaDemo,
                    @texto = @textoPreguntaEncuestaDemo,
                    @orden = @ordenPreguntaDemo;

                SELECT @idPreguntaEncuestaDemo = idPreguntaEncuesta FROM @NuevaPreguntaEncuestaDemo;

                DECLARE @ordenOpcionDemo INT = 1;
                WHILE @ordenOpcionDemo <= 4
                BEGIN
                    DECLARE @textoOpcionDemo NVARCHAR(200);

                    DELETE FROM @NuevaOpcionPreguntaDemo;

                    SELECT @textoOpcionDemo = opcion
                    FROM @PreguntasOpcionesDemo
                    WHERE numeroEncuesta = @numeroEncuestaDemo
                      AND ordenPregunta = @ordenPreguntaDemo
                      AND ordenOpcion = @ordenOpcionDemo;

                    INSERT INTO @NuevaOpcionPreguntaDemo (idOpcionPregunta)
                    EXEC dbo.sp_OpcionPregunta_Insertar
                        @idPreguntaEncuesta = @idPreguntaEncuestaDemo,
                        @texto = @textoOpcionDemo,
                        @orden = @ordenOpcionDemo;

                    SET @ordenOpcionDemo += 1;
                END

                SET @ordenPreguntaDemo += 1;
            END

            EXEC dbo.sp_Encuesta_CambiarEstado
                @idEncuesta = @idEncuestaDemo,
                @estado = N'Activa';
        END

        IF (SELECT COUNT(*) FROM dbo.PreguntaEncuesta WHERE idEncuesta = @idEncuestaDemo)
           <> (SELECT COUNT(DISTINCT ordenPregunta) FROM @PreguntasOpcionesDemo WHERE numeroEncuesta = @numeroEncuestaDemo)
        BEGIN
            THROW 51724, 'Una encuesta demo no tiene la cantidad esperada de preguntas.', 1;
        END

        IF EXISTS
        (
            SELECT 1
            FROM dbo.PreguntaEncuesta pregunta
            WHERE pregunta.idEncuesta = @idEncuestaDemo
              AND (SELECT COUNT(*) FROM dbo.OpcionPregunta opcion
                   WHERE opcion.idPreguntaEncuesta = pregunta.idPreguntaEncuesta) <> 4
        )
        BEGIN
            THROW 51725, 'Una pregunta demo no tiene las cuatro opciones esperadas.', 1;
        END

        INSERT INTO @EncuestasProcesadas (numero, idEncuesta)
        VALUES (@numeroEncuestaDemo, @idEncuestaDemo);

        SET @numeroEncuestaDemo += 1;
    END

    DECLARE @EncuestadosDemo TABLE
    (
        numero INT IDENTITY(1,1) PRIMARY KEY,
        numeroEncuesta INT NOT NULL,
        idUsuarioExterno INT NULL,
        identificadorNavegador CHAR(64) NOT NULL,
        patronOpciones INT NOT NULL
    );

    INSERT INTO @EncuestadosDemo
        (numeroEncuesta, idUsuarioExterno, identificadorNavegador, patronOpciones)
    SELECT encuesta.numero, participante.idUsuarioExterno,
           LOWER(CONVERT(CHAR(64), HASHBYTES('SHA2_256',
               N'StageUp-87-' + CONVERT(NVARCHAR(10), encuesta.numero) + N'-' + participante.codigo), 2)),
           participante.patron
    FROM (VALUES (1),(2)) encuesta(numero)
    CROSS JOIN
    (
        VALUES
            (@idDanteEtapa4, N'Dante', 1),
            (@idMariaEtapa4, N'Maria', 2),
            (@idClaraEtapa4, N'Clara', 3),
            (@idValeriaEtapa4, N'Valeria', 4),
            (NULL, N'VisitanteA', 1),
            (NULL, N'VisitanteB', 2)
    ) participante(idUsuarioExterno, codigo, patron);

    DECLARE @numeroRespuestaDemo INT = 1;
    DECLARE @cantidadRespuestasDemo INT = (SELECT COUNT(*) FROM @EncuestadosDemo);
    DECLARE @idEncuestaRespuestaDemo INT;
    DECLARE @idUsuarioRespuestaDemo INT;
    DECLARE @identificadorRespuestaDemo CHAR(64);
    DECLARE @patronRespuestaDemo INT;
    DECLARE @opcionesRespuestaDemo NVARCHAR(MAX);
    DECLARE @descripcionRespuestaDemo NVARCHAR(2000);
    DECLARE @NuevaRespuestaDemo TABLE (idRespuestaEncuesta INT NOT NULL);

    WHILE @numeroRespuestaDemo <= @cantidadRespuestasDemo
    BEGIN
        SELECT @idEncuestaRespuestaDemo = NULL,
               @idUsuarioRespuestaDemo = NULL,
               @identificadorRespuestaDemo = NULL,
               @patronRespuestaDemo = NULL,
               @opcionesRespuestaDemo = NULL,
               @descripcionRespuestaDemo = NULL;

        DELETE FROM @NuevaRespuestaDemo;

        SELECT @idEncuestaRespuestaDemo = procesada.idEncuesta,
               @idUsuarioRespuestaDemo = encuestado.idUsuarioExterno,
               @identificadorRespuestaDemo = encuestado.identificadorNavegador,
               @patronRespuestaDemo = encuestado.patronOpciones
        FROM @EncuestadosDemo encuestado
        INNER JOIN @EncuestasProcesadas procesada ON procesada.numero = encuestado.numeroEncuesta
        WHERE encuestado.numero = @numeroRespuestaDemo;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.RespuestaEncuesta respuesta
            WHERE respuesta.idEncuesta = @idEncuestaRespuestaDemo
              AND
              (
                  (@idUsuarioRespuestaDemo IS NOT NULL AND respuesta.idUsuarioExterno = @idUsuarioRespuestaDemo)
                  OR respuesta.identificadorAnonimo = @identificadorRespuestaDemo
                  OR respuesta.identificadorNavegador = @identificadorRespuestaDemo
              )
        )
        BEGIN
            SELECT @opcionesRespuestaDemo = STUFF
            (
                (
                    SELECT N',' + CONVERT(NVARCHAR(20), pregunta.idPreguntaEncuesta) + N':'
                           + CONVERT(NVARCHAR(20), opcion.idOpcionPregunta)
                    FROM dbo.PreguntaEncuesta pregunta
                    INNER JOIN dbo.OpcionPregunta opcion
                        ON opcion.idPreguntaEncuesta = pregunta.idPreguntaEncuesta
                       AND opcion.orden = ((pregunta.orden + @patronRespuestaDemo - 2) % 4) + 1
                    WHERE pregunta.idEncuesta = @idEncuestaRespuestaDemo
                    ORDER BY pregunta.orden
                    FOR XML PATH(N''), TYPE
                ).value(N'.', N'NVARCHAR(MAX)'),
                1, 1, N''
            );

            SET @descripcionRespuestaDemo = CASE
                WHEN @idUsuarioRespuestaDemo IS NULL
                    THEN N'Un visitante sin sesión respondió una encuesta de demostración.'
                ELSE N'Un usuario respondió una encuesta de demostración.'
            END;

            INSERT INTO @NuevaRespuestaDemo (idRespuestaEncuesta)
            EXEC dbo.sp_RespuestaEncuesta_Registrar
                @idEncuesta = @idEncuestaRespuestaDemo,
                @idUsuarioExterno = @idUsuarioRespuestaDemo,
                @identificadorNavegador = @identificadorRespuestaDemo,
                @opciones = @opcionesRespuestaDemo,
                @descripcionBitacora = @descripcionRespuestaDemo;
        END

        SET @numeroRespuestaDemo += 1;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- ===========================================================================
-- Sexta etapa:
--  11. Preparar solicitudes de soporte realistas en distintos estados.
--
-- Las tarjetas de la billetera se cargan con 87_CargarTarjetasDemo.ps1 para
-- que pasen por BLL_Billetera y se cifren con las claves de cada instalación.
-- No se insertan números ni datos cifrados fijos desde SQL.
-- ===========================================================================

IF OBJECT_ID(N'dbo.Ticket', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TicketMensaje', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TicketActualizacion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Notificacion', N'U') IS NULL
BEGIN
    THROW 51750, 'Primero deben ejecutarse los scripts del módulo de soporte.', 1;
END
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ahoraSoporteDemo DATETIME = GETDATE();
    DECLARE @idAnaSoporte INT;
    DECLARE @idMarcosSoporte INT;
    DECLARE @idAgenteSoporte INT;
    DECLARE @idReservaComprobante INT;
    DECLARE @idReservaEquipamiento INT;
    DECLARE @idTicketResuelto INT;
    DECLARE @idTicketEnProceso INT;
    DECLARE @idTicketPendiente INT;
    DECLARE @ticketResueltoNuevo BIT = 0;
    DECLARE @ticketEnProcesoNuevo BIT = 0;
    DECLARE @ticketPendienteNuevo BIT = 0;

    IF (SELECT COUNT(*) FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'ana.solicitante@stageup.test') <> 1
        THROW 51751, 'No se encontró una única cuenta de demostración para Ana.', 1;

    IF (SELECT COUNT(*) FROM dbo.UsuarioExterno
        WHERE correoElectronico = N'marcos.gestor@stageup.test') <> 1
        THROW 51752, 'No se encontró una única cuenta de demostración para Marcos.', 1;

    IF (SELECT COUNT(*) FROM dbo.UsuarioInterno
        WHERE correoElectronico = N'admin@stageup.test' AND activo = 1) <> 1
        THROW 51753, 'No se encontró una única cuenta interna activa para atender soporte.', 1;

    SELECT @idAnaSoporte = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = N'ana.solicitante@stageup.test';

    SELECT @idMarcosSoporte = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = N'marcos.gestor@stageup.test';

    SELECT @idAgenteSoporte = idUsuarioInterno
    FROM dbo.UsuarioInterno
    WHERE correoElectronico = N'admin@stageup.test' AND activo = 1;

    SELECT TOP (1) @idReservaComprobante = reserva.idReserva
    FROM dbo.Reserva reserva
    INNER JOIN dbo.EspacioArtistico espacio
        ON espacio.idEspacioArtistico = reserva.idEspacioArtistico
    WHERE reserva.idUsuarioExternoSolicitante = @idAnaSoporte
      AND reserva.estadoReserva = N'Finalizada'
      AND espacio.nombreEspacio = N'Estudio Fotográfico Norte'
    ORDER BY reserva.fechaSolicitada DESC, reserva.idReserva DESC;

    SELECT TOP (1) @idReservaEquipamiento = reserva.idReserva
    FROM dbo.Reserva reserva
    INNER JOIN dbo.EspacioArtistico espacio
        ON espacio.idEspacioArtistico = reserva.idEspacioArtistico
    WHERE reserva.idUsuarioExternoSolicitante = @idAnaSoporte
      AND reserva.estadoReserva = N'Aceptada'
      AND espacio.nombreEspacio = N'Estudio Sonoro Colegiales'
    ORDER BY reserva.fechaSolicitada DESC, reserva.idReserva DESC;

    IF @idReservaComprobante IS NULL OR @idReservaEquipamiento IS NULL
        THROW 51754, 'No se encontraron las reservas necesarias para los casos de soporte de demostración.', 1;

    -- Caso resuelto: conversación completa y reserva histórica relacionada.
    SELECT TOP (1) @idTicketResuelto = idTicket
    FROM dbo.Ticket
    WHERE idUsuarioExterno = @idAnaSoporte
      AND asunto = N'No encontraba el comprobante de mi reserva fotográfica'
    ORDER BY idTicket;

    IF @idTicketResuelto IS NULL
    BEGIN
        DECLARE @fechaTicketResuelto DATETIME = DATEADD(DAY, -12, @ahoraSoporteDemo);
        DECLARE @fechaRespuestaResuelta DATETIME = DATEADD(HOUR, 3, @fechaTicketResuelto);
        DECLARE @fechaMensajeAnaResuelto DATETIME = DATEADD(HOUR, 5, @fechaTicketResuelto);
        DECLARE @fechaResolucionDemo DATETIME = DATEADD(HOUR, 6, @fechaTicketResuelto);

        INSERT INTO dbo.Ticket
            (idUsuarioExterno, idReservaAsociada, categoria, asunto, descripcion,
             informacionAdicional, origen, estado, idUsuarioInternoAsignado,
             fechaCreacion, fechaUltimaActividad, fechaResolucion, fechaCierre,
             fechaUltimaLecturaUsuario, fechaUltimaLecturaSoporte)
        VALUES
            (@idAnaSoporte, @idReservaComprobante, N'Pago',
             N'No encontraba el comprobante de mi reserva fotográfica',
             N'La reserva figura finalizada y el pago está registrado, pero no encontraba dónde descargar el comprobante para presentarlo en la producción.',
             N'Necesito el comprobante correspondiente a la reserva relacionada.',
             N'Reserva', N'Resuelta', @idAgenteSoporte,
             @fechaTicketResuelto, @fechaResolucionDemo, @fechaResolucionDemo, NULL,
             DATEADD(MINUTE, 30, @fechaResolucionDemo), DATEADD(MINUTE, 30, @fechaResolucionDemo));

        SET @idTicketResuelto = SCOPE_IDENTITY();
        SET @ticketResueltoNuevo = 1;

        INSERT INTO dbo.TicketMensaje
            (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio, esInicial)
        VALUES
            (@idTicketResuelto, NULL, @idAgenteSoporte,
             N'Hola Ana. Revisamos la reserva y el pago está correctamente registrado. Podés abrir el detalle de la reserva y elegir “Ver comprobante” para descargarlo.',
             @fechaRespuestaResuelta, 0),
            (@idTicketResuelto, @idAnaSoporte, NULL,
             N'Ya lo encontré y pude descargarlo. ¿El mismo archivo sirve como comprobante para la producción?',
             @fechaMensajeAnaResuelto, 0),
            (@idTicketResuelto, NULL, @idAgenteSoporte,
             N'Sí, ese PDF contiene el número de operación, el importe y los datos de la reserva. Es el comprobante correcto. Dejamos la solicitud resuelta.',
             @fechaResolucionDemo, 0);

        INSERT INTO dbo.TicketActualizacion
            (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo,
             idUsuarioInterno, idUsuarioExterno, detalle, fecha)
        VALUES
            (@idTicketResuelto, N'Alta', NULL, N'Pendiente', NULL, @idAnaSoporte,
             N'Solicitud registrada.', @fechaTicketResuelto),
            (@idTicketResuelto, N'Asignacion', NULL, NULL, @idAgenteSoporte, NULL,
             N'Asignada a Admin StageUp al responder.', @fechaRespuestaResuelta),
            (@idTicketResuelto, N'CambioEstado', N'Pendiente', N'EnProceso', @idAgenteSoporte, NULL,
             N'Respuesta registrada: la solicitud está siendo atendida.', @fechaRespuestaResuelta),
            (@idTicketResuelto, N'CambioEstado', N'EnProceso', N'Resuelta', @idAgenteSoporte, NULL,
             N'Respuesta registrada y solicitud marcada como resuelta.', @fechaResolucionDemo);

        INSERT INTO dbo.Notificacion
            (idUsuarioExterno, tipo, titulo, mensaje, urlDestino, leida, fechaCreacion)
        VALUES
            (@idAnaSoporte, N'RespuestaTicket', N'Soporte respondió tu solicitud',
             N'Soporte respondió tu consulta sobre el comprobante y la marcó como resuelta.',
             N'~/Soporte.aspx?ver=' + CONVERT(NVARCHAR(20), @idTicketResuelto), 1, @fechaResolucionDemo);
    END

    -- Caso en proceso: soporte respondió y Ana agregó una precisión que el
    -- equipo todavía no leyó. Sirve para mostrar los indicadores de novedad.
    SELECT TOP (1) @idTicketEnProceso = idTicket
    FROM dbo.Ticket
    WHERE idUsuarioExterno = @idAnaSoporte
      AND asunto = N'Consulta sobre el ingreso y el equipamiento incluido'
    ORDER BY idTicket;

    IF @idTicketEnProceso IS NULL
    BEGIN
        DECLARE @fechaTicketEnProceso DATETIME = DATEADD(DAY, -1, @ahoraSoporteDemo);
        DECLARE @fechaRespuestaEnProceso DATETIME = DATEADD(HOUR, 4, @fechaTicketEnProceso);
        DECLARE @fechaMensajeAnaEnProceso DATETIME = DATEADD(HOUR, 20, @fechaTicketEnProceso);

        INSERT INTO dbo.Ticket
            (idUsuarioExterno, idReservaAsociada, categoria, asunto, descripcion,
             informacionAdicional, origen, estado, idUsuarioInternoAsignado,
             fechaCreacion, fechaUltimaActividad, fechaResolucion, fechaCierre,
             fechaUltimaLecturaUsuario, fechaUltimaLecturaSoporte)
        VALUES
            (@idAnaSoporte, @idReservaEquipamiento, N'Reserva',
             N'Consulta sobre el ingreso y el equipamiento incluido',
             N'Necesito confirmar con cuánto tiempo de anticipación podemos ingresar y qué elementos de audio están incluidos en la reserva.',
             N'La consulta corresponde al Estudio Sonoro Colegiales y al ensayo próximo.',
             N'Reserva', N'EnProceso', @idAgenteSoporte,
             @fechaTicketEnProceso, @fechaMensajeAnaEnProceso, NULL, NULL,
             @fechaMensajeAnaEnProceso, @fechaRespuestaEnProceso);

        SET @idTicketEnProceso = SCOPE_IDENTITY();
        SET @ticketEnProcesoNuevo = 1;

        INSERT INTO dbo.TicketMensaje
            (idTicket, idUsuarioExterno, idUsuarioInterno, mensaje, fechaEnvio, esInicial)
        VALUES
            (@idTicketEnProceso, NULL, @idAgenteSoporte,
             N'Hola Ana. La reserva incluye consola, dos micrófonos y parlantes de monitoreo. El ingreso habitual es quince minutos antes; estamos confirmando si ese día pueden ingresar con mayor anticipación.',
             @fechaRespuestaEnProceso, 0),
            (@idTicketEnProceso, @idAnaSoporte, NULL,
             N'Perfecto, gracias. ¿Podrían confirmar también si hay dos pies de micrófono disponibles? Con eso ya quedaría resuelta mi consulta.',
             @fechaMensajeAnaEnProceso, 0);

        INSERT INTO dbo.TicketActualizacion
            (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo,
             idUsuarioInterno, idUsuarioExterno, detalle, fecha)
        VALUES
            (@idTicketEnProceso, N'Alta', NULL, N'Pendiente', NULL, @idAnaSoporte,
             N'Solicitud registrada.', @fechaTicketEnProceso),
            (@idTicketEnProceso, N'Asignacion', NULL, NULL, @idAgenteSoporte, NULL,
             N'Asignada a Admin StageUp al responder.', @fechaRespuestaEnProceso),
            (@idTicketEnProceso, N'CambioEstado', N'Pendiente', N'EnProceso', @idAgenteSoporte, NULL,
             N'Respuesta registrada: la solicitud está siendo atendida.', @fechaRespuestaEnProceso);

        INSERT INTO dbo.Notificacion
            (idUsuarioExterno, tipo, titulo, mensaje, urlDestino, leida, fechaCreacion)
        VALUES
            (@idAnaSoporte, N'RespuestaTicket', N'Soporte respondió tu solicitud',
             N'Soporte respondió tu consulta sobre el ingreso y el equipamiento del estudio.',
             N'~/Soporte.aspx?ver=' + CONVERT(NVARCHAR(20), @idTicketEnProceso), 1, @fechaRespuestaEnProceso);
    END

    -- Caso pendiente: Marcos lo registró y todavía no fue tomado por soporte.
    SELECT TOP (1) @idTicketPendiente = idTicket
    FROM dbo.Ticket
    WHERE idUsuarioExterno = @idMarcosSoporte
      AND asunto = N'No puedo actualizar el horario de uno de mis espacios'
    ORDER BY idTicket;

    IF @idTicketPendiente IS NULL
    BEGIN
        DECLARE @fechaTicketPendiente DATETIME = DATEADD(HOUR, -2, @ahoraSoporteDemo);

        INSERT INTO dbo.Ticket
            (idUsuarioExterno, idReservaAsociada, categoria, asunto, descripcion,
             informacionAdicional, origen, estado, idUsuarioInternoAsignado,
             fechaCreacion, fechaUltimaActividad, fechaResolucion, fechaCierre,
             fechaUltimaLecturaUsuario, fechaUltimaLecturaSoporte)
        VALUES
            (@idMarcosSoporte, NULL, N'Espacio',
             N'No puedo actualizar el horario de uno de mis espacios',
             N'Al editar la disponibilidad del sábado guardo el nuevo horario, pero al volver al calendario sigo viendo el bloque anterior.',
             N'Ocurre en Estudio Sonoro Colegiales. Probé cerrar sesión y volver a ingresar.',
             N'Asistente', N'Pendiente', NULL,
             @fechaTicketPendiente, @fechaTicketPendiente, NULL, NULL,
             @fechaTicketPendiente, NULL);

        SET @idTicketPendiente = SCOPE_IDENTITY();
        SET @ticketPendienteNuevo = 1;

        INSERT INTO dbo.TicketActualizacion
            (idTicket, tipoActualizacion, estadoAnterior, estadoNuevo,
             idUsuarioInterno, idUsuarioExterno, detalle, fecha)
        VALUES
            (@idTicketPendiente, N'Alta', NULL, N'Pendiente', NULL, @idMarcosSoporte,
             N'Solicitud registrada desde el asistente.', @fechaTicketPendiente);

        INSERT INTO dbo.Notificacion
            (idUsuarioExterno, tipo, titulo, mensaje, urlDestino, leida, fechaCreacion)
        VALUES
            (@idMarcosSoporte, N'SolicitudSoporteRegistrada', N'Solicitud de soporte registrada',
             N'Registramos tu solicitud sobre la disponibilidad del espacio. Está pendiente de revisión.',
             N'~/Soporte.aspx?ver=' + CONVERT(NVARCHAR(20), @idTicketPendiente), 0, @fechaTicketPendiente);
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- ===========================================================================
-- Séptima etapa:
--  12. Preparar usuarios externos e internos para la demostración.
--
-- Se ejecuta mediante 87_PrepararUsuariosDemo.ps1 porque las contraseñas
-- deben pasar por ProtectorDeCredenciales (hash PBKDF2 y sal aleatoria), y los
-- roles y usuarios internos por sus BLL para conservar validaciones y bitácora.
-- El cargador activa a Lucía como solicitante, crea su solicitud pendiente de
-- gestora, unifica las claves de las cuentas demo y configura cuatro roles de
-- negocio con permisos mínimos. No guarda contraseñas en texto plano en SQL.
-- ===========================================================================

