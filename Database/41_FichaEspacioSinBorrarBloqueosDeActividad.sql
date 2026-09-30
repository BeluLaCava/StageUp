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

IF OBJECT_ID('dbo.sp_FichaEspacio_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_Guardar;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_Guardar
    @idEspacioArtistico     INT,
    @fotoRuta               NVARCHAR(300)   = NULL,
    @provincia              NVARCHAR(100),
    @ciudad                 NVARCHAR(150),
    @direccion              NVARCHAR(300),
    @capacidadMaxima        INT,
    @precioHora             DECIMAL(10,2),
    @moneda                 NVARCHAR(3),
    @tipoPiso               NVARCHAR(100)   = NULL,
    @detalleEquipamiento    NVARCHAR(1000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM dbo.FichaEspacio WHERE idEspacioArtistico = @idEspacioArtistico)
        BEGIN
            UPDATE dbo.FichaEspacio
            SET fotoRuta = @fotoRuta,
                provincia = @provincia,
                ciudad = @ciudad,
                direccion = @direccion,
                capacidadMaxima = @capacidadMaxima,
                precioHora = @precioHora,
                moneda = @moneda,
                tipoPiso = @tipoPiso,
                detalleEquipamiento = @detalleEquipamiento,
                fechaUltimaModificacion = GETDATE()
            WHERE idEspacioArtistico = @idEspacioArtistico;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.FichaEspacio
                (idEspacioArtistico, fotoRuta, provincia, ciudad, direccion, capacidadMaxima,
                 precioHora, moneda, tipoPiso, detalleEquipamiento, fechaUltimaModificacion)
            VALUES
                (@idEspacioArtistico, @fotoRuta, @provincia, @ciudad, @direccion, @capacidadMaxima,
                 @precioHora, @moneda, @tipoPiso, @detalleEquipamiento, GETDATE());
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_FichaEspacio_LimpiarDatosEditables', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_LimpiarDatosEditables;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_LimpiarDatosEditables
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.FichaEspacioEquipamiento WHERE idEspacioArtistico = @idEspacioArtistico;
        DELETE FROM dbo.EspacioFoto WHERE idEspacioArtistico = @idEspacioArtistico;

        -- Solo las franjas cargadas a mano desde la ficha. Los bloqueos
        -- generados desde "Mis actividades" se conservan siempre.
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

IF OBJECT_ID('dbo.sp_FranjaEspacio_ListarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ListarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado, origen
    FROM dbo.FranjaEspacio
    WHERE idEspacioArtistico = @idEspacioArtistico;
END
GO

IF OBJECT_ID('dbo.sp_FranjaEspacio_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta, f.bloqueado, f.origen
    FROM dbo.FranjaEspacio f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1;
END
GO

-- Limpieza de datos: copias "manuales" de un bloqueo de actividad que quedaron
-- duplicadas por el guardado anterior (misma franja exacta que un bloqueo con
-- origen = 'Actividad' del mismo espacio). El bloqueo real de la actividad se
-- conserva, así que no cambia qué horarios quedan bloqueados.
DELETE m
FROM dbo.FranjaEspacio m
WHERE m.origen = N'Manual'
  AND m.bloqueado = 1
  AND EXISTS
  (
      SELECT 1
      FROM dbo.FranjaEspacio a
      WHERE a.origen = N'Actividad'
        AND a.idEspacioArtistico = m.idEspacioArtistico
        AND a.minutoDesde = m.minutoDesde
        AND a.minutoHasta = m.minutoHasta
        AND ((a.diaSemana = m.diaSemana) OR (a.diaSemana IS NULL AND m.diaSemana IS NULL))
        AND ((a.fecha = m.fecha) OR (a.fecha IS NULL AND m.fecha IS NULL))
  );
GO

IF OBJECT_ID('dbo.sp_FichaEspacio_EliminarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_EliminarPorEspacio;
GO
