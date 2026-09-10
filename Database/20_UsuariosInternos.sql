USE StageUp;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.UsuarioInterno', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AreaInterna', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RolInterno', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PermisoInterno', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RolInternoPermiso', N'U') IS NULL
   OR COL_LENGTH(N'dbo.PermisoInterno', N'urlAsociada') IS NULL
BEGIN
    RAISERROR(N'Primero deben ejecutarse los scripts base de seguridad, roles y permisos.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.sp_AreaInterna_ListarActivas', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_AreaInterna_ListarActivas;
END
GO

CREATE PROCEDURE dbo.sp_AreaInterna_ListarActivas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        idAreaInterna,
        nombreArea,
        descripcion,
        estadoArea,
        fechaAlta,
        fechaBaja,
        fechaUltimaModificacion,
        activo
    FROM dbo.AreaInterna
    WHERE activo = 1
      AND estadoArea = N'Activa'
    ORDER BY nombreArea;
END
GO

IF OBJECT_ID(N'dbo.sp_AreaInterna_ObtenerPorId', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_AreaInterna_ObtenerPorId;
END
GO

CREATE PROCEDURE dbo.sp_AreaInterna_ObtenerPorId
    @idAreaInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        idAreaInterna,
        nombreArea,
        descripcion,
        estadoArea,
        fechaAlta,
        fechaBaja,
        fechaUltimaModificacion,
        activo
    FROM dbo.AreaInterna
    WHERE idAreaInterna = @idAreaInterna;
END
GO

IF OBJECT_ID(N'dbo.sp_UsuarioInterno_Listar', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UsuarioInterno_Listar;
END
GO

CREATE PROCEDURE dbo.sp_UsuarioInterno_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.idUsuarioInterno,
        u.idAreaInterna,
        u.idRolInterno,
        a.nombreArea,
        r.nombreRol,
        u.nombre,
        u.apellido,
        u.correoElectronico,
        u.passwordHash,
        u.estadoCuenta,
        u.fechaAlta,
        u.fechaBaja,
        u.fechaUltimaModificacion,
        u.activo
    FROM dbo.UsuarioInterno u
    INNER JOIN dbo.AreaInterna a ON a.idAreaInterna = u.idAreaInterna
    INNER JOIN dbo.RolInterno r ON r.idRolInterno = u.idRolInterno
    ORDER BY u.activo DESC, u.apellido, u.nombre, u.idUsuarioInterno;
END
GO

IF OBJECT_ID(N'dbo.sp_UsuarioInterno_ObtenerPorId', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UsuarioInterno_ObtenerPorId;
END
GO

CREATE PROCEDURE dbo.sp_UsuarioInterno_ObtenerPorId
    @idUsuarioInterno INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.idUsuarioInterno,
        u.idAreaInterna,
        u.idRolInterno,
        a.nombreArea,
        r.nombreRol,
        u.nombre,
        u.apellido,
        u.correoElectronico,
        u.passwordHash,
        u.estadoCuenta,
        u.fechaAlta,
        u.fechaBaja,
        u.fechaUltimaModificacion,
        u.activo
    FROM dbo.UsuarioInterno u
    INNER JOIN dbo.AreaInterna a ON a.idAreaInterna = u.idAreaInterna
    INNER JOIN dbo.RolInterno r ON r.idRolInterno = u.idRolInterno
    WHERE u.idUsuarioInterno = @idUsuarioInterno;
END
GO

IF OBJECT_ID(N'dbo.sp_UsuarioInterno_ExisteCorreo', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UsuarioInterno_ExisteCorreo;
END
GO

CREATE PROCEDURE dbo.sp_UsuarioInterno_ExisteCorreo
    @correoElectronico NVARCHAR(300),
    @idUsuarioInternoExcluido INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM dbo.UsuarioInterno
    WHERE LOWER(LTRIM(RTRIM(correoElectronico))) = LOWER(LTRIM(RTRIM(@correoElectronico)))
      AND (@idUsuarioInternoExcluido IS NULL OR idUsuarioInterno <> @idUsuarioInternoExcluido);
END
GO

IF OBJECT_ID(N'dbo.sp_UsuarioInterno_Insertar', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UsuarioInterno_Insertar;
END
GO

CREATE PROCEDURE dbo.sp_UsuarioInterno_Insertar
    @idAreaInterna INT,
    @idRolInterno INT,
    @nombre NVARCHAR(200),
    @apellido NVARCHAR(200),
    @correoElectronico NVARCHAR(300),
    @passwordHash NVARCHAR(510),
    @estadoCuenta NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.AreaInterna
        WHERE idAreaInterna = @idAreaInterna
          AND activo = 1
          AND estadoArea = N'Activa'
    )
    BEGIN
        RAISERROR(N'El área interna seleccionada no existe o no está activa.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.RolInterno
        WHERE idRolInterno = @idRolInterno
          AND activo = 1
          AND estadoRol = N'Activo'
    )
    BEGIN
        RAISERROR(N'El rol interno seleccionado no existe o no está activo.', 16, 1);
        RETURN;
    END

    IF @estadoCuenta NOT IN (N'Activa', N'Inactiva')
    BEGIN
        RAISERROR(N'El estado de cuenta no es válido.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.UsuarioInterno
        WHERE LOWER(LTRIM(RTRIM(correoElectronico))) = LOWER(LTRIM(RTRIM(@correoElectronico)))
    )
    BEGIN
        RAISERROR(N'Ya existe un usuario interno con ese correo electrónico.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.UsuarioInterno
    (
        idAreaInterna,
        idRolInterno,
        nombre,
        apellido,
        correoElectronico,
        passwordHash,
        estadoCuenta,
        fechaAlta,
        fechaBaja,
        fechaUltimaModificacion,
        activo
    )
    VALUES
    (
        @idAreaInterna,
        @idRolInterno,
        LTRIM(RTRIM(@nombre)),
        LTRIM(RTRIM(@apellido)),
        LOWER(LTRIM(RTRIM(@correoElectronico))),
        @passwordHash,
        @estadoCuenta,
        GETDATE(),
        CASE WHEN @estadoCuenta = N'Inactiva' THEN GETDATE() ELSE NULL END,
        NULL,
        CASE WHEN @estadoCuenta = N'Activa' THEN 1 ELSE 0 END
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idUsuarioInterno;
END
GO

IF OBJECT_ID(N'dbo.sp_UsuarioInterno_Modificar', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UsuarioInterno_Modificar;
END
GO

CREATE PROCEDURE dbo.sp_UsuarioInterno_Modificar
    @idUsuarioInterno INT,
    @idAreaInterna INT,
    @idRolInterno INT,
    @nombre NVARCHAR(200),
    @apellido NVARCHAR(200),
    @correoElectronico NVARCHAR(300),
    @passwordHash NVARCHAR(510) = NULL,
    @estadoCuenta NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE idUsuarioInterno = @idUsuarioInterno)
    BEGIN
        RAISERROR(N'No se encontró el usuario interno seleccionado.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.AreaInterna
        WHERE idAreaInterna = @idAreaInterna
          AND activo = 1
          AND estadoArea = N'Activa'
    )
    BEGIN
        RAISERROR(N'El área interna seleccionada no existe o no está activa.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.RolInterno
        WHERE idRolInterno = @idRolInterno
          AND activo = 1
          AND estadoRol = N'Activo'
    )
    BEGIN
        RAISERROR(N'El rol interno seleccionado no existe o no está activo.', 16, 1);
        RETURN;
    END

    IF @estadoCuenta NOT IN (N'Activa', N'Inactiva')
    BEGIN
        RAISERROR(N'El estado de cuenta no es válido.', 16, 1);
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.UsuarioInterno
        WHERE LOWER(LTRIM(RTRIM(correoElectronico))) = LOWER(LTRIM(RTRIM(@correoElectronico)))
          AND idUsuarioInterno <> @idUsuarioInterno
    )
    BEGIN
        RAISERROR(N'Ya existe un usuario interno con ese correo electrónico.', 16, 1);
        RETURN;
    END

    UPDATE dbo.UsuarioInterno
    SET idAreaInterna = @idAreaInterna,
        idRolInterno = @idRolInterno,
        nombre = LTRIM(RTRIM(@nombre)),
        apellido = LTRIM(RTRIM(@apellido)),
        correoElectronico = LOWER(LTRIM(RTRIM(@correoElectronico))),
        passwordHash = COALESCE(@passwordHash, passwordHash),
        estadoCuenta = @estadoCuenta,
        fechaBaja = CASE
                        WHEN @estadoCuenta = N'Inactiva' THEN COALESCE(fechaBaja, GETDATE())
                        ELSE NULL
                    END,
        fechaUltimaModificacion = GETDATE(),
        activo = CASE WHEN @estadoCuenta = N'Activa' THEN 1 ELSE 0 END
    WHERE idUsuarioInterno = @idUsuarioInterno;
END
GO

IF OBJECT_ID(N'dbo.sp_UsuarioInterno_Baja', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UsuarioInterno_Baja;
END
GO

CREATE PROCEDURE dbo.sp_UsuarioInterno_Baja
    @idUsuarioInterno INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.UsuarioInterno
    SET estadoCuenta = N'Inactiva',
        activo = 0,
        fechaBaja = COALESCE(fechaBaja, GETDATE()),
        fechaUltimaModificacion = GETDATE()
    WHERE idUsuarioInterno = @idUsuarioInterno
      AND activo = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'No se encontró un usuario interno activo para dar de baja.', 16, 1);
    END
END
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE nombreArea = N'Administración')
    BEGIN
        INSERT INTO dbo.AreaInterna (nombreArea, descripcion, estadoArea, fechaAlta, activo)
        VALUES (N'Administración', N'Gestión administrativa y control general de StageUp.', N'Activa', GETDATE(), 1);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE nombreArea = N'Desarrollo')
    BEGIN
        INSERT INTO dbo.AreaInterna (nombreArea, descripcion, estadoArea, fechaAlta, activo)
        VALUES (N'Desarrollo', N'Desarrollo y mantenimiento de la plataforma.', N'Activa', GETDATE(), 1);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE nombreArea = N'Soporte')
    BEGIN
        INSERT INTO dbo.AreaInterna (nombreArea, descripcion, estadoArea, fechaAlta, activo)
        VALUES (N'Soporte', N'Atención y seguimiento de solicitudes de soporte.', N'Activa', GETDATE(), 1);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE nombreArea = N'Comercialización')
    BEGIN
        INSERT INTO dbo.AreaInterna (nombreArea, descripcion, estadoArea, fechaAlta, activo)
        VALUES (N'Comercialización', N'Gestión comercial y crecimiento de StageUp.', N'Activa', GETDATE(), 1);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaInterna WHERE nombreArea = N'Marketing')
    BEGIN
        INSERT INTO dbo.AreaInterna (nombreArea, descripcion, estadoArea, fechaAlta, activo)
        VALUES (N'Marketing', N'Comunicación, campañas y posicionamiento de StageUp.', N'Activa', GETDATE(), 1);
    END

    DECLARE @idPermisoUsuarioInterno INT;

    SELECT TOP 1 @idPermisoUsuarioInterno = idPermisoInterno
    FROM dbo.PermisoInterno
    WHERE codigoPermiso = N'GESTIONAR_USUARIOS_INTERNOS';

    IF @idPermisoUsuarioInterno IS NULL
    BEGIN
        INSERT INTO dbo.PermisoInterno
        (
            codigoPermiso,
            nombrePermiso,
            descripcion,
            modulo,
            accion,
            estadoPermiso,
            urlAsociada,
            fechaAlta,
            activo
        )
        VALUES
        (
            N'GESTIONAR_USUARIOS_INTERNOS',
            N'Usuarios internos',
            N'Dar de alta, consultar, modificar y dar de baja lógica las cuentas del equipo de Artera.',
            N'Administración',
            N'Gestionar',
            N'Activo',
            N'~/Interno/GestionUsuariosInternos.aspx',
            GETDATE(),
            1
        );

        SET @idPermisoUsuarioInterno = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.PermisoInterno
        SET nombrePermiso = N'Usuarios internos',
            descripcion = N'Dar de alta, consultar, modificar y dar de baja lógica las cuentas del equipo de Artera.',
            modulo = N'Administración',
            accion = N'Gestionar',
            estadoPermiso = N'Activo',
            urlAsociada = N'~/Interno/GestionUsuariosInternos.aspx',
            fechaUltimaModificacion = GETDATE(),
            activo = 1
        WHERE idPermisoInterno = @idPermisoUsuarioInterno;
    END

    DECLARE @idRolAdministrador INT;

    SELECT TOP 1 @idRolAdministrador = idRolInterno
    FROM dbo.RolInterno
    WHERE nombreRol = N'Administrador'
      AND activo = 1
    ORDER BY idRolInterno;

    IF @idRolAdministrador IS NOT NULL
    BEGIN
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.RolInternoPermiso
            WHERE idRolInterno = @idRolAdministrador
              AND idPermisoInterno = @idPermisoUsuarioInterno
        )
        BEGIN
            INSERT INTO dbo.RolInternoPermiso
                (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
            VALUES
                (@idRolAdministrador, @idPermisoUsuarioInterno, GETDATE(), 1);
        END
        ELSE
        BEGIN
            UPDATE dbo.RolInternoPermiso
            SET activo = 1
            WHERE idRolInterno = @idRolAdministrador
              AND idPermisoInterno = @idPermisoUsuarioInterno;
        END
    END

    IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
    BEGIN
        DECLARE @idGrupoAdministracion INT;
        DECLARE @idComponenteUsuarioInterno INT;

        SELECT TOP 1 @idGrupoAdministracion = idComponentePermiso
        FROM dbo.ComponentePermiso
        WHERE tipoComponente = N'Grupo'
          AND idComponentePadre IS NULL
          AND nombre = N'Administración'
        ORDER BY idComponentePermiso;

        IF @idGrupoAdministracion IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (NULL, N'Grupo', NULL, N'Administración', NULL, NULL, 100, 1);

            SET @idGrupoAdministracion = CAST(SCOPE_IDENTITY() AS INT);
        END

        SELECT TOP 1 @idComponenteUsuarioInterno = idComponentePermiso
        FROM dbo.ComponentePermiso
        WHERE codigoPermiso = N'GESTIONAR_USUARIOS_INTERNOS'
        ORDER BY idComponentePermiso;

        IF @idComponenteUsuarioInterno IS NULL
        BEGIN
            INSERT INTO dbo.ComponentePermiso
                (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
            VALUES
                (@idGrupoAdministracion, N'Permiso', N'GESTIONAR_USUARIOS_INTERNOS', N'Usuarios internos',
                 N'Dar de alta, consultar, modificar y dar de baja lógica las cuentas del equipo de Artera.',
                 N'~/Interno/GestionUsuariosInternos.aspx', 20, 1);

            SET @idComponenteUsuarioInterno = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ComponentePermiso
            SET idComponentePadre = @idGrupoAdministracion,
                tipoComponente = N'Permiso',
                nombre = N'Usuarios internos',
                descripcion = N'Dar de alta, consultar, modificar y dar de baja lógica las cuentas del equipo de Artera.',
                urlAsociada = N'~/Interno/GestionUsuariosInternos.aspx',
                orden = 20,
                fechaUltimaModificacion = GETDATE(),
                activo = 1
            WHERE idComponentePermiso = @idComponenteUsuarioInterno;
        END

        IF @idRolAdministrador IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.RolInternoComponentePermiso
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteUsuarioInterno
            )
            BEGIN
                INSERT INTO dbo.RolInternoComponentePermiso
                    (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                VALUES
                    (@idRolAdministrador, @idComponenteUsuarioInterno, GETDATE(), 1);
            END
            ELSE
            BEGIN
                UPDATE dbo.RolInternoComponentePermiso
                SET activo = 1
                WHERE idRolInterno = @idRolAdministrador
                  AND idComponentePermiso = @idComponenteUsuarioInterno;
            END
        END
    END

    IF OBJECT_ID(N'dbo.EtiquetaTraduccion', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.Traduccion', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.Idioma', N'U') IS NOT NULL
    BEGIN
        MERGE dbo.EtiquetaTraduccion AS destino
        USING
        (
            VALUES
            (N'AdminUsuarios_Seccion', N'Administración', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Titulo', N'Usuarios internos', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Descripcion', N'Creá las cuentas del equipo de Artera y asignales un área, un rol y el estado de acceso correspondiente.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Nombre', N'Nombre *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Apellido', N'Apellido *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Correo', N'Correo electrónico *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Area', N'Área interna *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Rol', N'Rol asignado *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Estado', N'Estado de la cuenta *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Password', N'Contraseña *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_ConfirmarPassword', N'Confirmar contraseña *', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_AyudaRol', N'El usuario heredará todos los permisos configurados para este rol.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PermisosRol', N'Permisos del rol actual', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Guardar', N'Guardar usuario', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Cancelar', N'Cancelar edición', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Listado', N'Equipo interno', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_GestionarRoles', N'Gestionar roles', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_SinUsuarios', N'Todavía no hay usuarios internos para mostrar', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_SinUsuariosAyuda', N'Completá el formulario para crear la primera cuenta del equipo.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_AreaCorta', N'Área:', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_RolCorto', N'Rol:', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Editar', N'Ver / editar', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Baja', N'Dar de baja', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PlaceholderNombre', N'Nombre', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PlaceholderApellido', N'Apellido', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PlaceholderCorreo', N'persona@stageup.com', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PlaceholderPassword', N'Mínimo 8 caracteres', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PlaceholderConfirmarPassword', N'Repetí la contraseña', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_MenuDescripcion', N'Dar de alta, consultar, modificar y dar de baja lógica las cuentas del equipo de Artera.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_ConfirmarBaja', N'¿Seguro que querés dar de baja este usuario interno? Se conservará su historial.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Nuevo', N'Nuevo usuario interno', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_EditarTitulo', N'Editar usuario interno', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_AyudaAlta', N'Todos los campos identificados con un asterisco son obligatorios.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_AyudaEdicion', N'Actualizá los datos de la cuenta seleccionada.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PasswordInicial', N'Contraseña inicial', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PasswordNueva', N'Nueva contraseña', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PasswordRegla', N'Debe tener al menos 8 caracteres, letras y números.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_PasswordOpcional', N'Dejá ambos campos vacíos para conservar la contraseña actual.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_GuardarCambios', N'Guardar cambios', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_SinPermisos', N'Este rol todavía no tiene permisos configurados.', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_SeleccionArea', N'Seleccioná un área', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_SeleccionRol', N'Seleccioná un rol', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Activa', N'Activa', N'Interno/GestionUsuariosInternos'),
            (N'AdminUsuarios_Inactiva', N'Inactiva', N'Interno/GestionUsuariosInternos')
        ) AS origen(claveEtiqueta, textoPredeterminado, modulo)
            ON destino.claveEtiqueta = origen.claveEtiqueta
        WHEN MATCHED THEN
            UPDATE SET
                destino.textoPredeterminado = origen.textoPredeterminado,
                destino.modulo = origen.modulo,
                destino.fechaUltimaModificacion = GETDATE(),
                destino.activo = 1
        WHEN NOT MATCHED THEN
            INSERT (claveEtiqueta, textoPredeterminado, modulo, activo)
            VALUES (origen.claveEtiqueta, origen.textoPredeterminado, origen.modulo, 1);

        DECLARE @idEspanol INT;
        DECLARE @idIngles INT;

        SELECT TOP 1 @idEspanol = idIdioma
        FROM dbo.Idioma
        WHERE codigoIdioma = N'es-AR'
           OR codigoIdioma = N'es'
           OR codigoIdioma LIKE N'es-%'
           OR nombreIdioma IN (N'Español', N'Espanol', N'Spanish')
        ORDER BY idIdioma;

        SELECT TOP 1 @idIngles = idIdioma
        FROM dbo.Idioma
        WHERE codigoIdioma = N'en-US'
           OR codigoIdioma = N'en'
           OR codigoIdioma LIKE N'en-%'
           OR codigoIdioma = N'us'
           OR nombreIdioma IN (N'Inglés', N'Ingles', N'English')
        ORDER BY idIdioma;

        IF @idEspanol IS NOT NULL
        BEGIN
            MERGE dbo.Traduccion AS destino
            USING
            (
                SELECT @idEspanol, idEtiquetaTraduccion, textoPredeterminado
                FROM dbo.EtiquetaTraduccion
                WHERE claveEtiqueta LIKE N'AdminUsuarios[_]%'
            ) AS origen(idIdioma, idEtiquetaTraduccion, textoTraducido)
                ON destino.idIdioma = origen.idIdioma
               AND destino.idEtiquetaTraduccion = origen.idEtiquetaTraduccion
            WHEN MATCHED THEN
                UPDATE SET destino.textoTraducido = origen.textoTraducido,
                           destino.fechaUltimaModificacion = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (idIdioma, idEtiquetaTraduccion, textoTraducido)
                VALUES (origen.idIdioma, origen.idEtiquetaTraduccion, origen.textoTraducido);
        END

        IF @idIngles IS NOT NULL
        BEGIN
            MERGE dbo.Traduccion AS destino
            USING
            (
                SELECT @idIngles, etiqueta.idEtiquetaTraduccion, traducciones.textoTraducido
                FROM
                (
                    VALUES
                    (N'AdminUsuarios_Seccion', N'Administration'),
                    (N'AdminUsuarios_Titulo', N'Internal users'),
                    (N'AdminUsuarios_Descripcion', N'Create Artera team accounts and assign each one an area, a role, and the appropriate access status.'),
                    (N'AdminUsuarios_Nombre', N'First name *'),
                    (N'AdminUsuarios_Apellido', N'Last name *'),
                    (N'AdminUsuarios_Correo', N'Email *'),
                    (N'AdminUsuarios_Area', N'Internal area *'),
                    (N'AdminUsuarios_Rol', N'Assigned role *'),
                    (N'AdminUsuarios_Estado', N'Account status *'),
                    (N'AdminUsuarios_Password', N'Password *'),
                    (N'AdminUsuarios_ConfirmarPassword', N'Confirm password *'),
                    (N'AdminUsuarios_AyudaRol', N'The user will inherit all permissions configured for this role.'),
                    (N'AdminUsuarios_PermisosRol', N'Current role permissions'),
                    (N'AdminUsuarios_Guardar', N'Save user'),
                    (N'AdminUsuarios_Cancelar', N'Cancel editing'),
                    (N'AdminUsuarios_Listado', N'Internal team'),
                    (N'AdminUsuarios_GestionarRoles', N'Manage roles'),
                    (N'AdminUsuarios_SinUsuarios', N'There are no internal users to display yet'),
                    (N'AdminUsuarios_SinUsuariosAyuda', N'Complete the form to create the first team account.'),
                    (N'AdminUsuarios_AreaCorta', N'Area:'),
                    (N'AdminUsuarios_RolCorto', N'Role:'),
                    (N'AdminUsuarios_Editar', N'View / edit'),
                    (N'AdminUsuarios_Baja', N'Deactivate'),
                    (N'AdminUsuarios_PlaceholderNombre', N'First name'),
                    (N'AdminUsuarios_PlaceholderApellido', N'Last name'),
                    (N'AdminUsuarios_PlaceholderCorreo', N'person@stageup.com'),
                    (N'AdminUsuarios_PlaceholderPassword', N'At least 8 characters'),
                    (N'AdminUsuarios_PlaceholderConfirmarPassword', N'Repeat the password'),
                    (N'AdminUsuarios_MenuDescripcion', N'Create, view, edit, and logically deactivate Artera team accounts.'),
                    (N'AdminUsuarios_ConfirmarBaja', N'Are you sure you want to deactivate this internal user? Their history will be preserved.'),
                    (N'AdminUsuarios_Nuevo', N'New internal user'),
                    (N'AdminUsuarios_EditarTitulo', N'Edit internal user'),
                    (N'AdminUsuarios_AyudaAlta', N'All fields marked with an asterisk are required.'),
                    (N'AdminUsuarios_AyudaEdicion', N'Update the selected account details.'),
                    (N'AdminUsuarios_PasswordInicial', N'Initial password'),
                    (N'AdminUsuarios_PasswordNueva', N'New password'),
                    (N'AdminUsuarios_PasswordRegla', N'It must have at least 8 characters, letters, and numbers.'),
                    (N'AdminUsuarios_PasswordOpcional', N'Leave both fields blank to keep the current password.'),
                    (N'AdminUsuarios_GuardarCambios', N'Save changes'),
                    (N'AdminUsuarios_SinPermisos', N'This role does not have any permissions configured yet.'),
                    (N'AdminUsuarios_SeleccionArea', N'Select an area'),
                    (N'AdminUsuarios_SeleccionRol', N'Select a role'),
                    (N'AdminUsuarios_Activa', N'Active'),
                    (N'AdminUsuarios_Inactiva', N'Inactive')
                ) AS traducciones(claveEtiqueta, textoTraducido)
                INNER JOIN dbo.EtiquetaTraduccion etiqueta
                    ON etiqueta.claveEtiqueta = traducciones.claveEtiqueta
            ) AS origen(idIdioma, idEtiquetaTraduccion, textoTraducido)
                ON destino.idIdioma = origen.idIdioma
               AND destino.idEtiquetaTraduccion = origen.idEtiquetaTraduccion
            WHEN MATCHED THEN
                UPDATE SET destino.textoTraducido = origen.textoTraducido,
                           destino.fechaUltimaModificacion = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (idIdioma, idEtiquetaTraduccion, textoTraducido)
                VALUES (origen.idIdioma, origen.idEtiquetaTraduccion, origen.textoTraducido);
        END
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @mensajeError NVARCHAR(2048);
    SET @mensajeError = ERROR_MESSAGE();
    RAISERROR(N'%s', 16, 1, @mensajeError);
END CATCH;
GO
