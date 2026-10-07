IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.RolInterno', N'U') IS NULL OR OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/08_RolesYPermisos.sql y Database/16_ComponentePermiso.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-001-012 A14: no puede haber dos roles ACTIVOS con el mismo nombre. Un rol
-- dado de baja no bloquea su nombre. El índice solo se crea si los datos
-- actuales ya lo cumplen (si hubiera repetidos, la validación queda en la BLL
-- y en sp_RolInterno_GuardarConPermisos).
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RolInterno_NombreActivo' AND object_id = OBJECT_ID(N'dbo.RolInterno'))
   AND NOT EXISTS (
        SELECT 1 FROM dbo.RolInterno WHERE activo = 1
        GROUP BY LTRIM(RTRIM(nombreRol)) HAVING COUNT(*) > 1)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_RolInterno_NombreActivo
        ON dbo.RolInterno (nombreRol) WHERE activo = 1;
END
GO

-- ---------------------------------------------------------------------------
-- A11 paso 4: listado de roles con área, estado y cantidades (usuarios
-- activos que lo tienen y elementos de permisos asignados).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_RolInterno_ListarResumen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_ListarResumen;
GO
CREATE PROCEDURE dbo.sp_RolInterno_ListarResumen
    @estado NVARCHAR(20) = N'Activos'
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.idRolInterno, r.idAreaInterna, a.nombreArea, r.nombreRol, r.descripcion, r.estadoRol,
           r.fechaAlta, r.fechaBaja, r.fechaUltimaModificacion, r.activo,
           (SELECT COUNT(*) FROM dbo.UsuarioInterno u
             WHERE u.idRolInterno = r.idRolInterno AND u.activo = 1) AS cantidadUsuariosActivos,
           (SELECT COUNT(*) FROM dbo.RolInternoComponentePermiso rc
             INNER JOIN dbo.ComponentePermiso c ON c.idComponentePermiso = rc.idComponentePermiso AND c.activo = 1
             WHERE rc.idRolInterno = r.idRolInterno) AS cantidadComponentes
    FROM dbo.RolInterno r
    LEFT JOIN dbo.AreaInterna a ON a.idAreaInterna = r.idAreaInterna
    WHERE (@estado = N'Todos')
       OR (@estado = N'Inactivos' AND r.activo = 0)
       OR (@estado NOT IN (N'Todos', N'Inactivos') AND r.activo = 1)
    ORDER BY r.activo DESC, r.nombreRol;
END
GO

-- A14: otro rol activo con el mismo nombre (sin distinguir mayúsculas ni
-- espacios en los extremos).
IF OBJECT_ID('dbo.sp_RolInterno_ExisteNombreActivo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_ExisteNombreActivo;
GO
CREATE PROCEDURE dbo.sp_RolInterno_ExisteNombreActivo
    @nombreRol NVARCHAR(200),
    @idRolInternoExcluido INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS cantidad
    FROM dbo.RolInterno
    WHERE activo = 1
      AND LOWER(LTRIM(RTRIM(nombreRol))) = LOWER(LTRIM(RTRIM(@nombreRol)))
      AND (@idRolInternoExcluido IS NULL OR idRolInterno <> @idRolInternoExcluido);
END
GO

-- ---------------------------------------------------------------------------
-- A11 pasos 10 a 12 y A12 pasos 7 a 9: alta o modificación del rol junto con
-- sus permisos, todo en una sola transacción (si algo falla no queda un rol
-- sin permisos). @idsComponentes es la lista de ids separados por coma de los
-- grupos o permisos sueltos tildados. Solo se guardan componentes que existen
-- y están activos; si no queda ninguno, no se guarda nada (A13).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_RolInterno_GuardarConPermisos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RolInterno_GuardarConPermisos;
GO
CREATE PROCEDURE dbo.sp_RolInterno_GuardarConPermisos
    @idRolInterno   INT = NULL,
    @idAreaInterna  INT = NULL,
    @nombreRol      NVARCHAR(200),
    @descripcion    NVARCHAR(510) = NULL,
    @idsComponentes NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @componentes TABLE (idComponentePermiso INT PRIMARY KEY);
    INSERT INTO @componentes (idComponentePermiso)
    SELECT DISTINCT c.idComponentePermiso
    FROM STRING_SPLIT(ISNULL(@idsComponentes, N''), N',') s
    INNER JOIN dbo.ComponentePermiso c ON c.idComponentePermiso = TRY_CONVERT(INT, LTRIM(RTRIM(s.value)))
    WHERE c.activo = 1;

    IF NOT EXISTS (SELECT 1 FROM @componentes)
    BEGIN
        RAISERROR(N'El rol debe tener al menos un permiso asignado.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.RolInterno
               WHERE activo = 1
                 AND LOWER(LTRIM(RTRIM(nombreRol))) = LOWER(LTRIM(RTRIM(@nombreRol)))
                 AND (@idRolInterno IS NULL OR idRolInterno <> @idRolInterno))
    BEGIN
        RAISERROR(N'Ya existe un rol activo con ese nombre.', 16, 1);
        RETURN;
    END

    IF @idAreaInterna IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE idAreaInterna = @idAreaInterna AND activo = 1)
    BEGIN
        RAISERROR(N'El área interna seleccionada no existe o no está activa.', 16, 1);
        RETURN;
    END

    IF @idRolInterno IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RolInterno WHERE idRolInterno = @idRolInterno AND activo = 1)
    BEGIN
        RAISERROR(N'El rol indicado no existe o está dado de baja.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @idRolInterno IS NULL
        BEGIN
            INSERT INTO dbo.RolInterno (idAreaInterna, nombreRol, descripcion, estadoRol, fechaAlta, activo)
            VALUES (@idAreaInterna, LTRIM(RTRIM(@nombreRol)), @descripcion, N'Activo', GETDATE(), 1);

            SET @idRolInterno = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.RolInterno
            SET idAreaInterna = @idAreaInterna,
                nombreRol = LTRIM(RTRIM(@nombreRol)),
                descripcion = @descripcion,
                fechaUltimaModificacion = GETDATE()
            WHERE idRolInterno = @idRolInterno;
        END

        DELETE FROM dbo.RolInternoComponentePermiso WHERE idRolInterno = @idRolInterno;

        INSERT INTO dbo.RolInternoComponentePermiso (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
        SELECT @idRolInterno, idComponentePermiso, GETDATE(), 1
        FROM @componentes;

        COMMIT TRANSACTION;

        SELECT @idRolInterno AS idRolInterno;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
