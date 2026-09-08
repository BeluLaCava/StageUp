SET NOCOUNT ON;
GO

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

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE correoElectronico = N'lucia.pendiente@stageup.test')
BEGIN
    INSERT INTO dbo.UsuarioExterno
        (nombre, apellido, correoElectronico, passwordHash, estadoCuenta, perfilUsuario,
         aceptaTerminos, aceptaPoliticaPrivacidad, fechaAceptacionTerminos)
    VALUES
        (N'Lucía', N'Pendiente', N'lucia.pendiente@stageup.test',
         N'100000.u10EblOs4eN30V6tAjlYoQ==.OIw3vfzBTltbN9IgqBUA+xt/xkwGYXyks+/TTviO0gw=',
         N'PendienteActivacion', N'ExternoSolicitante', 1, 1, GETDATE());

    DECLARE @idPendiente INT = SCOPE_IDENTITY();

    INSERT INTO dbo.CodigoActivacion (idUsuarioExterno, codigo, fechaVencimiento)
    VALUES (@idPendiente, N'123456', DATEADD(YEAR, 1, GETDATE()));
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.EspacioArtistico WHERE nombreEspacio = N'Sala Principal StageUp')
BEGIN
    DECLARE @idGestor INT;
    SELECT @idGestor = idUsuarioExterno FROM dbo.UsuarioExterno WHERE correoElectronico = N'marcos.gestor@stageup.test';

    IF @idGestor IS NOT NULL
    BEGIN
        INSERT INTO dbo.EspacioArtistico
            (idUsuarioGestor, nombreEspacio, descripcion, tipoEspacio, estadoEspacio, publicado, activo, fechaAlta, fechaPublicacion)
        VALUES
            (@idGestor, N'Sala Principal StageUp', N'Sala de ensayo equipada para actividades musicales y teatrales.', N'Sala de ensayo', N'Publicado', 1, 1, GETDATE(), GETDATE()),
            (@idGestor, N'Estudio Fotográfico Norte', N'Estudio con luces e infraestructura para sesiones fotográficas y audiovisuales.', N'Estudio', N'Publicado', 1, 1, GETDATE(), GETDATE());
    END
END
GO
