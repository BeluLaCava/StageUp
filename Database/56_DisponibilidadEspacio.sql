IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.FranjaEspacio', N'U') IS NULL
   OR COL_LENGTH(N'dbo.FranjaEspacio', N'origen') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/09_FichaEspacio.sql y Database/26_ActividadesYNotificaciones.sql.', 1;
END
GO

IF COL_LENGTH(N'dbo.FranjaEspacio', N'motivoBloqueo') IS NULL
    ALTER TABLE dbo.FranjaEspacio ADD motivoBloqueo NVARCHAR(300) NULL;
GO
IF COL_LENGTH(N'dbo.FranjaEspacio', N'fechaAlta') IS NULL
    ALTER TABLE dbo.FranjaEspacio ADD fechaAlta DATETIME NULL
        CONSTRAINT DF_FranjaEspacio_fechaAlta DEFAULT (GETDATE());
GO

-- ---------------------------------------------------------------------------
-- Lecturas
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_FranjaEspacio_ListarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ListarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idFranjaEspacio, f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta,
           f.bloqueado, f.origen, f.motivoBloqueo, f.idActividad, a.nombre AS nombreActividad
    FROM dbo.FranjaEspacio f
    LEFT JOIN dbo.Actividad a ON a.idActividad = f.idActividad
    WHERE f.idEspacioArtistico = @idEspacioArtistico;
END
GO

IF OBJECT_ID('dbo.sp_FranjaEspacio_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idFranjaEspacio, f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta,
           f.bloqueado, f.origen, f.motivoBloqueo, f.idActividad, a.nombre AS nombreActividad
    FROM dbo.FranjaEspacio f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    LEFT JOIN dbo.Actividad a ON a.idActividad = f.idActividad
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1;
END
GO

-- ---------------------------------------------------------------------------
-- Guardado de la ficha: la disponibilidad se reemplaza solo al crear el
-- espacio (definición inicial). Al editarlo se conserva.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_FichaEspacio_LimpiarDatosEditables', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_LimpiarDatosEditables;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_LimpiarDatosEditables
    @idEspacioArtistico INT,
    @incluirFranjas     BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.FichaEspacioEquipamiento WHERE idEspacioArtistico = @idEspacioArtistico;
        DELETE FROM dbo.EspacioFoto WHERE idEspacioArtistico = @idEspacioArtistico;

        -- Solo las franjas cargadas a mano. Los bloqueos generados desde
        -- "Mis actividades" se conservan siempre.
        IF @incluirFranjas = 1
            DELETE FROM dbo.FranjaEspacio
            WHERE idEspacioArtistico = @idEspacioArtistico
              AND origen = N'Manual';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------------------
-- Alta, modificación y eliminación de una franja manual
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_FranjaEspacio_InsertarManual', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_InsertarManual;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_InsertarManual
    @idEspacioArtistico INT,
    @diaSemana          TINYINT         = NULL,
    @fecha              DATE            = NULL,
    @minutoDesde        SMALLINT,
    @minutoHasta        SMALLINT,
    @bloqueado          BIT,
    @motivoBloqueo      NVARCHAR(300)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.FranjaEspacio
            (idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado, origen, motivoBloqueo, fechaAlta)
        VALUES
            (@idEspacioArtistico, @diaSemana, @fecha, @minutoDesde, @minutoHasta, @bloqueado, N'Manual',
             CASE WHEN @bloqueado = 1 THEN @motivoBloqueo ELSE NULL END, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS idFranjaEspacio;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_FranjaEspacio_ModificarManual', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ModificarManual;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ModificarManual
    @idFranjaEspacio    INT,
    @idEspacioArtistico INT,
    @diaSemana          TINYINT         = NULL,
    @fecha              DATE            = NULL,
    @minutoDesde        SMALLINT,
    @minutoHasta        SMALLINT,
    @motivoBloqueo      NVARCHAR(300)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.FranjaEspacio
        SET diaSemana = @diaSemana,
            fecha = @fecha,
            minutoDesde = @minutoDesde,
            minutoHasta = @minutoHasta,
            motivoBloqueo = CASE WHEN bloqueado = 1 THEN @motivoBloqueo ELSE NULL END
        WHERE idFranjaEspacio = @idFranjaEspacio
          AND idEspacioArtistico = @idEspacioArtistico
          AND origen = N'Manual';

        SELECT @@ROWCOUNT AS filasAfectadas;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_FranjaEspacio_EliminarManual', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_EliminarManual;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_EliminarManual
    @idFranjaEspacio    INT,
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.FranjaEspacio
        WHERE idFranjaEspacio = @idFranjaEspacio
          AND idEspacioArtistico = @idEspacioArtistico
          AND origen = N'Manual';

        SELECT @@ROWCOUNT AS filasAfectadas;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------------------
-- Búsqueda con fecha y horario (mismo criterio que BLL_Reserva y que el
-- planificador del detalle del espacio):
--  * una franja abierta de fecha concreta reemplaza el horario semanal de
--    ese día (los bloqueos ya no cuentan como excepción);
--  * los bloqueos de esa fecha y los semanales de ese día se descuentan
--    siempre.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_EspacioArtistico_BuscarPublicados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_BuscarPublicados
    @textoBusqueda          NVARCHAR(300)   = NULL,
    @tipoEspacio            NVARCHAR(200)   = NULL,
    @ubicacion              NVARCHAR(150)   = NULL,
    @precioMaximo           DECIMAL(18,2)   = NULL,
    @capacidadMinima        INT             = NULL,
    @tipoPiso               NVARCHAR(100)   = NULL,
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
                                AND NOT EXISTS (
                                    SELECT 1 FROM dbo.FranjaEspacio ex
                                    WHERE ex.idEspacioArtistico = e.idEspacioArtistico
                                      AND ex.fecha = @fechaDisponibilidad
                                      AND ex.bloqueado = 0
                                )
                            )
                      )
                      AND (@minutoDesde IS NULL OR @minutoHasta IS NULL
                           OR (@minutoDesde >= fr.minutoDesde AND @minutoHasta <= fr.minutoHasta))
                      -- Una franja tapada entera por un bloqueo (por ejemplo,
                      -- "cerrar el día completo") no cuenta como disponible.
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.FranjaEspacio cb
                          WHERE cb.idEspacioArtistico = e.idEspacioArtistico
                            AND cb.bloqueado = 1
                            AND (cb.fecha = @fechaDisponibilidad
                                 OR (cb.fecha IS NULL AND cb.diaSemana = @diaSemana))
                            AND cb.minutoDesde <= fr.minutoDesde
                            AND cb.minutoHasta >= fr.minutoHasta
                      )
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
                AND (
                    -- Bloqueos manuales (CU-001-008 A8) y de actividades
                    -- internas: rigen los de esa fecha y también los
                    -- semanales de ese día. Un bloqueo parcial de una fecha
                    -- ya no reemplaza al horario habitual del día.
                    @minutoDesde IS NULL OR @minutoHasta IS NULL
                    OR NOT EXISTS (
                        SELECT 1 FROM dbo.FranjaEspacio fb
                        WHERE fb.idEspacioArtistico = e.idEspacioArtistico
                          AND fb.bloqueado = 1
                          AND (fb.fecha = @fechaDisponibilidad
                               OR (fb.fecha IS NULL AND fb.diaSemana = @diaSemana))
                          AND @minutoDesde < fb.minutoHasta
                          AND fb.minutoDesde < @minutoHasta
                    )
                )
            )
      )
    ORDER BY e.fechaPublicacion DESC;
END
GO
