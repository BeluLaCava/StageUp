IF DB_ID(N'StageUp') IS NULL
BEGIN
    CREATE DATABASE StageUp;
END
GO

USE StageUp;
GO

IF OBJECT_ID(N'dbo.CatalogoComercial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogoComercial
    (
        idCatalogoComercial  INT IDENTITY(1,1) NOT NULL,
        tipoCatalogo         NVARCHAR(40)      NOT NULL,
        codigo               NVARCHAR(40)      NOT NULL,
        nombre               NVARCHAR(120)     NOT NULL,
        orden                INT               NOT NULL CONSTRAINT DF_CatalogoComercial_orden DEFAULT (0),
        activo               BIT               NOT NULL CONSTRAINT DF_CatalogoComercial_activo DEFAULT (1),
        CONSTRAINT PK_CatalogoComercial PRIMARY KEY CLUSTERED (idCatalogoComercial),
        CONSTRAINT UQ_CatalogoComercial UNIQUE (tipoCatalogo, codigo),
        CONSTRAINT CK_CatalogoComercial_tipo CHECK (tipoCatalogo IN
            (N'PublicoObjetivo', N'CanalDifusion', N'TipoEntidad', N'MedioContacto', N'TipoInteraccion',
             N'TipoInstitucion', N'TipoContenido', N'CanalUso', N'CanalOrigen'))
    );
END
GO

MERGE dbo.CatalogoComercial AS destino
USING (VALUES
    (N'PublicoObjetivo', N'Artistas', N'Artistas', 10),
    (N'PublicoObjetivo', N'Docentes', N'Docentes', 20),
    (N'PublicoObjetivo', N'Companias', N'Compañías', 30),
    (N'PublicoObjetivo', N'Productores', N'Productores independientes', 40),
    (N'PublicoObjetivo', N'EscuelasDanza', N'Escuelas de danza', 50),
    (N'PublicoObjetivo', N'Estudios', N'Estudios', 60),
    (N'PublicoObjetivo', N'Teatros', N'Teatros', 70),
    (N'PublicoObjetivo', N'CentrosCulturales', N'Centros culturales', 80),
    (N'PublicoObjetivo', N'BaresCulturales', N'Bares culturales', 90),
    (N'PublicoObjetivo', N'InstitucionesEducativas', N'Instituciones educativas', 100),
    (N'PublicoObjetivo', N'GestoresEspacios', N'Gestores de espacios artísticos', 110),

    (N'CanalDifusion', N'RedesSociales', N'Redes sociales', 10),
    (N'CanalDifusion', N'Correo', N'Correo electrónico', 20),
    (N'CanalDifusion', N'SitioWeb', N'Sitio web de StageUp', 30),
    (N'CanalDifusion', N'Eventos', N'Eventos presenciales', 40),
    (N'CanalDifusion', N'Aliados', N'Difusión con aliados', 50),
    (N'CanalDifusion', N'Medios', N'Medios de comunicación', 60),
    (N'CanalDifusion', N'Recomendacion', N'Recomendación (boca a boca)', 70),
    (N'CanalDifusion', N'Otro', N'Otro', 80),

    (N'TipoEntidad', N'Artista', N'Artista', 10),
    (N'TipoEntidad', N'Docente', N'Docente', 20),
    (N'TipoEntidad', N'Productor', N'Productor independiente', 30),
    (N'TipoEntidad', N'Compania', N'Compañía', 40),
    (N'TipoEntidad', N'GestorEspacio', N'Gestor de espacio', 50),
    (N'TipoEntidad', N'EscuelaDanza', N'Escuela de danza', 60),
    (N'TipoEntidad', N'Teatro', N'Teatro', 70),
    (N'TipoEntidad', N'CentroCultural', N'Centro cultural', 80),
    (N'TipoEntidad', N'BarCultural', N'Bar cultural', 90),
    (N'TipoEntidad', N'InstitucionEducativa', N'Institución educativa', 100),
    (N'TipoEntidad', N'ReferenteCultural', N'Referente cultural', 110),
    (N'TipoEntidad', N'InstitucionSector', N'Institución del sector artístico', 120),

    (N'MedioContacto', N'Correo', N'Correo electrónico', 10),
    (N'MedioContacto', N'Telefono', N'Teléfono', 20),
    (N'MedioContacto', N'WhatsApp', N'WhatsApp', 30),
    (N'MedioContacto', N'RedSocial', N'Red social', 40),
    (N'MedioContacto', N'SitioWeb', N'Sitio web', 50),
    (N'MedioContacto', N'Presencial', N'Presencial', 60),

    (N'TipoInteraccion', N'Llamada', N'Llamada', 10),
    (N'TipoInteraccion', N'Correo', N'Correo electrónico', 20),
    (N'TipoInteraccion', N'Mensaje', N'Mensaje (WhatsApp o redes)', 30),
    (N'TipoInteraccion', N'Reunion', N'Reunión', 40),
    (N'TipoInteraccion', N'Visita', N'Visita', 50),
    (N'TipoInteraccion', N'Evento', N'Evento', 60),
    (N'TipoInteraccion', N'Otro', N'Otro', 70),

    (N'TipoInstitucion', N'InstitucionEducativa', N'Institución educativa', 10),
    (N'TipoInstitucion', N'EscuelaDanza', N'Escuela de danza', 20),
    (N'TipoInstitucion', N'Teatro', N'Teatro', 30),
    (N'TipoInstitucion', N'CentroCultural', N'Centro cultural', 40),
    (N'TipoInstitucion', N'BarCultural', N'Bar cultural', 50),
    (N'TipoInstitucion', N'OrganismoMunicipal', N'Organismo municipal', 60),
    (N'TipoInstitucion', N'OrganismoProvincial', N'Organismo provincial', 70),
    (N'TipoInstitucion', N'OrganismoNacional', N'Organismo nacional', 80),
    (N'TipoInstitucion', N'AsociacionCultural', N'Asociación cultural', 90),
    (N'TipoInstitucion', N'ProgramaCultural', N'Programa cultural', 100),
    (N'TipoInstitucion', N'ReferenteInstitucional', N'Referente institucional del sector', 110),

    (N'TipoContenido', N'Imagen', N'Imagen', 10),
    (N'TipoContenido', N'Video', N'Video', 20),
    (N'TipoContenido', N'TextoComercial', N'Texto comercial', 30),
    (N'TipoContenido', N'Presentacion', N'Presentación institucional', 40),
    (N'TipoContenido', N'PiezaRedes', N'Pieza para redes sociales', 50),
    (N'TipoContenido', N'MaterialInformativo', N'Material informativo', 60),
    (N'TipoContenido', N'Folleto', N'Folleto digital', 70),
    (N'TipoContenido', N'ComunicacionInterna', N'Recurso de comunicación interna', 80),

    (N'CanalUso', N'RedesSociales', N'Redes sociales', 10),
    (N'CanalUso', N'Campanias', N'Campañas comerciales', 20),
    (N'CanalUso', N'Presentaciones', N'Presentaciones institucionales', 30),
    (N'CanalUso', N'Correo', N'Comunicaciones por correo', 40),
    (N'CanalUso', N'Reuniones', N'Reuniones comerciales', 50),
    (N'CanalUso', N'Aliados', N'Difusión con aliados estratégicos', 60),

    (N'CanalOrigen', N'Campania', N'Campaña comercial', 10),
    (N'CanalOrigen', N'Recomendacion', N'Recomendación', 20),
    (N'CanalOrigen', N'ContactoDirecto', N'Contacto directo', 30),
    (N'CanalOrigen', N'Alianza', N'Alianza estratégica', 40),
    (N'CanalOrigen', N'RedSocial', N'Redes sociales', 50),
    (N'CanalOrigen', N'Otro', N'Otro canal', 60)
) AS origen (tipoCatalogo, codigo, nombre, orden)
ON destino.tipoCatalogo = origen.tipoCatalogo AND destino.codigo = origen.codigo
WHEN NOT MATCHED THEN
    INSERT (tipoCatalogo, codigo, nombre, orden, activo)
    VALUES (origen.tipoCatalogo, origen.codigo, origen.nombre, origen.orden, 1);
GO

-- ---------------------------------------------------------------------------
-- CU-004-001 Campañas comerciales. Estados: Planificada, Activa, Finalizada,
-- Cancelada. La baja lógica (activa = 0) retira la campaña del listado activo
-- y conserva el estado que tenía.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CampaniaComercial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaniaComercial
    (
        idCampaniaComercial          INT IDENTITY(1,1) NOT NULL,
        nombre                       NVARCHAR(150)     NOT NULL,
        descripcion                  NVARCHAR(2000)    NOT NULL,
        canalDifusion                NVARCHAR(40)      NOT NULL,
        fechaInicio                  DATE              NOT NULL,
        fechaFin                     DATE              NOT NULL,
        estadoCampania               NVARCHAR(20)      NOT NULL,
        idUsuarioInternoResponsable  INT               NULL,
        fechaAlta                    DATETIME          NOT NULL CONSTRAINT DF_CampaniaComercial_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion      DATETIME          NOT NULL CONSTRAINT DF_CampaniaComercial_fechaMod DEFAULT (GETDATE()),
        fechaBaja                    DATETIME          NULL,
        activa                       BIT               NOT NULL CONSTRAINT DF_CampaniaComercial_activa DEFAULT (1),
        CONSTRAINT PK_CampaniaComercial PRIMARY KEY CLUSTERED (idCampaniaComercial),
        CONSTRAINT FK_CampaniaComercial_UsuarioInterno FOREIGN KEY (idUsuarioInternoResponsable)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_CampaniaComercial_estado CHECK (estadoCampania IN (N'Planificada', N'Activa', N'Finalizada', N'Cancelada')),
        CONSTRAINT CK_CampaniaComercial_fechas CHECK (fechaInicio <= fechaFin),
        CONSTRAINT CK_CampaniaComercial_nombre CHECK (LEN(LTRIM(RTRIM(nombre))) > 0)
    );
END
GO

-- Público objetivo de cada campaña (uno o más segmentos).
IF OBJECT_ID(N'dbo.CampaniaPublicoObjetivo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaniaPublicoObjetivo
    (
        idCampaniaComercial  INT          NOT NULL,
        codigoSegmento       NVARCHAR(40) NOT NULL,
        CONSTRAINT PK_CampaniaPublicoObjetivo PRIMARY KEY CLUSTERED (idCampaniaComercial, codigoSegmento),
        CONSTRAINT FK_CampaniaPublicoObjetivo_Campania FOREIGN KEY (idCampaniaComercial)
            REFERENCES dbo.CampaniaComercial (idCampaniaComercial) ON DELETE CASCADE
    );
END
GO

-- ---------------------------------------------------------------------------
-- CU-004-003 Alianzas estratégicas. Estados: EnEvaluacion, Activa, Pausada,
-- Finalizada, DadaDeBaja. Sin fecha de fin = vigencia abierta.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.AlianzaEstrategica', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlianzaEstrategica
    (
        idAlianzaEstrategica         INT IDENTITY(1,1) NOT NULL,
        nombreEntidad                NVARCHAR(200)     NOT NULL,
        tipoInstitucion              NVARCHAR(40)      NOT NULL,
        datosContacto                NVARCHAR(500)     NOT NULL,
        idUsuarioInternoResponsable  INT               NOT NULL,
        objetivoComercial            NVARCHAR(1000)    NOT NULL,
        alcance                      NVARCHAR(1000)    NOT NULL,
        descripcionAcuerdo           NVARCHAR(2000)    NOT NULL,
        beneficiosAsociados          NVARCHAR(2000)    NOT NULL,
        fechaInicio                  DATE              NOT NULL,
        fechaFin                     DATE              NULL,
        estadoAlianza                NVARCHAR(20)      NOT NULL,
        observaciones                NVARCHAR(2000)    NULL,
        fechaAlta                    DATETIME          NOT NULL CONSTRAINT DF_AlianzaEstrategica_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion      DATETIME          NOT NULL CONSTRAINT DF_AlianzaEstrategica_fechaMod DEFAULT (GETDATE()),
        fechaBaja                    DATETIME          NULL,
        activa                       BIT               NOT NULL CONSTRAINT DF_AlianzaEstrategica_activa DEFAULT (1),
        CONSTRAINT PK_AlianzaEstrategica PRIMARY KEY CLUSTERED (idAlianzaEstrategica),
        CONSTRAINT FK_AlianzaEstrategica_UsuarioInterno FOREIGN KEY (idUsuarioInternoResponsable)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_AlianzaEstrategica_estado CHECK (estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada', N'Finalizada', N'DadaDeBaja')),
        CONSTRAINT CK_AlianzaEstrategica_fechas CHECK (fechaFin IS NULL OR fechaInicio <= fechaFin),
        CONSTRAINT CK_AlianzaEstrategica_entidad CHECK (LEN(LTRIM(RTRIM(nombreEntidad))) > 0)
    );
END
GO

-- Seguimiento de cada alianza (interacciones, cambios de estado,
-- renovaciones, finalización y baja).
IF OBJECT_ID(N'dbo.SeguimientoAlianza', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SeguimientoAlianza
    (
        idSeguimientoAlianza       INT IDENTITY(1,1) NOT NULL,
        idAlianzaEstrategica       INT               NOT NULL,
        tipoRegistro               NVARCHAR(20)      NOT NULL,
        fechaInteraccion           DATE              NOT NULL,
        tipoInteraccion            NVARCHAR(40)      NULL,
        observacion                NVARCHAR(2000)    NOT NULL,
        avance                     NVARCHAR(1000)    NULL,
        accionPendiente            NVARCHAR(1000)    NULL,
        idUsuarioInternoAsignado   INT               NULL,
        estadoAnterior             NVARCHAR(20)      NULL,
        estadoResultante           NVARCHAR(20)      NULL,
        idUsuarioInternoRegistro   INT               NOT NULL,
        fechaRegistro              DATETIME          NOT NULL CONSTRAINT DF_SeguimientoAlianza_fecha DEFAULT (GETDATE()),
        CONSTRAINT PK_SeguimientoAlianza PRIMARY KEY CLUSTERED (idSeguimientoAlianza),
        CONSTRAINT FK_SeguimientoAlianza_Alianza FOREIGN KEY (idAlianzaEstrategica)
            REFERENCES dbo.AlianzaEstrategica (idAlianzaEstrategica),
        CONSTRAINT FK_SeguimientoAlianza_Asignado FOREIGN KEY (idUsuarioInternoAsignado)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT FK_SeguimientoAlianza_Registro FOREIGN KEY (idUsuarioInternoRegistro)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_SeguimientoAlianza_tipo CHECK (tipoRegistro IN
            (N'Seguimiento', N'CambioEstado', N'Renovacion', N'Finalizacion', N'Baja', N'Reactivacion'))
    );

    CREATE INDEX IX_SeguimientoAlianza_Alianza ON dbo.SeguimientoAlianza (idAlianzaEstrategica, fechaRegistro);
END
GO

-- Historial de vigencias (cada renovación guarda el período anterior).
IF OBJECT_ID(N'dbo.AlianzaVigencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlianzaVigencia
    (
        idAlianzaVigencia     INT IDENTITY(1,1) NOT NULL,
        idAlianzaEstrategica  INT               NOT NULL,
        fechaInicioAnterior   DATE              NOT NULL,
        fechaFinAnterior      DATE              NULL,
        fechaInicioNueva      DATE              NOT NULL,
        fechaFinNueva         DATE              NULL,
        observacion           NVARCHAR(1000)    NULL,
        idUsuarioInterno      INT               NOT NULL,
        fechaRegistro         DATETIME          NOT NULL CONSTRAINT DF_AlianzaVigencia_fecha DEFAULT (GETDATE()),
        CONSTRAINT PK_AlianzaVigencia PRIMARY KEY CLUSTERED (idAlianzaVigencia),
        CONSTRAINT FK_AlianzaVigencia_Alianza FOREIGN KEY (idAlianzaEstrategica)
            REFERENCES dbo.AlianzaEstrategica (idAlianzaEstrategica),
        CONSTRAINT FK_AlianzaVigencia_UsuarioInterno FOREIGN KEY (idUsuarioInterno)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno)
    );
END
GO

-- Una campaña puede involucrar varias alianzas y una alianza varias campañas.
IF OBJECT_ID(N'dbo.CampaniaAlianza', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaniaAlianza
    (
        idCampaniaComercial   INT      NOT NULL,
        idAlianzaEstrategica  INT      NOT NULL,
        fechaVinculo          DATETIME NOT NULL CONSTRAINT DF_CampaniaAlianza_fecha DEFAULT (GETDATE()),
        CONSTRAINT PK_CampaniaAlianza PRIMARY KEY CLUSTERED (idCampaniaComercial, idAlianzaEstrategica),
        CONSTRAINT FK_CampaniaAlianza_Campania FOREIGN KEY (idCampaniaComercial)
            REFERENCES dbo.CampaniaComercial (idCampaniaComercial),
        CONSTRAINT FK_CampaniaAlianza_Alianza FOREIGN KEY (idAlianzaEstrategica)
            REFERENCES dbo.AlianzaEstrategica (idAlianzaEstrategica)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Orígenes de usuario (canales de captación definidos por Artera). Un origen
-- puede apuntar a una campaña o a una alianza. El código se usa en los
-- enlaces de difusión (?origen=codigo).
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OrigenUsuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrigenUsuario
    (
        idOrigenUsuario          INT IDENTITY(1,1) NOT NULL,
        nombreOrigen             NVARCHAR(150)     NOT NULL,
        codigo                   VARCHAR(40)       NOT NULL,
        canalOrigen              NVARCHAR(40)      NOT NULL,
        descripcion              NVARCHAR(1000)    NULL,
        idCampaniaComercial      INT               NULL,
        idAlianzaEstrategica     INT               NULL,
        visibleEnRegistro        BIT               NOT NULL CONSTRAINT DF_OrigenUsuario_visible DEFAULT (0),
        fechaRegistro            DATETIME          NOT NULL CONSTRAINT DF_OrigenUsuario_fecha DEFAULT (GETDATE()),
        fechaUltimaModificacion  DATETIME          NOT NULL CONSTRAINT DF_OrigenUsuario_fechaMod DEFAULT (GETDATE()),
        activo                   BIT               NOT NULL CONSTRAINT DF_OrigenUsuario_activo DEFAULT (1),
        CONSTRAINT PK_OrigenUsuario PRIMARY KEY CLUSTERED (idOrigenUsuario),
        CONSTRAINT UQ_OrigenUsuario_codigo UNIQUE (codigo),
        CONSTRAINT FK_OrigenUsuario_Campania FOREIGN KEY (idCampaniaComercial)
            REFERENCES dbo.CampaniaComercial (idCampaniaComercial),
        CONSTRAINT FK_OrigenUsuario_Alianza FOREIGN KEY (idAlianzaEstrategica)
            REFERENCES dbo.AlianzaEstrategica (idAlianzaEstrategica),
        CONSTRAINT CK_OrigenUsuario_codigo CHECK (codigo NOT LIKE '%[^a-z0-9-]%' AND LEN(codigo) >= 2)
    );
END
GO

MERGE dbo.OrigenUsuario AS destino
USING (VALUES
    (N'Redes sociales', 'redes-sociales', N'RedSocial', N'Llegó por Instagram, Facebook u otra red social.', 10),
    (N'Recomendación de alguien', 'recomendacion', N'Recomendacion', N'Se lo recomendó otra persona.', 20),
    (N'Contacto directo del equipo', 'contacto-directo', N'ContactoDirecto', N'El equipo de StageUp lo contactó.', 30),
    (N'Búsqueda en internet', 'busqueda-internet', N'Otro', N'Encontró StageUp buscando en internet.', 40),
    (N'Otro', 'otro', N'Otro', NULL, 50)
) AS origen (nombreOrigen, codigo, canalOrigen, descripcion, orden)
ON destino.codigo = origen.codigo
WHEN NOT MATCHED THEN
    INSERT (nombreOrigen, codigo, canalOrigen, descripcion, visibleEnRegistro, activo)
    VALUES (origen.nombreOrigen, origen.codigo, origen.canalOrigen, origen.descripcion, 1, 1);
GO

-- Origen de incorporación de cada usuario externo (CU-004-004).
IF COL_LENGTH(N'dbo.UsuarioExterno', N'idOrigenUsuario') IS NULL
BEGIN
    ALTER TABLE dbo.UsuarioExterno ADD idOrigenUsuario INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioExterno_OrigenUsuario')
BEGIN
    ALTER TABLE dbo.UsuarioExterno WITH CHECK
        ADD CONSTRAINT FK_UsuarioExterno_OrigenUsuario FOREIGN KEY (idOrigenUsuario)
        REFERENCES dbo.OrigenUsuario (idOrigenUsuario);
END
GO

-- ---------------------------------------------------------------------------
-- CU-004-002 Contactos comerciales. Estados: Pendiente, Contactado,
-- Interesado, Rechazado, Incorporado. Un contacto no es un usuario de
-- StageUp; al incorporarse se lo puede asociar con su cuenta.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ContactoComercial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactoComercial
    (
        idContactoComercial      INT IDENTITY(1,1) NOT NULL,
        nombreContacto           NVARCHAR(150)     NOT NULL,
        apellidoContacto         NVARCHAR(150)     NULL,
        entidadRepresentada      NVARCHAR(200)     NULL,
        tipoEntidad              NVARCHAR(40)      NOT NULL,
        medioContacto            NVARCHAR(40)      NOT NULL,
        correoElectronico        NVARCHAR(254)     NULL,
        telefono                 NVARCHAR(40)      NULL,
        telefonoNormalizado      VARCHAR(20)       NULL,
        otroDatoContacto         NVARCHAR(300)     NULL,
        idOrigenUsuario          INT               NOT NULL,
        observaciones            NVARCHAR(2000)    NOT NULL,
        estadoContacto           NVARCHAR(20)      NOT NULL,
        idCampaniaComercial      INT               NULL,
        idAlianzaEstrategica     INT               NULL,
        idUsuarioExterno         INT               NULL,
        fechaIncorporacion       DATETIME          NULL,
        idUsuarioInternoAlta     INT               NULL,
        fechaAlta                DATETIME          NOT NULL CONSTRAINT DF_ContactoComercial_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion  DATETIME          NOT NULL CONSTRAINT DF_ContactoComercial_fechaMod DEFAULT (GETDATE()),
        activo                   BIT               NOT NULL CONSTRAINT DF_ContactoComercial_activo DEFAULT (1),
        CONSTRAINT PK_ContactoComercial PRIMARY KEY CLUSTERED (idContactoComercial),
        CONSTRAINT FK_ContactoComercial_Origen FOREIGN KEY (idOrigenUsuario)
            REFERENCES dbo.OrigenUsuario (idOrigenUsuario),
        CONSTRAINT FK_ContactoComercial_Campania FOREIGN KEY (idCampaniaComercial)
            REFERENCES dbo.CampaniaComercial (idCampaniaComercial),
        CONSTRAINT FK_ContactoComercial_Alianza FOREIGN KEY (idAlianzaEstrategica)
            REFERENCES dbo.AlianzaEstrategica (idAlianzaEstrategica),
        CONSTRAINT FK_ContactoComercial_UsuarioExterno FOREIGN KEY (idUsuarioExterno)
            REFERENCES dbo.UsuarioExterno (idUsuarioExterno),
        CONSTRAINT FK_ContactoComercial_UsuarioInterno FOREIGN KEY (idUsuarioInternoAlta)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_ContactoComercial_estado CHECK (estadoContacto IN
            (N'Pendiente', N'Contactado', N'Interesado', N'Rechazado', N'Incorporado')),
        CONSTRAINT CK_ContactoComercial_usuario CHECK (idUsuarioExterno IS NULL OR estadoContacto = N'Incorporado'),
        CONSTRAINT CK_ContactoComercial_nombre CHECK (LEN(LTRIM(RTRIM(nombreContacto))) > 0)
    );

    CREATE INDEX IX_ContactoComercial_Correo ON dbo.ContactoComercial (correoElectronico);
    CREATE INDEX IX_ContactoComercial_Telefono ON dbo.ContactoComercial (telefonoNormalizado);
END
GO

-- Seguimiento de cada contacto (interacciones y cambios de estado).
IF OBJECT_ID(N'dbo.SeguimientoContacto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SeguimientoContacto
    (
        idSeguimientoContacto        INT IDENTITY(1,1) NOT NULL,
        idContactoComercial          INT               NOT NULL,
        fechaSeguimiento             DATE              NOT NULL,
        tipoSeguimiento              NVARCHAR(40)      NOT NULL,
        descripcion                  NVARCHAR(2000)    NOT NULL,
        resultado                    NVARCHAR(1000)    NULL,
        interesManifestado           NVARCHAR(1000)    NULL,
        proximaAccion                NVARCHAR(1000)    NULL,
        fechaProximaAccion           DATE              NULL,
        estadoAnterior               NVARCHAR(20)      NULL,
        estadoResultante             NVARCHAR(20)      NULL,
        idCampaniaComercial          INT               NULL,
        idUsuarioInternoResponsable  INT               NOT NULL,
        fechaRegistro                DATETIME          NOT NULL CONSTRAINT DF_SeguimientoContacto_fecha DEFAULT (GETDATE()),
        activo                       BIT               NOT NULL CONSTRAINT DF_SeguimientoContacto_activo DEFAULT (1),
        CONSTRAINT PK_SeguimientoContacto PRIMARY KEY CLUSTERED (idSeguimientoContacto),
        CONSTRAINT FK_SeguimientoContacto_Contacto FOREIGN KEY (idContactoComercial)
            REFERENCES dbo.ContactoComercial (idContactoComercial),
        CONSTRAINT FK_SeguimientoContacto_Campania FOREIGN KEY (idCampaniaComercial)
            REFERENCES dbo.CampaniaComercial (idCampaniaComercial),
        CONSTRAINT FK_SeguimientoContacto_UsuarioInterno FOREIGN KEY (idUsuarioInternoResponsable)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno)
    );

    CREATE INDEX IX_SeguimientoContacto_Contacto ON dbo.SeguimientoContacto (idContactoComercial, fechaRegistro);
END
GO

-- ---------------------------------------------------------------------------
-- CU-004-005 Contenido promocional. Estados: Activo, EnRevision,
-- Desactualizado, DadoDeBaja. Lleva un archivo (guardado fuera del sitio
-- público) o un enlace.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ContenidoPromocional', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContenidoPromocional
    (
        idContenidoPromocional       INT IDENTITY(1,1) NOT NULL,
        titulo                       NVARCHAR(200)     NOT NULL,
        descripcion                  NVARCHAR(2000)    NULL,
        tipoContenido                NVARCHAR(40)      NOT NULL,
        canalPublicacion             NVARCHAR(40)      NOT NULL,
        idCampaniaComercial          INT               NULL,
        idAlianzaEstrategica         INT               NULL,
        estadoContenido              NVARCHAR(20)      NOT NULL,
        urlContenido                 NVARCHAR(1000)    NULL,
        rutaArchivo                  NVARCHAR(260)     NULL,
        nombreArchivoOriginal        NVARCHAR(260)     NULL,
        tipoMimeArchivo              NVARCHAR(100)     NULL,
        tamanioArchivo               INT               NULL,
        idUsuarioInternoResponsable  INT               NULL,
        fechaAlta                    DATETIME          NOT NULL CONSTRAINT DF_ContenidoPromocional_fechaAlta DEFAULT (GETDATE()),
        fechaUltimaModificacion      DATETIME          NOT NULL CONSTRAINT DF_ContenidoPromocional_fechaMod DEFAULT (GETDATE()),
        fechaBaja                    DATETIME          NULL,
        activo                       BIT               NOT NULL CONSTRAINT DF_ContenidoPromocional_activo DEFAULT (1),
        CONSTRAINT PK_ContenidoPromocional PRIMARY KEY CLUSTERED (idContenidoPromocional),
        CONSTRAINT FK_ContenidoPromocional_Campania FOREIGN KEY (idCampaniaComercial)
            REFERENCES dbo.CampaniaComercial (idCampaniaComercial),
        CONSTRAINT FK_ContenidoPromocional_Alianza FOREIGN KEY (idAlianzaEstrategica)
            REFERENCES dbo.AlianzaEstrategica (idAlianzaEstrategica),
        CONSTRAINT FK_ContenidoPromocional_UsuarioInterno FOREIGN KEY (idUsuarioInternoResponsable)
            REFERENCES dbo.UsuarioInterno (idUsuarioInterno),
        CONSTRAINT CK_ContenidoPromocional_estado CHECK (estadoContenido IN (N'Activo', N'EnRevision', N'Desactualizado', N'DadoDeBaja')),
        CONSTRAINT CK_ContenidoPromocional_recurso CHECK (urlContenido IS NOT NULL OR rutaArchivo IS NOT NULL),
        CONSTRAINT CK_ContenidoPromocional_titulo CHECK (LEN(LTRIM(RTRIM(titulo))) > 0)
    );
END
GO

-- ===========================================================================
-- Procedimientos: catálogos
-- ===========================================================================
IF OBJECT_ID('dbo.sp_CatalogoComercial_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CatalogoComercial_Listar;
GO
CREATE PROCEDURE dbo.sp_CatalogoComercial_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tipoCatalogo, codigo, nombre, orden
    FROM dbo.CatalogoComercial
    WHERE activo = 1
    ORDER BY tipoCatalogo, orden, nombre;
END
GO

-- ===========================================================================
-- Procedimientos: orígenes de usuario
-- ===========================================================================
IF OBJECT_ID('dbo.sp_OrigenUsuario_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OrigenUsuario_Listar;
GO
CREATE PROCEDURE dbo.sp_OrigenUsuario_Listar
    @soloActivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.idOrigenUsuario, o.nombreOrigen, o.codigo, o.canalOrigen, o.descripcion,
           o.idCampaniaComercial, c.nombre AS nombreCampania,
           o.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           o.visibleEnRegistro, o.fechaRegistro, o.fechaUltimaModificacion, o.activo,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u WHERE u.idOrigenUsuario = o.idOrigenUsuario) AS cantidadUsuarios,
           (SELECT COUNT(*) FROM dbo.ContactoComercial ct WHERE ct.idOrigenUsuario = o.idOrigenUsuario) AS cantidadContactos
    FROM dbo.OrigenUsuario o
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = o.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = o.idAlianzaEstrategica
    WHERE @soloActivos = 0 OR o.activo = 1
    ORDER BY o.activo DESC, o.nombreOrigen;
END
GO

IF OBJECT_ID('dbo.sp_OrigenUsuario_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OrigenUsuario_Obtener;
GO
CREATE PROCEDURE dbo.sp_OrigenUsuario_Obtener
    @idOrigenUsuario INT = NULL,
    @codigo          VARCHAR(40) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 o.idOrigenUsuario, o.nombreOrigen, o.codigo, o.canalOrigen, o.descripcion,
           o.idCampaniaComercial, c.nombre AS nombreCampania,
           o.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           o.visibleEnRegistro, o.fechaRegistro, o.fechaUltimaModificacion, o.activo,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u WHERE u.idOrigenUsuario = o.idOrigenUsuario) AS cantidadUsuarios,
           (SELECT COUNT(*) FROM dbo.ContactoComercial ct WHERE ct.idOrigenUsuario = o.idOrigenUsuario) AS cantidadContactos
    FROM dbo.OrigenUsuario o
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = o.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = o.idAlianzaEstrategica
    WHERE (@idOrigenUsuario IS NOT NULL AND o.idOrigenUsuario = @idOrigenUsuario)
       OR (@idOrigenUsuario IS NULL AND @codigo IS NOT NULL AND o.codigo = @codigo);
END
GO

IF OBJECT_ID('dbo.sp_OrigenUsuario_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OrigenUsuario_Guardar;
GO
-- Alta (@idOrigenUsuario NULL) o modificación. THROW 51300 si el código ya
-- lo usa otro origen; 51301 si la campaña o la alianza no existen.
CREATE PROCEDURE dbo.sp_OrigenUsuario_Guardar
    @idOrigenUsuario       INT = NULL,
    @nombreOrigen          NVARCHAR(150),
    @codigo                VARCHAR(40),
    @canalOrigen           NVARCHAR(40),
    @descripcion           NVARCHAR(1000) = NULL,
    @idCampaniaComercial   INT = NULL,
    @idAlianzaEstrategica  INT = NULL,
    @visibleEnRegistro     BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.OrigenUsuario WHERE codigo = @codigo
               AND (@idOrigenUsuario IS NULL OR idOrigenUsuario <> @idOrigenUsuario))
        THROW 51300, 'El código ya lo usa otro origen.', 1;

    IF @idCampaniaComercial IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.CampaniaComercial WHERE idCampaniaComercial = @idCampaniaComercial AND activa = 1)
        THROW 51301, 'La campaña no está disponible.', 1;

    IF @idAlianzaEstrategica IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.AlianzaEstrategica WHERE idAlianzaEstrategica = @idAlianzaEstrategica AND activa = 1)
        THROW 51301, 'La alianza no está disponible.', 1;

    IF @idOrigenUsuario IS NULL
    BEGIN
        INSERT INTO dbo.OrigenUsuario
            (nombreOrigen, codigo, canalOrigen, descripcion, idCampaniaComercial, idAlianzaEstrategica, visibleEnRegistro)
        VALUES
            (@nombreOrigen, @codigo, @canalOrigen, @descripcion, @idCampaniaComercial, @idAlianzaEstrategica, @visibleEnRegistro);

        SELECT CAST(SCOPE_IDENTITY() AS INT);
        RETURN;
    END

    UPDATE dbo.OrigenUsuario
    SET nombreOrigen = @nombreOrigen,
        codigo = @codigo,
        canalOrigen = @canalOrigen,
        descripcion = @descripcion,
        idCampaniaComercial = @idCampaniaComercial,
        idAlianzaEstrategica = @idAlianzaEstrategica,
        visibleEnRegistro = @visibleEnRegistro,
        fechaUltimaModificacion = GETDATE()
    WHERE idOrigenUsuario = @idOrigenUsuario;

    SELECT @idOrigenUsuario;
END
GO

IF OBJECT_ID('dbo.sp_OrigenUsuario_CambiarActivo', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_OrigenUsuario_CambiarActivo;
GO
CREATE PROCEDURE dbo.sp_OrigenUsuario_CambiarActivo
    @idOrigenUsuario INT,
    @activo          BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrigenUsuario
    SET activo = @activo,
        fechaUltimaModificacion = GETDATE()
    WHERE idOrigenUsuario = @idOrigenUsuario;

    SELECT @@ROWCOUNT;
END
GO

-- Asigna el origen a un usuario externo. Con @soloSiVacio = 1 no pisa un
-- origen ya registrado (el primero que se informa es el de incorporación).
IF OBJECT_ID('dbo.sp_UsuarioExterno_AsignarOrigen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_AsignarOrigen;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_AsignarOrigen
    @idUsuarioExterno  INT,
    @idOrigenUsuario   INT,
    @soloSiVacio       BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE u
    SET u.idOrigenUsuario = @idOrigenUsuario
    FROM dbo.UsuarioExterno u
    WHERE u.idUsuarioExterno = @idUsuarioExterno
      AND (@soloSiVacio = 0 OR u.idOrigenUsuario IS NULL)
      AND EXISTS (SELECT 1 FROM dbo.OrigenUsuario o WHERE o.idOrigenUsuario = @idOrigenUsuario AND o.activo = 1);

    SELECT @@ROWCOUNT;
END
GO

-- ===========================================================================
-- Procedimientos: opciones para vincular (formularios de campañas,
-- contactos, alianzas y contenido promocional)
-- ===========================================================================
IF OBJECT_ID('dbo.sp_Comercial_ListarParaVincular', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Comercial_ListarParaVincular;
GO
-- @tipo: Campania, Alianza, Contacto, Contenido, Origen. Devuelve solo lo
-- que está disponible para asociar.
CREATE PROCEDURE dbo.sp_Comercial_ListarParaVincular
    @tipo NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF @tipo = N'Campania'
        SELECT idCampaniaComercial AS id, nombre, estadoCampania AS estado,
               CONVERT(NVARCHAR(10), fechaInicio, 103) + N' al ' + CONVERT(NVARCHAR(10), fechaFin, 103) AS detalle,
               CAST(NULL AS INT) AS idCampaniaActual
        FROM dbo.CampaniaComercial
        WHERE activa = 1 AND estadoCampania <> N'Cancelada'
        ORDER BY nombre;
    ELSE IF @tipo = N'Alianza'
        SELECT idAlianzaEstrategica AS id, nombreEntidad AS nombre, estadoAlianza AS estado,
               CAST(NULL AS NVARCHAR(200)) AS detalle, CAST(NULL AS INT) AS idCampaniaActual
        FROM dbo.AlianzaEstrategica
        WHERE activa = 1 AND estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada')
        ORDER BY nombreEntidad;
    ELSE IF @tipo = N'Contacto'
        SELECT ct.idContactoComercial AS id,
               ct.nombreContacto + ISNULL(N' ' + ct.apellidoContacto, N'') AS nombre,
               ct.estadoContacto AS estado,
               ct.entidadRepresentada AS detalle,
               ct.idCampaniaComercial AS idCampaniaActual
        FROM dbo.ContactoComercial ct
        WHERE ct.activo = 1
        ORDER BY ct.nombreContacto, ct.apellidoContacto;
    ELSE IF @tipo = N'Contenido'
        SELECT idContenidoPromocional AS id, titulo AS nombre, estadoContenido AS estado,
               CAST(NULL AS NVARCHAR(200)) AS detalle, idCampaniaComercial AS idCampaniaActual
        FROM dbo.ContenidoPromocional
        WHERE activo = 1 AND estadoContenido <> N'DadoDeBaja'
        ORDER BY titulo;
    ELSE IF @tipo = N'Origen'
        SELECT idOrigenUsuario AS id, nombreOrigen AS nombre, canalOrigen AS estado,
               CAST(codigo AS NVARCHAR(200)) AS detalle, idCampaniaComercial AS idCampaniaActual
        FROM dbo.OrigenUsuario
        WHERE activo = 1
        ORDER BY nombreOrigen;
END
GO

-- ===========================================================================
-- Procedimientos: campañas comerciales (CU-004-001)
-- ===========================================================================
IF OBJECT_ID('dbo.sp_CampaniaComercial_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_Listar;
GO
-- @vista: Activas (sin las dadas de baja), Bajas o Todas.
-- Vigencia: campañas cuyo período se cruza con [@desde, @hasta].
CREATE PROCEDURE dbo.sp_CampaniaComercial_Listar
    @vista          NVARCHAR(10) = N'Activas',
    @estado         NVARCHAR(20) = NULL,
    @canalDifusion  NVARCHAR(40) = NULL,
    @segmento       NVARCHAR(40) = NULL,
    @desde          DATE = NULL,
    @hasta          DATE = NULL,
    @texto          NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.idCampaniaComercial, c.nombre, c.descripcion, c.canalDifusion, c.fechaInicio, c.fechaFin,
           c.estadoCampania, c.idUsuarioInternoResponsable,
           ui.nombre + N' ' + ui.apellido AS nombreResponsable,
           c.fechaAlta, c.fechaUltimaModificacion, c.fechaBaja, c.activa,
           STUFF((SELECT N',' + p.codigoSegmento
                  FROM dbo.CampaniaPublicoObjetivo p
                  WHERE p.idCampaniaComercial = c.idCampaniaComercial
                  ORDER BY p.codigoSegmento
                  FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(1000)'), 1, 1, N'') AS publicoObjetivo,
           (SELECT COUNT(*) FROM dbo.ContactoComercial ct WHERE ct.idCampaniaComercial = c.idCampaniaComercial AND ct.activo = 1) AS cantidadContactos,
           (SELECT COUNT(*) FROM dbo.CampaniaAlianza ca WHERE ca.idCampaniaComercial = c.idCampaniaComercial) AS cantidadAlianzas,
           (SELECT COUNT(*) FROM dbo.ContenidoPromocional cp WHERE cp.idCampaniaComercial = c.idCampaniaComercial AND cp.activo = 1) AS cantidadContenidos,
           (SELECT COUNT(*) FROM dbo.OrigenUsuario o WHERE o.idCampaniaComercial = c.idCampaniaComercial) AS cantidadOrigenes
    FROM dbo.CampaniaComercial c
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = c.idUsuarioInternoResponsable
    WHERE (@vista = N'Todas' OR (@vista = N'Activas' AND c.activa = 1) OR (@vista = N'Bajas' AND c.activa = 0))
      AND (@estado IS NULL OR c.estadoCampania = @estado)
      AND (@canalDifusion IS NULL OR c.canalDifusion = @canalDifusion)
      AND (@segmento IS NULL OR EXISTS (SELECT 1 FROM dbo.CampaniaPublicoObjetivo p
                                        WHERE p.idCampaniaComercial = c.idCampaniaComercial AND p.codigoSegmento = @segmento))
      AND (@desde IS NULL OR c.fechaFin >= @desde)
      AND (@hasta IS NULL OR c.fechaInicio <= @hasta)
      AND (@texto IS NULL OR c.nombre LIKE N'%' + @texto + N'%' OR c.descripcion LIKE N'%' + @texto + N'%')
    ORDER BY c.activa DESC,
             CASE c.estadoCampania WHEN N'Activa' THEN 1 WHEN N'Planificada' THEN 2 WHEN N'Finalizada' THEN 3 ELSE 4 END,
             c.fechaInicio DESC, c.nombre;
END
GO

IF OBJECT_ID('dbo.sp_CampaniaComercial_Contar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_Contar;
GO
CREATE PROCEDURE dbo.sp_CampaniaComercial_Contar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS total,
           SUM(CASE WHEN activa = 1 THEN 1 ELSE 0 END) AS activas,
           SUM(CASE WHEN activa = 0 THEN 1 ELSE 0 END) AS bajas
    FROM dbo.CampaniaComercial;
END
GO

IF OBJECT_ID('dbo.sp_CampaniaComercial_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_Obtener;
GO
CREATE PROCEDURE dbo.sp_CampaniaComercial_Obtener
    @idCampaniaComercial INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.idCampaniaComercial, c.nombre, c.descripcion, c.canalDifusion, c.fechaInicio, c.fechaFin,
           c.estadoCampania, c.idUsuarioInternoResponsable,
           ui.nombre + N' ' + ui.apellido AS nombreResponsable,
           c.fechaAlta, c.fechaUltimaModificacion, c.fechaBaja, c.activa,
           STUFF((SELECT N',' + p.codigoSegmento
                  FROM dbo.CampaniaPublicoObjetivo p
                  WHERE p.idCampaniaComercial = c.idCampaniaComercial
                  ORDER BY p.codigoSegmento
                  FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(1000)'), 1, 1, N'') AS publicoObjetivo,
           (SELECT COUNT(*) FROM dbo.ContactoComercial ct WHERE ct.idCampaniaComercial = c.idCampaniaComercial AND ct.activo = 1) AS cantidadContactos,
           (SELECT COUNT(*) FROM dbo.CampaniaAlianza ca WHERE ca.idCampaniaComercial = c.idCampaniaComercial) AS cantidadAlianzas,
           (SELECT COUNT(*) FROM dbo.ContenidoPromocional cp WHERE cp.idCampaniaComercial = c.idCampaniaComercial AND cp.activo = 1) AS cantidadContenidos,
           (SELECT COUNT(*) FROM dbo.OrigenUsuario o WHERE o.idCampaniaComercial = c.idCampaniaComercial) AS cantidadOrigenes
    FROM dbo.CampaniaComercial c
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = c.idUsuarioInternoResponsable
    WHERE c.idCampaniaComercial = @idCampaniaComercial;
END
GO

IF OBJECT_ID('dbo.sp_CampaniaComercial_ListarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_ListarVinculos;
GO
-- Contactos, alianzas, contenido promocional y orígenes de una campaña, con
-- la cantidad de usuarios que llegaron por cada origen.
CREATE PROCEDURE dbo.sp_CampaniaComercial_ListarVinculos
    @idCampaniaComercial INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'Contacto' AS tipo, ct.idContactoComercial AS id,
           ct.nombreContacto + ISNULL(N' ' + ct.apellidoContacto, N'') AS nombre,
           ct.estadoContacto AS estado, CAST(NULL AS INT) AS cantidad
    FROM dbo.ContactoComercial ct
    WHERE ct.idCampaniaComercial = @idCampaniaComercial AND ct.activo = 1
    UNION ALL
    SELECT N'Alianza', a.idAlianzaEstrategica, a.nombreEntidad, a.estadoAlianza, NULL
    FROM dbo.CampaniaAlianza ca
    INNER JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = ca.idAlianzaEstrategica
    WHERE ca.idCampaniaComercial = @idCampaniaComercial
    UNION ALL
    SELECT N'Contenido', cp.idContenidoPromocional, cp.titulo, cp.estadoContenido, NULL
    FROM dbo.ContenidoPromocional cp
    WHERE cp.idCampaniaComercial = @idCampaniaComercial AND cp.activo = 1
    UNION ALL
    SELECT N'Origen', o.idOrigenUsuario, o.nombreOrigen, CASE WHEN o.activo = 1 THEN N'Activo' ELSE N'Inactivo' END,
           (SELECT COUNT(*) FROM dbo.UsuarioExterno u WHERE u.idOrigenUsuario = o.idOrigenUsuario)
    FROM dbo.OrigenUsuario o
    WHERE o.idCampaniaComercial = @idCampaniaComercial
    ORDER BY 1, 3;
END
GO

-- Vincula la campaña con los ids indicados (listas separadas por coma) y
-- desvincula los que ya no están. THROW 51302 si algún vínculo no está
-- disponible (A16).
IF OBJECT_ID('dbo.sp_CampaniaComercial_GuardarVinculos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_GuardarVinculos;
GO
CREATE PROCEDURE dbo.sp_CampaniaComercial_GuardarVinculos
    @idCampaniaComercial  INT,
    @idsContactos         NVARCHAR(MAX) = NULL,
    @idsAlianzas          NVARCHAR(MAX) = NULL,
    @idsContenidos        NVARCHAR(MAX) = NULL,
    @idsOrigenes          NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @contactos TABLE (id INT PRIMARY KEY);
    DECLARE @alianzas TABLE (id INT PRIMARY KEY);
    DECLARE @contenidos TABLE (id INT PRIMARY KEY);
    DECLARE @origenes TABLE (id INT PRIMARY KEY);

    INSERT INTO @contactos SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsContactos, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @alianzas SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsAlianzas, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @contenidos SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsContenidos, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;
    INSERT INTO @origenes SELECT DISTINCT TRY_CAST(value AS INT) FROM STRING_SPLIT(ISNULL(@idsOrigenes, N''), N',') WHERE TRY_CAST(value AS INT) IS NOT NULL;

    -- Lo nuevo tiene que estar disponible; lo que ya estaba vinculado se
    -- puede conservar aunque haya cambiado de estado.
    IF EXISTS (SELECT 1 FROM @contactos l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ContactoComercial ct WHERE ct.idContactoComercial = l.id
                                 AND (ct.activo = 1 OR ct.idCampaniaComercial = @idCampaniaComercial)))
       OR EXISTS (SELECT 1 FROM @alianzas l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.AlianzaEstrategica a WHERE a.idAlianzaEstrategica = l.id
                                 AND ((a.activa = 1 AND a.estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada'))
                                      OR EXISTS (SELECT 1 FROM dbo.CampaniaAlianza ca WHERE ca.idCampaniaComercial = @idCampaniaComercial
                                                 AND ca.idAlianzaEstrategica = a.idAlianzaEstrategica))))
       OR EXISTS (SELECT 1 FROM @contenidos l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ContenidoPromocional cp WHERE cp.idContenidoPromocional = l.id
                                 AND ((cp.activo = 1 AND cp.estadoContenido <> N'DadoDeBaja') OR cp.idCampaniaComercial = @idCampaniaComercial)))
       OR EXISTS (SELECT 1 FROM @origenes l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.OrigenUsuario o WHERE o.idOrigenUsuario = l.id
                                 AND (o.activo = 1 OR o.idCampaniaComercial = @idCampaniaComercial)))
        THROW 51302, 'Un vínculo comercial no está disponible.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.ContactoComercial SET idCampaniaComercial = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial AND idContactoComercial NOT IN (SELECT id FROM @contactos);
        UPDATE ct SET ct.idCampaniaComercial = @idCampaniaComercial, ct.fechaUltimaModificacion = GETDATE()
        FROM dbo.ContactoComercial ct INNER JOIN @contactos l ON l.id = ct.idContactoComercial
        WHERE ISNULL(ct.idCampaniaComercial, 0) <> @idCampaniaComercial;

        DELETE FROM dbo.CampaniaAlianza
        WHERE idCampaniaComercial = @idCampaniaComercial AND idAlianzaEstrategica NOT IN (SELECT id FROM @alianzas);
        INSERT INTO dbo.CampaniaAlianza (idCampaniaComercial, idAlianzaEstrategica)
        SELECT @idCampaniaComercial, l.id FROM @alianzas l
        WHERE NOT EXISTS (SELECT 1 FROM dbo.CampaniaAlianza ca WHERE ca.idCampaniaComercial = @idCampaniaComercial AND ca.idAlianzaEstrategica = l.id);

        UPDATE dbo.ContenidoPromocional SET idCampaniaComercial = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial AND idContenidoPromocional NOT IN (SELECT id FROM @contenidos);
        UPDATE cp SET cp.idCampaniaComercial = @idCampaniaComercial, cp.fechaUltimaModificacion = GETDATE()
        FROM dbo.ContenidoPromocional cp INNER JOIN @contenidos l ON l.id = cp.idContenidoPromocional
        WHERE ISNULL(cp.idCampaniaComercial, 0) <> @idCampaniaComercial;

        UPDATE dbo.OrigenUsuario SET idCampaniaComercial = NULL, fechaUltimaModificacion = GETDATE()
        WHERE idCampaniaComercial = @idCampaniaComercial AND idOrigenUsuario NOT IN (SELECT id FROM @origenes);
        UPDATE o SET o.idCampaniaComercial = @idCampaniaComercial, o.fechaUltimaModificacion = GETDATE()
        FROM dbo.OrigenUsuario o INNER JOIN @origenes l ON l.id = o.idOrigenUsuario
        WHERE ISNULL(o.idCampaniaComercial, 0) <> @idCampaniaComercial;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_CampaniaComercial_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_Guardar;
GO
-- Alta (@idCampaniaComercial NULL) o modificación de los datos de la
-- campaña y su público objetivo (lista de segmentos separados por coma).
-- THROW 51303 si la campaña no existe o está dada de baja.
CREATE PROCEDURE dbo.sp_CampaniaComercial_Guardar
    @idCampaniaComercial          INT = NULL,
    @nombre                       NVARCHAR(150),
    @descripcion                  NVARCHAR(2000),
    @canalDifusion                NVARCHAR(40),
    @fechaInicio                  DATE,
    @fechaFin                     DATE,
    @estadoCampania               NVARCHAR(20),
    @publicoObjetivo              NVARCHAR(1000),
    @idUsuarioInternoResponsable  INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @idCampaniaComercial IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.CampaniaComercial WHERE idCampaniaComercial = @idCampaniaComercial AND activa = 1)
        THROW 51303, 'La campaña no existe o está dada de baja.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @idCampaniaComercial IS NULL
        BEGIN
            INSERT INTO dbo.CampaniaComercial
                (nombre, descripcion, canalDifusion, fechaInicio, fechaFin, estadoCampania, idUsuarioInternoResponsable)
            VALUES
                (@nombre, @descripcion, @canalDifusion, @fechaInicio, @fechaFin, @estadoCampania, @idUsuarioInternoResponsable);

            SET @idCampaniaComercial = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.CampaniaComercial
            SET nombre = @nombre,
                descripcion = @descripcion,
                canalDifusion = @canalDifusion,
                fechaInicio = @fechaInicio,
                fechaFin = @fechaFin,
                estadoCampania = @estadoCampania,
                fechaUltimaModificacion = GETDATE()
            WHERE idCampaniaComercial = @idCampaniaComercial;
        END

        DELETE FROM dbo.CampaniaPublicoObjetivo WHERE idCampaniaComercial = @idCampaniaComercial;
        INSERT INTO dbo.CampaniaPublicoObjetivo (idCampaniaComercial, codigoSegmento)
        SELECT DISTINCT @idCampaniaComercial, LTRIM(RTRIM(value))
        FROM STRING_SPLIT(ISNULL(@publicoObjetivo, N''), N',')
        WHERE LEN(LTRIM(RTRIM(value))) > 0;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @idCampaniaComercial;
END
GO

IF OBJECT_ID('dbo.sp_CampaniaComercial_DarDeBaja', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CampaniaComercial_DarDeBaja;
GO
CREATE PROCEDURE dbo.sp_CampaniaComercial_DarDeBaja
    @idCampaniaComercial INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.CampaniaComercial
    SET activa = 0,
        fechaBaja = GETDATE(),
        fechaUltimaModificacion = GETDATE()
    WHERE idCampaniaComercial = @idCampaniaComercial
      AND activa = 1;

    SELECT @@ROWCOUNT;
END
GO

-- ===========================================================================
-- Procedimientos: contactos comerciales (CU-004-002)
-- ===========================================================================
IF OBJECT_ID('dbo.sp_ContactoComercial_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContactoComercial_Listar;
GO
CREATE PROCEDURE dbo.sp_ContactoComercial_Listar
    @estado               NVARCHAR(20) = NULL,
    @tipoEntidad          NVARCHAR(40) = NULL,
    @idOrigenUsuario      INT = NULL,
    @idCampaniaComercial  INT = NULL,
    @texto                NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ct.idContactoComercial, ct.nombreContacto, ct.apellidoContacto, ct.entidadRepresentada,
           ct.tipoEntidad, ct.medioContacto, ct.correoElectronico, ct.telefono, ct.otroDatoContacto,
           ct.idOrigenUsuario, o.nombreOrigen,
           ct.observaciones, ct.estadoContacto,
           ct.idCampaniaComercial, c.nombre AS nombreCampania,
           ct.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           ct.idUsuarioExterno, ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno, ue.correoElectronico AS correoUsuarioExterno,
           ct.fechaIncorporacion, ct.fechaAlta, ct.fechaUltimaModificacion, ct.activo,
           (SELECT COUNT(*) FROM dbo.SeguimientoContacto s WHERE s.idContactoComercial = ct.idContactoComercial AND s.activo = 1) AS cantidadSeguimientos
    FROM dbo.ContactoComercial ct
    INNER JOIN dbo.OrigenUsuario o ON o.idOrigenUsuario = ct.idOrigenUsuario
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = ct.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = ct.idAlianzaEstrategica
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = ct.idUsuarioExterno
    WHERE ct.activo = 1
      AND (@estado IS NULL OR ct.estadoContacto = @estado)
      AND (@tipoEntidad IS NULL OR ct.tipoEntidad = @tipoEntidad)
      AND (@idOrigenUsuario IS NULL OR ct.idOrigenUsuario = @idOrigenUsuario)
      AND (@idCampaniaComercial IS NULL OR ct.idCampaniaComercial = @idCampaniaComercial)
      AND (@texto IS NULL
           OR ct.nombreContacto LIKE N'%' + @texto + N'%'
           OR ct.apellidoContacto LIKE N'%' + @texto + N'%'
           OR ct.entidadRepresentada LIKE N'%' + @texto + N'%'
           OR ct.correoElectronico LIKE N'%' + @texto + N'%')
    ORDER BY ct.fechaUltimaModificacion DESC, ct.nombreContacto;
END
GO

IF OBJECT_ID('dbo.sp_ContactoComercial_Contar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContactoComercial_Contar;
GO
CREATE PROCEDURE dbo.sp_ContactoComercial_Contar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS total,
           SUM(CASE WHEN estadoContacto = N'Pendiente' THEN 1 ELSE 0 END) AS pendientes,
           SUM(CASE WHEN estadoContacto = N'Contactado' THEN 1 ELSE 0 END) AS contactados,
           SUM(CASE WHEN estadoContacto = N'Interesado' THEN 1 ELSE 0 END) AS interesados,
           SUM(CASE WHEN estadoContacto = N'Rechazado' THEN 1 ELSE 0 END) AS rechazados,
           SUM(CASE WHEN estadoContacto = N'Incorporado' THEN 1 ELSE 0 END) AS incorporados
    FROM dbo.ContactoComercial
    WHERE activo = 1;
END
GO

IF OBJECT_ID('dbo.sp_ContactoComercial_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContactoComercial_Obtener;
GO
CREATE PROCEDURE dbo.sp_ContactoComercial_Obtener
    @idContactoComercial INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ct.idContactoComercial, ct.nombreContacto, ct.apellidoContacto, ct.entidadRepresentada,
           ct.tipoEntidad, ct.medioContacto, ct.correoElectronico, ct.telefono, ct.otroDatoContacto,
           ct.idOrigenUsuario, o.nombreOrigen,
           ct.observaciones, ct.estadoContacto,
           ct.idCampaniaComercial, c.nombre AS nombreCampania,
           ct.idAlianzaEstrategica, a.nombreEntidad AS nombreAlianza,
           ct.idUsuarioExterno, ue.nombre + N' ' + ue.apellido AS nombreUsuarioExterno, ue.correoElectronico AS correoUsuarioExterno,
           ct.fechaIncorporacion, ct.fechaAlta, ct.fechaUltimaModificacion, ct.activo,
           (SELECT COUNT(*) FROM dbo.SeguimientoContacto s WHERE s.idContactoComercial = ct.idContactoComercial AND s.activo = 1) AS cantidadSeguimientos
    FROM dbo.ContactoComercial ct
    INNER JOIN dbo.OrigenUsuario o ON o.idOrigenUsuario = ct.idOrigenUsuario
    LEFT JOIN dbo.CampaniaComercial c ON c.idCampaniaComercial = ct.idCampaniaComercial
    LEFT JOIN dbo.AlianzaEstrategica a ON a.idAlianzaEstrategica = ct.idAlianzaEstrategica
    LEFT JOIN dbo.UsuarioExterno ue ON ue.idUsuarioExterno = ct.idUsuarioExterno
    WHERE ct.idContactoComercial = @idContactoComercial;
END
GO

IF OBJECT_ID('dbo.sp_ContactoComercial_BuscarDuplicado', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContactoComercial_BuscarDuplicado;
GO
-- A13: mismo correo, mismo teléfono, o mismo nombre y apellido en la misma
-- entidad.
CREATE PROCEDURE dbo.sp_ContactoComercial_BuscarDuplicado
    @idContactoExcluido   INT = NULL,
    @correoElectronico    NVARCHAR(254) = NULL,
    @telefonoNormalizado  VARCHAR(20) = NULL,
    @nombreContacto       NVARCHAR(150),
    @apellidoContacto     NVARCHAR(150) = NULL,
    @entidadRepresentada  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 ct.idContactoComercial,
           ct.nombreContacto + ISNULL(N' ' + ct.apellidoContacto, N'') AS nombre,
           CASE WHEN @correoElectronico IS NOT NULL AND ct.correoElectronico = @correoElectronico THEN N'correo'
                WHEN @telefonoNormalizado IS NOT NULL AND ct.telefonoNormalizado = @telefonoNormalizado THEN N'teléfono'
                ELSE N'nombre y entidad' END AS coincidencia
    FROM dbo.ContactoComercial ct
    WHERE ct.activo = 1
      AND (@idContactoExcluido IS NULL OR ct.idContactoComercial <> @idContactoExcluido)
      AND ((@correoElectronico IS NOT NULL AND ct.correoElectronico = @correoElectronico)
           OR (@telefonoNormalizado IS NOT NULL AND ct.telefonoNormalizado = @telefonoNormalizado)
           OR (ct.nombreContacto = @nombreContacto
               AND ISNULL(ct.apellidoContacto, N'') = ISNULL(@apellidoContacto, N'')
               AND ISNULL(ct.entidadRepresentada, N'') = ISNULL(@entidadRepresentada, N'')))
    ORDER BY ct.idContactoComercial;
END
GO

IF OBJECT_ID('dbo.sp_ContactoComercial_Guardar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ContactoComercial_Guardar;
GO
-- Alta (@idContactoComercial NULL) o modificación. Si cambia el estado,
-- queda un registro de seguimiento con el estado anterior y el nuevo (A8).
-- Al pasar a Incorporado se guarda la fecha y, si se indica la cuenta, el
-- usuario toma el origen del contacto cuando no tenía uno (A9).
-- THROW 51310 contacto inexistente, 51311 vínculo no disponible,
-- 51312 cuenta de usuario inexistente.
CREATE PROCEDURE dbo.sp_ContactoComercial_Guardar
    @idContactoComercial   INT = NULL,
    @nombreContacto        NVARCHAR(150),
    @apellidoContacto      NVARCHAR(150) = NULL,
    @entidadRepresentada   NVARCHAR(200) = NULL,
    @tipoEntidad           NVARCHAR(40),
    @medioContacto         NVARCHAR(40),
    @correoElectronico     NVARCHAR(254) = NULL,
    @telefono              NVARCHAR(40) = NULL,
    @telefonoNormalizado   VARCHAR(20) = NULL,
    @otroDatoContacto      NVARCHAR(300) = NULL,
    @idOrigenUsuario       INT,
    @observaciones         NVARCHAR(2000),
    @estadoContacto        NVARCHAR(20),
    @idCampaniaComercial   INT = NULL,
    @idAlianzaEstrategica  INT = NULL,
    @idUsuarioExterno      INT = NULL,
    @observacionCambio     NVARCHAR(2000) = NULL,
    @idUsuarioInterno      INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estadoAnterior NVARCHAR(20), @campaniaAnterior INT, @alianzaAnterior INT, @origenAnterior INT;

    IF @idContactoComercial IS NOT NULL
    BEGIN
        SELECT @estadoAnterior = estadoContacto, @campaniaAnterior = idCampaniaComercial,
               @alianzaAnterior = idAlianzaEstrategica, @origenAnterior = idOrigenUsuario
        FROM dbo.ContactoComercial
        WHERE idContactoComercial = @idContactoComercial AND activo = 1;

        IF @estadoAnterior IS NULL
            THROW 51310, 'El contacto no existe.', 1;
    END

    -- Lo que ya estaba vinculado se puede conservar; lo nuevo tiene que estar disponible.
    IF NOT EXISTS (SELECT 1 FROM dbo.OrigenUsuario WHERE idOrigenUsuario = @idOrigenUsuario
                   AND (activo = 1 OR idOrigenUsuario = ISNULL(@origenAnterior, 0)))
        THROW 51311, 'El origen no está disponible.', 1;

    IF @idCampaniaComercial IS NOT NULL AND ISNULL(@campaniaAnterior, 0) <> @idCampaniaComercial
       AND NOT EXISTS (SELECT 1 FROM dbo.CampaniaComercial WHERE idCampaniaComercial = @idCampaniaComercial
                       AND activa = 1 AND estadoCampania <> N'Cancelada')
        THROW 51311, 'La campaña no está disponible.', 1;

    IF @idAlianzaEstrategica IS NOT NULL AND ISNULL(@alianzaAnterior, 0) <> @idAlianzaEstrategica
       AND NOT EXISTS (SELECT 1 FROM dbo.AlianzaEstrategica WHERE idAlianzaEstrategica = @idAlianzaEstrategica
                       AND activa = 1 AND estadoAlianza IN (N'EnEvaluacion', N'Activa', N'Pausada'))
        THROW 51311, 'La alianza no está disponible.', 1;

    IF @idUsuarioExterno IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioExterno WHERE idUsuarioExterno = @idUsuarioExterno)
        THROW 51312, 'La cuenta de usuario no existe.', 1;

    IF @estadoContacto <> N'Incorporado'
        SET @idUsuarioExterno = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @idContactoComercial IS NULL
        BEGIN
            INSERT INTO dbo.ContactoComercial
                (nombreContacto, apellidoContacto, entidadRepresentada, tipoEntidad, medioContacto,
                 correoElectronico, telefono, telefonoNormalizado, otroDatoContacto, idOrigenUsuario,
                 observaciones, estadoContacto, idCampaniaComercial, idAlianzaEstrategica, idUsuarioExterno,
                 fechaIncorporacion, idUsuarioInternoAlta)
            VALUES
                (@nombreContacto, @apellidoContacto, @entidadRepresentada, @tipoEntidad, @medioContacto,
                 @correoElectronico, @telefono, @telefonoNormalizado, @otroDatoContacto, @idOrigenUsuario,
                 @observaciones, @estadoContacto, @idCampaniaComercial, @idAlianzaEstrategica, @idUsuarioExterno,
                 CASE WHEN @estadoContacto = N'Incorporado' THEN GETDATE() END, @idUsuarioInterno);

            SET @idContactoComercial = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.ContactoComercial
            SET nombreContacto = @nombreContacto,
                apellidoContacto = @apellidoContacto,
                entidadRepresentada = @entidadRepresentada,
                tipoEntidad = @tipoEntidad,
                medioContacto = @medioContacto,
                correoElectronico = @correoElectronico,
                telefono = @telefono,
                telefonoNormalizado = @telefonoNormalizado,
                otroDatoContacto = @otroDatoContacto,
                idOrigenUsuario = @idOrigenUsuario,
                observaciones = @observaciones,
                estadoContacto = @estadoContacto,
                idCampaniaComercial = @idCampaniaComercial,
                idAlianzaEstrategica = @idAlianzaEstrategica,
                idUsuarioExterno = @idUsuarioExterno,
                fechaIncorporacion = CASE
                    WHEN @estadoContacto <> N'Incorporado' THEN NULL
                    WHEN @estadoAnterior <> N'Incorporado' THEN GETDATE()
                    ELSE fechaIncorporacion END,
                fechaUltimaModificacion = GETDATE()
            WHERE idContactoComercial = @idContactoComercial;

            IF @estadoAnterior <> @estadoContacto
            BEGIN
                INSERT INTO dbo.SeguimientoContacto
                    (idContactoComercial, fechaSeguimiento, tipoSeguimiento, descripcion,
                     estadoAnterior, estadoResultante, idCampaniaComercial, idUsuarioInternoResponsable)
                VALUES
                    (@idContactoComercial, CAST(GETDATE() AS DATE), N'CambioEstado',
                     ISNULL(NULLIF(LTRIM(RTRIM(@observacionCambio)), N''), N'Cambio de estado desde la edición del contacto.'),
                     @estadoAnterior, @estadoContacto, @idCampaniaComercial, @idUsuarioInterno);
            END
        END

        IF @idUsuarioExterno IS NOT NULL
        BEGIN
            UPDATE dbo.UsuarioExterno
            SET idOrigenUsuario = @idOrigenUsuario
            WHERE idUsuarioExterno = @idUsuarioExterno
              AND idOrigenUsuario IS NULL;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @idContactoComercial;
END
GO

IF OBJECT_ID('dbo.sp_SeguimientoContacto_Insertar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SeguimientoContacto_Insertar;
GO
-- A7: registra la interacción y, si se indica, el nuevo estado comercial.
CREATE PROCEDURE dbo.sp_SeguimientoContacto_Insertar
    @idContactoComercial  INT,
    @fechaSeguimiento     DATE,
    @tipoSeguimiento      NVARCHAR(40),
    @descripcion          NVARCHAR(2000),
    @resultado            NVARCHAR(1000) = NULL,
    @interesManifestado   NVARCHAR(1000) = NULL,
    @proximaAccion        NVARCHAR(1000) = NULL,
    @fechaProximaAccion   DATE = NULL,
    @estadoResultante     NVARCHAR(20) = NULL,
    @idUsuarioInterno     INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estadoAnterior NVARCHAR(20), @idCampania INT;
    SELECT @estadoAnterior = estadoContacto, @idCampania = idCampaniaComercial
    FROM dbo.ContactoComercial
    WHERE idContactoComercial = @idContactoComercial AND activo = 1;

    IF @estadoAnterior IS NULL
        THROW 51310, 'El contacto no existe.', 1;

    IF @estadoResultante = @estadoAnterior
        SET @estadoResultante = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.SeguimientoContacto
            (idContactoComercial, fechaSeguimiento, tipoSeguimiento, descripcion, resultado, interesManifestado,
             proximaAccion, fechaProximaAccion, estadoAnterior, estadoResultante, idCampaniaComercial,
             idUsuarioInternoResponsable)
        VALUES
            (@idContactoComercial, @fechaSeguimiento, @tipoSeguimiento, @descripcion, @resultado, @interesManifestado,
             @proximaAccion, @fechaProximaAccion,
             CASE WHEN @estadoResultante IS NULL THEN NULL ELSE @estadoAnterior END, @estadoResultante,
             @idCampania, @idUsuarioInterno);

        UPDATE dbo.ContactoComercial
        SET estadoContacto = ISNULL(@estadoResultante, estadoContacto),
            idUsuarioExterno = CASE WHEN ISNULL(@estadoResultante, estadoContacto) = N'Incorporado' THEN idUsuarioExterno ELSE NULL END,
            fechaIncorporacion = CASE
                WHEN @estadoResultante IS NULL THEN fechaIncorporacion
                WHEN @estadoResultante = N'Incorporado' THEN GETDATE()
                ELSE NULL END,
            fechaUltimaModificacion = GETDATE()
        WHERE idContactoComercial = @idContactoComercial;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @estadoAnterior AS estadoAnterior, ISNULL(@estadoResultante, @estadoAnterior) AS estadoActual;
END
GO

IF OBJECT_ID('dbo.sp_SeguimientoContacto_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SeguimientoContacto_Listar;
GO
CREATE PROCEDURE dbo.sp_SeguimientoContacto_Listar
    @idContactoComercial INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.idSeguimientoContacto, s.idContactoComercial, s.fechaSeguimiento, s.tipoSeguimiento, s.descripcion,
           s.resultado, s.interesManifestado, s.proximaAccion, s.fechaProximaAccion,
           s.estadoAnterior, s.estadoResultante, s.idUsuarioInternoResponsable,
           ui.nombre + N' ' + ui.apellido AS nombreResponsable, s.fechaRegistro
    FROM dbo.SeguimientoContacto s
    LEFT JOIN dbo.UsuarioInterno ui ON ui.idUsuarioInterno = s.idUsuarioInternoResponsable
    WHERE s.idContactoComercial = @idContactoComercial AND s.activo = 1
    ORDER BY s.fechaRegistro DESC, s.idSeguimientoContacto DESC;
END
GO

-- A9: cuenta de StageUp con la que se asocia un contacto incorporado.
IF OBJECT_ID('dbo.sp_UsuarioExterno_BuscarParaVincular', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UsuarioExterno_BuscarParaVincular;
GO
CREATE PROCEDURE dbo.sp_UsuarioExterno_BuscarParaVincular
    @correoElectronico NVARCHAR(254)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 u.idUsuarioExterno, u.nombre, u.apellido, u.correoElectronico, u.perfilUsuario, u.estadoCuenta
    FROM dbo.UsuarioExterno u
    WHERE u.correoElectronico = @correoElectronico;
END
GO

-- ===========================================================================
-- Permisos y menú "Comercialización" (campañas y contactos; el script 81
-- suma alianzas, métricas y contenido promocional).
-- ===========================================================================
IF OBJECT_ID('tempdb..#RegistrarPermisoComercial') IS NOT NULL DROP PROCEDURE #RegistrarPermisoComercial;
GO
CREATE PROCEDURE #RegistrarPermisoComercial
    @codigo         NVARCHAR(100),
    @nombre         NVARCHAR(200),
    @descripcion    NVARCHAR(500),
    @url            NVARCHAR(300),
    @accion         NVARCHAR(50),
    @orden          INT,
    @agregarAlMenu  BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @idPermiso INT =
        (
            SELECT TOP 1 idPermisoInterno FROM dbo.PermisoInterno WHERE codigoPermiso = @codigo ORDER BY idPermisoInterno
        );

        IF @idPermiso IS NULL
        BEGIN
            INSERT INTO dbo.PermisoInterno
                (codigoPermiso, nombrePermiso, descripcion, modulo, accion, estadoPermiso, urlAsociada, activo)
            VALUES
                (@codigo, @nombre, @descripcion, N'Comercialización', @accion, N'Activo', @url, 1);

            SET @idPermiso = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE dbo.PermisoInterno
            SET nombrePermiso = @nombre, descripcion = @descripcion, modulo = N'Comercialización', accion = @accion,
                estadoPermiso = N'Activo', urlAsociada = @url, activo = 1, fechaUltimaModificacion = GETDATE()
            WHERE idPermisoInterno = @idPermiso;
        END

        DECLARE @idRolAdministrador INT =
        (
            SELECT TOP 1 idRolInterno FROM dbo.RolInterno WHERE nombreRol = N'Administrador' AND activo = 1 ORDER BY idRolInterno
        );

        IF @idRolAdministrador IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.RolInternoPermiso WHERE idRolInterno = @idRolAdministrador AND idPermisoInterno = @idPermiso)
        BEGIN
            INSERT INTO dbo.RolInternoPermiso (idRolInterno, idPermisoInterno, fechaAsignacion, activo)
            VALUES (@idRolAdministrador, @idPermiso, GETDATE(), 1);
        END
        ELSE IF @idRolAdministrador IS NOT NULL
        BEGIN
            UPDATE dbo.RolInternoPermiso SET activo = 1
            WHERE idRolInterno = @idRolAdministrador AND idPermisoInterno = @idPermiso;
        END

        DECLARE @idComponente INT = NULL;

        IF OBJECT_ID(N'dbo.ComponentePermiso', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.RolInternoComponentePermiso', N'U') IS NOT NULL
        BEGIN
            DECLARE @idGrupoComercial INT =
            (
                SELECT TOP 1 idComponentePermiso FROM dbo.ComponentePermiso
                WHERE tipoComponente = N'Grupo' AND idComponentePadre IS NULL AND nombre = N'Comercialización'
                ORDER BY idComponentePermiso
            );

            IF @idGrupoComercial IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (NULL, N'Grupo', NULL, N'Comercialización',
                     N'Campañas, contactos, alianzas, métricas y contenido promocional.', NULL, 400, 1);

                SET @idGrupoComercial = CAST(SCOPE_IDENTITY() AS INT);
            END

            SET @idComponente =
            (
                SELECT TOP 1 idComponentePermiso FROM dbo.ComponentePermiso WHERE codigoPermiso = @codigo ORDER BY idComponentePermiso
            );

            IF @idComponente IS NULL
            BEGIN
                INSERT INTO dbo.ComponentePermiso
                    (idComponentePadre, tipoComponente, codigoPermiso, nombre, descripcion, urlAsociada, orden, activo)
                VALUES
                    (@idGrupoComercial, N'Permiso', @codigo, @nombre, @descripcion, @url, @orden, 1);

                SET @idComponente = CAST(SCOPE_IDENTITY() AS INT);
            END
            ELSE
            BEGIN
                UPDATE dbo.ComponentePermiso
                SET idComponentePadre = @idGrupoComercial, tipoComponente = N'Permiso', nombre = @nombre,
                    descripcion = @descripcion, urlAsociada = @url, activo = 1, fechaUltimaModificacion = GETDATE()
                WHERE idComponentePermiso = @idComponente;
            END

            IF @idRolAdministrador IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.RolInternoComponentePermiso
                               WHERE idRolInterno = @idRolAdministrador AND idComponentePermiso = @idComponente)
                BEGIN
                    INSERT INTO dbo.RolInternoComponentePermiso (idRolInterno, idComponentePermiso, fechaAsignacion, activo)
                    VALUES (@idRolAdministrador, @idComponente, GETDATE(), 1);
                END
                ELSE
                BEGIN
                    UPDATE dbo.RolInternoComponentePermiso SET activo = 1
                    WHERE idRolInterno = @idRolAdministrador AND idComponentePermiso = @idComponente;
                END
            END
        END

        IF @agregarAlMenu = 1
           AND OBJECT_ID(N'dbo.OpcionMenu', N'U') IS NOT NULL
           AND @idComponente IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.OpcionMenu WHERE idComponentePermiso = @idComponente)
        BEGIN
            INSERT INTO dbo.OpcionMenu (texto, descripcion, url, modulo, orden, idComponentePermiso, activo)
            SELECT @nombre, @descripcion, @url, N'Comercialización', ISNULL(MAX(orden), 0) + 10, @idComponente, 1
            FROM dbo.OpcionMenu;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        DECLARE @mensajeError NVARCHAR(4000);
        SET @mensajeError = ERROR_MESSAGE();
        RAISERROR(N'%s', 16, 1, @mensajeError);
    END CATCH
END
GO

EXEC #RegistrarPermisoComercial
    @codigo = N'GESTIONAR_CAMPANIAS_COMERCIALES',
    @nombre = N'Campañas comerciales',
    @descripcion = N'Registrar, consultar, modificar y dar de baja campañas comerciales y los orígenes de usuario.',
    @url = N'~/Interno/Comercializacion/CampaniasComerciales.aspx',
    @accion = N'Modificar',
    @orden = 410;
GO

EXEC #RegistrarPermisoComercial
    @codigo = N'GESTIONAR_CONTACTOS_COMERCIALES',
    @nombre = N'Contactos comerciales',
    @descripcion = N'Registrar, consultar y actualizar contactos comerciales y su seguimiento.',
    @url = N'~/Interno/Comercializacion/ContactosComerciales.aspx',
    @accion = N'Modificar',
    @orden = 420;
GO

DROP PROCEDURE #RegistrarPermisoComercial;
GO
