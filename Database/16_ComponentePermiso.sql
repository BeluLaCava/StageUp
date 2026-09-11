USE StageUp;
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.RolInterno', N'U') IS NULL OR OBJECT_ID(N'dbo.PermisoInterno', N'U') IS NULL OR OBJECT_ID(N'dbo.RolInternoPermiso', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/08_RolesYPermisos.sql.', 1;
END
GO


IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComponentePermiso
    (
        idComponentePermiso        INT IDENTITY(1,1) NOT NULL,
        idComponentePadre          INT NULL,
        tipoComponente              NVARCHAR(20) NOT NULL,
        codigoPermiso               NVARCHAR(100) NULL,
        nombre                      NVARCHAR(200) NOT NULL,
        descripcion                 NVARCHAR(510) NULL,
        urlAsociada                 NVARCHAR(300) NULL,
        orden                       INT NOT NULL CONSTRAINT DF_ComponentePermiso_orden DEFAULT (0),
        fechaAlta                   DATETIME NOT NULL CONSTRAINT DF_ComponentePermiso_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion     DATETIME NULL,
        activo                      BIT NOT NULL CONSTRAINT DF_ComponentePermiso_activo DEFAULT (1),
        CONSTRAINT PK_ComponentePermiso PRIMARY KEY CLUSTERED (idComponentePermiso),
        CONSTRAINT FK_ComponentePermiso_Padre FOREIGN KEY (idComponentePadre) REFERENCES dbo.ComponentePermiso (idComponentePermiso),
        CONSTRAINT CK_ComponentePermiso_Tipo CHECK (tipoComponente IN (N'Grupo', N'Permiso'))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_ComponentePermiso_Codigo' AND object_id = OBJECT_ID(N'dbo.ComponentePermiso'))
BEGIN
    CREATE UNIQUE INDEX UQ_ComponentePermiso_Codigo ON dbo.ComponentePermiso (codigoPermiso) WHERE codigoPermiso IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolInternoComponentePermiso
    (
        idRolInternoComponentePermiso  INT IDENTITY(1,1) NOT NULL,
        idRolInterno                    INT NOT NULL,
        idComponentePermiso             INT NOT NULL,
        fechaAsignacion                 DATETIME NOT NULL CONSTRAINT DF_RolInternoComponentePermiso_fecha DEFAULT (GETDATE()),
        activo                           BIT NOT NULL CONSTRAINT DF_RolInternoComponentePermiso_activo DEFAULT (1),
        CONSTRAINT PK_RolInternoComponentePermiso PRIMARY KEY CLUSTERED (idRolInternoComponentePermiso),
        CONSTRAINT FK_RolInternoComponentePermiso_Rol FOREIGN KEY (idRolInterno) REFERENCES dbo.RolInterno (idRolInterno),
        CONSTRAINT FK_RolInternoComponentePermiso_Componente FOREIGN KEY (idComponentePermiso) REFERENCES dbo.ComponentePermiso (idComponentePermiso),
        CONSTRAINT UQ_RolInternoComponentePermiso UNIQUE (idRolInterno, idComponentePermiso)
    );
END
GO


INSERT INTO dbo.ComponentePermiso (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
SELECT NULL, N'Grupo', NULL, modulos.modulo, NULL, NULL, modulos.ordenModulo, 1
FROM (
    SELECT modulo, ROW_NUMBER() OVER (ORDER BY MIN(nombrePermiso)) AS ordenModulo
    FROM dbo.PermisoInterno
    WHERE activo = 1
    GROUP BY modulo
) AS modulos
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ComponentePermiso cp
    WHERE cp.tipoComponente = N'Grupo' AND cp.idComponentePadre IS NULL AND cp.nombre = modulos.modulo
);
GO

INSERT INTO dbo.ComponentePermiso (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
SELECT grp.idComponentePermiso, N'Permiso', p.codigoPermiso, p.nombrePermiso, p.descripcion, p.urlAsociada,
       ROW_NUMBER() OVER (PARTITION BY p.modulo ORDER BY p.nombrePermiso), 1
FROM dbo.PermisoInterno p
INNER JOIN dbo.ComponentePermiso grp
    ON grp.tipoComponente = N'Grupo' AND grp.idComponentePadre IS NULL AND grp.nombre = p.modulo
WHERE p.activo = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.ComponentePermiso cp WHERE cp.codigoPermiso = p.codigoPermiso);
GO

INSERT INTO dbo.RolInternoComponentePermiso (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
SELECT rp.idRolInterno, cp.idComponentePermiso, rp.fechaAsignacion, 1
FROM dbo.RolInternoPermiso rp
INNER JOIN dbo.PermisoInterno p ON p.idPermisoInterno = rp.idPermisoInterno
INNER JOIN dbo.ComponentePermiso cp ON cp.codigoPermiso = p.codigoPermiso
WHERE rp.activo = 1
  AND p.activo = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolInternoComponentePermiso rcp
      WHERE rcp.idRolInterno = rp.idRolInterno AND rcp.idComponentePermiso = cp.idComponentePermiso
  );
GO


IF OBJECT_ID(N'dbo.sp_ComponentePermiso_ListarTodos', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_ComponentePermiso_ListarTodos;
GO
CREATE PROCEDURE dbo.sp_ComponentePermiso_ListarTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idComponentePermiso, idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden
    FROM dbo.ComponentePermiso
    WHERE activo = 1
    ORDER BY idComponentePadre, orden, nombre;
END
GO

IF OBJECT_ID(N'dbo.sp_RolInternoComponentePermiso_ListarPorRol', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInternoComponentePermiso_ListarPorRol;
GO
CREATE PROCEDURE dbo.sp_RolInternoComponentePermiso_ListarPorRol
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT cp.idComponentePermiso
    FROM dbo.RolInternoComponentePermiso rcp
    INNER JOIN dbo.ComponentePermiso cp ON cp.idComponentePermiso = rcp.idComponentePermiso
    WHERE rcp.idRolInterno = @idRolInterno
      AND rcp.activo = 1
      AND cp.activo = 1;
END
GO

IF OBJECT_ID(N'dbo.sp_RolInternoComponentePermiso_EliminarPorRol', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInternoComponentePermiso_EliminarPorRol;
GO
CREATE PROCEDURE dbo.sp_RolInternoComponentePermiso_EliminarPorRol
    @idRolInterno INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.RolInternoComponentePermiso WHERE idRolInterno = @idRolInterno;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.sp_RolInternoComponentePermiso_Insertar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInternoComponentePermiso_Insertar;
GO
CREATE PROCEDURE dbo.sp_RolInternoComponentePermiso_Insertar
    @idRolInterno           INT,
    @idComponentePermiso    INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.RolInternoComponentePermiso (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
        VALUES (@idRolInterno, @idComponentePermiso, GETDATE(), 1);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
