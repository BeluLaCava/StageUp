IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

SET NOCOUNT ON;
GO

-- Ana: solicitante que nunca pidió ser gestora. En "Mis espacios" ve el mensaje
-- de "todavía no sos gestor" (no el ABM). Sirve para probar el gate por perfil.
IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'ana.solicitante@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaActivacion)
    VALUES
        (N'Ana', N'Solicitante', N'ana.solicitante@stageup.test',
         N'100000.nSURedsZcaOaJwBVjykgrQ==.EIHLJTA1iMPHadHWOH7Olv06Axzp1ekbnjwAW6trOLs=',
         N'Activa', N'ExternoSolicitante', 1, 1, GETDATE(), GETDATE());
END
GO

-- Marcos: gestor habilitado, con espacios propios ya publicados.
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
GO

-- Carla: segundo gestor, para tener espacios de más de un gestor distinto en el catálogo.
IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'carla.gestora@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos, fechaActivacion)
    VALUES
        (N'Carla', N'Espacios', N'carla.gestora@stageup.test',
         N'100000.4UyTqCiktr0QpVcUEo1G+Q==.zUDI6gn9wRbCO3MDyZaXdOCahRMYvqNLic3+LCBtXY0=',
         N'Activa', N'GestorEspacios', 1, 1, GETDATE(), GETDATE());
END
GO

-- Lucía: cuenta todavía sin activar (para probar/demostrar el flujo de activación
-- por código) y, además, con una solicitud de habilitación como gestora ya cargada
-- y pendiente de aprobación (para probar el mensaje de "pendiente" en Mis espacios
-- una vez que la cuenta se activa).
IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'lucia.pendiente@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos)
    VALUES
        (N'Lucía', N'Pendiente', N'lucia.pendiente@stageup.test',
         N'100000.u10EblOs4eN30V6tAjlYoQ==.OIw3vfzBTltbN9IgqBUA+xt/xkwGYXyks+/TTviO0gw=',
         N'PendienteActivacion', N'PendienteHabilitacionGestor', 1, 1, GETDATE());

    DECLARE @idPendiente INT = SCOPE_IDENTITY();

    INSERT INTO dbo.CodigoActivacion (idUsuarioExterno, codigo, fechaVencimiento)
    VALUES (@idPendiente, N'123456', DATEADD(YEAR, 1, GETDATE()));
END
GO

-- Si Lucía ya existía de una corrida anterior de este script (antes de que este perfil
-- se agregara), esto asegura que igual quede con la solicitud de gestora pendiente.
UPDATE dbo.UsuarioExterno
SET perfilUsuario = N'PendienteHabilitacionGestor'
WHERE correoElectronico = N'lucia.pendiente@stageup.test'
  AND perfilUsuario <> N'PendienteHabilitacionGestor';
GO

-- Espacios de Marcos: variedad de tipos, y dos "Salón" para poder probar
-- comparación/búsqueda entre espacios del mismo tipo.
IF NOT EXISTS (SELECT 1 FROM dbo.EspacioArtistico WHERE nombreEspacio = N'Sala Principal StageUp')
BEGIN
    DECLARE @idGestorMarcos INT;
    SELECT @idGestorMarcos = idUsuarioExterno FROM dbo.UsuarioExterno WHERE correoElectronico = N'marcos.gestor@stageup.test';

    IF @idGestorMarcos IS NOT NULL
    BEGIN
        INSERT INTO dbo.EspacioArtistico
            (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion)
        VALUES
            (@idGestorMarcos, N'Sala Principal StageUp', N'Sala de ensayo equipada para actividades musicales y teatrales.', N'Sala de ensayo', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestorMarcos, N'Estudio Fotográfico Norte', N'Estudio con luces e infraestructura para sesiones fotográficas y audiovisuales.', N'Estudio', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestorMarcos, N'Salón de Eventos Recoleta', N'Salón amplio para presentaciones, muestras y eventos artísticos.', N'Salón de eventos', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestorMarcos, N'Salón de Ensayos Once', N'Salón chico ideal para ensayos de bandas y grupos de teatro.', N'Salón de ensayos', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestorMarcos, N'Teatro Under Almagro', N'Sala teatral independiente con capacidad para funciones reducidas.', N'Teatro', N'Publicado', 1, 1, GETDATE(), GETDATE());
    END
END
GO

-- Espacios de Carla: más variedad y otro "Salón" para comparación, de un gestor distinto.
IF NOT EXISTS (SELECT 1 FROM dbo.EspacioArtistico WHERE nombreEspacio = N'Auditorio Belgrano')
BEGIN
    DECLARE @idGestorCarla INT;
    SELECT @idGestorCarla = idUsuarioExterno FROM dbo.UsuarioExterno WHERE correoElectronico = N'carla.gestora@stageup.test';

    IF @idGestorCarla IS NOT NULL
    BEGIN
        INSERT INTO dbo.EspacioArtistico
            (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion)
        VALUES
            (@idGestorCarla, N'Auditorio Belgrano', N'Auditorio con butacas fijas para presentaciones y charlas artísticas.', N'Auditorio', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestorCarla, N'Teatro Colonial del Sur', N'Teatro tradicional con escenario amplio y buena acústica.', N'Teatro', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestorCarla, N'Salón Multiespacio Palermo', N'Salón versátil, se adapta tanto a ensayos como a muestras abiertas.', N'Salón', N'Publicado', 1, 1, GETDATE(), GETDATE());
    END
END
GO
