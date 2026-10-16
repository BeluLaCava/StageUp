IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.NodoAsistente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NodoAsistente
    (
        idNodoAsistente  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NodoAsistente PRIMARY KEY,
        idNodoPadre      INT            NULL,
        codigo           NVARCHAR(60)   NOT NULL,
        tipo             NVARCHAR(10)   NOT NULL,
        textoOpcion      NVARCHAR(120)  NOT NULL,
        mensaje          NVARCHAR(1000) NOT NULL,
        palabrasClave    NVARCHAR(300)  NULL,
        orden            INT            NOT NULL CONSTRAINT DF_NodoAsistente_orden DEFAULT (10),
        activo           BIT            NOT NULL CONSTRAINT DF_NodoAsistente_activo DEFAULT (1),
        fechaAlta        DATETIME       NOT NULL CONSTRAINT DF_NodoAsistente_fechaAlta DEFAULT (GETDATE()),
        CONSTRAINT UQ_NodoAsistente_codigo UNIQUE (codigo),
        CONSTRAINT FK_NodoAsistente_Padre FOREIGN KEY (idNodoPadre) REFERENCES dbo.NodoAsistente (idNodoAsistente),
        CONSTRAINT CK_NodoAsistente_tipo CHECK (tipo IN (N'Pregunta', N'Respuesta'))
    );

    CREATE INDEX IX_NodoAsistente_Padre ON dbo.NodoAsistente (idNodoPadre, orden);
END
GO

IF OBJECT_ID(N'dbo.AccionNodoAsistente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccionNodoAsistente
    (
        idAccionNodoAsistente  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AccionNodoAsistente PRIMARY KEY,
        idNodoAsistente        INT           NOT NULL,
        texto                  NVARCHAR(80)  NOT NULL,
        tipoAccion             NVARCHAR(10)  NOT NULL,
        destino                NVARCHAR(200) NULL,
        orden                  INT           NOT NULL CONSTRAINT DF_AccionNodoAsistente_orden DEFAULT (10),
        CONSTRAINT FK_AccionNodoAsistente_Nodo FOREIGN KEY (idNodoAsistente) REFERENCES dbo.NodoAsistente (idNodoAsistente),
        CONSTRAINT CK_AccionNodoAsistente_tipo CHECK (tipoAccion IN (N'Enlace', N'Nodo', N'Soporte'))
    );

    CREATE INDEX IX_AccionNodoAsistente_Nodo ON dbo.AccionNodoAsistente (idNodoAsistente, orden);
END
GO

IF COL_LENGTH(N'dbo.ConsultaAsistente', N'idNodoAsistente') IS NULL
    ALTER TABLE dbo.ConsultaAsistente ADD idNodoAsistente INT NULL
        CONSTRAINT FK_ConsultaAsistente_Nodo FOREIGN KEY REFERENCES dbo.NodoAsistente (idNodoAsistente);
GO

-- Las preguntas frecuentes nuevas ya no se usan en el asistente salvo que
-- se marquen a propósito.
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Faq_usarEnAsistente' AND definition <> N'((0))')
    ALTER TABLE dbo.Faq DROP CONSTRAINT DF_Faq_usarEnAsistente;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Faq_usarEnAsistente')
    ALTER TABLE dbo.Faq ADD CONSTRAINT DF_Faq_usarEnAsistente DEFAULT (0) FOR usarEnAsistente;
GO

-- -----------------------------------------------------------------------------
-- Contenido inicial del árbol (solo la primera vez). Los textos pueden usar
-- {PoliticaCancelacion} y {HorasLimitePago}: BLL_AsistenteAyuda los
-- reemplaza con los parámetros vigentes.
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.NodoAsistente)
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    -- Desacople de las preguntas frecuentes (una sola vez).
    UPDATE dbo.Faq SET usarEnAsistente = 0 WHERE usarEnAsistente = 1;

    DECLARE @nodos TABLE (codigo NVARCHAR(60), padre NVARCHAR(60), tipo NVARCHAR(10), orden INT, opcion NVARCHAR(120), mensaje NVARCHAR(1000), palabras NVARCHAR(300));
    INSERT INTO @nodos (codigo, padre, tipo, orden, opcion, mensaje, palabras) VALUES
    (N'inicio', NULL, N'Pregunta', 0, N'Inicio',
        N'Hola, soy el asistente de StageUp. ¿Con qué necesitás ayuda?', NULL),

    -- 1. Reservas
    (N'reservas', N'inicio', N'Pregunta', 10, N'Reservas',
        N'¿Qué necesitás hacer con una reserva?', N'reserva reservas reservar turno'),
    (N'reservas.hacer', N'reservas', N'Respuesta', 10, N'Quiero hacer una reserva',
        N'Para reservar, entrá a "Explorar espacios", elegí un espacio, seleccioná fecha, horario y duración, y enviá la solicitud. El gestor va a revisarla y te va a responder desde la plataforma. Cuando la acepte, te avisamos para que la pagues.',
        N'hacer reservar solicitar nueva alquilar sala'),
    (N'reservas.cancelar', N'reservas', N'Pregunta', 20, N'Quiero cancelar una reserva',
        N'¿Tu reserva ya fue aceptada por el gestor?', N'cancelar cancelo cancelación anular dar de baja reserva'),
    (N'reservas.cancelar.aceptada', N'reservas.cancelar', N'Respuesta', 10, N'Sí, ya fue aceptada',
        N'Podés cancelarla desde "Mis reservas". Como la reserva ya estaba aceptada, se aplica la política de cancelación según la anticipación. La plataforma te muestra si corresponde algún cargo antes de que confirmes.',
        NULL),
    (N'reservas.cancelar.pendiente', N'reservas.cancelar', N'Respuesta', 20, N'No, todavía está pendiente',
        N'Si el gestor todavía no la aceptó, podés cancelarla desde "Mis reservas" sin ningún cargo.',
        NULL),
    (N'reservas.cancelar.nosabe', N'reservas.cancelar', N'Respuesta', 30, N'No estoy seguro/a',
        N'En "Mis reservas" cada reserva muestra su estado. Si dice "Pendiente", la cancelación no tiene cargo. Si dice "Aceptada", se aplica la política de cancelación y la plataforma te muestra el cargo antes de confirmar.',
        NULL),
    (N'reservas.estado', N'reservas', N'Respuesta', 30, N'No sé en qué estado está mi reserva',
        N'En "Mis reservas" ves cada reserva con su estado: Pendiente (el gestor todavía no respondió), Aceptada (falta o ya se registró el pago), Rechazada, Cancelada o Finalizada. Cada cambio también te llega como notificación.',
        N'estado estados seguimiento aceptada rechazada pendiente'),
    (N'reservas.gestor', N'reservas', N'Respuesta', 40, N'El gestor no respondió',
        N'El gestor recibe tu solicitud y tiene que aceptarla o rechazarla. Mientras siga pendiente no se te cobra nada: podés esperar, cancelarla sin cargo y buscar otro espacio. Si se acerca la fecha y no tuviste respuesta, podés avisarnos.',
        N'gestor responde respondio contesta demora espera'),
    (N'reservas.politica', N'reservas', N'Respuesta', 50, N'Ver la política de cancelación',
        N'{PoliticaCancelacion} Lo que pagaste vuelve como saldo a favor en "Mi cuenta corriente", descontando el cargo que corresponda.',
        N'politica cargo cargos cuanto cancelar cancelacion devolucion reintegro anticipacion'),

    -- 2. Pagos y cuenta corriente
    (N'pagos', N'inicio', N'Pregunta', 20, N'Pagos y cuenta corriente',
        N'¿Qué problema tenés con pagos?', N'pago pagos pagar cobro corriente'),
    (N'pagos.pagar', N'pagos', N'Respuesta', 10, N'Quiero pagar una reserva',
        N'Cuando el gestor acepta tu reserva, tenés {HorasLimitePago} horas para pagarla. Entrá a "Mis reservas" y elegí "Pagar": podés usar una tarjeta, una tarjeta guardada en "Mi billetera" o tu saldo a favor.',
        N'pagar abonar plazo tarjeta'),
    (N'pagos.saldo', N'pagos', N'Respuesta', 20, N'Tengo saldo a favor',
        N'El saldo a favor aparece en "Mi cuenta corriente". Al pagar una reserva, podés elegir si querés usar todo el saldo, una parte, o pagar con tarjeta.',
        N'saldo favor credito devolucion'),
    (N'pagos.movimiento', N'pagos', N'Respuesta', 30, N'No entiendo un movimiento',
        N'En "Mi cuenta corriente" cada movimiento indica qué es (pago, saldo aplicado, devolución, cargo por cancelación, nota de crédito o de débito) y a qué reserva corresponde. Desde el detalle del pago también ves la factura.',
        N'movimiento movimientos resumen factura nota debito credito'),
    (N'pagos.rechazada', N'pagos', N'Pregunta', 40, N'Mi tarjeta fue rechazada',
        N'¿Qué te mostró la plataforma al intentar pagar?', N'rechazada rechazo rechazaron tarjeta error pago'),
    (N'pagos.rechazada.datos', N'pagos.rechazada', N'Respuesta', 10, N'Datos inválidos o tarjeta vencida',
        N'Revisá el número, el vencimiento y el código de seguridad, y que la tarjeta esté vigente. Si la tenías guardada en "Mi billetera" y venció, eliminala y cargala de nuevo.',
        NULL),
    (N'pagos.rechazada.banco', N'pagos.rechazada', N'Respuesta', 20, N'El banco o el medio de pago la rechazó',
        N'El rechazo lo informa el medio de pago (por ejemplo, por fondos insuficientes o límite). No se te cobró nada: podés intentar con otra tarjeta o usar tu saldo a favor dentro del plazo para pagar.',
        NULL),
    (N'pagos.rechazada.cobro', N'pagos.rechazada', N'Respuesta', 30, N'Me cobraron pero la reserva sigue sin pagar',
        N'A veces el medio de pago tarda unos minutos en confirmar. Revisá "Mis reservas" en un rato. Si pasó más de una hora y sigue sin pagar, registrá una solicitud de soporte indicando la reserva y la fecha del pago.',
        N'cobraron debitaron debito doble veces descontaron'),

    -- 3. Espacios y gestores
    (N'espacios', N'inicio', N'Pregunta', 30, N'Espacios y gestores',
        N'¿Qué necesitás hacer?', N'espacio espacios gestor sala publicar'),
    (N'espacios.ofrecer', N'espacios', N'Respuesta', 10, N'Quiero ofrecer un espacio',
        N'Primero tenés que habilitarte como gestor: entrá a "Ofrecer espacio", completá el formulario con tus datos y los del espacio y envialo. El equipo de StageUp lo revisa y te avisa. Cuando te aprueban, cargás el espacio, las fotos y la disponibilidad desde "Mis espacios".',
        N'ofrecer habilitar habilitacion ser gestor publicar alta'),
    (N'espacios.editar', N'espacios', N'Respuesta', 20, N'Quiero editar un espacio publicado',
        N'Entrá a "Mis espacios", elegí el espacio y abrí "Editar". Los cambios se ven en la ficha pública al guardar. Si necesitás que deje de aparecer por un tiempo, podés pausarlo.',
        N'editar modificar cambiar fotos precio pausar'),
    (N'espacios.disponibilidad', N'espacios', N'Respuesta', 30, N'Quiero cargar disponibilidad',
        N'Entrá a "Mis espacios", elegí el espacio y abrí "Disponibilidad". Ahí podés cargar días y horarios disponibles o bloquear horarios específicos.',
        N'disponibilidad horarios dias bloquear calendario'),
    (N'espacios.solicitudes', N'espacios', N'Respuesta', 40, N'Quiero revisar solicitudes recibidas',
        N'En "Solicitudes recibidas" ves las reservas pendientes de tus espacios. Podés aceptarlas o rechazarlas con un comentario; el solicitante recibe el aviso.',
        N'solicitudes recibidas aceptar rechazar pedidos'),

    -- 4. Soporte técnico
    (N'tecnico', N'inicio', N'Pregunta', 40, N'Soporte técnico',
        N'¿Qué problema técnico tenés?', N'error problema tecnico falla anda'),
    (N'tecnico.ingresar', N'tecnico', N'Respuesta', 10, N'No puedo iniciar sesión',
        N'En "Iniciar sesión" elegí "Olvidé mi contraseña" y seguí los pasos del correo. Si después de varios intentos fallidos no podés ingresar, esperá unos minutos antes de volver a intentar.',
        N'iniciar sesion ingresar login contraseña clave olvide'),
    (N'tecnico.registro', N'tecnico', N'Respuesta', 15, N'No puedo crear mi cuenta',
        N'Entrá a "Registrarse", completá tus datos y aceptá los términos. Te enviamos un código por correo: ingresalo para activar la cuenta. Si el correo ya está registrado, usá "Olvidé mi contraseña" desde "Iniciar sesión".',
        N'crear cuenta registrarme registro alta activar nueva'),
    (N'tecnico.correo', N'tecnico', N'Respuesta', 20, N'No me llega el correo con el código',
        N'Revisá la carpeta de spam o correo no deseado y que la dirección esté bien escrita. Podés pedir que te reenviemos el código desde la misma pantalla donde lo tenés que ingresar.',
        N'correo mail codigo activacion verificacion llega'),
    (N'tecnico.pagina', N'tecnico', N'Respuesta', 30, N'Una página muestra un error o no carga',
        N'Probá actualizar la página, cerrar sesión y volver a ingresar, o usar otro navegador. Si el error sigue, registrá una solicitud de soporte contando qué estabas haciendo y qué mensaje viste.',
        N'pagina error carga lento bloqueada'),

    -- 5. No encuentro lo que necesito
    (N'noencuentro', N'inicio', N'Respuesta', 50, N'No encuentro lo que necesito',
        N'No pude encontrar una respuesta clara para tu consulta. Podés registrar una solicitud de soporte para que el equipo de StageUp la revise.',
        NULL);

    INSERT INTO dbo.NodoAsistente (codigo, tipo, textoOpcion, mensaje, palabrasClave, orden, activo)
    SELECT codigo, tipo, opcion, mensaje, palabras, orden, 1 FROM @nodos;

    UPDATE n
    SET n.idNodoPadre = p.idNodoAsistente
    FROM dbo.NodoAsistente n
    INNER JOIN @nodos v ON v.codigo = n.codigo
    INNER JOIN dbo.NodoAsistente p ON p.codigo = v.padre;

    DECLARE @acciones TABLE (codigo NVARCHAR(60), orden INT, texto NVARCHAR(80), tipo NVARCHAR(10), destino NVARCHAR(200));
    INSERT INTO @acciones (codigo, orden, texto, tipo, destino) VALUES
    (N'reservas.hacer', 10, N'Ir a Explorar espacios', N'Enlace', N'~/Explorar/ResultadosBusqueda.aspx'),
    (N'reservas.hacer', 20, N'Ver mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'reservas.cancelar.aceptada', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'reservas.cancelar.aceptada', 20, N'Ver política de cancelación', N'Nodo', N'reservas.politica'),
    (N'reservas.cancelar.aceptada', 30, N'Crear solicitud de soporte', N'Soporte', NULL),
    (N'reservas.cancelar.pendiente', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'reservas.cancelar.nosabe', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'reservas.cancelar.nosabe', 20, N'Ver política de cancelación', N'Nodo', N'reservas.politica'),
    (N'reservas.estado', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'reservas.estado', 20, N'Ver mis notificaciones', N'Enlace', N'~/Notificaciones.aspx'),
    (N'reservas.gestor', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'reservas.gestor', 20, N'Buscar otro espacio', N'Enlace', N'~/Explorar/ResultadosBusqueda.aspx'),
    (N'reservas.gestor', 30, N'Crear solicitud de soporte', N'Soporte', NULL),
    (N'reservas.politica', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'pagos.pagar', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'pagos.pagar', 20, N'Ir a Mi billetera', N'Enlace', N'~/MiBilletera.aspx'),
    (N'pagos.saldo', 10, N'Ir a Mi cuenta corriente', N'Enlace', N'~/MiCuentaCorriente.aspx'),
    (N'pagos.saldo', 20, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'pagos.saldo', 30, N'Crear solicitud de soporte', N'Soporte', NULL),
    (N'pagos.movimiento', 10, N'Ir a Mi cuenta corriente', N'Enlace', N'~/MiCuentaCorriente.aspx'),
    (N'pagos.rechazada.datos', 10, N'Ir a Mi billetera', N'Enlace', N'~/MiBilletera.aspx'),
    (N'pagos.rechazada.banco', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'pagos.rechazada.cobro', 10, N'Ir a Mis reservas', N'Enlace', N'~/MisReservas.aspx'),
    (N'pagos.rechazada.cobro', 20, N'Crear solicitud de soporte', N'Soporte', NULL),
    (N'espacios.ofrecer', 10, N'Ir a Ofrecer espacio', N'Enlace', N'~/OfrecerEspacio.aspx'),
    (N'espacios.editar', 10, N'Ir a Mis espacios', N'Enlace', N'~/MisEspacios.aspx'),
    (N'espacios.disponibilidad', 10, N'Ir a Mis espacios', N'Enlace', N'~/MisEspacios.aspx'),
    (N'espacios.disponibilidad', 20, N'Crear solicitud de soporte', N'Soporte', NULL),
    (N'espacios.solicitudes', 10, N'Ir a Solicitudes recibidas', N'Enlace', N'~/SolicitudesRecibidas.aspx'),
    (N'tecnico.ingresar', 10, N'Ir a Iniciar sesión', N'Enlace', N'~/IniciarSesion.aspx'),
    (N'tecnico.registro', 10, N'Ir a Registrarse', N'Enlace', N'~/Registrarse.aspx'),
    (N'tecnico.pagina', 10, N'Crear solicitud de soporte', N'Soporte', NULL),
    (N'noencuentro', 10, N'Crear solicitud de soporte', N'Soporte', NULL);

    INSERT INTO dbo.AccionNodoAsistente (idNodoAsistente, texto, tipoAccion, destino, orden)
    SELECT n.idNodoAsistente, a.texto, a.tipo, a.destino, a.orden
    FROM @acciones a
    INNER JOIN dbo.NodoAsistente n ON n.codigo = a.codigo;

    COMMIT TRANSACTION;
END
GO

-- -----------------------------------------------------------------------------
-- SP
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_NodoAsistente_ListarActivos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_NodoAsistente_ListarActivos;
GO
CREATE PROCEDURE dbo.sp_NodoAsistente_ListarActivos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idNodoAsistente, idNodoPadre, codigo, tipo, textoOpcion, mensaje, palabrasClave, orden
    FROM dbo.NodoAsistente
    WHERE activo = 1
    ORDER BY idNodoPadre, orden, idNodoAsistente;
END
GO

IF OBJECT_ID('dbo.sp_AccionNodoAsistente_ListarActivas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AccionNodoAsistente_ListarActivas;
GO
CREATE PROCEDURE dbo.sp_AccionNodoAsistente_ListarActivas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.idAccionNodoAsistente, a.idNodoAsistente, a.texto, a.tipoAccion, a.destino, a.orden
    FROM dbo.AccionNodoAsistente a
    INNER JOIN dbo.NodoAsistente n ON n.idNodoAsistente = a.idNodoAsistente AND n.activo = 1
    ORDER BY a.idNodoAsistente, a.orden, a.idAccionNodoAsistente;
END
GO

-- Consulta escrita: queda registrada con el nodo del árbol o la pregunta
-- frecuente vinculada que la respondió (reconocida) o sin respuesta (A5).
IF OBJECT_ID('dbo.sp_ConsultaAsistente_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsultaAsistente_Registrar;
GO
CREATE PROCEDURE dbo.sp_ConsultaAsistente_Registrar
    @texto             NVARCHAR(200),
    @textoNormalizado  NVARCHAR(200),
    @idFaqRespuesta    INT = NULL,
    @idNodoAsistente   INT = NULL,
    @idUsuarioExterno  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ConsultaAsistente (texto, textoNormalizado, idFaqRespuesta, idNodoAsistente, reconocida, idUsuarioExterno)
    VALUES (@texto, @textoNormalizado, @idFaqRespuesta, @idNodoAsistente,
            CASE WHEN @idFaqRespuesta IS NULL AND @idNodoAsistente IS NULL THEN 0 ELSE 1 END, @idUsuarioExterno);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idConsultaAsistente;
END
GO
