IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- =============================================================================
-- 09_FichaEspacio.sql
-- ADVERTENCIA: SCRIPT INICIAL DESTRUCTIVO (contiene DROP TABLE).
--
-- Sirve para armar la base desde cero. NO debe ejecutarse a mano sobre una base
-- con datos: borraría estas tablas y todo su contenido: FichaEspacio, FichaEspacioEquipamiento, FranjaEspacio.
-- El flujo normal de instalación y actualización es Database/EjecutarTodosLosScripts.ps1,
-- que usa dbo._ScriptsEjecutados y nunca vuelve a correr un script ya aplicado.
-- Ver Database/LEEME_Scripts.md.
--
-- Protección: si alguna de esas tablas ya tiene filas, el script se detiene acá
-- con un error y no ejecuta nada más (SET NOEXEC ON). En una base nueva las tablas
-- no existen o están vacías, así que sigue normalmente.
-- Nota (cascada FichaEspacio -> FranjaEspacio): el ON DELETE CASCADE sigue
-- existiendo como regla de integridad, pero desde el script 41 la aplicación ya no
-- borra la ficha al guardarla (sp_FichaEspacio_Guardar hace UPDATE), así que
-- guardar desde "Mis espacios" nunca elimina bloqueos generados por actividades.
-- =============================================================================
IF EXISTS
(
    SELECT 1
    FROM sys.partitions p
    WHERE p.index_id IN (0, 1)
      AND p.rows > 0
      AND p.object_id IN
      (
        OBJECT_ID(N'dbo.FichaEspacio', N'U'),
        OBJECT_ID(N'dbo.FichaEspacioEquipamiento', N'U'),
        OBJECT_ID(N'dbo.FranjaEspacio', N'U')
      )
)
BEGIN
    RAISERROR(N'09_FichaEspacio.sql es un script inicial destructivo y la base ya tiene datos en sus tablas. No se ejecutó nada. Para actualizar una base existente usá EjecutarTodosLosScripts.ps1.', 16, 1);
    SET NOEXEC ON;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_GuardarFichaV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_GuardarFichaV2;
GO

IF OBJECT_ID('dbo.FranjaEspacio', 'U') IS NOT NULL DROP TABLE dbo.FranjaEspacio;
GO
IF OBJECT_ID('dbo.FichaEspacioEquipamiento', 'U') IS NOT NULL DROP TABLE dbo.FichaEspacioEquipamiento;
GO
IF OBJECT_ID('dbo.FichaEspacio', 'U') IS NOT NULL DROP TABLE dbo.FichaEspacio;
GO

CREATE TABLE dbo.FichaEspacio
(
    idEspacioArtistico      INT             NOT NULL,
    fotoRuta                NVARCHAR(300)   NULL,
    provincia               NVARCHAR(100)   NOT NULL,
    ciudad                  NVARCHAR(150)   NOT NULL,
    direccion               NVARCHAR(300)   NOT NULL,
    capacidadMaxima         INT             NOT NULL,
    precioHora              DECIMAL(10,2)   NOT NULL,
    moneda                  NVARCHAR(3)     NOT NULL CONSTRAINT DF_FichaEspacio_moneda DEFAULT (N'ARS'),
    tipoPiso                NVARCHAR(100)   NULL,
    detalleEquipamiento     NVARCHAR(1000)  NULL,
    fechaUltimaModificacion DATETIME        NOT NULL CONSTRAINT DF_FichaEspacio_fechaUltimaModificacion DEFAULT (GETDATE()),
    CONSTRAINT PK_FichaEspacio PRIMARY KEY CLUSTERED (idEspacioArtistico ASC),
    CONSTRAINT FK_FichaEspacio_EspacioArtistico FOREIGN KEY (idEspacioArtistico)
        REFERENCES dbo.EspacioArtistico (idEspacioArtistico),
    CONSTRAINT CK_FichaEspacio_moneda CHECK (moneda IN (N'ARS', N'USD')),
    CONSTRAINT CK_FichaEspacio_capacidadMaxima CHECK (capacidadMaxima BETWEEN 1 AND 100000),
    CONSTRAINT CK_FichaEspacio_precioHora CHECK (precioHora > 0)
);
GO

CREATE TABLE dbo.FichaEspacioEquipamiento
(
    idEspacioArtistico  INT             NOT NULL,
    codigoEquipamiento  NVARCHAR(50)    NOT NULL,
    CONSTRAINT PK_FichaEspacioEquipamiento PRIMARY KEY CLUSTERED (idEspacioArtistico ASC, codigoEquipamiento ASC),
    CONSTRAINT FK_FichaEspacioEquipamiento_FichaEspacio FOREIGN KEY (idEspacioArtistico)
        REFERENCES dbo.FichaEspacio (idEspacioArtistico) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.FranjaEspacio
(
    idFranjaEspacio     INT IDENTITY(1,1) NOT NULL,
    idEspacioArtistico  INT             NOT NULL,
    diaSemana           TINYINT         NULL,
    fecha               DATE            NULL,
    minutoDesde         SMALLINT        NOT NULL,
    minutoHasta         SMALLINT        NOT NULL,
    bloqueado           BIT             NOT NULL CONSTRAINT DF_FranjaEspacio_bloqueado DEFAULT (0),
    CONSTRAINT PK_FranjaEspacio PRIMARY KEY CLUSTERED (idFranjaEspacio ASC),
    CONSTRAINT FK_FranjaEspacio_FichaEspacio FOREIGN KEY (idEspacioArtistico)
        REFERENCES dbo.FichaEspacio (idEspacioArtistico) ON DELETE CASCADE,
    CONSTRAINT CK_FranjaEspacio_diaOFecha CHECK (
    (fecha IS NULL AND diaSemana IS NOT NULL) OR (fecha IS NOT NULL AND diaSemana IS NULL)
),
    CONSTRAINT CK_FranjaEspacio_diaSemana CHECK (diaSemana IS NULL OR diaSemana BETWEEN 1 AND 7),
    CONSTRAINT CK_FranjaEspacio_horario CHECK (minutoDesde >= 0 AND minutoHasta <= 1440 AND minutoHasta > minutoDesde)
);
GO


IF OBJECT_ID('dbo.sp_FichaEspacio_EliminarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_EliminarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_EliminarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.FichaEspacio WHERE idEspacioArtistico = @idEspacioArtistico;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_FichaEspacio_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_Insertar;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_Insertar
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
        INSERT INTO dbo.FichaEspacio
            (idEspacioArtistico, fotoRuta, provincia, ciudad, direccion, capacidadMaxima,
             precioHora, moneda, tipoPiso, detalleEquipamiento, fechaUltimaModificacion)
        VALUES
            (@idEspacioArtistico, @fotoRuta, @provincia, @ciudad, @direccion, @capacidadMaxima,
             @precioHora, @moneda, @tipoPiso, @detalleEquipamiento, GETDATE());
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO


IF OBJECT_ID('dbo.sp_FichaEspacioEquipamiento_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacioEquipamiento_Insertar;
GO
CREATE PROCEDURE dbo.sp_FichaEspacioEquipamiento_Insertar
    @idEspacioArtistico INT,
    @codigoEquipamiento NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.FichaEspacioEquipamiento (idEspacioArtistico, codigoEquipamiento)
        VALUES (@idEspacioArtistico, @codigoEquipamiento);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_FichaEspacioEquipamiento_ListarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idEspacioArtistico, codigoEquipamiento
    FROM dbo.FichaEspacioEquipamiento
    WHERE idEspacioArtistico = @idEspacioArtistico;
END
GO

IF OBJECT_ID('dbo.sp_FichaEspacioEquipamiento_ListarPorUsuarioGestor', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPorUsuarioGestor;
GO
CREATE PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPorUsuarioGestor
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT fe.idEspacioArtistico, fe.codigoEquipamiento
    FROM dbo.FichaEspacioEquipamiento fe
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = fe.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_FichaEspacioEquipamiento_ListarPublicados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPublicados;
GO
CREATE PROCEDURE dbo.sp_FichaEspacioEquipamiento_ListarPublicados
AS
BEGIN
    SET NOCOUNT ON;
    SELECT fe.idEspacioArtistico, fe.codigoEquipamiento
    FROM dbo.FichaEspacioEquipamiento fe
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = fe.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1;
END
GO


IF OBJECT_ID('dbo.sp_FranjaEspacio_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_Insertar;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_Insertar
    @idEspacioArtistico INT,
    @diaSemana          TINYINT     = NULL,
    @fecha              DATE        = NULL,
    @minutoDesde        SMALLINT,
    @minutoHasta        SMALLINT,
    @bloqueado          BIT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.FranjaEspacio (idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado)
        VALUES (@idEspacioArtistico, @diaSemana, @fecha, @minutoDesde, @minutoHasta, @bloqueado);
    END TRY
    BEGIN CATCH
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
    SELECT idEspacioArtistico, diaSemana, fecha, minutoDesde, minutoHasta, bloqueado
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
    SELECT f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta, f.bloqueado
    FROM dbo.FranjaEspacio f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_FranjaEspacio_ListarPublicados', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FranjaEspacio_ListarPublicados;
GO
CREATE PROCEDURE dbo.sp_FranjaEspacio_ListarPublicados
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idEspacioArtistico, f.diaSemana, f.fecha, f.minutoDesde, f.minutoHasta, f.bloqueado
    FROM dbo.FranjaEspacio f
    INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = f.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1;
END
GO


IF OBJECT_ID('dbo.sp_EspacioArtistico_ObtenerPorIdV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ObtenerPorIdV2;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ObtenerPorIdV2
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE e.idEspacioArtistico = @idEspacioArtistico;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPorUsuarioGestorV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPorUsuarioGestorV2;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPorUsuarioGestorV2
    @idUsuarioGestor INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE e.idUsuarioGestor = @idUsuarioGestor
      AND e.activo = 1
    ORDER BY e.fechaAlta DESC;
END
GO

IF OBJECT_ID('dbo.sp_EspacioArtistico_ListarPublicadosV2', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_EspacioArtistico_ListarPublicadosV2;
GO
CREATE PROCEDURE dbo.sp_EspacioArtistico_ListarPublicadosV2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.idEspacioArtistico, e.idUsuarioGestor, e.nombreEspacio, e.descripcion, e.tipoEspacio,
        e.estadoEspacio, e.publicado, e.activo, e.fechaAlta, e.fechaPublicacion, e.fechaBaja, e.fechaUltimaModificacion,
        f.fotoRuta, f.provincia, f.ciudad, f.direccion, f.capacidadMaxima, f.precioHora, f.moneda, f.tipoPiso, f.detalleEquipamiento
    FROM dbo.EspacioArtistico e
    LEFT JOIN dbo.FichaEspacio f ON f.idEspacioArtistico = e.idEspacioArtistico
    WHERE e.activo = 1
      AND e.publicado = 1
    ORDER BY e.fechaPublicacion DESC;
END
GO

-- Deja la sesión como estaba si la protección de arriba frenó el script.
SET NOEXEC OFF;
GO
