USE StageUp;
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Notificacion', N'U') IS NULL OR OBJECT_ID(N'dbo.Reserva', N'U') IS NULL
   OR OBJECT_ID(N'dbo.EspacioArtistico', N'U') IS NULL OR OBJECT_ID(N'dbo.FranjaEspacio', N'U') IS NULL
BEGIN
    THROW 51000, 'Faltan tablas base (Notificacion/Reserva/EspacioArtistico/FranjaEspacio). Revisa que los scripts anteriores ya se hayan aplicado.', 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Notificacion') AND name = N'IX_Notificacion_Usuario')
BEGIN
    CREATE INDEX IX_Notificacion_Usuario ON dbo.Notificacion (idUsuarioExterno, leida, fechaCreacion DESC)
        INCLUDE (tipo, mensaje, urlDestino);
END
GO

-- 2) Reserva: listado de reservas de un solicitante (Mis reservas), ordenado
--    por fecha de creacion.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reserva') AND name = N'IX_Reserva_Solicitante')
BEGIN
    CREATE INDEX IX_Reserva_Solicitante ON dbo.Reserva (idUsuarioExternoSolicitante, fechaCreacion DESC);
END
GO

-- 3) Reserva: chequeo de solapamiento y listados por espacio/fecha (usado al
--    reservar y al armar la disponibilidad de un espacio).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reserva') AND name = N'IX_Reserva_Espacio_Fecha_Estado')
BEGIN
    CREATE INDEX IX_Reserva_Espacio_Fecha_Estado ON dbo.Reserva (idEspacioArtistico, fechaSolicitada, estadoReserva)
        INCLUDE (minutoDesde, minutoHasta);
END
GO

-- 4) Reserva: procesos batch que filtran por estado (finalizar vencidas,
--    generar recordatorios de 24hs).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reserva') AND name = N'IX_Reserva_Estado')
BEGIN
    CREATE INDEX IX_Reserva_Estado ON dbo.Reserva (estadoReserva)
        INCLUDE (fechaSolicitada, minutoDesde, minutoHasta, recordatorioEnviado, idEspacioArtistico, idUsuarioExternoSolicitante);
END
GO

-- 5) EspacioArtistico: "Mis espacios" de un gestor, y filtros de
--    activo/publicado que se repiten en el catalogo y en el panel del gestor.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EspacioArtistico') AND name = N'IX_EspacioArtistico_Gestor')
BEGIN
    CREATE INDEX IX_EspacioArtistico_Gestor ON dbo.EspacioArtistico (idUsuarioGestor, activo, publicado);
END
GO

-- 6) FranjaEspacio: se recorre por subconsultas correlacionadas en cada
--    busqueda del catalogo y en el armado del calendario de un espacio,
--    siempre filtrando por espacio + bloqueado.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FranjaEspacio') AND name = N'IX_FranjaEspacio_Espacio_Bloqueado')
BEGIN
    CREATE INDEX IX_FranjaEspacio_Espacio_Bloqueado ON dbo.FranjaEspacio (idEspacioArtistico, bloqueado)
        INCLUDE (diaSemana, fecha, minutoDesde, minutoHasta);
END
GO
