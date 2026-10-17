# Guía de revisión de la segunda entrega

## 1. Preparación del entorno

1. Actualizar el repositorio en la rama `master` (`git pull`). El último commit es `6f70d7f`.
2. Abrir PowerShell en la carpeta `Database` y ejecutar `EjecutarTodosLosScripts.ps1`. El script aplica solo los que faltan, hasta el `49_PagosNotasYCuentaCorriente.sql`, y al final no tiene que mostrar errores.
3. Configurar las claves y credenciales como se explica en el punto 2.
4. Abrir la solución en Visual Studio, compilarla (Compilar → Recompilar solución) y ejecutarla.
5. Iniciar sesión con el usuario administrador. Si ya había una sesión abierta, cerrarla y volver a entrar para que se carguen los permisos nuevos (pagos, parámetros, reportes y búsqueda).
6. Para las pruebas conviene tener tres cuentas: el administrador interno, un usuario externo gestor de espacios y otro usuario externo que haga de cliente. Para que lleguen los mails, los dos usuarios externos tienen que tener correos reales.

## 2. Claves y credenciales

Todas las claves van en el archivo `StageUp.UI/AppSettings.private.config`, que nunca se sube a git.

- Si el archivo no existe, se crea copiando `StageUp.UI/AppSettings.private.config.example` en la misma carpeta y renombrando la copia a `AppSettings.private.config`.
- Cada clave se completa en el atributo `value="..."` de su línea, en una sola línea, sin espacios ni saltos de línea.
- La aplicación no detecta sola los cambios en este archivo. Después de modificarlo hay que detener la ejecución en Visual Studio (Mayús + F5) y volver a iniciarla (F5).

### 2.1 Contraseña de aplicación de Gmail (correo SMTP)

La contraseña de aplicación anterior quedó en el historial de GitHub, así que hay que revocarla y generar una nueva:

1. Entrar a https://myaccount.google.com con la cuenta de Gmail que StageUp usa para enviar los correos.
2. Ir a **Seguridad**. En "Cómo acceder a Google", confirmar que la **Verificación en dos pasos** esté activada. Sin ella, Google no permite contraseñas de aplicación.
3. Entrar a **Contraseñas de aplicaciones**: está al final de la pantalla de Verificación en dos pasos, o se abre directo en https://myaccount.google.com/apppasswords. Google puede pedir la contraseña de la cuenta otra vez.
4. **Revocar la anterior:** en la lista aparece la contraseña que usaba StageUp. Eliminarla con el ícono de tacho. Desde ese momento la que quedó en GitHub deja de servir.
5. **Generar una nueva:** escribir un nombre (por ejemplo `StageUp`) y presionar **Crear**. Google muestra un código de 16 letras en cuatro grupos, y lo muestra **una sola vez**.
6. Copiar el código sin espacios y pegarlo en el archivo:
   - `SmtpPassword`: el código nuevo.
   - `SmtpUsuario` y `CorreoRemitente`: la dirección completa de esa cuenta de Gmail.
   - `SmtpHost` = `smtp.gmail.com`, `SmtpPuerto` = `587`, `SmtpUsarSsl` = `true`.
7. Reiniciar la aplicación y verificar el envío. Por ejemplo, con "¿Olvidaste tu contraseña?" en el inicio de sesión, o con **Enviar prueba** en Novedades y newsletter.

### 2.2 reCAPTCHA

- **Para probar en la máquina local** alcanzan las claves de prueba públicas de Google. El captcha siempre se aprueba y muestra un aviso de "solo para pruebas":
  - `RecaptchaSiteKey` = `6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI`
  - `RecaptchaSecretKey` = `6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe`
- **Para claves propias**: entrar a https://www.google.com/recaptcha/admin, crear un sitio de tipo reCAPTCHA v2 ("No soy un robot"), agregar el dominio `localhost` y copiar la clave del sitio y la clave secreta.

### 2.3 Clave simétrica (AES)

Con esta clave se cifran datos guardados en la base, como el teléfono de los usuarios.

- **Si la base ya tiene usuarios cargados con teléfono**, hay que usar la misma clave con la que se cifraron. Si se cambia, esos datos no se pueden leer.
- **Si es una base nueva**, se genera una clave nueva en PowerShell:

  ```powershell
  $b = New-Object byte[] 32
  [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
  [Convert]::ToBase64String($b)
  ```

  El resultado (44 caracteres terminados en `=`) va en `ClaveEncriptacionSimetrica`.

### 2.4 Claves asimétricas (RSA)

Se usan para la exportación cifrada a XML (ítems 9, 22 y 23).

1. Con el administrador, entrar a **Herramientas de seguridad** en el menú interno.
2. En "Claves asimétricas (RSA)" presionar **Generar nuevo par de claves**. Aparecen dos cuadros de texto: `ClaveAsimetricaPublica` y `ClaveAsimetricaPrivada`.
3. Copiar el contenido **completo** de cada cuadro: hacer clic adentro, Ctrl + A y Ctrl + C. Son textos largos en Base64.
4. Pegar cada uno en la línea del mismo nombre de `AppSettings.private.config`, en una sola línea.
5. **Importante:** la clave privada no se guarda en ningún lado. Si se sale de la página sin copiarla, hay que generar un par nuevo. Conviene guardar una copia en un lugar seguro, fuera del repositorio. Además, generar un par nuevo invalida los archivos que se exportaron con el par anterior.
6. Guardar el archivo y reiniciar la aplicación. En Herramientas de seguridad, el "Estado actual" tiene que mostrar las claves como configuradas.

## 3. Recorrido completo del negocio

1. **Registro:** registrar un usuario nuevo en Registrarse y verificar que llegue el mail de activación con la plantilla de StageUp. Activar la cuenta.
2. **Habilitación del gestor:** con ese usuario, ir a Mis espacios y presionar **Solicitar ser gestor de espacios**. Con el administrador, aprobarla en **Aprobación de gestores**.
3. **Alta del espacio:** con el gestor, ir a Mis espacios y presionar **Nuevo espacio**. Completar la ficha (ubicación, precio, capacidad, fotos y horarios disponibles) y presionar **Guardar y publicar**.
4. **Búsqueda y solicitud:** con la cuenta del cliente, buscar el espacio desde Explorar espacios y desde **Buscar** en el menú superior. Abrir el detalle, elegir fecha, horario y duración, y enviar la solicitud. Verificar que lleguen los mails de "solicitud enviada" al cliente y de "nueva solicitud" al gestor.
5. **Aceptación:** con el gestor, aceptar la solicitud en Solicitudes recibidas. Ahí tiene que figurar "Esperando el pago del solicitante". Con el cliente, en Mis reservas tiene que figurar "Pendiente de pago" con la fecha límite y el botón **Pagar reserva**. El mail de aceptación también indica la fecha límite.
6. **Pago rechazado:** en Pagar, usar la tarjeta de prueba `4000 0000 0000 0002`, cualquier titular con nombre y apellido, un vencimiento futuro y un código de 3 dígitos. Tiene que aparecer el rechazo del banco y la reserva tiene que seguir pendiente de pago.
7. **Pago aprobado:** pagar con `4111 1111 1111 1111`. Verificar:
   - la pantalla de comprobante y el mail del comprobante;
   - el mail al gestor;
   - en Mi cuenta corriente del gestor (pestaña "Como gestor de espacios"), el ingreso y la comisión de StageUp;
   - en "Ver seguimiento" de la reserva, el paso "Pago recibido".
8. **Cancelación:** con el cliente, cancelar la reserva pagada. El mensaje de confirmación indica cuánto se devuelve. En Mi cuenta corriente tienen que aparecer la nota de crédito (NC-…) y el saldo a favor. Si faltan menos de 24 hs para la reserva, también la nota de débito (ND-…) por la penalidad. Tienen que llegar los mails de los comprobantes.
9. **Pago con saldo a favor:** solicitar otra reserva, aceptarla y, al pagar, dejar marcado "Usar mi saldo a favor". La pantalla muestra cuánto se cubre con saldo y cuánto con tarjeta. Si el saldo alcanza, no pide tarjeta.
10. **Plazo de pago vencido:** solicitar y aceptar otra reserva sin pagarla. Tomar su número (aparece como N° en Mis reservas) y ejecutar en SQL Server Management Studio, sobre la base StageUp:

    ```sql
    UPDATE dbo.Reserva SET fechaLimitePago = DATEADD(MINUTE, -1, GETDATE()) WHERE idReserva = N;
    ```

    Al recargar Mis reservas, la reserva tiene que figurar como cancelada porque no se pagó a tiempo, y tienen que llegar la notificación y el mail.
11. **Finalización y calificación:** sobre una reserva **pagada**, ejecutar:

    ```sql
    UPDATE dbo.Reserva SET fechaSolicitada = DATEADD(DAY, -1, CAST(GETDATE() AS DATE)) WHERE idReserva = N;
    ```

    Al recargar Mis reservas, la reserva tiene que figurar como finalizada y tiene que llegar la invitación a calificar. El cliente califica el espacio desde Mis reservas y el gestor califica al cliente desde Solicitudes recibidas. Después se revisa el ranking "Espacios mejor valorados" en Explorar espacios.
12. **Soporte:** desde una reserva en Mis reservas, presionar **Contactar soporte** y enviar la consulta.
    - Con el administrador, el menú interno tiene que mostrar el contador de tickets abiertos.
    - En Gestión de soporte, presionar **Tomar este ticket** y responder. El ticket muestra los datos de la reserva.
    - El cliente tiene que recibir la notificación y el mail.
13. **Administración:**
    - **Pagos y cuentas corrientes:**
      - buscar al cliente y ver su cuenta;
      - emitir una nota de débito manual con motivo;
      - con el cliente, pagar esa deuda desde Mi cuenta corriente;
      - volver al administrador y emitir y luego anular una nota de crédito manual;
      - buscar al gestor y registrar una liquidación (un importe mayor a su saldo tiene que rechazarse);
      - en "Pagos recibidos y rechazados", revisar que figure el pago rechazado del paso 6.
    - **Parámetros de la plataforma:** cambiar la comisión de StageUp y verificar que el próximo pago la use.
    - **Reportes y estadísticas:** revisar el tablero y los gráficos, cambiar la agrupación (día, semana, mes, año), filtrar por provincia y exportar el CSV.
    - **Registros de actividad:** verificar que todo lo anterior quedó en la bitácora: pagos, comprobantes, liquidación, parámetros y cancelaciones.

## 4. ABMC de todas las tablas (ítem 15 bis)

Por cada entidad se verifica qué se puede dar de alta, dar de baja, modificar y consultar, y en qué pantalla:

- **Usuarios externos:** alta en Registrarse, modificación en Mi perfil, consulta en Búsqueda en la plataforma y Aprobación de gestores. *A revisar:* si hace falta una baja o un bloqueo desde administración.
- **Usuarios internos, roles, permisos y grupos:** Usuarios internos, Gestión de roles, Permisos del rol y Organizar permisos.
- **Menú:** Gestión del menú.
- **Idiomas y traducciones:** Gestión de idiomas y Configurar idioma.
- **Espacios (ficha, fotos y disponibilidad):** Mis espacios del gestor (alta, edición, pausa, baja). Consulta pública en Explorar.
- **Actividades y participantes:** Mis actividades.
- **Reservas:** alta desde el detalle del espacio, aceptar y rechazar en Solicitudes recibidas, cancelar, pagar y consultar en Mis reservas. *Faltante conocido:* no hay una pantalla de administración que liste todas las reservas; hoy solo se consultan desde Búsqueda en la plataforma.
- **Calificaciones:** alta desde Mis reservas y Solicitudes recibidas, consulta en el detalle del espacio. *A revisar:* si hace falta moderarlas o darlas de baja desde administración.
- **Encuestas, preguntas y opciones:** Gestión de encuestas. Las respuestas se cargan en Encuestas.
- **Novedades y newsletter:** Novedades y newsletter.
- **Preguntas frecuentes:** Gestión de FAQ.
- **Tickets y mensajes de soporte:** Soporte y Gestión de soporte.
- **Pagos:** alta en Pagar y consulta en Pagos y cuentas corrientes. Por diseño no se modifican.
- **Notas de crédito y débito:** alta, anulación y consulta en Pagos y cuentas corrientes. Por diseño no se borran: se anulan con un movimiento inverso.
- **Movimientos de cuenta corriente:** consulta en Mi cuenta corriente y en Pagos y cuentas corrientes. Las liquidaciones se dan de alta desde administración.
- **Parámetros de la plataforma:** modificación y consulta.
- **Backups:** Backup y restauración.
- **Notificaciones y bitácora:** las genera el sistema. Se consultan en la campanita y en Registros de actividad. Por diseño no tienen ABM manual.

Cualquier entidad a la que le falte una operación se anota para resolverla.

## 5. Pruebas manuales pendientes

1. **Rendimiento (ítem 36):** con la base cargada con volumen (cientos de espacios y miles de reservas), se recorren el catálogo, el detalle de espacio, Mis reservas, Solicitudes recibidas, Registros de actividad, las notificaciones y Reportes y estadísticas. Si alguna pantalla tarda, se mide su consulta en Management Studio con `SET STATISTICS IO, TIME ON`.
2. **Newsletter por SMTP real (ítem 2):** en Novedades y newsletter, primero **Enviar prueba** y después **Enviar newsletter** a destinatarios reales. Se revisa que el HTML del correo se vea bien y que el link lleve a Novedades.
3. **Errores personalizados (ítem 11):**
   - En `StageUp.UI/Web.config`, cambiar temporalmente `customErrors mode="RemoteOnly"` por `mode="On"`. Con `RemoteOnly`, en la propia máquina se sigue viendo el detalle técnico.
   - Entrar a una dirección inexistente, por ejemplo `/noexiste.aspx`: tiene que aparecer la página "Página no encontrada".
   - Forzar un error entrando a `Buscar.aspx?q=<script>`: tiene que aparecer la página de error genérica, y el error tiene que quedar en Registros de actividad con el tipo de operación ERROR.
   - Al terminar, volver a `RemoteOnly`.
4. **Exportación a XML cifrado (ítems 9, 22 y 23):** con las claves del punto 2.4 ya configuradas:
   - En Registros de actividad, aplicar un filtro y presionar **Exportar filtro actual a XML cifrado**. Se descarga un archivo.
   - Abrirlo con el Bloc de notas: el contenido no tiene que poder leerse.
   - En Herramientas de seguridad, en "Importar y descifrar una exportación", elegir el archivo y presionar **Descifrar**. Tiene que aparecer el XML legible con los registros exportados.
