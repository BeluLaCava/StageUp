USE StageUp;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.EspacioArtistico', N'U') IS NULL
BEGIN
    RAISERROR(N'Primero debe ejecutarse Database/02_EspacioArtistico.sql.', 16, 1);
END
GO

IF OBJECT_ID(N'dbo.FichaEspacio', N'U') IS NULL
BEGIN
    RAISERROR(N'Primero debe ejecutarse Database/09_FichaEspacio.sql.', 16, 1);
END
GO

IF OBJECT_ID(N'dbo.EspacioFoto', N'U') IS NULL
BEGIN
    RAISERROR(N'Primero debe ejecutarse Database/13_EspacioFoto.sql.', 16, 1);
END
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'marcos.gestor@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaActivacion)
    VALUES
        (N'Marcos', N'Gestor', N'marcos.gestor@stageup.test',
         N'100000.4UyTqCiktr0QpVcUEo1G+Q==.zUDI6gn9wRbCO3MDyZaXdOCahRMYvqNLic3+LCBtXY0=',
         N'Activa', N'GestorEspacios', 1, 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'clara.espacios@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaActivacion)
    VALUES
        (N'Clara', N'Espacios', N'clara.espacios@stageup.test',
         N'100000.4UyTqCiktr0QpVcUEo1G+Q==.zUDI6gn9wRbCO3MDyZaXdOCahRMYvqNLic3+LCBtXY0=',
         N'Activa', N'GestorEspacios', 1, 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'valeria.produccion@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaActivacion)
    VALUES
        (N'Valeria', N'Producción', N'valeria.produccion@stageup.test',
         N'100000.4UyTqCiktr0QpVcUEo1G+Q==.zUDI6gn9wRbCO3MDyZaXdOCahRMYvqNLic3+LCBtXY0=',
         N'Activa', N'GestorEspacios', 1, 1, GETDATE(), GETDATE());
END

DECLARE @EspaciosDemo TABLE
(
    nombreEspacio       NVARCHAR(300)   NOT NULL,
    descripcion         NVARCHAR(2000)  NOT NULL,
    tipoEspacio         NVARCHAR(200)   NOT NULL,
    gestorCorreo        NVARCHAR(300)   NOT NULL,
    provincia           NVARCHAR(100)   NOT NULL,
    ciudad              NVARCHAR(150)   NOT NULL,
    direccion           NVARCHAR(300)   NOT NULL,
    capacidadMaxima     INT             NOT NULL,
    precioHora          DECIMAL(10,2)   NOT NULL,
    moneda              NVARCHAR(3)     NOT NULL,
    tipoPiso            NVARCHAR(100)   NULL,
    detalleEquipamiento NVARCHAR(1000)  NULL,
    fotoRuta            NVARCHAR(300)   NOT NULL,
    equipamiento        NVARCHAR(500)   NOT NULL
);

INSERT INTO @EspaciosDemo
    (nombreEspacio, descripcion, tipoEspacio, gestorCorreo, provincia, ciudad, direccion,
     capacidadMaxima, precioHora, moneda, tipoPiso, detalleEquipamiento, fotoRuta, equipamiento)
VALUES
    (N'Teatro Colón',
     N'Teatro histórico de gran formato para conciertos, ópera, ballet y producciones escénicas de alta escala.',
     N'Teatro', N'marcos.gestor@stageup.test', N'Buenos Aires', N'CABA', N'Cerrito 628',
     2500, 5000000.00, N'ARS', N'Madera escénica',
     N'Sala principal con escenario italiano, iluminación profesional, sonido, camarines y asistencia técnica.',
     N'~/Content/Uploads/Espacios/c5738538bc214a06b501ec98d573c4ca.jpg',
     N'ESCENARIO,ILUMINACION,SONIDO,EQUIPAMIENTO,INSTRUMENTOS'),

    (N'Teatro San Telmo',
     N'Sala teatral de formato medio preparada para obras independientes, muestras y presentaciones musicales.',
     N'Teatro', N'clara.espacios@stageup.test', N'Buenos Aires', N'CABA', N'Defensa 1120',
     420, 380000.00, N'ARS', N'Madera',
     N'Escenario frontal, parrilla de luces, consola de sonido y camarines para elenco.',
     N'~/Content/Uploads/Espacios/6d96f757361f4248b419156303e6cb3c.jpg',
     N'ESCENARIO,ILUMINACION,SONIDO,EQUIPAMIENTO'),

    (N'Teatro del Parque',
     N'Teatro barrial con platea cómoda para muestras, ciclos de música de cámara y obras de pequeño elenco.',
     N'Teatro', N'valeria.produccion@stageup.test', N'Buenos Aires', N'La Plata', N'Calle 50 790',
     280, 210000.00, N'ARS', N'Madera',
     N'Escenario elevado, luces regulables, sonido de sala y espacio técnico para operación.',
     N'~/Content/Uploads/Espacios/04612b68b6af455ab56adc148dc54a88.jpg',
     N'ESCENARIO,ILUMINACION,SONIDO'),

    (N'Teatro Independencia Palermo',
     N'Espacio teatral flexible para teatro independiente, stand up, ensayos abiertos y charlas performáticas.',
     N'Teatro', N'clara.espacios@stageup.test', N'Buenos Aires', N'CABA', N'Gorriti 4850',
     180, 165000.00, N'ARS', N'Piso técnico',
     N'Caja negra adaptable, luces escénicas, sonido, sillas móviles y apoyo de producción.',
     N'~/Content/Uploads/Espacios/6d96f757361f4248b419156303e6cb3c.jpg',
     N'ESCENARIO,ILUMINACION,SONIDO,EQUIPAMIENTO'),

    (N'Sala Principal StageUp',
     N'Sala amplia y equipada para ensayos musicales, teatrales y encuentros creativos con equipos técnicos disponibles.',
     N'Sala', N'marcos.gestor@stageup.test', N'Buenos Aires', N'CABA', N'Av. Corrientes 1540',
     120, 95000.00, N'ARS', N'Piso vinílico',
     N'Cuenta con sonido, iluminación básica, mobiliario móvil y apoyo para armado de escenas.',
     N'~/Content/Uploads/Espacios/b2c3d4e5f64789a123b456c789d012a1.jpg',
     N'SONIDO,ILUMINACION,EQUIPAMIENTO'),

    (N'Sala Ensayo Palermo',
     N'Sala práctica para ensayos de bandas, teatro físico y preparación de muestras en grupos medianos.',
     N'Sala', N'marcos.gestor@stageup.test', N'Buenos Aires', N'CABA', N'Soler 4321',
     35, 28000.00, N'ARS', N'Goma acústica',
     N'Aislamiento acústico, consola simple, micrófonos, amplificadores y sillas móviles.',
     N'~/Content/Uploads/Espacios/b2c3d4e5f64789a123b456c789d012a1.jpg',
     N'SONIDO,INSTRUMENTOS,EQUIPAMIENTO'),

    (N'Sala Movimiento Sur',
     N'Sala luminosa para danza, entrenamiento corporal, seminarios de movimiento y clases grupales.',
     N'Sala', N'valeria.produccion@stageup.test', N'Buenos Aires', N'Avellaneda', N'Av. Mitre 725',
     45, 32000.00, N'ARS', N'Piso flotante',
     N'Espejos de pared, barras, piso apto danza, iluminación cálida y sistema de audio.',
     N'~/Content/Uploads/Espacios/c175ef7799514c2e9856d749d62de299.jpg',
     N'ESPEJOS,SONIDO,ILUMINACION,EQUIPAMIENTO'),

    (N'Sala Microescena Boedo',
     N'Sala compacta con disposición flexible para muestras íntimas, lecturas dramatizadas y ensayos con público.',
     N'Sala', N'clara.espacios@stageup.test', N'Buenos Aires', N'CABA', N'Boedo 845',
     80, 52000.00, N'ARS', N'Madera',
     N'Pequeño escenario, luces teatrales, sonido básico, telón y butacas móviles.',
     N'~/Content/Uploads/Espacios/b2c3d4e5f64789a123b456c789d012a1.jpg',
     N'ESCENARIO,ILUMINACION,SONIDO,EQUIPAMIENTO'),

    (N'Sala Acústica Norte',
     N'Sala tratada acústicamente para ensayos de cámara, grabaciones simples y presentaciones privadas.',
     N'Sala', N'valeria.produccion@stageup.test', N'Buenos Aires', N'Vicente López', N'Av. Maipú 1910',
     60, 60000.00, N'ARS', N'Alfombra técnica',
     N'Tratamiento acústico, piano vertical, atriles, micrófonos y consola de mezcla.',
     N'~/Content/Uploads/Espacios/b2c3d4e5f64789a123b456c789d012a1.jpg',
     N'SONIDO,INSTRUMENTOS,EQUIPAMIENTO'),

    (N'Sala Taller Cultural Once',
     N'Sala multiuso para talleres, casting, lecturas de guion, clínicas artísticas y ensayos escénicos.',
     N'Sala', N'clara.espacios@stageup.test', N'Buenos Aires', N'CABA', N'Pasteur 350',
     55, 42000.00, N'ARS', N'Cemento alisado',
     N'Mobiliario modular, luces móviles, parlantes, proyector y espacio de guardado temporal.',
     N'~/Content/Uploads/Espacios/b2c3d4e5f64789a123b456c789d012a1.jpg',
     N'ILUMINACION,SONIDO,EQUIPAMIENTO'),

    (N'Estudio Fotográfico Norte',
     N'Estudio con luces e infraestructura para sesiones fotográficas, contenido audiovisual y campañas pequeñas.',
     N'Estudio', N'marcos.gestor@stageup.test', N'Buenos Aires', N'Vicente López', N'Lavalle 1420',
     18, 36000.00, N'ARS', N'Cemento alisado',
     N'Fondos intercambiables, iluminación continua, flashes, mesa de producto y asistencia básica.',
     N'~/Content/Uploads/Espacios/a1b2c3d4e5f64789a123b456c789d012.jpg',
     N'ILUMINACION,EQUIPAMIENTO'),

    (N'Estudio Audiovisual Belgrano',
     N'Estudio preparado para entrevistas, reels, videoclips sencillos y producción de contenido digital.',
     N'Estudio', N'valeria.produccion@stageup.test', N'Buenos Aires', N'CABA', N'Juramento 2440',
     25, 48000.00, N'ARS', N'Cemento alisado',
     N'Ciclorama blanco, paneles LED, trípodes, micrófonos y cortinas blackout.',
     N'~/Content/Uploads/Espacios/a1b2c3d4e5f64789a123b456c789d012.jpg',
     N'ILUMINACION,SONIDO,EQUIPAMIENTO'),

    (N'Estudio Creativo Palermo',
     N'Estudio versátil para fotografía editorial, workshops, pruebas de cámara y producción de piezas visuales.',
     N'Estudio', N'clara.espacios@stageup.test', N'Buenos Aires', N'CABA', N'Costa Rica 5580',
     20, 41000.00, N'ARS', N'Madera clara',
     N'Luz natural, fondos textiles, mobiliario de apoyo, reflectores y mesa de maquillaje.',
     N'~/Content/Uploads/Espacios/a1b2c3d4e5f64789a123b456c789d012.jpg',
     N'ILUMINACION,EQUIPAMIENTO'),

    (N'Estudio Sonoro Colegiales',
     N'Estudio chico para podcast, locuciones, doblaje, grabación musical ligera y edición de audio.',
     N'Estudio', N'marcos.gestor@stageup.test', N'Buenos Aires', N'CABA', N'Conesa 875',
     12, 30000.00, N'ARS', N'Alfombra acústica',
     N'Cabina tratada, interfaz de audio, micrófonos condenser, auriculares y monitores.',
     N'~/Content/Uploads/Espacios/a1b2c3d4e5f64789a123b456c789d012.jpg',
     N'SONIDO,INSTRUMENTOS,EQUIPAMIENTO'),

    (N'Estudio Luz Natural San Telmo',
     N'Estudio para fotografía lifestyle, contenido de marca, lookbooks y producciones con luz natural.',
     N'Estudio', N'valeria.produccion@stageup.test', N'Buenos Aires', N'CABA', N'Chile 610',
     15, 34000.00, N'ARS', N'Pinotea',
     N'Ventanales amplios, fondos neutros, reflectores, percheros, mesa de styling y apoyo de producción.',
     N'~/Content/Uploads/Espacios/a1b2c3d4e5f64789a123b456c789d012.jpg',
     N'ILUMINACION,EQUIPAMIENTO');

DECLARE
    @nombreEspacio NVARCHAR(300),
    @descripcion NVARCHAR(2000),
    @tipoEspacio NVARCHAR(200),
    @gestorCorreo NVARCHAR(300),
    @provincia NVARCHAR(100),
    @ciudad NVARCHAR(150),
    @direccion NVARCHAR(300),
    @capacidadMaxima INT,
    @precioHora DECIMAL(10,2),
    @moneda NVARCHAR(3),
    @tipoPiso NVARCHAR(100),
    @detalleEquipamiento NVARCHAR(1000),
    @fotoRuta NVARCHAR(300),
    @equipamiento NVARCHAR(500),
    @idUsuarioGestor INT,
    @idEspacioArtistico INT;

DECLARE espacios_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT nombreEspacio, descripcion, tipoEspacio, gestorCorreo, provincia, ciudad, direccion,
       capacidadMaxima, precioHora, moneda, tipoPiso, detalleEquipamiento, fotoRuta, equipamiento
FROM @EspaciosDemo;

OPEN espacios_cursor;

FETCH NEXT FROM espacios_cursor INTO
    @nombreEspacio, @descripcion, @tipoEspacio, @gestorCorreo, @provincia, @ciudad, @direccion,
    @capacidadMaxima, @precioHora, @moneda, @tipoPiso, @detalleEquipamiento, @fotoRuta, @equipamiento;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @idUsuarioGestor = NULL;
    SET @idEspacioArtistico = NULL;

    SELECT @idUsuarioGestor = idUsuarioExterno
    FROM dbo.UsuarioExterno
    WHERE correoElectronico = @gestorCorreo;

    SELECT @idEspacioArtistico = idEspacioArtistico
    FROM dbo.EspacioArtistico
    WHERE nombreEspacio = @nombreEspacio;

    IF @idEspacioArtistico IS NULL
    BEGIN
        INSERT INTO dbo.EspacioArtistico
            (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio,
             publicado, activo, fechaAlta, fechaPublicacion)
        VALUES
            (@idUsuarioGestor, @nombreEspacio, @descripcion, @tipoEspacio, N'Publicado',
             1, 1, GETDATE(), GETDATE());

        SET @idEspacioArtistico = CONVERT(INT, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE dbo.EspacioArtistico
        SET idUsuarioGestor = @idUsuarioGestor,
            descripcion = @descripcion,
            tipoEspacio = @tipoEspacio,
            estadoEspacio = N'Publicado',
            publicado = 1,
            activo = 1,
            fechaPublicacion = COALESCE(fechaPublicacion, GETDATE()),
            fechaBaja = NULL,
            fechaUltimaModificacion = GETDATE()
        WHERE idEspacioArtistico = @idEspacioArtistico;

        DELETE FROM dbo.FichaEspacio WHERE idEspacioArtistico = @idEspacioArtistico;
    END

    INSERT INTO dbo.FichaEspacio
        (idEspacioArtistico, fotoRuta, provincia, ciudad, direccion, capacidadMaxima,
         precioHora, moneda, tipoPiso, detalleEquipamiento, fechaUltimaModificacion)
    VALUES
        (@idEspacioArtistico, @fotoRuta, @provincia, @ciudad, @direccion, @capacidadMaxima,
         @precioHora, @moneda, @tipoPiso, @detalleEquipamiento, GETDATE());

    INSERT INTO dbo.FichaEspacioEquipamiento (idEspacioArtistico, codigoEquipamiento)
    SELECT @idEspacioArtistico, LTRIM(RTRIM(value))
    FROM STRING_SPLIT(@equipamiento, N',')
    WHERE LEN(LTRIM(RTRIM(value))) > 0;

    INSERT INTO dbo.FranjaEspacio (idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado)
    SELECT @idEspacioArtistico, diaSemana, NULL, 540, 1320, 0
    FROM (VALUES (1), (2), (3), (4), (5)) AS dias(diaSemana);

    INSERT INTO dbo.EspacioFoto (idEspacioArtistico, rutaFoto, orden, esPrincipal)
    VALUES (@idEspacioArtistico, @fotoRuta, 0, 1);

    FETCH NEXT FROM espacios_cursor INTO
        @nombreEspacio, @descripcion, @tipoEspacio, @gestorCorreo, @provincia, @ciudad, @direccion,
        @capacidadMaxima, @precioHora, @moneda, @tipoPiso, @detalleEquipamiento, @fotoRuta, @equipamiento;
END

CLOSE espacios_cursor;
DEALLOCATE espacios_cursor;

COMMIT TRANSACTION;
GO

SELECT
    COUNT(*) AS espaciosDemoPublicados
FROM dbo.EspacioArtistico
WHERE publicado = 1
  AND activo = 1
  AND nombreEspacio IN
  (
      N'Teatro Colón',
      N'Teatro San Telmo',
      N'Teatro del Parque',
      N'Teatro Independencia Palermo',
      N'Sala Principal StageUp',
      N'Sala Ensayo Palermo',
      N'Sala Movimiento Sur',
      N'Sala Microescena Boedo',
      N'Sala Acústica Norte',
      N'Sala Taller Cultural Once',
      N'Estudio Fotográfico Norte',
      N'Estudio Audiovisual Belgrano',
      N'Estudio Creativo Palermo',
      N'Estudio Sonoro Colegiales',
      N'Estudio Luz Natural San Telmo'
  );
GO
