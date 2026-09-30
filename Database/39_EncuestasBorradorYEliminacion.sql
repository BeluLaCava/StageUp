IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Encuesta', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero debe ejecutarse Database/36_EncuestasYGraficoResultados.sql.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_ListarConResultadosParaUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario;
GO
CREATE PROCEDURE dbo.sp_Encuesta_ListarConResultadosParaUsuario
    @idUsuarioExterno   INT,
    @perfilUsuario      NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    -- Encuestas ya empezadas y dirigidas al perfil del usuario, que además
    -- están vencidas o cerradas, o que el usuario ya respondió (para que vea
    -- el gráfico al instante apenas responde, aunque la encuesta siga
    -- vigente y abierta para otros usuarios).
    SELECT e.idEncuesta, e.titulo, e.descripcion, e.fechaInicio, e.fechaVencimiento, e.publicoObjetivo, e.estado, e.fechaAlta, e.fechaUltimaModificacion
    FROM dbo.Encuesta e
    WHERE e.estado IN (N'Activa', N'Cerrada') -- un Borrador nunca se publicó: no tiene resultados, aunque esté vencido
      AND e.fechaInicio <= GETDATE()
      AND (e.publicoObjetivo = N'Todos' OR e.publicoObjetivo = @perfilUsuario)
      AND
      (
          e.estado = N'Cerrada'
          OR e.fechaVencimiento < GETDATE()
          OR EXISTS
          (
              SELECT 1 FROM dbo.RespuestaEncuesta r
              WHERE r.idEncuesta = e.idEncuesta AND r.idUsuarioExterno = @idUsuarioExterno
          )
      )
    ORDER BY e.fechaVencimiento DESC;
END
GO

IF OBJECT_ID('dbo.sp_Encuesta_EliminarBorrador', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Encuesta_EliminarBorrador;
GO
CREATE PROCEDURE dbo.sp_Encuesta_EliminarBorrador
    @idEncuesta INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Eliminación controlada: solo borra si la encuesta sigue en Borrador y
    -- no tiene ninguna respuesta. Las preguntas y opciones caen por
    -- ON DELETE CASCADE. Una encuesta publicada nunca se borra (se cierra).
    DELETE FROM dbo.Encuesta
    WHERE idEncuesta = @idEncuesta
      AND estado = N'Borrador'
      AND NOT EXISTS (SELECT 1 FROM dbo.RespuestaEncuesta r WHERE r.idEncuesta = @idEncuesta);

    SELECT @@ROWCOUNT AS filasEliminadas;
END
GO
