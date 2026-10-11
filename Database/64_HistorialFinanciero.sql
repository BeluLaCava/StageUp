IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.TransaccionEconomica', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/62_PagosReservasControl.sql y Database/63_TransaccionesEconomicas.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-002-003: historial financiero. Una fila por operación económica, venga
-- de donde venga:
--   TX  transacción económica (reserva abonada o cargo por cancelación)
--   PG  pago de una reserva que no se confirmó (pendiente, fallido, en
--       revisión o anulado) y pagos de deuda de la cuenta corriente
--   RS  reserva aceptada que todavía espera el pago o cuyo plazo venció
--   LQ  liquidación transferida a un gestor
--   CP  nota de crédito o de débito manual
-- La consulta no modifica nada: es una vista sobre las tablas existentes.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.vw_HistorialFinanciero', N'V') IS NOT NULL DROP VIEW dbo.vw_HistorialFinanciero;
GO
CREATE VIEW dbo.vw_HistorialFinanciero
AS
SELECT
    CAST(N'TX' AS NVARCHAR(2))            AS origen,
    t.idTransaccion                        AS idOrigen,
    CAST(t.codigo AS NVARCHAR(20))         AS codigo,
    CAST(t.tipoOperacion AS NVARCHAR(30))  AS tipoOperacion,
    t.idReserva,
    t.idPago,
    CASE WHEN t.tipoOperacion = N'ReservaAbonada' THEN fc.idComprobante ELSE t.idComprobante END AS idComprobante,
    t.idUsuarioSolicitante,
    t.idUsuarioGestor,
    t.idEspacioArtistico,
    t.moneda,
    t.montoTotal,
    t.porcentajeComision,
    t.importeComision,
    t.importeGestor,
    t.porcentajeCargo,
    CASE WHEN t.tipoOperacion = N'CargoCancelacion' THEN t.montoTotal END AS importeCargo,
    CAST(t.estadoPago AS NVARCHAR(20))     AS estadoPago,
    t.referenciaPago,
    t.estadoRegistro,
    t.fechaGeneracion                      AS fechaOperacion,
    t.observacion
FROM dbo.TransaccionEconomica t
OUTER APPLY (SELECT TOP 1 c.idComprobante FROM dbo.Comprobante c
             WHERE c.idPago = t.idPago AND c.tipo = N'FC' ORDER BY c.idComprobante) fc

UNION ALL

SELECT
    N'PG',
    p.idPago,
    N'PG-' + RIGHT(N'00000000' + CAST(p.idPago AS NVARCHAR(10)), 8),
    CASE WHEN p.concepto = N'Deuda' THEN N'PagoDeuda' ELSE N'PagoNoConfirmado' END,
    p.idReserva,
    p.idPago,
    fc.idComprobante,
    ISNULL(r.idUsuarioExternoSolicitante, p.idUsuarioExterno),
    e.idUsuarioGestor,
    e.idEspacioArtistico,
    p.moneda,
    p.importeTotal,
    NULL, NULL, NULL, NULL, NULL,
    CASE p.estado
        WHEN N'Aprobado'   THEN N'Pagado'
        WHEN N'Pendiente'  THEN N'Pendiente'
        WHEN N'Rechazado'  THEN N'Fallido'
        WHEN N'Anulado'    THEN N'Anulado'
        ELSE N'EnRevision'
    END,
    p.identificadorExterno,
    N'Registrada',
    ISNULL(p.fechaOperacion, p.fechaPago),
    COALESCE(p.motivoRechazo, p.observacion)
FROM dbo.Pago p
LEFT JOIN dbo.Reserva r ON r.idReserva = p.idReserva
LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
OUTER APPLY (SELECT TOP 1 c.idComprobante FROM dbo.Comprobante c
             WHERE c.idPago = p.idPago AND c.tipo = N'FC' ORDER BY c.idComprobante) fc
WHERE p.concepto = N'Deuda'
   OR (p.concepto = N'Reserva' AND p.estado <> N'Aprobado')

UNION ALL

SELECT
    N'RS',
    r.idReserva,
    N'RS-' + RIGHT(N'00000000' + CAST(r.idReserva AS NVARCHAR(10)), 8),
    N'ReservaSinPago',
    r.idReserva,
    NULL,
    NULL,
    r.idUsuarioExternoSolicitante,
    e.idUsuarioGestor,
    e.idEspacioArtistico,
    ISNULL(r.moneda, N'ARS'),
    ISNULL(r.importeEstimado, 0),
    NULL, NULL, NULL, NULL, NULL,
    CASE WHEN r.estadoPago = N'Vencido' THEN N'Vencido' ELSE N'Pendiente' END,
    NULL,
    N'Registrada',
    ISNULL(r.fechaResolucion, r.fechaCreacion),
    CASE WHEN r.estadoPago = N'Vencido'
         THEN N'Venció el plazo para pagar: la reserva se canceló sin cargo.'
         ELSE N'La reserva fue aceptada y espera el pago del solicitante.' END
FROM dbo.Reserva r
INNER JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
WHERE r.estadoPago IN (N'Pendiente', N'Vencido')
  AND NOT EXISTS (SELECT 1 FROM dbo.Pago p
                  WHERE p.idReserva = r.idReserva AND p.estado IN (N'Aprobado', N'Pendiente', N'Observado'))

UNION ALL

SELECT
    N'LQ',
    m.idMovimiento,
    N'LQ-' + RIGHT(N'00000000' + CAST(m.idMovimiento AS NVARCHAR(10)), 8),
    N'Liquidacion',
    NULL,
    NULL,
    NULL,
    NULL,
    m.idUsuarioExterno,
    NULL,
    m.moneda,
    -m.importe,
    NULL, NULL,
    -m.importe,
    NULL, NULL,
    N'Liquidado',
    NULL,
    N'Registrada',
    m.fechaMovimiento,
    m.descripcion
FROM dbo.MovimientoCuentaCorriente m
WHERE m.tipoMovimiento = N'Liquidacion' AND m.rolCuenta = N'Gestor'

UNION ALL

SELECT
    N'CP',
    c.idComprobante,
    CAST(c.numero AS NVARCHAR(20)),
    CASE WHEN c.tipo = N'NC' THEN N'NotaCreditoManual' ELSE N'NotaDebitoManual' END,
    c.idReserva,
    NULL,
    c.idComprobante,
    CASE WHEN c.rolCuenta = N'Cliente' THEN c.idUsuarioExterno END,
    CASE WHEN c.rolCuenta = N'Gestor' THEN c.idUsuarioExterno ELSE e.idUsuarioGestor END,
    e.idEspacioArtistico,
    c.moneda,
    c.importe,
    NULL, NULL, NULL, NULL, NULL,
    CASE WHEN c.estado = N'Anulado' THEN N'Anulado' ELSE N'Aplicado' END,
    NULL,
    N'Registrada',
    c.fechaEmision,
    c.motivo
FROM dbo.Comprobante c
LEFT JOIN dbo.Reserva r ON r.idReserva = c.idReserva
LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = r.idEspacioArtistico
WHERE c.origen = N'AjusteManual' AND c.tipo IN (N'NC', N'ND');
GO

-- ---------------------------------------------------------------------------
-- Pasos 4 a 9 y A4: listado general o filtrado por período, estado de pago,
-- tipo de operación, gestor del espacio y texto (usuario, espacio, código o
-- número de reserva). Sin filtros devuelve el listado general (A3).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_HistorialFinanciero_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HistorialFinanciero_Listar;
GO
CREATE PROCEDURE dbo.sp_HistorialFinanciero_Listar
    @tipoOperacion   NVARCHAR(30)  = NULL,
    @estadoPago      NVARCHAR(20)  = NULL,
    @idUsuarioGestor INT           = NULL,
    @desde           DATE          = NULL,
    @hasta           DATE          = NULL,
    @texto           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @textoLimpio NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@texto)), N'');
    DECLARE @like NVARCHAR(110) = CASE WHEN @textoLimpio IS NULL THEN NULL ELSE N'%' + @textoLimpio + N'%' END;

    SELECT TOP 500
        h.origen, h.idOrigen, h.codigo, h.tipoOperacion, h.idReserva, h.idPago, h.idComprobante,
        h.idUsuarioSolicitante, h.idUsuarioGestor, h.idEspacioArtistico, h.moneda, h.montoTotal,
        h.porcentajeComision, h.importeComision, h.importeGestor, h.porcentajeCargo, h.importeCargo,
        h.estadoPago, h.referenciaPago, h.estadoRegistro, h.fechaOperacion, h.observacion,
        LTRIM(RTRIM(ISNULL(s.nombre, N'') + N' ' + ISNULL(s.apellido, N''))) AS nombreSolicitante,
        LTRIM(RTRIM(ISNULL(g.nombre, N'') + N' ' + ISNULL(g.apellido, N''))) AS nombreGestor,
        e.nombreEspacio,
        c.numero AS numeroComprobante, c.tipo AS tipoComprobante
    FROM dbo.vw_HistorialFinanciero h
    LEFT JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = h.idUsuarioSolicitante
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = h.idUsuarioGestor
    LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = h.idEspacioArtistico
    LEFT JOIN dbo.Comprobante c ON c.idComprobante = h.idComprobante
    WHERE (@tipoOperacion IS NULL OR h.tipoOperacion = @tipoOperacion)
      AND (@estadoPago IS NULL OR h.estadoPago = @estadoPago)
      AND (@idUsuarioGestor IS NULL OR h.idUsuarioGestor = @idUsuarioGestor)
      AND (@desde IS NULL OR h.fechaOperacion >= @desde)
      AND (@hasta IS NULL OR h.fechaOperacion < DATEADD(DAY, 1, CAST(@hasta AS DATETIME)))
      AND (@like IS NULL
           OR s.nombre + N' ' + s.apellido LIKE @like OR s.correoElectronico LIKE @like
           OR g.nombre + N' ' + g.apellido LIKE @like OR g.correoElectronico LIKE @like
           OR e.nombreEspacio LIKE @like OR h.codigo LIKE @like OR c.numero LIKE @like
           OR CAST(h.idReserva AS NVARCHAR(10)) = @textoLimpio)
    ORDER BY h.fechaOperacion DESC, h.codigo DESC;
END
GO

-- A2: distingue "no hay operaciones" de "no hay resultados para el filtro".
IF OBJECT_ID('dbo.sp_HistorialFinanciero_Contar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HistorialFinanciero_Contar;
GO
CREATE PROCEDURE dbo.sp_HistorialFinanciero_Contar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS total FROM dbo.vw_HistorialFinanciero;
END
GO

-- Paso 6: gestores con operaciones, para el filtro "Gestor del espacio".
IF OBJECT_ID('dbo.sp_HistorialFinanciero_ListarGestores', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HistorialFinanciero_ListarGestores;
GO
CREATE PROCEDURE dbo.sp_HistorialFinanciero_ListarGestores
AS
BEGIN
    SET NOCOUNT ON;
    SELECT g.idUsuarioExterno,
           LTRIM(RTRIM(ISNULL(g.nombre, N'') + N' ' + ISNULL(g.apellido, N''))) AS nombre,
           g.correoElectronico
    FROM dbo.UsuarioExterno g
    WHERE EXISTS (SELECT 1 FROM dbo.vw_HistorialFinanciero h WHERE h.idUsuarioGestor = g.idUsuarioExterno)
    ORDER BY g.nombre, g.apellido;
END
GO

-- ---------------------------------------------------------------------------
-- Pasos 11 a 13: detalle de una operación. Sin fila = A7.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_HistorialFinanciero_ObtenerDetalle', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HistorialFinanciero_ObtenerDetalle;
GO
CREATE PROCEDURE dbo.sp_HistorialFinanciero_ObtenerDetalle
    @origen    NVARCHAR(2),
    @idOrigen  INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.origen, h.idOrigen, h.codigo, h.tipoOperacion, h.idReserva, h.idPago, h.idComprobante,
        h.idUsuarioSolicitante, h.idUsuarioGestor, h.idEspacioArtistico, h.moneda, h.montoTotal,
        h.porcentajeComision, h.importeComision, h.importeGestor, h.porcentajeCargo, h.importeCargo,
        h.estadoPago, h.referenciaPago, h.estadoRegistro, h.fechaOperacion, h.observacion,
        LTRIM(RTRIM(ISNULL(s.nombre, N'') + N' ' + ISNULL(s.apellido, N''))) AS nombreSolicitante, s.correoElectronico AS correoSolicitante,
        LTRIM(RTRIM(ISNULL(g.nombre, N'') + N' ' + ISNULL(g.apellido, N''))) AS nombreGestor, g.correoElectronico AS correoGestor,
        e.nombreEspacio,
        c.numero AS numeroComprobante, c.tipo AS tipoComprobante,
        r.fechaSolicitada, r.minutoDesde, r.minutoHasta, r.importeEstimado, r.estadoReserva,
        r.estadoPago AS estadoPagoReserva, r.fechaCancelacion, r.fechaLimitePago,
        p.estado AS estadoPagoRegistrado, p.origen AS origenPago, p.medioInformado, p.marcaTarjeta, p.ultimosDigitos,
        p.importeTarjeta, p.importeSaldo, p.codigoAutorizacion
    FROM dbo.vw_HistorialFinanciero h
    LEFT JOIN dbo.UsuarioExterno s ON s.idUsuarioExterno = h.idUsuarioSolicitante
    LEFT JOIN dbo.UsuarioExterno g ON g.idUsuarioExterno = h.idUsuarioGestor
    LEFT JOIN dbo.EspacioArtistico e ON e.idEspacioArtistico = h.idEspacioArtistico
    LEFT JOIN dbo.Comprobante c ON c.idComprobante = h.idComprobante
    LEFT JOIN dbo.Reserva r ON r.idReserva = h.idReserva
    LEFT JOIN dbo.Pago p ON p.idPago = h.idPago
    WHERE h.origen = @origen AND h.idOrigen = @idOrigen;
END
GO

-- Comprobantes de la operación: factura del pago y notas de crédito o débito
-- de la misma reserva.
IF OBJECT_ID('dbo.sp_HistorialFinanciero_ListarComprobantes', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HistorialFinanciero_ListarComprobantes;
GO
CREATE PROCEDURE dbo.sp_HistorialFinanciero_ListarComprobantes
    @idReserva      INT = NULL,
    @idPago         INT = NULL,
    @idComprobante  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.idComprobante, c.tipo, c.numero, c.idUsuarioExterno, c.rolCuenta, c.idReserva, c.importe, c.moneda,
           c.origen, c.motivo, c.estado, c.fechaEmision, c.fechaAnulacion, c.motivoAnulacion,
           LTRIM(RTRIM(ISNULL(u.nombre, N'') + N' ' + ISNULL(u.apellido, N''))) AS nombreUsuario
    FROM dbo.Comprobante c
    LEFT JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = c.idUsuarioExterno
    WHERE (@idReserva IS NOT NULL AND c.idReserva = @idReserva)
       OR (@idPago IS NOT NULL AND c.idPago = @idPago)
       OR (@idComprobante IS NOT NULL AND c.idComprobante = @idComprobante)
    ORDER BY c.fechaEmision, c.idComprobante;
END
GO

-- Impacto en saldo: movimientos de cuenta corriente de la operación.
IF OBJECT_ID('dbo.sp_HistorialFinanciero_ListarMovimientos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HistorialFinanciero_ListarMovimientos;
GO
CREATE PROCEDURE dbo.sp_HistorialFinanciero_ListarMovimientos
    @idReserva      INT = NULL,
    @idPago         INT = NULL,
    @idComprobante  INT = NULL,
    @idMovimiento   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT m.idMovimiento, m.idUsuarioExterno, m.rolCuenta, m.tipoMovimiento, m.importe, m.moneda, m.descripcion,
           m.idReserva, m.idPago, m.idComprobante, m.fechaMovimiento,
           LTRIM(RTRIM(ISNULL(u.nombre, N'') + N' ' + ISNULL(u.apellido, N''))) AS nombreUsuario
    FROM dbo.MovimientoCuentaCorriente m
    LEFT JOIN dbo.UsuarioExterno u ON u.idUsuarioExterno = m.idUsuarioExterno
    WHERE (@idReserva IS NOT NULL AND m.idReserva = @idReserva)
       OR (@idPago IS NOT NULL AND m.idPago = @idPago)
       OR (@idComprobante IS NOT NULL AND m.idComprobante = @idComprobante)
       OR (@idMovimiento IS NOT NULL AND m.idMovimiento = @idMovimiento)
    ORDER BY m.fechaMovimiento, m.idMovimiento;
END
GO

-- ---------------------------------------------------------------------------
-- Permiso y opción de menú "Historial financiero" (paso 2 y A1).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#RegistrarPermisoAdministracion') IS NOT NULL DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
CREATE PROCEDURE #RegistrarPermisoAdministracion
    @codigo       NVARCHAR(100),
    @nombre       NVARCHAR(200),
    @descripcion  NVARCHAR(500),
    @url          NVARCHAR(300),
    @accion       NVARCHAR(50),
    @orden        INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @idPermiso INT =
        (
            SELECT TOP 1 idPermisoInterno
            FROM dbo.PermisoInterno
            WHERE codigoPermiso = @codigo
            ORDER BY idPermisoInterno
        );

        IF @idPermiso IS NULL
        BEGIN
            INSERT INTO dbo.PermisoInterno
                (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
            VALUES
                (@codigo, @nombre,
                 @descripcion,
                 N'Administración', @accion, N'Activo', @url, 1);

            SET @idPermiso = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.PermisoInterno
            SET nombrePermiso = @nombre,
                descripcion = @descripcion,
                modulo = N'Administración',
                accion = @accion,
                estadoPermiso = N'Activo',
                urlAsociada = @url,
                activo = 1,
                fechaUltimaModificacion = GETDATE()
            WHERE idPermisoInterno = @idPermiso;
        END

        DECLARE @idRolAdministrador INT =
        (
            SELECT TOP 1 idRolInterno
            FROM dbo.RolInterno
            WHERE nombreRol = N'Administrador'
              AND activo = 1
            ORDER BY idRolInterno
        );

        IF @idRolAdministrador IS NOT NULL
           AND NOT EXISTS
           (
               SELECT 1
               FROM dbo.RolInternoPermiso
               WHERE idRolInterno = @idRolAdministrador
                 AND idPermisoInterno = @idPermiso
           )
        BEGIN
            INSERT INTO dbo.RolInternoPermiso
                (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
            VALUES
                (@idRolAdministrador, @idPermiso, GETDATE(), 1);
        END
        ELSE IF @idRolAdministrador IS NOT NULL
        BEGIN
            UPDATE dbo.RolInternoPermiso
            SET activo = 1
            WHERE idRolInterno = @idRolAdministrador
              AND idPermisoInterno = @idPermiso;
        END

        IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
        BEGIN
            DECLARE @idGrupoAdministracion INT =
            (
                SELECT TOP 1 idComponentePermiso
                FROM dbo.ComponentePermiso
                WHERE tipoComponente = N'Grupo'
                  AND idComponentePadre IS NULL
                  AND nombre = N'Administración'
                ORDER BY idComponentePermiso
            );

            IF @idGrupoAdministracion IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (NULL, N'Grupo', NULL, N'Administración', NULL, NULL, 100, 1);

                SET @idGrupoAdministracion = CAST(SCOPE_IDENTITY() AS INT);
            END

            DECLARE @idComponente INT =
            (
                SELECT TOP 1 idComponentePermiso
                FROM dbo.ComponentePermiso
                WHERE codigoPermiso = @codigo
                ORDER BY idComponentePermiso
            );

            IF @idComponente IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (@idGrupoAdministracion, N'Permiso', @codigo, @nombre,
                     @descripcion,
                     @url, @orden, 1);

                SET @idComponente = CAST(SCOPE_IDENTITY() AS INT);
            END
            ELSE
            BEGIN
                UPDATE dbo.ComponentePermiso
                SET idComponentePadre = @idGrupoAdministracion,
                    tipoComponente = N'Permiso',
                    nombre = @nombre,
                    descripcion = @descripcion,
                    urlAsociada = @url,
                    activo = 1,
                    fechaUltimaModificacion = GETDATE()
                WHERE idComponentePermiso = @idComponente;
            END

            IF @idRolAdministrador IS NOT NULL
            BEGIN
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RolInternoComponentePermiso
                    WHERE idRolInterno = @idRolAdministrador
                      AND idComponentePermiso = @idComponente
                )
                BEGIN
                    INSERT INTO dbo.RolInternoComponentePermiso
                        (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                    VALUES
                        (@idRolAdministrador, @idComponente, GETDATE(), 1);
                END
                ELSE
                BEGIN
                    UPDATE dbo.RolInternoComponentePermiso
                    SET activo = 1
                    WHERE idRolInterno = @idRolAdministrador
                      AND idComponentePermiso = @idComponente;
                END
            END
        END

        -- Opción del menú dinámico (script 45), al final del menú.
        IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
           AND @idComponente IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponente)
        BEGIN
            INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
            SELECT @nombre,
                   @descripcion,
                   @url, N'Administración',
                   ISNULL(MAX(orden), 0) + 10,
                   @idComponente, 1
            FROM dbo.OpcionMenu;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        DECLARE @mensajeError NVARCHAR(4000);
        SET @mensajeError = ERROR_MESSAGE();
        RAISERROR(N'%s', 16, 1, @mensajeError);
    END CATCH
END
GO

EXEC #RegistrarPermisoAdministracion
    @codigo = N'CONSULTAR_HISTORIAL_FINANCIERO',
    @nombre = N'Historial financiero',
    @descripcion = N'Consultar el historial financiero de StageUp: reservas abonadas, pagos, cargos por cancelación, comisiones, facturas, liquidaciones y saldos de los gestores.',
    @url = N'~/Interno/HistorialFinanciero.aspx',
    @accion = N'Consultar',
    @orden = 27;
GO

DROP PROCEDURE #RegistrarPermisoAdministracion;
GO
