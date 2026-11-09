USE [StageUp];
GO
CREATE OR ALTER PROCEDURE dbo.sp_EnvioNewsletter_MarcarEnviando
    @idEnvioNewsletter INT,
    @idSuscriptorNewsletter INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @actualizados TABLE (estado NVARCHAR(20));

    UPDATE d
    SET estado = CASE WHEN s.estado = N'Activo' THEN N'Enviando' ELSE N'Omitido' END,
        fechaUltimoIntento = CASE WHEN s.estado = N'Activo' THEN GETDATE() ELSE d.fechaUltimoIntento END
    OUTPUT inserted.estado INTO @actualizados (estado)
    FROM dbo.EnvioNewsletterDestinatario d
    INNER JOIN dbo.SuscriptorNewsletter s ON s.idSuscriptorNewsletter = d.idSuscriptorNewsletter
    WHERE d.idEnvioNewsletter = @idEnvioNewsletter
      AND d.idSuscriptorNewsletter = @idSuscriptorNewsletter
      AND d.estado IN (N'Pendiente', N'Fallido');

    SELECT COUNT(*) FROM @actualizados WHERE estado = N'Enviando';
END
GO
