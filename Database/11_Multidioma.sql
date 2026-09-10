USE StageUp;
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.PermisoInterno', N'U') IS NULL OR OBJECT_ID(N'dbo.RolInterno', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/08_RolesYPermisos.sql.', 1;
END
GO

IF OBJECT_ID(N'dbo.Idioma', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Idioma
    (
        idIdioma                   INT IDENTITY(1,1) NOT NULL,
        codigoIdioma               NVARCHAR(20) NOT NULL,
        nombreIdioma               NVARCHAR(100) NOT NULL,
        esPredeterminado           BIT NOT NULL CONSTRAINT DF_Idioma_esPredeterminado DEFAULT (0),
        estadoIdioma               NVARCHAR(50) NOT NULL CONSTRAINT DF_Idioma_estado DEFAULT (N'Activo'),
        fechaAlta                  DATETIME NOT NULL CONSTRAINT DF_Idioma_fechaAlta DEFAULT (GETDATE()),
        fechaBaja                  DATETIME NULL,
        fechaUltimaModificacion    DATETIME NULL,
        activo                     BIT NOT NULL CONSTRAINT DF_Idioma_activo DEFAULT (1),
        CONSTRAINT PK_Idioma PRIMARY KEY CLUSTERED (idIdioma),
        CONSTRAINT UQ_Idioma_codigo UNIQUE (codigoIdioma)
    );
END
GO

IF OBJECT_ID(N'dbo.EtiquetaTraduccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EtiquetaTraduccion
    (
        idEtiquetaTraduccion       INT IDENTITY(1,1) NOT NULL,
        claveEtiqueta              NVARCHAR(200) NOT NULL,
        textoPredeterminado        NVARCHAR(2000) NOT NULL,
        modulo                     NVARCHAR(100) NOT NULL,
        fechaAlta                  DATETIME NOT NULL CONSTRAINT DF_EtiquetaTraduccion_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion    DATETIME NULL,
        activo                     BIT NOT NULL CONSTRAINT DF_EtiquetaTraduccion_activo DEFAULT (1),
        CONSTRAINT PK_EtiquetaTraduccion PRIMARY KEY CLUSTERED (idEtiquetaTraduccion),
        CONSTRAINT UQ_EtiquetaTraduccion_clave UNIQUE (claveEtiqueta)
    );
END
GO

IF OBJECT_ID(N'dbo.Traduccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Traduccion
    (
        idTraduccion               INT IDENTITY(1,1) NOT NULL,
        idIdioma                   INT NOT NULL,
        idEtiquetaTraduccion       INT NOT NULL,
        textoTraducido             NVARCHAR(2000) NOT NULL,
        fechaAlta                  DATETIME NOT NULL CONSTRAINT DF_Traduccion_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion    DATETIME NULL,
        CONSTRAINT PK_Traduccion PRIMARY KEY CLUSTERED (idTraduccion),
        CONSTRAINT FK_Traduccion_Idioma FOREIGN KEY (idIdioma) REFERENCES dbo.Idioma (idIdioma),
        CONSTRAINT FK_Traduccion_Etiqueta FOREIGN KEY (idEtiquetaTraduccion) REFERENCES dbo.EtiquetaTraduccion (idEtiquetaTraduccion),
        CONSTRAINT UQ_Traduccion_IdiomaEtiqueta UNIQUE (idIdioma, idEtiquetaTraduccion)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE codigoIdioma = N'es-AR')
BEGIN
    INSERT INTO dbo.Idioma (codigoIdioma, nombreIdioma, esPredeterminado, estadoIdioma, activo)
    VALUES (N'es-AR', N'Español', 1, N'Activo', 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE activo = 1 AND esPredeterminado = 1)
BEGIN
    UPDATE dbo.Idioma
    SET esPredeterminado = CASE WHEN codigoIdioma = N'es-AR' THEN 1 ELSE 0 END,
        activo = CASE WHEN codigoIdioma = N'es-AR' THEN 1 ELSE activo END,
        estadoIdioma = CASE WHEN codigoIdioma = N'es-AR' THEN N'Activo' ELSE estadoIdioma END,
        fechaBaja = CASE WHEN codigoIdioma = N'es-AR' THEN NULL ELSE fechaBaja END,
        fechaUltimaModificacion = GETDATE();
END
GO

MERGE dbo.EtiquetaTraduccion AS destino
USING
(
    VALUES
        (N'General_Idioma', N'Idioma', N'General'),
        (N'General_Guardar', N'Guardar', N'General'),
        (N'General_Cancelar', N'Cancelar', N'General'),
        (N'General_Editar', N'Editar', N'General'),
        (N'General_Eliminar', N'Eliminar', N'General'),
        (N'General_Volver', N'Volver', N'General'),
        (N'General_Buscar', N'Buscar', N'General'),
        (N'General_SinResultados', N'No encontramos resultados.', N'General'),
        (N'Nav_Inicio', N'Inicio', N'Navegación'),
        (N'Nav_ExplorarEspacios', N'Explorar espacios', N'Navegación'),
        (N'Nav_MisReservas', N'Mis reservas', N'Navegación'),
        (N'Nav_MisEspacios', N'Mis espacios', N'Navegación'),
        (N'Nav_MiPerfil', N'Mi perfil', N'Navegación'),
        (N'Nav_IniciarSesion', N'Iniciar sesión', N'Navegación'),
        (N'Nav_CerrarSesion', N'Cerrar sesión', N'Navegación'),
        (N'Auth_Correo', N'Correo electrónico', N'Acceso'),
        (N'Auth_Contrasena', N'Contraseña', N'Acceso'),
        (N'Auth_Registrarse', N'Registrarse', N'Acceso'),
        (N'Espacio_PrecioHora', N'Precio por hora', N'Espacios'),
        (N'Espacio_Capacidad', N'Capacidad', N'Espacios'),
        (N'Espacio_Ubicacion', N'Ubicación', N'Espacios'),
        (N'Espacio_Disponibilidad', N'Disponibilidad', N'Espacios'),
        (N'Espacio_Equipamiento', N'Equipamiento', N'Espacios'),
        (N'Reserva_Solicitar', N'Solicitar reserva', N'Reservas'),
        (N'Reserva_Aceptar', N'Aceptar solicitud', N'Reservas'),
        (N'Reserva_Rechazar', N'Rechazar', N'Reservas'),
        (N'Reserva_Pendiente', N'Pendiente', N'Reservas'),
        (N'Reserva_Aceptada', N'Aceptada', N'Reservas'),
        (N'Reserva_Rechazada', N'Rechazada', N'Reservas'),
        (N'Perfil_DatosPersonales', N'Datos personales', N'Perfil'),
        (N'Perfil_CambiarContrasena', N'Cambiar contraseña', N'Perfil'),
        (N'Admin_Panel', N'Panel administrativo', N'Administración'),
        (N'Admin_GestionRoles', N'Gestión de roles y permisos', N'Administración'),
        (N'Admin_GestionIdiomas', N'Gestión de idiomas', N'Administración'),
        (N'Idiomas_Nuevo', N'Nuevo idioma', N'Idiomas'),
        (N'Idiomas_Nombre', N'Nombre del idioma', N'Idiomas'),
        (N'Idiomas_Codigo', N'Código de cultura', N'Idiomas'),
        (N'Idiomas_Predeterminado', N'Idioma predeterminado', N'Idiomas'),
        (N'Idiomas_Configurar', N'Configurar traducciones', N'Idiomas'),
        (N'Idiomas_DarBaja', N'Dar de baja', N'Idiomas'),
        (N'Idiomas_Traducciones', N'Traducciones', N'Idiomas'),
        (N'Idiomas_TextoBase', N'Texto en español', N'Idiomas'),
        (N'Idiomas_TextoTraducido', N'Traducción', N'Idiomas'),
        (N'Idiomas_Pendientes', N'Pendientes', N'Idiomas'),
        (N'Idiomas_Completadas', N'Completadas', N'Idiomas')
) AS origen (claveEtiqueta, textoPredeterminado, modulo)
ON destino.claveEtiqueta = origen.claveEtiqueta
WHEN MATCHED THEN
    UPDATE SET
        destino.textoPredeterminado = origen.textoPredeterminado,
        destino.modulo = origen.modulo,
        destino.activo = 1,
        destino.fechaUltimaModificacion = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (claveEtiqueta, textoPredeterminado, modulo, activo)
    VALUES (origen.claveEtiqueta, origen.textoPredeterminado, origen.modulo, 1);
GO

DECLARE @idEspanol INT = (SELECT TOP 1 idIdioma FROM dbo.Idioma WHERE codigoIdioma = N'es-AR');

INSERT INTO dbo.Traduccion (idIdioma, idEtiquetaTraduccion, textoTraducido)
SELECT @idEspanol, e.idEtiquetaTraduccion, e.textoPredeterminado
FROM dbo.EtiquetaTraduccion e
WHERE e.activo = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.Traduccion t
      WHERE t.idIdioma = @idEspanol
        AND t.idEtiquetaTraduccion = e.idEtiquetaTraduccion
  );
GO

IF OBJECT_ID(N'dbo.sp_Idioma_Listar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Idioma_Listar;
GO
CREATE PROCEDURE dbo.sp_Idioma_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.idIdioma,
        i.codigoIdioma,
        i.nombreIdioma,
        i.esPredeterminado,
        i.estadoIdioma,
        i.fechaAlta,
        i.fechaBaja,
        i.fechaUltimaModificacion,
        i.activo,
        (SELECT COUNT(*) FROM dbo.EtiquetaTraduccion e WHERE e.activo = 1) AS cantidadEtiquetas,
        (SELECT COUNT(*) FROM dbo.Traduccion t INNER JOIN dbo.EtiquetaTraduccion e ON e.idEtiquetaTraduccion = t.idEtiquetaTraduccion WHERE t.idIdioma = i.idIdioma AND e.activo = 1) AS cantidadTraducciones
    FROM dbo.Idioma i
    WHERE i.activo = 1
    ORDER BY i.esPredeterminado DESC, i.nombreIdioma;
END
GO

IF OBJECT_ID(N'dbo.sp_Idioma_ObtenerPorId', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Idioma_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Idioma_ObtenerPorId
    @idIdioma INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.idIdioma,
        i.codigoIdioma,
        i.nombreIdioma,
        i.esPredeterminado,
        i.estadoIdioma,
        i.fechaAlta,
        i.fechaBaja,
        i.fechaUltimaModificacion,
        i.activo,
        (SELECT COUNT(*) FROM dbo.EtiquetaTraduccion e WHERE e.activo = 1) AS cantidadEtiquetas,
        (SELECT COUNT(*) FROM dbo.Traduccion t INNER JOIN dbo.EtiquetaTraduccion e ON e.idEtiquetaTraduccion = t.idEtiquetaTraduccion WHERE t.idIdioma = i.idIdioma AND e.activo = 1) AS cantidadTraducciones
    FROM dbo.Idioma i
    WHERE i.idIdioma = @idIdioma;
END
GO

IF OBJECT_ID(N'dbo.sp_Idioma_Insertar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Idioma_Insertar;
GO
CREATE PROCEDURE dbo.sp_Idioma_Insertar
    @codigoIdioma       NVARCHAR(20),
    @nombreIdioma       NVARCHAR(100),
    @esPredeterminado   BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.Idioma WHERE codigoIdioma = @codigoIdioma)
        BEGIN
            THROW 51001, 'Ya existe un idioma con ese código.', 1;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE activo = 1 AND esPredeterminado = 1)
        BEGIN
            SET @esPredeterminado = 1;
        END

        IF @esPredeterminado = 1
        BEGIN
            UPDATE dbo.Idioma
            SET esPredeterminado = 0,
                fechaUltimaModificacion = GETDATE()
            WHERE activo = 1 AND esPredeterminado = 1;
        END

        INSERT INTO dbo.Idioma
            (codigoIdioma, nombreIdioma, esPredeterminado, estadoIdioma, activo)
        VALUES
            (@codigoIdioma, @nombreIdioma, @esPredeterminado, N'Activo', 1);

        DECLARE @idIdioma INT = CAST(SCOPE_IDENTITY() AS INT);

        COMMIT TRANSACTION;
        SELECT @idIdioma AS idIdioma;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.sp_Idioma_Modificar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Idioma_Modificar;
GO
CREATE PROCEDURE dbo.sp_Idioma_Modificar
    @idIdioma           INT,
    @codigoIdioma       NVARCHAR(20),
    @nombreIdioma       NVARCHAR(100),
    @esPredeterminado   BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE idIdioma = @idIdioma AND activo = 1)
        BEGIN
            THROW 51002, 'No se encontró el idioma seleccionado.', 1;
        END

        IF EXISTS (SELECT 1 FROM dbo.Idioma WHERE codigoIdioma = @codigoIdioma AND idIdioma <> @idIdioma)
        BEGIN
            THROW 51003, 'Ya existe otro idioma con ese código.', 1;
        END

        IF EXISTS (SELECT 1 FROM dbo.Idioma WHERE idIdioma = @idIdioma AND esPredeterminado = 1) AND @esPredeterminado = 0
        BEGIN
            THROW 51004, 'El idioma predeterminado no puede quedar sin reemplazo.', 1;
        END

        IF @esPredeterminado = 1
        BEGIN
            UPDATE dbo.Idioma
            SET esPredeterminado = 0,
                fechaUltimaModificacion = GETDATE()
            WHERE activo = 1 AND idIdioma <> @idIdioma AND esPredeterminado = 1;
        END

        UPDATE dbo.Idioma
        SET codigoIdioma = @codigoIdioma,
            nombreIdioma = @nombreIdioma,
            esPredeterminado = @esPredeterminado,
            fechaUltimaModificacion = GETDATE()
        WHERE idIdioma = @idIdioma AND activo = 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.sp_Idioma_DarDeBaja', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Idioma_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_Idioma_DarDeBaja
    @idIdioma INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Idioma WHERE idIdioma = @idIdioma AND activo = 1 AND esPredeterminado = 1)
    BEGIN
        THROW 51005, 'No se puede dar de baja el idioma predeterminado.', 1;
    END

    UPDATE dbo.Idioma
    SET activo = 0,
        estadoIdioma = N'Inactivo',
        fechaBaja = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idIdioma = @idIdioma AND activo = 1;
END
GO

IF OBJECT_ID(N'dbo.sp_Traduccion_ListarConfiguracion', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Traduccion_ListarConfiguracion;
GO
CREATE PROCEDURE dbo.sp_Traduccion_ListarConfiguracion
    @idIdioma INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.idTraduccion,
        e.idEtiquetaTraduccion,
        e.claveEtiqueta,
        e.textoPredeterminado,
        e.modulo,
        t.textoTraducido,
        t.fechaUltimaModificacion
    FROM dbo.EtiquetaTraduccion e
    LEFT JOIN dbo.Traduccion t
        ON t.idEtiquetaTraduccion = e.idEtiquetaTraduccion
       AND t.idIdioma = @idIdioma
    WHERE e.activo = 1
    ORDER BY e.modulo, e.claveEtiqueta;
END
GO

IF OBJECT_ID(N'dbo.sp_Traduccion_Guardar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Traduccion_Guardar;
GO
CREATE PROCEDURE dbo.sp_Traduccion_Guardar
    @idIdioma               INT,
    @idEtiquetaTraduccion   INT,
    @textoTraducido         NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Idioma WHERE idIdioma = @idIdioma AND activo = 1)
    BEGIN
        THROW 51006, 'No se encontró el idioma seleccionado.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.EtiquetaTraduccion WHERE idEtiquetaTraduccion = @idEtiquetaTraduccion AND activo = 1)
    BEGIN
        THROW 51007, 'No se encontró la etiqueta seleccionada.', 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.Traduccion WHERE idIdioma = @idIdioma AND idEtiquetaTraduccion = @idEtiquetaTraduccion)
    BEGIN
        UPDATE dbo.Traduccion
        SET textoTraducido = @textoTraducido,
            fechaUltimaModificacion = GETDATE()
        WHERE idIdioma = @idIdioma
          AND idEtiquetaTraduccion = @idEtiquetaTraduccion;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Traduccion (idIdioma, idEtiquetaTraduccion, textoTraducido)
        VALUES (@idIdioma, @idEtiquetaTraduccion, @textoTraducido);
    END
END
GO

IF OBJECT_ID(N'dbo.sp_Traduccion_Eliminar', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Traduccion_Eliminar;
GO
CREATE PROCEDURE dbo.sp_Traduccion_Eliminar
    @idIdioma               INT,
    @idEtiquetaTraduccion   INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Traduccion
    WHERE idIdioma = @idIdioma
      AND idEtiquetaTraduccion = @idEtiquetaTraduccion;
END
GO

IF OBJECT_ID(N'dbo.sp_Traduccion_ListarDiccionario', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_Traduccion_ListarDiccionario;
GO
CREATE PROCEDURE dbo.sp_Traduccion_ListarDiccionario
    @idIdioma INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.claveEtiqueta,
        COALESCE(NULLIF(t.textoTraducido, N''), e.textoPredeterminado) AS textoTraducido
    FROM dbo.EtiquetaTraduccion e
    LEFT JOIN dbo.Traduccion t
        ON t.idEtiquetaTraduccion = e.idEtiquetaTraduccion
       AND t.idIdioma = @idIdioma
    WHERE e.activo = 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PermisoInterno WHERE codigoPermiso = N'GESTIONAR_IDIOMAS')
BEGIN
    INSERT INTO dbo.PermisoInterno
        (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
    VALUES
        (N'GESTIONAR_IDIOMAS', N'Gestión de idiomas', N'Dar de alta, modificar y dar de baja idiomas, y configurar sus traducciones.', N'Administración', N'Ver', N'Activo', N'~/Interno/GestionIdiomas.aspx', 1);
END
ELSE
BEGIN
    UPDATE dbo.PermisoInterno
    SET nombrePermiso = N'Gestión de idiomas',
        descripcion = N'Dar de alta, modificar y dar de baja idiomas, y configurar sus traducciones.',
        modulo = N'Administración',
        accion = N'Ver',
        estadoPermiso = N'Activo',
        urlAsociada = N'~/Interno/GestionIdiomas.aspx',
        activo = 1,
        fechaUltimaModificacion = GETDATE()
    WHERE codigoPermiso = N'GESTIONAR_IDIOMAS';
END
GO

DECLARE @idRolAdministrador INT =
(
    SELECT TOP 1 idRolInterno
    FROM dbo.RolInterno
    WHERE nombreRol = N'Administrador' AND activo = 1
    ORDER BY idRolInterno
);

DECLARE @idPermisoIdiomas INT =
(
    SELECT idPermisoInterno
    FROM dbo.PermisoInterno
    WHERE codigoPermiso = N'GESTIONAR_IDIOMAS'
);

IF @idRolAdministrador IS NOT NULL AND @idPermisoIdiomas IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM dbo.RolInternoPermiso
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoIdiomas
    )
    BEGIN
        UPDATE dbo.RolInternoPermiso
        SET activo = 1,
            fechaAsignacion = GETDATE()
        WHERE idRolInterno = @idRolAdministrador
          AND idPermisoInterno = @idPermisoIdiomas;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.RolInternoPermiso
            (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
        VALUES
            (@idRolAdministrador, @idPermisoIdiomas, GETDATE(), 1);
    END
END
GO
