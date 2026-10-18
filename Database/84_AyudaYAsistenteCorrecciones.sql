IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

-- ===========================================================================
-- Preguntas frecuentes (CU-003-001 / CU-003-004).
--   * El asistente ya no usa las preguntas frecuentes: la columna
--     usarEnAsistente queda en la tabla (historial) pero no se lee ni se
--     escribe más.
--   * esBasica: el centro de ayuda muestra solo una selección breve de
--     preguntas básicas, sin filtros. Las categorías siguen organizando el
--     contenido internamente.
-- ===========================================================================
IF COL_LENGTH(N'dbo.Faq', N'esBasica') IS NULL
BEGIN
    ALTER TABLE dbo.Faq ADD esBasica BIT NOT NULL CONSTRAINT DF_Faq_esBasica DEFAULT (0);
END
GO

-- Selección inicial (una sola vez, solo si todavía no hay ninguna marcada):
-- la primera pregunta publicada de cada categoría activa, según su orden.
-- No se borra ni se modifica ninguna otra pregunta; después se ajusta desde
-- "Contenido de ayuda".
IF NOT EXISTS (SELECT 1 FROM dbo.Faq WHERE esBasica = 1)
BEGIN
    ;WITH Primeras AS
    (
        SELECT f.idFaq,
               ROW_NUMBER() OVER (PARTITION BY f.idCategoriaAyuda ORDER BY f.orden, f.idFaq) AS posicion
        FROM dbo.Faq f
        INNER JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = f.idCategoriaAyuda AND c.activa = 1
        WHERE f.estado = N'Activo'
    )
    UPDATE f SET f.esBasica = 1
    FROM dbo.Faq f
    INNER JOIN Primeras p ON p.idFaq = f.idFaq
    WHERE p.posicion = 1;
END
GO

IF OBJECT_ID('dbo.sp_CategoriaAyuda_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CategoriaAyuda_Listar;
GO
CREATE PROCEDURE dbo.sp_CategoriaAyuda_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idCategoriaAyuda, c.codigo, c.nombre, c.descripcion, c.orden, c.activa,
           (SELECT COUNT(*) FROM dbo.Faq f WHERE f.idCategoriaAyuda = c.idCategoriaAyuda AND f.estado = N'Activo') AS cantidadActivas,
           (SELECT COUNT(*) FROM dbo.Faq f WHERE f.idCategoriaAyuda = c.idCategoriaAyuda AND f.estado = N'Activo' AND f.esBasica = 1) AS cantidadBasicas
    FROM dbo.CategoriaAyuda c
    ORDER BY c.orden, c.nombre;
END
GO

IF OBJECT_ID('dbo.sp_Faq_ListarActivas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_ListarActivas;
GO
CREATE PROCEDURE dbo.sp_Faq_ListarActivas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idFaq, f.pregunta, f.respuesta, f.orden, f.activo, f.estado, f.idCategoriaAyuda, f.esBasica, f.palabrasClave,
           f.fechaAlta, f.fechaUltimaModificacion, c.nombre AS nombreCategoria, c.orden AS ordenCategoria
    FROM dbo.Faq f
    INNER JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = f.idCategoriaAyuda AND c.activa = 1
    WHERE f.estado = N'Activo'
    ORDER BY c.orden, f.orden, f.idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Listar;
GO
CREATE PROCEDURE dbo.sp_Faq_Listar
    @estado            NVARCHAR(10) = NULL,
    @idCategoriaAyuda  INT = NULL,
    @texto             NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @like NVARCHAR(110) = CASE WHEN NULLIF(LTRIM(RTRIM(@texto)), N'') IS NULL THEN NULL ELSE N'%' + LTRIM(RTRIM(@texto)) + N'%' END;

    SELECT f.idFaq, f.pregunta, f.respuesta, f.orden, f.activo, f.estado, f.idCategoriaAyuda, f.esBasica, f.palabrasClave,
           f.fechaAlta, f.fechaUltimaModificacion, c.nombre AS nombreCategoria, c.orden AS ordenCategoria
    FROM dbo.Faq f
    LEFT JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = f.idCategoriaAyuda
    WHERE (@estado IS NULL OR f.estado = @estado)
      AND (@idCategoriaAyuda IS NULL OR f.idCategoriaAyuda = @idCategoriaAyuda)
      AND (@like IS NULL OR f.pregunta LIKE @like OR f.respuesta LIKE @like OR f.palabrasClave LIKE @like)
    ORDER BY CASE f.estado WHEN N'Activo' THEN 0 WHEN N'Borrador' THEN 1 ELSE 2 END, c.orden, f.orden, f.idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_ObtenerPorId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_ObtenerPorId;
GO
CREATE PROCEDURE dbo.sp_Faq_ObtenerPorId
    @idFaq INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idFaq, f.pregunta, f.respuesta, f.orden, f.activo, f.estado, f.idCategoriaAyuda, f.esBasica, f.palabrasClave,
           f.fechaAlta, f.fechaUltimaModificacion, c.nombre AS nombreCategoria, c.orden AS ordenCategoria, c.activa AS categoriaActiva
    FROM dbo.Faq f
    LEFT JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = f.idCategoriaAyuda
    WHERE f.idFaq = @idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Insertar;
GO
CREATE PROCEDURE dbo.sp_Faq_Insertar
    @pregunta          NVARCHAR(300),
    @respuesta         NVARCHAR(MAX),
    @orden             INT,
    @estado            NVARCHAR(10),
    @idCategoriaAyuda  INT = NULL,
    @esBasica          BIT = 0,
    @palabrasClave     NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Faq (pregunta, respuesta, orden, activo, estado, idCategoriaAyuda, esBasica, palabrasClave, fechaAlta)
    VALUES (@pregunta, @respuesta, @orden, CASE WHEN @estado = N'Activo' THEN 1 ELSE 0 END, @estado, @idCategoriaAyuda,
            @esBasica, @palabrasClave, GETDATE());
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idFaq;
END
GO

IF OBJECT_ID('dbo.sp_Faq_Modificar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Modificar;
GO
CREATE PROCEDURE dbo.sp_Faq_Modificar
    @idFaq             INT,
    @pregunta          NVARCHAR(300),
    @respuesta         NVARCHAR(MAX),
    @orden             INT,
    @estado            NVARCHAR(10),
    @idCategoriaAyuda  INT = NULL,
    @esBasica          BIT = 0,
    @palabrasClave     NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Faq
    SET pregunta = @pregunta, respuesta = @respuesta, orden = @orden, estado = @estado,
        activo = CASE WHEN @estado = N'Activo' THEN 1 ELSE 0 END,
        idCategoriaAyuda = @idCategoriaAyuda, esBasica = @esBasica, palabrasClave = @palabrasClave,
        fechaUltimaModificacion = GETDATE()
    WHERE idFaq = @idFaq;
    SELECT @@ROWCOUNT AS filas;
END
GO

-- ===========================================================================
-- Asistente: pagos y cancelaciones (CU-003-001). Actualiza las bases que ya
-- tienen el árbol del script 70.
--   * Distingue la reserva aceptada sin pagar (se cancela sin cargo) de la
--     pagada (se aplica la política parametrizada).
--   * El pago vence al cumplirse el plazo configurado desde la aceptación o
--     al comenzar la reserva, lo que ocurra primero; si vence, la reserva se
--     cancela sola y sin cargo.
-- ===========================================================================
IF OBJECT_ID(N'dbo.NodoAsistente', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.NodoAsistente WHERE codigo = N'reservas.cancelar')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @idCancelar INT = (SELECT idNodoAsistente FROM dbo.NodoAsistente WHERE codigo = N'reservas.cancelar');

    UPDATE dbo.NodoAsistente
    SET mensaje = N'¿En qué estado está tu reserva? Lo ves en "Mis reservas".',
        palabrasClave = N'cancelar cancelo cancelación anular dar de baja reserva'
    WHERE codigo = N'reservas.cancelar';

    UPDATE dbo.NodoAsistente
    SET textoOpcion = N'Pendiente: el gestor todavía no respondió', orden = 10,
        mensaje = N'Si el gestor todavía no la aceptó, podés cancelarla desde "Mis reservas" sin ningún cargo.'
    WHERE codigo = N'reservas.cancelar.pendiente';

    IF NOT EXISTS (SELECT 1 FROM dbo.NodoAsistente WHERE codigo = N'reservas.cancelar.sinpagar')
    BEGIN
        INSERT INTO dbo.NodoAsistente (idNodoPadre, codigo, tipo, textoOpcion, mensaje, palabrasClave, orden, activo)
        VALUES (@idCancelar, N'reservas.cancelar.sinpagar', N'Respuesta', N'Aceptada, pero todavía no la pagué',
                N'Una reserva aceptada que todavía no pagaste se cancela sin cargo desde "Mis reservas": no se te cobró nada. ' +
                N'Si no la pagás, se cancela sola y sin cargo cuando vence el plazo para pagar: {HorasLimitePago} horas desde que el gestor la aceptó, ' +
                N'o el comienzo de la reserva si eso ocurre antes.',
                N'aceptada sin pagar impaga no pague pagar plazo vence vencida', 20, 1);
    END
    ELSE
    BEGIN
        UPDATE dbo.NodoAsistente
        SET idNodoPadre = @idCancelar, orden = 20, activo = 1
        WHERE codigo = N'reservas.cancelar.sinpagar';
    END

    UPDATE dbo.NodoAsistente
    SET textoOpcion = N'Aceptada y ya la pagué', orden = 30,
        mensaje = N'Como ya la pagaste, se aplica la política de cancelación vigente según la anticipación. Lo que pagaste vuelve como saldo a favor en "Mi cuenta corriente", ' +
                  N'descontando el cargo que corresponda. Antes de confirmar, la plataforma te muestra si hay cargo y de cuánto.',
        palabrasClave = N'pagada pague cancelar cargo devolucion reintegro'
    WHERE codigo = N'reservas.cancelar.aceptada';

    UPDATE dbo.NodoAsistente
    SET orden = 40,
        mensaje = N'En "Mis reservas" cada reserva muestra su estado. "Pendiente": se cancela sin cargo. "Aceptada" con el botón "Pagar": todavía no la pagaste, ' +
                  N'así que también se cancela sin cargo. "Aceptada" y pagada: se aplica la política de cancelación, y la plataforma te muestra el cargo antes de confirmar.'
    WHERE codigo = N'reservas.cancelar.nosabe';

    UPDATE dbo.NodoAsistente
    SET mensaje = N'En "Mis reservas" ves cada reserva con su estado: Pendiente (el gestor todavía no respondió), Aceptada (falta pagarla o ya está pagada), ' +
                  N'Rechazada, Cancelada o Finalizada. Una reserva aceptada hay que pagarla dentro de las {HorasLimitePago} horas o antes de que empiece, ' +
                  N'lo que ocurra primero; si no, se cancela sola y sin cargo. Cada cambio también te llega como notificación.'
    WHERE codigo = N'reservas.estado';

    UPDATE dbo.NodoAsistente
    SET mensaje = N'{PoliticaCancelacion} Lo que pagaste vuelve como saldo a favor en "Mi cuenta corriente", descontando el cargo que corresponda.'
    WHERE codigo = N'reservas.politica';

    UPDATE dbo.NodoAsistente
    SET mensaje = N'Cuando el gestor acepta tu reserva, tenés que pagarla antes de que venza el plazo: {HorasLimitePago} horas desde la aceptación, ' +
                  N'o el comienzo de la reserva si eso ocurre antes. Si vence sin pago, la reserva se cancela sola y sin cargo. ' +
                  N'Entrá a "Mis reservas" y elegí "Pagar": podés usar una tarjeta, una tarjeta guardada en "Mi billetera" o tu saldo a favor.'
    WHERE codigo = N'pagos.pagar';

    -- Acciones y presentación del paso nuevo.
    DECLARE @idSinPagar INT = (SELECT idNodoAsistente FROM dbo.NodoAsistente WHERE codigo = N'reservas.cancelar.sinpagar');
    IF NOT EXISTS (SELECT 1 FROM dbo.AccionNodoAsistente WHERE idNodoAsistente = @idSinPagar)
    BEGIN
        INSERT INTO dbo.AccionNodoAsistente (idNodoAsistente, texto, tipoAccion, destino, orden)
        VALUES (@idSinPagar, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx', 10),
               (@idSinPagar, N'Quiero pagarla', N'Nodo', N'pagos.pagar', 20);
    END

    IF COL_LENGTH(N'dbo.NodoAsistente', N'descripcionOpcion') IS NOT NULL
    BEGIN
        EXEC (N'UPDATE dbo.NodoAsistente SET descripcionOpcion = N''Se cancela sin cargo'' WHERE codigo IN (N''reservas.cancelar.pendiente'', N''reservas.cancelar.sinpagar'');
                UPDATE dbo.NodoAsistente SET descripcionOpcion = N''Se aplica la política de cancelación'' WHERE codigo = N''reservas.cancelar.aceptada'';
                UPDATE dbo.NodoAsistente SET descripcionOpcion = N''Pendiente, sin pagar o pagada'' WHERE codigo = N''reservas.cancelar'';');
    END

    COMMIT TRANSACTION;
END
GO
