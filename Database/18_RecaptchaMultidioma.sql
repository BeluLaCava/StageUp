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
        (N'Captcha_Titulo', N'Verificación de seguridad', N'Acceso'),
        (N'Captcha_Ayuda', N'Marcá la casilla para continuar.', N'Acceso'),
        (N'Captcha_ErrorVacio', N'Confirmá que no sos un robot.', N'Mensajes de negocio'),
        (N'Captcha_ErrorInvalido', N'La verificación de seguridad no fue válida. Intentá nuevamente.', N'Mensajes de negocio'),
        (N'Captcha_ErrorConfiguracion', N'El CAPTCHA no está configurado. Contactá al administrador.', N'Mensajes de negocio'),
        (N'Captcha_ErrorServicio', N'No pudimos validar el CAPTCHA en este momento. Probá nuevamente.', N'Mensajes de negocio')
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
            WHERE etiqueta.claveEtiqueta IN
            (
                N'Captcha_Titulo', N'Captcha_Ayuda', N'Captcha_ErrorVacio',
                N'Captcha_ErrorInvalido', N'Captcha_ErrorConfiguracion', N'Captcha_ErrorServicio'
            )
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
                (N'Captcha_Titulo', N'Security verification'),
                (N'Captcha_Ayuda', N'Check the box to continue.'),
                (N'Captcha_ErrorVacio', N'Confirm that you are not a robot.'),
                (N'Captcha_ErrorInvalido', N'The security verification was not valid. Please try again.'),
                (N'Captcha_ErrorConfiguracion', N'The CAPTCHA is not configured. Contact the administrator.'),
                (N'Captcha_ErrorServicio', N'We could not validate the CAPTCHA right now. Please try again.')
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
WHERE e.claveEtiqueta LIKE N'Captcha_%'
ORDER BY i.idIdioma, e.claveEtiqueta;
GO
