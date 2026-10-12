IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.Faq', N'U') IS NULL
BEGIN
    THROW 51000, 'Primero deben ejecutarse Database/04_FaqDinamica.sql y Database/29_FaqAbmYPermiso.sql.', 1;
END
GO

-- ---------------------------------------------------------------------------
-- CU-003-004: categorías temáticas del contenido de ayuda.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CategoriaAyuda', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CategoriaAyuda
    (
        idCategoriaAyuda  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CategoriaAyuda PRIMARY KEY,
        codigo            NVARCHAR(30)   NOT NULL CONSTRAINT UX_CategoriaAyuda_codigo UNIQUE,
        nombre            NVARCHAR(80)   NOT NULL,
        descripcion       NVARCHAR(200)  NULL,
        orden             INT            NOT NULL,
        activa            BIT            NOT NULL CONSTRAINT DF_CategoriaAyuda_activa DEFAULT (1)
    );
END
GO

MERGE dbo.CategoriaAyuda AS destino
USING (VALUES
    (N'Cuenta', N'Cuenta de usuario', N'Registro, inicio de sesión, datos personales y baja de la cuenta.', 10),
    (N'Busqueda', N'Búsqueda de espacios', N'Cómo encontrar espacios y usar los filtros.', 20),
    (N'Reservas', N'Reservas', N'Solicitar, aceptar y seguir una reserva.', 30),
    (N'Cancelaciones', N'Cancelaciones', N'Plazos y cargos al cancelar una reserva.', 40),
    (N'Pagos', N'Pagos', N'Pagar una reserva, saldo a favor y billetera de tarjetas.', 50),
    (N'Facturacion', N'Facturación', N'Facturas, notas de crédito y de débito.', 60),
    (N'Gestor', N'Habilitación como gestor', N'Cómo pasar a ser gestor de espacios.', 70),
    (N'Publicacion', N'Publicación de espacios', N'Publicar y administrar tus espacios.', 80),
    (N'Soporte', N'Soporte', N'Solicitudes de soporte y contacto con StageUp.', 90),
    (N'General', N'Uso general', N'Qué es StageUp y cómo funciona.', 100)
) AS origen (codigo, nombre, descripcion, orden)
ON destino.codigo = origen.codigo
WHEN NOT MATCHED THEN
    INSERT (codigo, nombre, descripcion, orden) VALUES (origen.codigo, origen.nombre, origen.descripcion, origen.orden);
GO

-- ---------------------------------------------------------------------------
-- Faq pasa a ser "contenido de ayuda": categoría, estado (Borrador, Activo,
-- Inactivo), si se usa en el asistente y palabras clave para reconocer
-- consultas escritas. activo se mantiene sincronizado con estado = Activo
-- (lo usan la búsqueda global y el listado público).
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Faq', N'idCategoriaAyuda') IS NULL
    ALTER TABLE dbo.Faq ADD idCategoriaAyuda INT NULL
        CONSTRAINT FK_Faq_CategoriaAyuda FOREIGN KEY REFERENCES dbo.CategoriaAyuda (idCategoriaAyuda);
GO
IF COL_LENGTH(N'dbo.Faq', N'estado') IS NULL
    ALTER TABLE dbo.Faq ADD estado NVARCHAR(10) NULL;
GO
IF COL_LENGTH(N'dbo.Faq', N'usarEnAsistente') IS NULL
    ALTER TABLE dbo.Faq ADD usarEnAsistente BIT NOT NULL CONSTRAINT DF_Faq_usarEnAsistente DEFAULT (1);
GO
IF COL_LENGTH(N'dbo.Faq', N'palabrasClave') IS NULL
    ALTER TABLE dbo.Faq ADD palabrasClave NVARCHAR(300) NULL;
GO

UPDATE dbo.Faq SET estado = CASE WHEN activo = 1 THEN N'Activo' ELSE N'Borrador' END WHERE estado IS NULL;
GO

UPDATE f
SET idCategoriaAyuda = c.idCategoriaAyuda
FROM dbo.Faq f
INNER JOIN dbo.CategoriaAyuda c ON c.codigo =
    CASE
        WHEN f.pregunta LIKE N'%buscar%' THEN N'Busqueda'
        WHEN f.pregunta LIKE N'%cancel%' THEN N'Cancelaciones'
        WHEN f.pregunta LIKE N'%reserva%' THEN N'Reservas'
        WHEN f.pregunta LIKE N'%pag%' THEN N'Pagos'
        WHEN f.pregunta LIKE N'%factura%' THEN N'Facturacion'
        WHEN f.pregunta LIKE N'%gestor%' THEN N'Gestor'
        WHEN f.pregunta LIKE N'%publica%' THEN N'Publicacion'
        WHEN f.pregunta LIKE N'%cuenta%' THEN N'Cuenta'
        WHEN f.pregunta LIKE N'%soporte%' THEN N'Soporte'
        ELSE N'General'
    END
WHERE f.idCategoriaAyuda IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Faq_estado')
BEGIN
    ALTER TABLE dbo.Faq ALTER COLUMN estado NVARCHAR(10) NOT NULL;
    ALTER TABLE dbo.Faq ADD CONSTRAINT CK_Faq_estado CHECK (estado IN (N'Borrador', N'Activo', N'Inactivo'));
END
GO

-- Contenido inicial por categoría (solo si la pregunta no existe).
INSERT INTO dbo.Faq (pregunta, respuesta, orden, activo, estado, idCategoriaAyuda, usarEnAsistente, palabrasClave)
SELECT v.pregunta, v.respuesta, v.orden, 1, N'Activo', c.idCategoriaAyuda, 1, v.palabras
FROM (VALUES
    (N'Cuenta', 10, N'¿Cómo creo mi cuenta en StageUp?', N'Entrá a "Registrarse", completá tus datos y aceptá los términos. Te enviamos un código por correo: ingresalo para activar la cuenta y ya podés iniciar sesión.', N'registro registrarme crear cuenta alta usuario codigo activacion'),
    (N'Cuenta', 20, N'Olvidé mi contraseña, ¿qué hago?', N'En "Iniciar sesión" elegí "Olvidé mi contraseña". Te mandamos un código por correo para que elijas una contraseña nueva.', N'contraseña clave olvide recuperar password ingresar login'),
    (N'Reservas', 20, N'¿Cuándo queda confirmada mi reserva?', N'Cuando el gestor acepta tu solicitud, la reserva queda aceptada y tenés un plazo para pagarla. Al registrarse el pago, la reserva queda confirmada y te llega el comprobante por correo.', N'confirmada confirmar reserva aceptada estado solicitud'),
    (N'Cancelaciones', 10, N'¿Cuánto me cobran si cancelo una reserva?', N'Si cancelás con 14 días o más de anticipación no tiene cargo. Entre 7 y 14 días se cobra el 50 % y con menos de 7 días, el 100 %. Lo que pagaste vuelve como saldo a favor, descontando el cargo. Una reserva aceptada que todavía no pagaste se cancela sin cargo.', N'cancelar cancelacion cargo penalidad devolucion reintegro'),
    (N'Pagos', 10, N'¿Cómo pago una reserva?', N'Desde "Mis reservas" elegí "Pagar" en la reserva aceptada. Podés pagar con tarjeta, con tu saldo a favor o combinando los dos, y elegir una tarjeta guardada en tu billetera.', N'pagar pago tarjeta abonar saldo billetera'),
    (N'Pagos', 20, N'¿Guardan los datos de mi tarjeta?', N'Solo si elegís guardarla en "Mi billetera". El número y el titular se guardan cifrados y el código de seguridad nunca se guarda: te lo pedimos en cada pago.', N'tarjeta guardar datos seguridad billetera cvv codigo'),
    (N'Facturacion', 10, N'¿Dónde veo la factura de mi pago?', N'En el detalle de la reserva pagada, elegí "Ver pago y factura". También ves tus movimientos en "Mi cuenta corriente".', N'factura comprobante recibo pago ticket'),
    (N'Publicacion', 10, N'¿Cómo publico mi espacio?', N'Una vez habilitado como gestor, entrá a "Mis espacios", cargá los datos, las fotos y la disponibilidad, y enviá el espacio a publicación.', N'publicar espacio sala alta mis espacios'),
    (N'Soporte', 10, N'¿Cómo contacto al equipo de soporte?', N'Si las preguntas frecuentes y el asistente no resuelven tu consulta, iniciá sesión y registrá una solicitud desde "Soporte". Podés seguir el estado y las respuestas desde ahí.', N'soporte ayuda contacto reclamo problema ticket solicitud')
) AS v (codigoCategoria, orden, pregunta, respuesta, palabras)
INNER JOIN dbo.CategoriaAyuda c ON c.codigo = v.codigoCategoria
WHERE NOT EXISTS (SELECT 1 FROM dbo.Faq f WHERE f.pregunta = v.pregunta);
GO

-- ---------------------------------------------------------------------------
-- CU-003-001 A5 / CU-003-004 A15: consultas escritas en el asistente, para
-- detectar las que no tuvieron respuesta y convertirlas en contenido.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConsultaAsistente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultaAsistente
    (
        idConsultaAsistente  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsultaAsistente PRIMARY KEY,
        texto                NVARCHAR(200)  NOT NULL,
        textoNormalizado     NVARCHAR(200)  NOT NULL,
        idFaqRespuesta       INT            NULL,
        reconocida           BIT            NOT NULL,
        idUsuarioExterno     INT            NULL,
        fecha                DATETIME       NOT NULL CONSTRAINT DF_ConsultaAsistente_fecha DEFAULT (GETDATE()),
        atendida             BIT            NOT NULL CONSTRAINT DF_ConsultaAsistente_atendida DEFAULT (0),
        CONSTRAINT FK_ConsultaAsistente_Faq FOREIGN KEY (idFaqRespuesta) REFERENCES dbo.Faq (idFaq),
        CONSTRAINT FK_ConsultaAsistente_Usuario FOREIGN KEY (idUsuarioExterno) REFERENCES dbo.UsuarioExterno (idUsuarioExterno)
    );

    CREATE INDEX IX_ConsultaAsistente_SinRespuesta ON dbo.ConsultaAsistente (textoNormalizado) WHERE reconocida = 0 AND atendida = 0;
END
GO

-- ===========================================================================
-- SP
-- ===========================================================================
IF OBJECT_ID('dbo.sp_CategoriaAyuda_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CategoriaAyuda_Listar;
GO
CREATE PROCEDURE dbo.sp_CategoriaAyuda_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idCategoriaAyuda, c.codigo, c.nombre, c.descripcion, c.orden, c.activa,
           (SELECT COUNT(*) FROM dbo.Faq f WHERE f.idCategoriaAyuda = c.idCategoriaAyuda AND f.estado = N'Activo') AS cantidadActivas,
           (SELECT COUNT(*) FROM dbo.Faq f WHERE f.idCategoriaAyuda = c.idCategoriaAyuda AND f.estado = N'Activo' AND f.usarEnAsistente = 1) AS cantidadAsistente
    FROM dbo.CategoriaAyuda c
    ORDER BY c.orden, c.nombre;
END
GO

-- Público (CU-003-001): solo contenido activo de categorías activas.
IF OBJECT_ID('dbo.sp_Faq_ListarActivas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_ListarActivas;
GO
CREATE PROCEDURE dbo.sp_Faq_ListarActivas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.idFaq, f.pregunta, f.respuesta, f.orden, f.activo, f.estado, f.idCategoriaAyuda, f.usarEnAsistente, f.palabrasClave,
           f.fechaAlta, f.fechaUltimaModificacion, c.nombre AS nombreCategoria, c.orden AS ordenCategoria
    FROM dbo.Faq f
    INNER JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = f.idCategoriaAyuda AND c.activa = 1
    WHERE f.estado = N'Activo'
    ORDER BY c.orden, f.orden, f.idFaq;
END
GO

-- Administración (CU-003-004 pasos 4 y 5), con filtros opcionales.
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

    SELECT f.idFaq, f.pregunta, f.respuesta, f.orden, f.activo, f.estado, f.idCategoriaAyuda, f.usarEnAsistente, f.palabrasClave,
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
    SELECT f.idFaq, f.pregunta, f.respuesta, f.orden, f.activo, f.estado, f.idCategoriaAyuda, f.usarEnAsistente, f.palabrasClave,
           f.fechaAlta, f.fechaUltimaModificacion, c.nombre AS nombreCategoria, c.orden AS ordenCategoria, c.activa AS categoriaActiva
    FROM dbo.Faq f
    LEFT JOIN dbo.CategoriaAyuda c ON c.idCategoriaAyuda = f.idCategoriaAyuda
    WHERE f.idFaq = @idFaq;
END
GO

-- Preguntas para detectar duplicados (A12): la BLL compara normalizado.
IF OBJECT_ID('dbo.sp_Faq_ListarPreguntas', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_ListarPreguntas;
GO
CREATE PROCEDURE dbo.sp_Faq_ListarPreguntas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idFaq, pregunta, estado FROM dbo.Faq;
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
    @usarEnAsistente   BIT,
    @palabrasClave     NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Faq (pregunta, respuesta, orden, activo, estado, idCategoriaAyuda, usarEnAsistente, palabrasClave, fechaAlta)
    VALUES (@pregunta, @respuesta, @orden, CASE WHEN @estado = N'Activo' THEN 1 ELSE 0 END, @estado, @idCategoriaAyuda,
            @usarEnAsistente, @palabrasClave, GETDATE());
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
    @usarEnAsistente   BIT,
    @palabrasClave     NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Faq
    SET pregunta = @pregunta, respuesta = @respuesta, orden = @orden, estado = @estado,
        activo = CASE WHEN @estado = N'Activo' THEN 1 ELSE 0 END,
        idCategoriaAyuda = @idCategoriaAyuda, usarEnAsistente = @usarEnAsistente, palabrasClave = @palabrasClave,
        fechaUltimaModificacion = GETDATE()
    WHERE idFaq = @idFaq;
    SELECT @@ROWCOUNT AS filas;
END
GO

IF OBJECT_ID('dbo.sp_Faq_CambiarEstado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_CambiarEstado;
GO
CREATE PROCEDURE dbo.sp_Faq_CambiarEstado
    @idFaq   INT,
    @estado  NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Faq
    SET estado = @estado, activo = CASE WHEN @estado = N'Activo' THEN 1 ELSE 0 END, fechaUltimaModificacion = GETDATE()
    WHERE idFaq = @idFaq;
    SELECT @@ROWCOUNT AS filas;
END
GO

-- Compatibilidad: los SP de baja/reactivación anteriores usan el estado.
IF OBJECT_ID('dbo.sp_Faq_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_Faq_DarDeBaja
    @idFaq INT
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_Faq_CambiarEstado @idFaq = @idFaq, @estado = N'Inactivo';
END
GO

IF OBJECT_ID('dbo.sp_Faq_Reactivar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Faq_Reactivar;
GO
CREATE PROCEDURE dbo.sp_Faq_Reactivar
    @idFaq INT
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.sp_Faq_CambiarEstado @idFaq = @idFaq, @estado = N'Activo';
END
GO

-- Asistente
IF OBJECT_ID('dbo.sp_ConsultaAsistente_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsultaAsistente_Registrar;
GO
CREATE PROCEDURE dbo.sp_ConsultaAsistente_Registrar
    @texto             NVARCHAR(200),
    @textoNormalizado  NVARCHAR(200),
    @idFaqRespuesta    INT = NULL,
    @idUsuarioExterno  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ConsultaAsistente (texto, textoNormalizado, idFaqRespuesta, reconocida, idUsuarioExterno)
    VALUES (@texto, @textoNormalizado, @idFaqRespuesta, CASE WHEN @idFaqRespuesta IS NULL THEN 0 ELSE 1 END, @idUsuarioExterno);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idConsultaAsistente;
END
GO

-- A15: consultas sin respuesta, agrupadas, para crear contenido.
IF OBJECT_ID('dbo.sp_ConsultaAsistente_ListarSinRespuesta', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsultaAsistente_ListarSinRespuesta;
GO
CREATE PROCEDURE dbo.sp_ConsultaAsistente_ListarSinRespuesta
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 20 textoNormalizado, MAX(texto) AS texto, COUNT(*) AS cantidad, MAX(fecha) AS ultimaFecha
    FROM dbo.ConsultaAsistente
    WHERE reconocida = 0 AND atendida = 0
    GROUP BY textoNormalizado
    ORDER BY COUNT(*) DESC, MAX(fecha) DESC;
END
GO

IF OBJECT_ID('dbo.sp_ConsultaAsistente_MarcarAtendida', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ConsultaAsistente_MarcarAtendida;
GO
CREATE PROCEDURE dbo.sp_ConsultaAsistente_MarcarAtendida
    @textoNormalizado NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ConsultaAsistente SET atendida = 1 WHERE textoNormalizado = @textoNormalizado AND reconocida = 0;
    SELECT @@ROWCOUNT AS filas;
END
GO

-- Nombre de la pantalla según el CU.
UPDATE dbo.ComponentePermiso
SET nombre = N'Contenido de ayuda',
    descripcion = N'Crear, editar, publicar y desactivar el contenido de ayuda (preguntas frecuentes por categoría) que usan el centro de ayuda y el asistente.',
    fechaUltimaModificacion = GETDATE()
WHERE codigoPermiso = N'GESTIONAR_FAQ';
GO

IF OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
    UPDATE dbo.OpcionMenu SET texto = N'Contenido de ayuda' WHERE url = N'~/Interno/GestionFaq.aspx';
GO
