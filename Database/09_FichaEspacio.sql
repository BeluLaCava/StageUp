IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- Reemplazo del modelo "ficha completa" del espacio artístico (foto, ubicación,
-- capacidad, precio por hora, piso, equipamiento y disponibilidad semanal), que
-- María había implementado guardando todo como un único JSON en una columna
-- fichaJson (con SPs "V2" creados a mano en la base, nunca commiteados como
-- script). Se rehace acá con columnas y tablas normales, en 1 a 1 y 1 a N con
-- dbo.EspacioArtistico, para que quede alineado al diccionario de datos.
--
-- FichaEspacio: 1 a 1 con EspacioArtistico (misma PK, también FK).
-- FichaEspacioEquipamiento: 1 a N, un renglón por característica marcada.
-- FranjaEspacio: 1 a N, un renglón por horario semanal o excepción de fecha.
--
-- El guardado sigue el mismo patrón "borrar todo y volver a insertar" que ya
-- se usa para RolInternoPermiso (ver 06_Reservas.sql / BLL_PermisoInterno): el
-- formulario de "Mis espacios" siempre manda la ficha completa, así que no hace
-- falta un UPDATE fila por fila. Los DELETE de FichaEspacio se propagan por
-- ON DELETE CASCADE a sus dos tablas hijas, así que no hace falta un
-- procedimiento aparte para vaciarlas antes de reinsertar.

-- Limpieza: sp_EspacioArtistico_GuardarFichaV2 era el procedimiento "todo en
-- uno" que guardaba directo contra fichaJson. Ya no lo llama nadie (GuardarFicha
-- ahora hace INSERT/UPDATE del espacio base y después guarda la ficha aparte),
-- así que si existe en la base de alguien que lo haya probado suelto, se saca.
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
    -- Cada franja es o bien semanal (diaSemana) o bien una excepción de una fecha
    -- concreta (fecha), nunca las dos cosas ni ninguna. Mismo criterio que ya
    -- valida BLL_EspacioArtistico.ValidarFicha en el lado de la aplicación.
    CONSTRAINT CK_FranjaEspacio_diaOFecha CHECK (
    (fecha IS NULL AND diaSemana IS NOT NULL) OR (fecha IS NOT NULL AND diaSemana IS NULL)
),
    CONSTRAINT CK_FranjaEspacio_diaSemana CHECK (diaSemana IS NULL OR diaSemana BETWEEN 1 AND 7),
    CONSTRAINT CK_FranjaEspacio_horario CHECK (minutoDesde >= 0 AND minutoHasta <= 1440 AND minutoHasta > minutoDesde)
);
GO

-- ==================== FichaEspacio ====================

IF OBJECT_ID('dbo.sp_FichaEspacio_EliminarPorEspacio', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_FichaEspacio_EliminarPorEspacio;
GO
CREATE PROCEDURE dbo.sp_FichaEspacio_EliminarPorEspacio
    @idEspacioArtistico INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- El DELETE se propaga por ON DELETE CASCADE a FichaEspacioEquipamiento
        -- y FranjaEspacio: no hace falta vaciarlas aparte antes de este paso.
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

-- ==================== FichaEspacioEquipamiento ====================

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

-- ==================== FranjaEspacio ====================

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

-- ==================== EspacioArtistico + FichaEspacio (lectura combinada) ====================
-- Reemplazan a las V2 que María había creado a mano en la base (nunca
-- commiteadas): misma idea (LEFT JOIN con la ficha, para que un espacio sin
-- ficha completa cargada todavía se pueda seguir leyendo), pero contra las
-- columnas normales en vez de fichaJson.

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
