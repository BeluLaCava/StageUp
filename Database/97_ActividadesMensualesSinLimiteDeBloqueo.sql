USE StageUp;
GO
CREATE OR ALTER VIEW dbo.vw_FranjaEspacioAgenda
AS
    SELECT f.idFranjaEspacio, f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta,
           f.bloqueado, f.origen, f.motivoBloqueo, f.idActividad, a.nombre AS nombreActividad,
           CAST(NULL AS TINYINT) AS semanaDelMes
    FROM dbo.FranjaEspacio f
    LEFT JOIN dbo.Actividad a ON a.idActividad = f.idActividad
    WHERE f.origen <> N'Actividad' OR a.modoRecurrencia IS NULL OR a.modoRecurrencia <> N'Mensual'
    UNION ALL
    SELECT CAST(NULL AS INT), a.idEspacioArtistico, a.diaSemanaMensual, CAST(NULL AS DATE), a.minutoDesde, a.minutoHasta,
           CAST(1 AS BIT), N'Actividad', CAST(NULL AS NVARCHAR(300)), a.idActividad, a.nombre, a.semanaDelMes
    FROM dbo.Actividad a
    WHERE a.activa = 1 AND a.modoRecurrencia = N'Mensual';
GO
CREATE OR ALTER PROCEDURE dbo.sp_FranjaEspacio_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.* FROM dbo.vw_FranjaEspacioAgenda f
    
    WHERE f.idEspacioArtistico = @idEspacioArtistico;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_FranjaEspacio_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.* FROM dbo.vw_FranjaEspacioAgenda f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_FranjaEspacio_ListarPublicados
    
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.* FROM dbo.vw_FranjaEspacioAgenda f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    WHERE e.activo = 1 AND e.publicado = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_FranjaEspacio_ListarPublicadosPorIds
    @idsEspacios NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.* FROM dbo.vw_FranjaEspacioAgenda f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    INNER JOIN STRING_SPLIT(@idsEspacios, ',') s ON TRY_CONVERT(INT, s.value) = f.idEspacioArtistico
    WHERE e.activo = 1 AND e.publicado = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados
    @textoBusqueda NVARCHAR(300) = NULL,
    @tipoEspacio NVARCHAR(200) = NULL,
    @ubicacion NVARCHAR(150) = NULL,
    @provincia NVARCHAR(100) = NULL,
    @ciudad NVARCHAR(100) = NULL,
    @zona NVARCHAR(100) = NULL,
    @precioMinimo DECIMAL(18,2) = NULL,
    @precioMaximo DECIMAL(18,2) = NULL,
    @capacidadMinima INT = NULL,
    @tipoPiso NVARCHAR(100) = NULL,
    @caracteristicas NVARCHAR(150) = NULL,
    @calificacionMinima DECIMAL(3,2) = NULL,
    @fechaDisponibilidad DATE = NULL,
    @minutoDesde SMALLINT = NULL,
    @minutoHasta SMALLINT = NULL,
    @reqEspejos BIT = 0,
    @reqSonido BIT = 0,
    @reqInstrumentos BIT = 0,
    @reqEquipamiento BIT = 0,
    @reqEscenario BIT = 0,
    @reqIluminacion BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @diaSemana TINYINT = NULL;
    IF @fechaDisponibilidad IS NOT NULL SET @diaSemana = ((DATEPART(WEEKDAY, @fechaDisponibilidad) + @@DATEFIRST - 2) % 7) + 1;
    SELECT e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
           e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
           f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento,
           f.superficieM2, f.alturaM, f.condicionesUso, f.reglasUso,
           g.nombre AS nombreGestor, g.apellido AS apellidoGestor, g.fechaActivacion AS gestorFechaActivacion, g.fechaAlta AS gestorFechaAlta,
           (SELECT COUNT(*) FROM dbo.EspacioArtistico e2 WHERE e2.idUsuarioGestor = e.idUsuarioGestor AND e2.activo = 1 AND e2.publicado = 1) AS cantidadEspaciosPublicadosGestor
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE e.activo = 1 AND e.publicado = 1
      AND (@textoBusqueda IS NULL OR e.nombreEspacio LIKE N'%' + @textoBusqueda + N'%' OR e.tipoEspacio LIKE N'%' + @textoBusqueda + N'%' OR e.descripcion LIKE N'%' + @textoBusqueda + N'%')
      AND (@tipoEspacio IS NULL OR e.tipoEspacio LIKE N'%' + @tipoEspacio + N'%')
      AND (@ubicacion IS NULL OR f.ciudad LIKE N'%' + @ubicacion + N'%' OR f.provincia LIKE N'%' + @ubicacion + N'%' OR f.direccion LIKE N'%' + @ubicacion + N'%')
      AND (@provincia IS NULL OR f.provincia LIKE N'%' + @provincia + N'%')
      AND (@ciudad IS NULL OR f.ciudad LIKE N'%' + @ciudad + N'%')
      AND (@zona IS NULL OR f.direccion LIKE N'%' + @zona + N'%' OR f.ciudad LIKE N'%' + @zona + N'%')
      AND (@precioMinimo IS NULL OR f.precioHora >= @precioMinimo)
      AND (@precioMaximo IS NULL OR f.precioHora <= @precioMaximo)
      AND (@capacidadMinima IS NULL OR f.capacidadMaxima >= @capacidadMinima)
      AND (@tipoPiso IS NULL OR f.tipoPiso LIKE N'%' + @tipoPiso + N'%')
      AND (@caracteristicas IS NULL OR e.descripcion LIKE N'%' + @caracteristicas + N'%' OR f.detalleEquipamiento LIKE N'%' + @caracteristicas + N'%' OR f.condicionesUso LIKE N'%' + @caracteristicas + N'%' OR f.reglasUso LIKE N'%' + @caracteristicas + N'%')
      AND (@calificacionMinima IS NULL OR ISNULL((SELECT AVG(CAST(c.puntaje AS DECIMAL(10,2))) FROM dbo.Calificacion c INNER JOIN dbo.Reserva rc ON rc.idReserva = c.idReserva WHERE rc.idEspacioArtistico = e.idEspacioArtistico AND c.tipoCalificacion = N'Espacio' AND c.activo = 1), 0) >= @calificacionMinima)
      AND (@reqEspejos = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'ESPEJOS'))
      AND (@reqSonido = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'SONIDO'))
      AND (@reqInstrumentos = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'INSTRUMENTOS'))
      AND (@reqEquipamiento = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'EQUIPAMIENTO'))
      AND (@reqEscenario = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'ESCENARIO'))
      AND (@reqIluminacion = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = N'ILUMINACION'))
      AND (@fechaDisponibilidad IS NULL OR (
          EXISTS (SELECT 1 FROM dbo.FranjaEspacio fr WHERE fr.idEspacioArtistico = e.idEspacioArtistico AND fr.bloqueado = 0 AND (fr.fecha = @fechaDisponibilidad OR (fr.fecha IS NULL AND fr.diaSemana = @diaSemana)) AND (@minutoDesde IS NULL OR (@minutoDesde >= fr.minutoDesde AND @minutoHasta <= fr.minutoHasta)))
          AND (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Reserva res WHERE res.idEspacioArtistico = e.idEspacioArtistico AND res.fechaSolicitada = @fechaDisponibilidad AND res.estadoReserva IN (N'Pendiente', N'Aceptada') AND @minutoDesde < res.minutoHasta AND res.minutoDesde < @minutoHasta))
          AND (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.vw_FranjaEspacioAgenda fb WHERE fb.idEspacioArtistico = e.idEspacioArtistico AND fb.bloqueado = 1 AND (fb.fecha = @fechaDisponibilidad OR (fb.fecha IS NULL AND fb.diaSemana = @diaSemana AND (fb.semanaDelMes IS NULL OR (fb.semanaDelMes < 5 AND (DAY(@fechaDisponibilidad) - 1) / 7 + 1 = fb.semanaDelMes) OR (fb.semanaDelMes = 5 AND DAY(@fechaDisponibilidad) + 7 > DAY(EOMONTH(@fechaDisponibilidad)))))) AND @minutoDesde < fb.minutoHasta AND fb.minutoDesde < @minutoHasta))
      ))
    ORDER BY e.fechaPublicacion DESC;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Busqueda_Publica
    @texto NVARCHAR(300) = NULL, @incluirEspacios BIT = 1, @incluirNovedades BIT = 1, @incluirFaq BIT = 1,
    @fechaDesde DATETIME = NULL, @fechaHasta DATETIME = NULL, @ubicacion NVARCHAR(100) = NULL,
    @tipoEspacio NVARCHAR(100) = NULL, @provincia NVARCHAR(100) = NULL, @ciudad NVARCHAR(100) = NULL, @zona NVARCHAR(100) = NULL,
    @precioMinimo DECIMAL(18,2) = NULL, @capacidadMinima INT = NULL, @precioMaximo DECIMAL(18,2) = NULL,
    @fechaDisponibilidad DATE = NULL, @minutoDesde SMALLINT = NULL, @minutoHasta SMALLINT = NULL,
    @equipamiento NVARCHAR(400) = NULL, @caracteristicas NVARCHAR(150) = NULL, @calificacionMinima DECIMAL(3,2) = NULL,
    @categoriaNovedad NVARCHAR(100) = NULL, @orden NVARCHAR(20) = N'Relevancia', @maximo INT = 25
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @patron NVARCHAR(310) = N'%' + ISNULL(@texto, N'') + N'%';
    DECLARE @hasta DATETIME = CASE WHEN @fechaHasta IS NULL THEN NULL ELSE DATEADD(DAY, 1, @fechaHasta) END;
    DECLARE @diaSemana TINYINT = NULL;
    IF @fechaDisponibilidad IS NOT NULL SET @diaSemana = ((DATEPART(WEEKDAY, @fechaDisponibilidad) + @@DATEFIRST - 2) % 7) + 1;
    CREATE TABLE #resultados (tipo NVARCHAR(30), idReferencia INT, titulo NVARCHAR(400), detalle NVARCHAR(1000), fecha DATETIME NULL, prioridad INT);
    DECLARE @soloEspacios BIT = CASE WHEN @ubicacion IS NOT NULL OR @tipoEspacio IS NOT NULL OR @provincia IS NOT NULL OR @ciudad IS NOT NULL OR @zona IS NOT NULL OR @precioMinimo IS NOT NULL OR @capacidadMinima IS NOT NULL OR @precioMaximo IS NOT NULL OR @fechaDisponibilidad IS NOT NULL OR @equipamiento IS NOT NULL OR @caracteristicas IS NOT NULL OR @calificacionMinima IS NOT NULL THEN 1 ELSE 0 END;
    IF @incluirEspacios = 1
        INSERT INTO #resultados
        SELECT N'Espacio', e.idEspacioArtistico, e.nombreEspacio, e.tipoEspacio + ISNULL(N' · ' + f.ciudad + N', ' + f.provincia, N'') + ISNULL(N' · Hasta ' + CAST(f.capacidadMaxima AS NVARCHAR(10)) + N' personas', N'') + ISNULL(N' · ' + ISNULL(f.moneda, N'ARS') + N' ' + CONVERT(NVARCHAR(30), f.precioHora) + N' por hora', N''), e.fechaPublicacion, CASE WHEN @texto IS NOT NULL AND e.nombreEspacio LIKE @patron THEN 0 ELSE 1 END
        FROM dbo.EspacioArtistico e LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
        WHERE e.activo = 1 AND e.publicado = 1
          AND (@texto IS NULL OR e.nombreEspacio LIKE @patron OR e.tipoEspacio LIKE @patron OR e.descripcion LIKE @patron OR f.ciudad LIKE @patron OR f.provincia LIKE @patron)
          AND (@tipoEspacio IS NULL OR e.tipoEspacio LIKE N'%' + @tipoEspacio + N'%')
          AND (@ubicacion IS NULL OR f.ciudad LIKE N'%' + @ubicacion + N'%' OR f.provincia LIKE N'%' + @ubicacion + N'%' OR f.direccion LIKE N'%' + @ubicacion + N'%')
          AND (@provincia IS NULL OR f.provincia LIKE N'%' + @provincia + N'%') AND (@ciudad IS NULL OR f.ciudad LIKE N'%' + @ciudad + N'%') AND (@zona IS NULL OR f.direccion LIKE N'%' + @zona + N'%' OR f.ciudad LIKE N'%' + @zona + N'%')
          AND (@precioMinimo IS NULL OR f.precioHora >= @precioMinimo) AND (@precioMaximo IS NULL OR f.precioHora <= @precioMaximo) AND (@capacidadMinima IS NULL OR f.capacidadMaxima >= @capacidadMinima)
          AND (@caracteristicas IS NULL OR e.descripcion LIKE N'%' + @caracteristicas + N'%' OR f.detalleEquipamiento LIKE N'%' + @caracteristicas + N'%' OR f.condicionesUso LIKE N'%' + @caracteristicas + N'%' OR f.reglasUso LIKE N'%' + @caracteristicas + N'%')
          AND (@equipamiento IS NULL OR NOT EXISTS (SELECT 1 FROM STRING_SPLIT(@equipamiento, N',') req WHERE NOT EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento q WHERE q.idEspacioArtistico = e.idEspacioArtistico AND q.codigoEquipamiento = LTRIM(RTRIM(req.value)))))
          AND (@calificacionMinima IS NULL OR ISNULL((SELECT AVG(CAST(c.puntaje AS DECIMAL(10,2))) FROM dbo.Calificacion c INNER JOIN dbo.Reserva rc ON rc.idReserva = c.idReserva WHERE rc.idEspacioArtistico = e.idEspacioArtistico AND c.tipoCalificacion = N'Espacio' AND c.activo = 1), 0) >= @calificacionMinima)
          AND (@fechaDisponibilidad IS NULL OR (EXISTS (SELECT 1 FROM dbo.FranjaEspacio fr WHERE fr.idEspacioArtistico = e.idEspacioArtistico AND fr.bloqueado = 0 AND (fr.fecha = @fechaDisponibilidad OR (fr.fecha IS NULL AND fr.diaSemana = @diaSemana)) AND (@minutoDesde IS NULL OR (@minutoDesde >= fr.minutoDesde AND @minutoHasta <= fr.minutoHasta))) AND (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Reserva r WHERE r.idEspacioArtistico = e.idEspacioArtistico AND r.fechaSolicitada = @fechaDisponibilidad AND r.estadoReserva IN (N'Pendiente', N'Aceptada') AND @minutoDesde < r.minutoHasta AND r.minutoDesde < @minutoHasta))))
          AND (@fechaDisponibilidad IS NULL OR (@minutoDesde IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.vw_FranjaEspacioAgenda fb WHERE fb.idEspacioArtistico = e.idEspacioArtistico AND fb.bloqueado = 1 AND (fb.fecha = @fechaDisponibilidad OR (fb.fecha IS NULL AND fb.diaSemana = @diaSemana AND (fb.semanaDelMes IS NULL OR (fb.semanaDelMes < 5 AND (DAY(@fechaDisponibilidad) - 1) / 7 + 1 = fb.semanaDelMes) OR (fb.semanaDelMes = 5 AND DAY(@fechaDisponibilidad) + 7 > DAY(EOMONTH(@fechaDisponibilidad)))))) AND @minutoDesde < fb.minutoHasta AND fb.minutoDesde < @minutoHasta)))
          AND (@fechaDesde IS NULL OR e.fechaPublicacion >= @fechaDesde) AND (@hasta IS NULL OR e.fechaPublicacion < @hasta);
    IF @incluirNovedades = 1 AND @soloEspacios = 0
        INSERT INTO #resultados SELECT N'Novedad', n.idNovedad, n.titulo, n.categoria + N' · ' + n.resumen, ISNULL(n.fechaPublicacion, n.fechaAlta), CASE WHEN @texto IS NOT NULL AND n.titulo LIKE @patron THEN 0 ELSE 1 END FROM dbo.vw_Novedad n WHERE n.estado = N'Vigente' AND (@texto IS NULL OR n.titulo LIKE @patron OR n.resumen LIKE @patron OR n.contenido LIKE @patron OR n.categoria LIKE @patron) AND (@categoriaNovedad IS NULL OR n.categoria = @categoriaNovedad) AND (@fechaDesde IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) >= @fechaDesde) AND (@hasta IS NULL OR ISNULL(n.fechaPublicacion, n.fechaAlta) < @hasta);
    IF @incluirFaq = 1 AND @soloEspacios = 0 AND @categoriaNovedad IS NULL
        INSERT INTO #resultados SELECT N'FAQ', q.idFaq, q.pregunta, LEFT(q.respuesta, 300), NULL, CASE WHEN @texto IS NOT NULL AND q.pregunta LIKE @patron THEN 0 ELSE 1 END FROM dbo.Faq q INNER JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = q.idCategoriaAyuda AND c.activa = 1 WHERE q.estado = N'Activo' AND (@texto IS NULL OR q.pregunta LIKE @patron OR q.respuesta LIKE @patron);
    SELECT tipo, idReferencia, titulo, detalle, fecha, posicion FROM (SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.tipo ORDER BY CASE WHEN @orden = N'Alfabetico' THEN r.titulo END ASC, CASE WHEN @orden = N'Relevancia' THEN r.prioridad END ASC, CASE WHEN @orden = N'Antiguos' THEN r.fecha END ASC, r.fecha DESC, r.titulo ASC) AS posicion FROM #resultados r) x WHERE posicion <= @maximo ORDER BY tipo, posicion;
END
GO
