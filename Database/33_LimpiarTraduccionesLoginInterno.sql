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
