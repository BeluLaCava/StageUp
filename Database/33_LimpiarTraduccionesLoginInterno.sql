-- =============================================================================
-- 33_LimpiarTraduccionesLoginInterno.sql
-- Limpieza para bases VIEJAS solamente.
--
-- La pantalla Interno/IniciarSesionInterno.aspx fue eliminada del proyecto (login
-- unico en IniciarSesion.aspx) y el script 17_MultidiomaCompleto.sql ya NO inserta
-- sus traducciones. Este script solo borra las etiquetas que hayan quedado en una
-- base creada antes de ese cambio. En una base nueva no encuentra nada que borrar
-- (los DELETE afectan 0 filas) y es seguro volver a ejecutarlo.
-- =============================================================================

IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.EtiquetaTraduccion', N'U') IS NULL OR OBJECT_ID(N'dbo.Traduccion', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/11_Multidioma.sql.', 1;
END
GO

DELETE t
FROM dbo.Traduccion t
INNER JOIN dbo.EtiquetaTraduccion e ON e.idEtiquetaTraduccion = t.idEtiquetaTraduccion
WHERE e.modulo = N'Interno/IniciarSesionInterno';

DELETE FROM dbo.EtiquetaTraduccion
WHERE modulo = N'Interno/IniciarSesionInterno';
