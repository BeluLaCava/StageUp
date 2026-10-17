IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF COL_LENGTH(N'dbo.ConsultaAsistente', N'resultadoApi') IS NULL
    ALTER TABLE dbo.ConsultaAsistente ADD resultadoApi NVARCHAR(20) NULL;
GO

IF OBJECT_ID('dbo.sp_ConsultaAsistente_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsultaAsistente_Registrar;
GO
CREATE PROCEDURE dbo.sp_ConsultaAsistente_Registrar
    @texto             NVARCHAR(200),
    @textoNormalizado  NVARCHAR(200),
    @idFaqRespuesta    INT = NULL,
    @idNodoAsistente   INT = NULL,
    @idUsuarioExterno  INT = NULL,
    @resultadoApi      NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ConsultaAsistente (texto, textoNormalizado, idFaqRespuesta, idNodoAsistente, reconocida, idUsuarioExterno, resultadoApi)
    VALUES (@texto, @textoNormalizado, @idFaqRespuesta, @idNodoAsistente,
            CASE WHEN @idFaqRespuesta IS NULL AND @idNodoAsistente IS NULL THEN 0 ELSE 1 END, @idUsuarioExterno, @resultadoApi);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idConsultaAsistente;
END
GO
