IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE master;
GO

IF OBJECT_ID('dbo.sp_StageUp_UltimaRestauracion', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_StageUp_UltimaRestauracion;
GO
CREATE PROCEDURE dbo.sp_StageUp_UltimaRestauracion
    @nombreBase SYSNAME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CONVERT(VARCHAR(20), ISNULL(MAX(rh.restore_history_id), 0)) AS marcaRestauracion
    FROM msdb.dbo.restorehistory rh
    WHERE rh.destination_database_name = @nombreBase;
END
GO

USE StageUp;
GO
