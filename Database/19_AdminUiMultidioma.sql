USE StageUp;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    IF OBJECT_ID(N'dbo.Idioma', N'U') IS NULL
       OR OBJECT_ID(N'dbo.EtiquetaTraduccion', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Traduccion', N'U') IS NULL
    BEGIN
        RAISERROR(N'Primero debe ejecutarse Database/11_Multidioma.sql.', 16, 1);
    END

    BEGIN TRANSACTION;

    ;MERGE dbo.EtiquetaTraduccion AS destino
    USING
    (
        VALUES
        (N'AdminUI_HerramientasDisponibles', N'Herramientas disponibles', N'Administración'),
        (N'AdminUI_QueGestionar', N'¿Qué querés gestionar hoy?', N'Administración'),
        (N'AdminUI_AccesosSegunRol', N'Los accesos dependen de los permisos de tu rol.', N'Administración'),
        (N'AdminUI_Configuracion', N'Configuración', N'Administración'),
        (N'AdminUI_Directorio', N'Directorio', N'Administración'),
        (N'AdminUI_AccesosDelRol', N'Accesos del rol', N'Administración'),
        (N'AdminUI_ActivarDesactivar', N'Podés activar o desactivar cada permiso.', N'Administración'),
        (N'AdminUI_Revision', N'Revisión', N'Administración'),
        (N'AdminUI_RevisarCuenta', N'Revisá cada cuenta antes de tomar una decisión.', N'Administración'),
        (N'AdminUI_PendienteRevision', N'Pendiente de revisión', N'Administración'),
        (N'AdminUI_BusquedaAvanzada', N'Búsqueda avanzada', N'Administración'),
        (N'AdminUI_Historial', N'Historial', N'Administración'),
        (N'AdminUI_ActividadReciente', N'Actividad ordenada desde la más reciente.', N'Administración')
    ) AS origen(claveEtiqueta, textoPredeterminado, modulo)
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

    DECLARE @idEspanol INT =
    (
        SELECT TOP 1 idIdioma
        FROM dbo.Idioma
        WHERE codigoIdioma = N'es-AR'
           OR codigoIdioma = N'es'
           OR codigoIdioma LIKE N'es-%'
           OR nombreIdioma IN (N'Español', N'Espanol', N'Spanish')
        ORDER BY CASE
                     WHEN codigoIdioma = N'es-AR' THEN 0
                     WHEN codigoIdioma = N'es' THEN 1
                     WHEN codigoIdioma LIKE N'es-%' THEN 2
                     ELSE 3
                 END,
                 idIdioma
    );

    IF @idEspanol IS NOT NULL
    BEGIN
        ;MERGE dbo.Traduccion AS destino
        USING
        (
            SELECT
                @idEspanol AS idIdioma,
                etiqueta.idEtiquetaTraduccion,
                etiqueta.textoPredeterminado AS textoTraducido
            FROM dbo.EtiquetaTraduccion etiqueta
            WHERE etiqueta.claveEtiqueta LIKE N'AdminUI[_]%'
        ) AS origen
            ON destino.idIdioma = origen.idIdioma
           AND destino.idEtiquetaTraduccion = origen.idEtiquetaTraduccion
        WHEN MATCHED THEN
            UPDATE SET
                destino.textoTraducido = origen.textoTraducido,
                destino.fechaUltimaModificacion = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (idIdioma, idEtiquetaTraduccion, textoTraducido)
            VALUES (origen.idIdioma, origen.idEtiquetaTraduccion, origen.textoTraducido);
    END

    DECLARE @idIngles INT =
    (
        SELECT TOP 1 idIdioma
        FROM dbo.Idioma
        WHERE codigoIdioma = N'en-US'
           OR codigoIdioma = N'en'
           OR codigoIdioma LIKE N'en-%'
           OR codigoIdioma = N'us'
           OR nombreIdioma IN (N'Inglés', N'Ingles', N'English')
        ORDER BY CASE
                     WHEN codigoIdioma = N'en-US' THEN 0
                     WHEN codigoIdioma = N'en' THEN 1
                     WHEN codigoIdioma LIKE N'en-%' THEN 2
                     WHEN codigoIdioma = N'us' THEN 3
                     ELSE 4
                 END,
                 idIdioma
    );

    IF @idIngles IS NOT NULL
    BEGIN
        ;MERGE dbo.Traduccion AS destino
        USING
        (
            SELECT
                @idIngles AS idIdioma,
                etiqueta.idEtiquetaTraduccion,
                traducciones.textoTraducido
            FROM
            (
                VALUES
                (N'AdminUI_HerramientasDisponibles', N'Available tools'),
                (N'AdminUI_QueGestionar', N'What would you like to manage today?'),
                (N'AdminUI_AccesosSegunRol', N'Available sections depend on your role permissions.'),
                (N'AdminUI_Configuracion', N'Configuration'),
                (N'AdminUI_Directorio', N'Directory'),
                (N'AdminUI_AccesosDelRol', N'Role access'),
                (N'AdminUI_ActivarDesactivar', N'You can enable or disable each permission.'),
                (N'AdminUI_Revision', N'Review'),
                (N'AdminUI_RevisarCuenta', N'Review each account before making a decision.'),
                (N'AdminUI_PendienteRevision', N'Pending review'),
                (N'AdminUI_BusquedaAvanzada', N'Advanced search'),
                (N'AdminUI_Historial', N'History'),
                (N'AdminUI_ActividadReciente', N'Activity sorted from newest to oldest.')
            ) AS traducciones(claveEtiqueta, textoTraducido)
            INNER JOIN dbo.EtiquetaTraduccion etiqueta
                ON etiqueta.claveEtiqueta = traducciones.claveEtiqueta
        ) AS origen
            ON destino.idIdioma = origen.idIdioma
           AND destino.idEtiquetaTraduccion = origen.idEtiquetaTraduccion
        WHEN MATCHED THEN
            UPDATE SET
                destino.textoTraducido = origen.textoTraducido,
                destino.fechaUltimaModificacion = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (idIdioma, idEtiquetaTraduccion, textoTraducido)
            VALUES (origen.idIdioma, origen.idEtiquetaTraduccion, origen.textoTraducido);
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    DECLARE @mensajeError NVARCHAR(2048) = ERROR_MESSAGE();
    RAISERROR(@mensajeError, 16, 1);
END CATCH;
GO

SELECT
    i.codigoIdioma,
    i.nombreIdioma,
    e.claveEtiqueta,
    t.textoTraducido
FROM dbo.Idioma i
CROSS JOIN dbo.EtiquetaTraduccion e
LEFT JOIN dbo.Traduccion t
    ON t.idIdioma = i.idIdioma
   AND t.idEtiquetaTraduccion = e.idEtiquetaTraduccion
WHERE e.claveEtiqueta LIKE N'AdminUI[_]%'
ORDER BY i.idIdioma, e.claveEtiqueta;
GO
