IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF COL_LENGTH(N'dbo.NodoAsistente', N'descripcionOpcion') IS NULL
    ALTER TABLE dbo.NodoAsistente ADD descripcionOpcion NVARCHAR(120) NULL;
GO

IF COL_LENGTH(N'dbo.NodoAsistente', N'icono') IS NULL
    ALTER TABLE dbo.NodoAsistente ADD icono NVARCHAR(30) NULL;
GO

IF COL_LENGTH(N'dbo.NodoAsistente', N'textoBotonLista') IS NULL
    ALTER TABLE dbo.NodoAsistente ADD textoBotonLista NVARCHAR(40) NULL;
GO

-- Contenido inicial (solo completa lo que esté vacío, para no pisar cambios).
DECLARE @estilo TABLE (codigo NVARCHAR(80) PRIMARY KEY, descripcion NVARCHAR(120) NULL, icono NVARCHAR(30) NULL, boton NVARCHAR(40) NULL);
INSERT INTO @estilo (codigo, descripcion, icono, boton) VALUES
(N'inicio', NULL, NULL, N'Menú principal'),

(N'reservas', N'Hacer, cancelar o seguir una reserva', N'reservas', N'Opciones de reservas'),
(N'reservas.hacer', N'Buscar un espacio y enviar la solicitud', N'reservar', NULL),
(N'reservas.cancelar', N'Con o sin cargo, según el estado', N'cancelar', NULL),
(N'reservas.cancelar.aceptada', N'Se aplica la política de cancelación', NULL, NULL),
(N'reservas.cancelar.pendiente', N'Se cancela sin cargo', NULL, NULL),
(N'reservas.cancelar.nosabe', N'Te ayudo a revisarlo', NULL, NULL),
(N'reservas.estado', N'Pendiente, aceptada, rechazada o finalizada', N'estado', NULL),
(N'reservas.gestor', N'Qué hacer si tu solicitud sigue pendiente', N'espera', NULL),
(N'reservas.politica', N'Cargos según la anticipación', N'politica', NULL),

(N'pagos', N'Pagar, saldo a favor, movimientos y tarjetas', N'pagos', N'Opciones de pagos'),
(N'pagos.pagar', N'Plazo y medios de pago', N'pagar', NULL),
(N'pagos.saldo', N'Cómo usarlo al pagar una reserva', N'saldo', NULL),
(N'pagos.movimiento', N'Tu cuenta corriente, explicada', N'movimiento', NULL),
(N'pagos.rechazada', N'Datos, rechazo del banco o cobro sin acreditar', N'tarjeta', NULL),
(N'pagos.rechazada.datos', N'Número, vencimiento o código', NULL, NULL),
(N'pagos.rechazada.banco', N'Fondos o límite insuficiente', NULL, NULL),
(N'pagos.rechazada.cobro', N'El pago no se acreditó', NULL, NULL),

(N'espacios', N'Ofrecer, editar y gestionar tus espacios', N'espacios', N'Opciones de espacios'),
(N'espacios.ofrecer', N'Habilitarte como gestor', N'ofrecer', NULL),
(N'espacios.editar', N'Fotos, precio, descripción o pausa', N'editar', NULL),
(N'espacios.disponibilidad', N'Días, horarios y bloqueos', N'calendario', NULL),
(N'espacios.solicitudes', N'Aceptar o rechazar reservas', N'solicitudes', NULL),

(N'tecnico', N'Ingreso, cuenta, correos y errores', N'tecnico', N'Opciones técnicas'),
(N'tecnico.ingresar', N'Recuperar tu contraseña', N'llave', NULL),
(N'tecnico.registro', N'Registro y activación de la cuenta', N'cuenta', NULL),
(N'tecnico.correo', N'Spam y reenvío del código', N'correo', NULL),
(N'tecnico.pagina', N'Errores al navegar la plataforma', N'error', NULL),

(N'noencuentro', N'Registrar una solicitud de soporte', N'soporte', NULL);

UPDATE n
SET descripcionOpcion = COALESCE(n.descripcionOpcion, e.descripcion),
    icono = COALESCE(n.icono, e.icono),
    textoBotonLista = COALESCE(n.textoBotonLista, e.boton)
FROM dbo.NodoAsistente n
INNER JOIN @estilo e ON e.codigo = n.codigo;

-- El saludo con el nombre lo arma el asistente; el inicio solo pregunta.
UPDATE dbo.NodoAsistente
SET mensaje = N'¿Qué te gustaría hacer hoy?'
WHERE codigo = N'inicio'
  AND mensaje = N'Hola, soy el asistente de StageUp. ¿Con qué necesitás ayuda?';
GO

IF OBJECT_ID('dbo.sp_NodoAsistente_ListarActivos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_NodoAsistente_ListarActivos;
GO
CREATE PROCEDURE dbo.sp_NodoAsistente_ListarActivos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idNodoAsistente, idNodoPadre, codigo, tipo, textoOpcion, mensaje, palabrasClave, orden,
           descripcionOpcion, icono, textoBotonLista
    FROM dbo.NodoAsistente
    WHERE activo = 1
    ORDER BY idNodoPadre, orden, idNodoAsistente;
END
GO
