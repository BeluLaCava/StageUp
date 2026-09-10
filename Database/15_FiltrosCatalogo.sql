IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- filtros completos del catálogo

IF OBJECT_ID('dbo.sp_EspacioArtistico_BuscarPublicados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados
    @textoBusqueda          NVARCHAR(300)   = NULL,
    @tipoEspacio            NVARCHAR(200)   = NULL,
    -- Un solo campo de ubicación en la pantalla ("Ciudad o zona"): se busca
    -- tanto en ciudad como en provincia, no hace falta que el usuario sepa
    -- distinguir cuál es cuál.
    @ubicacion              NVARCHAR(150)   = NULL,
    @precioMaximo           DECIMAL(18,2)   = NULL,
    @capacidadMinima        INT             = NULL,
    @tipoPiso               NVARCHAR(100)   = NULL,
    -- Disponibilidad: si se pasa @fechaDisponibilidad, el espacio tiene que
    -- tener una franja abierta ese día (respetando excepciones puntuales de
    -- esa fecha por sobre el horario semanal, mismo criterio que ya usa
    -- BLL_Reserva.ValidarHorarioSolicitado). Si además vienen @minutoDesde y
    -- @minutoHasta, esa franja tiene que cubrir exactamente ese rango Y no
    -- puede haber ya otra reserva Pendiente/Aceptada superpuesta (mismo
    -- criterio que sp_Reserva_ExisteSolapamiento del ítem 2).
    @fechaDisponibilidad    DATE            = NULL,
    @minutoDesde            SMALLINT        = NULL,
    @minutoHasta            SMALLINT        = NULL,
    @reqEspejos             BIT             = 0,
    @reqSonido              BIT             = 0,
    @reqInstrumentos        BIT             = 0,
    @reqEquipamiento        BIT             = 0,
    @reqEscenario           BIT             = 0,
    @reqIluminacion         BIT             = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Día de la semana en la misma convención que ya usa el resto del
    -- proyecto (1 = lunes ... 7 = domingo, ver FranjaEspacio.DiaSemana /
    -- detalle-espacio.js), independiente de la configuración regional del
    -- servidor (@@DATEFIRST se cancela en esta fórmula).
    DECLARE @diaSemana TINYINT = NULL;
    IF @fechaDisponibilidad IS NOT NULL
        SET @diaSemana = ((DATEPART(WEEKDAY, @fechaDisponibilidad) + @@DATEFIRST - 2) % 7) + 1;

    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento,
        g.nombre AS nombreGestor, g.apellido AS apellidoGestor,
        g.fechaActivacion AS gestorFechaActivacion, g.fechaAlta AS gestorFechaAlta,
        (SELECT COUNT(*) FROM dbo.EspacioArtistico e2
            WHERE e2.idUsuarioGestor = e.idUsuarioGestor AND e2.activo = 1 AND e2.publicado = 1) AS cantidadEspaciosPublicadosGestor
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = e.idUsuarioGestor
    WHERE e.activo = 1
      AND e.publicado = 1
      AND (@textoBusqueda IS NULL
           OR e.nombreEspacio LIKE '%' + @textoBusqueda + '%'
           OR e.tipoEspacio LIKE '%' + @textoBusqueda + '%'
           OR e.descripcion LIKE '%' + @textoBusqueda + '%')
      AND (@tipoEspacio IS NULL OR e.tipoEspacio LIKE '%' + @tipoEspacio + '%')
      AND (@ubicacion IS NULL OR f.ciudad LIKE '%' + @ubicacion + '%' OR f.provincia LIKE '%' + @ubicacion + '%')
      AND (@precioMaximo IS NULL OR (f.precioHora IS NOT NULL AND f.precioHora <= @precioMaximo))
      AND (@capacidadMinima IS NULL OR (f.capacidadMaxima IS NOT NULL AND f.capacidadMaxima >= @capacidadMinima))
      AND (@tipoPiso IS NULL OR f.tipoPiso LIKE '%' + @tipoPiso + '%')
      AND (@reqEspejos = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento eq WHERE eq.idEspacioArtistico = e.idEspacioArtistico AND eq.codigoEquipamiento = N'ESPEJOS'))
      AND (@reqSonido = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento eq WHERE eq.idEspacioArtistico = e.idEspacioArtistico AND eq.codigoEquipamiento = N'SONIDO'))
      AND (@reqInstrumentos = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento eq WHERE eq.idEspacioArtistico = e.idEspacioArtistico AND eq.codigoEquipamiento = N'INSTRUMENTOS'))
      AND (@reqEquipamiento = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento eq WHERE eq.idEspacioArtistico = e.idEspacioArtistico AND eq.codigoEquipamiento = N'EQUIPAMIENTO'))
      AND (@reqEscenario = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento eq WHERE eq.idEspacioArtistico = e.idEspacioArtistico AND eq.codigoEquipamiento = N'ESCENARIO'))
      AND (@reqIluminacion = 0 OR EXISTS (SELECT 1 FROM dbo.FichaEspacioEquipamiento eq WHERE eq.idEspacioArtistico = e.idEspacioArtistico AND eq.codigoEquipamiento = N'ILUMINACION'))
      AND (
            @fechaDisponibilidad IS NULL
            OR (
                EXISTS (
                    SELECT 1 FROM dbo.FranjaEspacio fr
                    WHERE fr.idEspacioArtistico = e.idEspacioArtistico
                      AND fr.bloqueado = 0
                      AND (
                            fr.fecha = @fechaDisponibilidad
                            OR (
                                fr.fecha IS NULL
                                AND fr.diaSemana = @diaSemana
                                -- Si hay alguna franja (bloqueada o no) con fecha exacta para
                                -- ese día, esa excepción manda y se ignora el horario semanal
                                -- (mismo criterio que ValidarHorarioSolicitado en la BLL).
                                AND NOT EXISTS (
                                    SELECT 1 FROM dbo.FranjaEspacio ex
                                    WHERE ex.idEspacioArtistico = e.idEspacioArtistico
                                      AND ex.fecha = @fechaDisponibilidad
                                )
                            )
                      )
                      AND (@minutoDesde IS NULL OR @minutoHasta IS NULL
                           OR (@minutoDesde >= fr.minutoDesde AND @minutoHasta <= fr.minutoHasta))
                )
                AND (
                    @minutoDesde IS NULL OR @minutoHasta IS NULL
                    OR NOT EXISTS (
                        SELECT 1 FROM dbo.Reserva res
                        WHERE res.idEspacioArtistico = e.idEspacioArtistico
                          AND res.fechaSolicitada = @fechaDisponibilidad
                          AND res.estadoReserva IN (N'Pendiente', N'Aceptada')
                          AND res.minutoDesde IS NOT NULL AND res.minutoHasta IS NOT NULL
                          AND @minutoDesde < res.minutoHasta
                          AND res.minutoDesde < @minutoHasta
                    )
                )
            )
      )
    ORDER BY e.fechaPublicacion DESC;
END
GO