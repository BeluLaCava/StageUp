param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$PasswordDemo = "StageUp2026"
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$webConfig = Join-Path $repo "StageUp.UI\Web.config"

if (-not (Test-Path -LiteralPath $webConfig)) {
    throw "No se encontró StageUp.UI\Web.config."
}

$ensamblados = @(
    (Join-Path $repo "StageUp.BE\bin\$Configuration\StageUp.BE.dll"),
    (Join-Path $repo "StageUp.DAL\bin\$Configuration\StageUp.DAL.dll"),
    (Join-Path $repo "StageUp.Seguridad\bin\$Configuration\StageUp.Seguridad.dll"),
    (Join-Path $repo "StageUp.Servicios\bin\$Configuration\StageUp.Servicios.dll"),
    (Join-Path $repo "StageUp.MPP\bin\$Configuration\StageUp.MPP.dll"),
    (Join-Path $repo "StageUp.BLL\bin\$Configuration\StageUp.BLL.dll")
)

$faltantes = @($ensamblados | Where-Object { -not (Test-Path -LiteralPath $_) })
if ($faltantes.Count -gt 0) {
    throw "Primero compilá StageUp.sln en $Configuration. Faltan ensamblados necesarios para usar la lógica real del sistema."
}

Add-Type -AssemblyName System.Configuration

[xml]$webXml = Get-Content -LiteralPath $webConfig -Raw
$conexionNodo = $webXml.configuration.connectionStrings.add |
    Where-Object { $_.name -eq "StageUpConnectionString" } |
    Select-Object -First 1
if ($null -eq $conexionNodo -or [string]::IsNullOrWhiteSpace([string]$conexionNodo.connectionString)) {
    throw "No se pudo leer StageUpConnectionString desde StageUp.UI\Web.config."
}

# ConfigurationManager lee la configuración de powershell.exe. Se incorpora
# en memoria la misma cadena del proyecto para que MPP y BLL usen la DAL real.
$cadenas = [System.Configuration.ConfigurationManager]::ConnectionStrings
$campoSoloLectura = [System.Configuration.ConfigurationElement].GetField(
    "_bReadOnly", [Reflection.BindingFlags]"NonPublic,Instance")
$campoSoloLectura.SetValue($cadenas, $false)
$campoColeccionSoloLectura = [System.Configuration.ConfigurationElementCollection].GetField(
    "bReadOnly", [Reflection.BindingFlags]"NonPublic,Instance")
$campoColeccionSoloLectura.SetValue($cadenas, $false)
if ($null -ne $cadenas["StageUpConnectionString"]) {
    $cadenas.Remove("StageUpConnectionString")
}
$cadenas.Add((New-Object System.Configuration.ConnectionStringSettings(
    "StageUpConnectionString", [string]$conexionNodo.connectionString, "System.Data.SqlClient")))

foreach ($ensamblado in $ensamblados) {
    [void][Reflection.Assembly]::LoadFrom($ensamblado)
}

function New-IntList {
    param([int[]]$Values)

    $lista = New-Object "System.Collections.Generic.List[int]"
    foreach ($valor in $Values) {
        [void]$lista.Add([int]$valor)
    }
    return $lista
}

function Test-SameSet {
    param($Left, $Right)

    $izquierda = @($Left | Sort-Object -Unique)
    $derecha = @($Right | Sort-Object -Unique)
    return $null -eq (Compare-Object -ReferenceObject $izquierda -DifferenceObject $derecha)
}

$mppExterno = New-Object StageUp.MPP.MPP_UsuarioExterno
$mppInterno = New-Object StageUp.MPP.MPP_UsuarioInterno
$mppArea = New-Object StageUp.MPP.MPP_AreaInterna
$mppMenu = New-Object StageUp.MPP.MPP_OpcionMenu
$bllBitacora = New-Object StageUp.BLL.BLL_Bitacora
$bllSolicitud = New-Object StageUp.BLL.BLL_SolicitudHabilitacionGestor
$bllRol = New-Object StageUp.BLL.BLL_RolInterno
$bllPermiso = New-Object StageUp.BLL.BLL_PermisoInterno
$bllInterno = New-Object StageUp.BLL.BLL_UsuarioInterno

$correoAdmin = "admin@stageup.test"
$adminConsulta = New-Object StageUp.BE.Entidades.UsuarioInterno
$adminConsulta.CorreoElectronico = $correoAdmin
$admin = $mppInterno.ObtenerPorCorreo($adminConsulta)
if ($null -eq $admin -or -not $admin.Activo) {
    throw "No se encontró la cuenta interna activa admin@stageup.test para auditar la preparación."
}

# ---------------------------------------------------------------------------
# Usuarios externos: se tocan solamente las cuentas de demostración conocidas.
# Cada contraseña obtiene su propia sal aleatoria mediante Seguridad; nunca se
# escribe PasswordDemo directamente en la base de datos.
# ---------------------------------------------------------------------------
$correosExternos = @(
    "ana.solicitante@stageup.test",
    "jibam91306@airhemp.com",
    "lucia.pendiente@stageup.test",
    "dantemajeras@gmail.com",
    "polioly.oficial@gmail.com",
    "marcos.gestor@stageup.test",
    "clara.espacios@stageup.test",
    "valeria.produccion@stageup.test"
)

foreach ($correo in $correosExternos) {
    $consulta = New-Object StageUp.BE.Entidades.UsuarioExterno
    $consulta.CorreoElectronico = $correo
    $usuario = $mppExterno.ObtenerPorCorreo($consulta)
    if ($null -eq $usuario) {
        throw "Falta la cuenta externa de demostración $correo. No se modificó una cuenta sustituta."
    }

    $passwordCorrecta = $false
    try {
        $passwordCorrecta = [StageUp.Seguridad.ProtectorDeCredenciales]::VerificarPassword($usuario, $PasswordDemo)
    }
    catch {
        $passwordCorrecta = $false
    }

    if (-not $passwordCorrecta) {
        [StageUp.Seguridad.ProtectorDeCredenciales]::ProtegerPassword($usuario, $PasswordDemo)
        $mppExterno.ActualizarPassword($usuario)
        $bllBitacora.Registrar(
            $usuario.IdUsuarioExterno, "MODIFICACION", "UsuarioExterno", $usuario.IdUsuarioExterno,
            "Preparacion local: actualizacion segura de la clave de la cuenta de demostracion.",
            "Database/87_PrepararUsuariosDemo.ps1")
        Write-Host ("Contraseña preparada: {0}." -f $correo) -ForegroundColor Green
    }
    else {
        Write-Host ("La contraseña ya estaba preparada: {0}." -f $correo)
    }
}

# Lucía queda activa como solicitante y con una solicitud de gestora pendiente.
$consultaLucia = New-Object StageUp.BE.Entidades.UsuarioExterno
$consultaLucia.CorreoElectronico = "lucia.pendiente@stageup.test"
$lucia = $mppExterno.ObtenerPorCorreo($consultaLucia)
if ($lucia.EstadoCuenta -ne "Activa") {
    $lucia.EstadoCuenta = "Activa"
    $mppExterno.ActivarCuenta($lucia)
    $bllBitacora.Registrar(
        $lucia.IdUsuarioExterno, "ACTIVACION", "UsuarioExterno", $lucia.IdUsuarioExterno,
        "Preparacion local: activacion de Lucia como solicitante para demostrar una solicitud de gestora pendiente.",
        "Database/87_PrepararUsuariosDemo.ps1")
}

$ultimaSolicitud = $bllSolicitud.ObtenerUltima($lucia.IdUsuarioExterno)
if ($null -eq $ultimaSolicitud -or $ultimaSolicitud.Estado -eq "Rechazada") {
    $solicitud = New-Object StageUp.BE.Entidades.SolicitudHabilitacionGestor
    $solicitud.NombreResponsable = "Luc$([char]0x00ED)a Pendiente"
    $solicitud.DocumentoResponsable = "32145678"
    $solicitud.TelefonoContacto = "+54 11 5555-0198"
    $solicitud.CorreoContacto = "lucia.pendiente@stageup.test"
    $solicitud.CondicionFiscal = "ConsumidorFinal"
    $solicitud.NombreEspacio = "Sala Cultural Horizonte"
    $solicitud.TipoEspacio = "Sala multiprop$([char]0x00F3)sito"
    $solicitud.Provincia = "Buenos Aires"
    $solicitud.Ciudad = "La Plata"
    $solicitud.DescripcionPropuesta = "Sala cultural barrial para ensayos, talleres art$([char]0x00ED)sticos y presentaciones de peque$([char]0x00F1)o formato."

    $resultadoSolicitud = $bllSolicitud.Registrar($lucia.IdUsuarioExterno, $solicitud)
    if (-not $resultadoSolicitud.Exitoso) {
        throw ("No se pudo crear la solicitud pendiente de Lucía: {0}" -f $resultadoSolicitud.Mensaje)
    }
    Write-Host "Solicitud de gestora pendiente creada para Lucía." -ForegroundColor Green
}
elseif ($ultimaSolicitud.Estado -ne "PendienteRevision") {
    throw "Lucía ya tiene una solicitud aprobada; no se reemplazó su historial por una solicitud ficticia."
}
else {
    Write-Host "Lucía ya tenía una solicitud de gestora pendiente."
}

# ---------------------------------------------------------------------------
# Roles internos: permisos de hoja explícitos. No se asignan los grupos
# Administración o Comercialización completos para evitar accesos heredados.
# ---------------------------------------------------------------------------
$arbol = $bllPermiso.ListarArbolCompleto()
$hojas = @($arbol.Listar())
$permisosPorCodigo = @{}
foreach ($hoja in $hojas) {
    if (-not [string]::IsNullOrWhiteSpace($hoja.CodigoPermiso)) {
        $permisosPorCodigo[$hoja.CodigoPermiso] = $hoja
    }
}

$areas = @($mppArea.ListarActivas())
$nombreComercializacion = "Comercializaci$([char]0x00F3)n"
$nombreAdministracion = "Administraci$([char]0x00F3)n"
$areaComercial = $areas | Where-Object { $_.NombreArea -eq $nombreComercializacion } | Select-Object -First 1
$areaMarketing = $areas | Where-Object { $_.NombreArea -eq "Marketing" } | Select-Object -First 1
$areaSoporte = $areas | Where-Object { $_.NombreArea -eq "Soporte" } | Select-Object -First 1
$areaAdministracion = $areas | Where-Object { $_.NombreArea -eq $nombreAdministracion } | Select-Object -First 1

if ($null -eq $areaComercial -or $null -eq $areaMarketing -or $null -eq $areaSoporte -or $null -eq $areaAdministracion) {
    throw "No se encontraron todas las áreas internas necesarias."
}

$nombreRolComercial = "Analista de Comercializaci$([char]0x00F3)n"
$rolesDemo = @(
    [PSCustomObject]@{
        Nombre = $nombreRolComercial
        Descripcion = "Gestiona campa$([char]0x00F1)as, contactos, alianzas, m$([char]0x00E9)tricas y contenido comercial sin acceso administrativo."
        Area = $areaComercial
        Correo = "florencia.comercial@stageup.test"
        NombreUsuario = "Florencia"
        ApellidoUsuario = "Ruiz"
        Permisos = @(
            "GESTIONAR_CAMPANIAS_COMERCIALES", "GESTIONAR_CONTACTOS_COMERCIALES",
            "GESTIONAR_ALIANZAS_ESTRATEGICAS", "CONSULTAR_METRICAS_COMERCIALES",
            "GESTIONAR_CONTENIDO_PROMOCIONAL", "REACTIVAR_REGISTROS_COMERCIALES",
            "BUSCAR_EN_PLATAFORMA", "VER_REPORTES"
        )
    },
    [PSCustomObject]@{
        Nombre = "Responsable de Marketing y Contenidos"
        Descripcion = "Administra novedades, newsletter, encuestas y preguntas frecuentes."
        Area = $areaMarketing
        Correo = "martina.marketing@stageup.test"
        NombreUsuario = "Martina"
        ApellidoUsuario = "L$([char]0x00F3)pez"
        Permisos = @(
            "GESTIONAR_NOVEDADES", "GESTIONAR_ENCUESTAS", "GESTIONAR_FAQ",
            "VER_REPORTES", "BUSCAR_EN_PLATAFORMA"
        )
    },
    [PSCustomObject]@{
        Nombre = "Agente de Soporte"
        Descripcion = "Atiende solicitudes, consulta informaci$([char]0x00F3)n operativa y puede reabrir casos cerrados."
        Area = $areaSoporte
        Correo = "sofia.soporte@stageup.test"
        NombreUsuario = "Sof$([char]0x00ED)a"
        ApellidoUsuario = "Mart$([char]0x00ED)nez"
        Permisos = @(
            "GESTIONAR_SOPORTE", "REABRIR_SOLICITUD_SOPORTE", "BUSCAR_EN_PLATAFORMA"
        )
    },
    [PSCustomObject]@{
        Nombre = "Operador de Pagos"
        Descripcion = "Controla pagos, cuentas corrientes, transacciones e historial financiero."
        Area = $areaAdministracion
        Correo = "nicolas.pagos@stageup.test"
        NombreUsuario = "Nicol$([char]0x00E1)s"
        ApellidoUsuario = "Fern$([char]0x00E1)ndez"
        Permisos = @(
            "GESTIONAR_PAGOS", "CONTROLAR_PAGOS_RESERVAS", "CONSULTAR_TRANSACCIONES",
            "CONSULTAR_HISTORIAL_FINANCIERO", "VER_REPORTES"
        )
    }
)

foreach ($definicion in $rolesDemo) {
    $idsPermisos = @()
    foreach ($codigo in $definicion.Permisos) {
        if (-not $permisosPorCodigo.ContainsKey($codigo)) {
            throw ("No existe el permiso activo {0}, requerido por {1}." -f $codigo, $definicion.Nombre)
        }
        $idsPermisos += [int]$permisosPorCodigo[$codigo].IdComponentePermiso
    }

    $rolExistente = @($bllRol.ListarResumen("Todos")) |
        Where-Object { $_.Activo -and $_.NombreRol -eq $definicion.Nombre } |
        Select-Object -First 1

    $guardarRol = $true
    if ($null -ne $rolExistente) {
        $idsActuales = @($bllPermiso.ListarIdsAsignados($rolExistente.IdRolInterno))
        $guardarRol = $rolExistente.IdAreaInterna -ne $definicion.Area.IdAreaInterna -or
            $rolExistente.Descripcion -ne $definicion.Descripcion -or
            -not (Test-SameSet $idsActuales $idsPermisos)
    }

    if ($guardarRol) {
        $rol = New-Object StageUp.BE.Entidades.RolInterno
        $rol.IdRolInterno = if ($null -eq $rolExistente) { 0 } else { $rolExistente.IdRolInterno }
        $rol.NombreRol = $definicion.Nombre
        $rol.Descripcion = $definicion.Descripcion
        $rol.IdAreaInterna = $definicion.Area.IdAreaInterna
        $resultadoRol = $bllRol.Guardar($rol, (New-IntList $idsPermisos), $admin.IdUsuarioInterno)
        if (-not $resultadoRol.Exitoso) {
            throw ("No se pudo guardar el rol {0}: {1}" -f $definicion.Nombre, $resultadoRol.Mensaje)
        }
        $definicion | Add-Member -NotePropertyName IdRol -NotePropertyValue ([int]$resultadoRol.Valor)
        Write-Host ("Rol preparado: {0}." -f $definicion.Nombre) -ForegroundColor Green
    }
    else {
        $definicion | Add-Member -NotePropertyName IdRol -NotePropertyValue ([int]$rolExistente.IdRolInterno)
        Write-Host ("El rol ya estaba preparado: {0}." -f $definicion.Nombre)
    }
}

# ---------------------------------------------------------------------------
# Usuarios internos: altas y modificaciones pasan por BLL_UsuarioInterno para
# conservar validaciones, bitácora y protección de administradores.
# ---------------------------------------------------------------------------
foreach ($definicion in $rolesDemo) {
    $consulta = New-Object StageUp.BE.Entidades.UsuarioInterno
    $consulta.CorreoElectronico = $definicion.Correo
    $usuario = $mppInterno.ObtenerPorCorreo($consulta)

    if ($null -eq $usuario) {
        $resultadoUsuario = $bllInterno.Registrar(
            $definicion.NombreUsuario, $definicion.ApellidoUsuario, $definicion.Correo,
            $definicion.Area.IdAreaInterna, $definicion.IdRol, "Activa",
            $PasswordDemo, $PasswordDemo, $admin.IdUsuarioInterno)
        if (-not $resultadoUsuario.Exitoso) {
            throw ("No se pudo crear {0}: {1}" -f $definicion.Correo, $resultadoUsuario.Mensaje)
        }
        Write-Host ("Usuario interno creado: {0}." -f $definicion.Correo) -ForegroundColor Green
    }
    else {
        $passwordCorrecta = [StageUp.Seguridad.ProtectorDeCredenciales]::VerificarPassword($usuario, $PasswordDemo)
        $cambioDatos = $usuario.Nombre -ne $definicion.NombreUsuario -or
            $usuario.Apellido -ne $definicion.ApellidoUsuario -or
            $usuario.IdAreaInterna -ne $definicion.Area.IdAreaInterna -or
            $usuario.IdRolInterno -ne $definicion.IdRol -or
            $usuario.EstadoCuenta -ne "Activa"

        if ($cambioDatos -or -not $passwordCorrecta) {
            $passwordNueva = if ($passwordCorrecta) { "" } else { $PasswordDemo }
            $resultadoUsuario = $bllInterno.Modificar(
                $usuario.IdUsuarioInterno, $definicion.NombreUsuario, $definicion.ApellidoUsuario,
                $definicion.Correo, $definicion.Area.IdAreaInterna, $definicion.IdRol, "Activa",
                $passwordNueva, $passwordNueva, $admin.IdUsuarioInterno)
            if (-not $resultadoUsuario.Exitoso) {
                throw ("No se pudo actualizar {0}: {1}" -f $definicion.Correo, $resultadoUsuario.Mensaje)
            }
            Write-Host ("Usuario interno actualizado: {0}." -f $definicion.Correo) -ForegroundColor Green
        }
        else {
            Write-Host ("El usuario interno ya estaba preparado: {0}." -f $definicion.Correo)
        }
    }
}

# El administrador se conserva con sus datos y rol; solamente se unifica su
# contraseña si todavía no usa la clave de demostración solicitada.
$admin = $mppInterno.ObtenerPorCorreo($adminConsulta)
if (-not [StageUp.Seguridad.ProtectorDeCredenciales]::VerificarPassword($admin, $PasswordDemo)) {
    $resultadoAdmin = $bllInterno.Modificar(
        $admin.IdUsuarioInterno, $admin.Nombre, $admin.Apellido, $admin.CorreoElectronico,
        $admin.IdAreaInterna, $admin.IdRolInterno, $admin.EstadoCuenta,
        $PasswordDemo, $PasswordDemo, $admin.IdUsuarioInterno)
    if (-not $resultadoAdmin.Exitoso) {
        throw ("No se pudo unificar la contraseña del administrador: {0}" -f $resultadoAdmin.Mensaje)
    }
}

# ---------------------------------------------------------------------------
# Verificación funcional: contraseña, permisos efectivos y menú dinámico.
# ---------------------------------------------------------------------------
foreach ($correo in $correosExternos) {
    $consulta = New-Object StageUp.BE.Entidades.UsuarioExterno
    $consulta.CorreoElectronico = $correo
    $usuario = $mppExterno.ObtenerPorCorreo($consulta)
    if (-not [StageUp.Seguridad.ProtectorDeCredenciales]::VerificarPassword($usuario, $PasswordDemo)) {
        throw "Falló la comprobación de contraseña para $correo."
    }
}

$opcionesMenu = @($mppMenu.ListarActivas())
foreach ($definicion in $rolesDemo) {
    $consulta = New-Object StageUp.BE.Entidades.UsuarioInterno
    $consulta.CorreoElectronico = $definicion.Correo
    $usuario = $mppInterno.ObtenerPorCorreo($consulta)
    if ($null -eq $usuario -or -not $usuario.Activo -or
        -not [StageUp.Seguridad.ProtectorDeCredenciales]::VerificarPassword($usuario, $PasswordDemo)) {
        throw ("La cuenta interna {0} no quedó activa y accesible." -f $definicion.Correo)
    }

    $codigosEfectivos = @($bllPermiso.ListarCodigosPermisosDeRol($definicion.IdRol))
    if (-not (Test-SameSet $codigosEfectivos $definicion.Permisos)) {
        throw ("Los permisos efectivos de {0} no coinciden con su rol." -f $definicion.Correo)
    }

    $idsConMenu = @($opcionesMenu |
        Where-Object { $definicion.Permisos -contains $_.CodigoPermiso } |
        Select-Object -ExpandProperty IdComponentePermiso -Unique)
    $itemsMenu = @($bllPermiso.ConstruirMenuParaRol($definicion.IdRol).ObtenerItems())
    if ($itemsMenu.Count -ne $idsConMenu.Count) {
        throw ("El menú de {0} no coincide con las opciones autorizadas por su rol." -f $definicion.Correo)
    }

    Write-Host ("Verificado: {0} tiene {1} permisos y {2} accesos de menú." -f
        $definicion.Correo, $codigosEfectivos.Count, $itemsMenu.Count) -ForegroundColor Cyan
}

$admin = $mppInterno.ObtenerPorCorreo($adminConsulta)
if (-not [StageUp.Seguridad.ProtectorDeCredenciales]::VerificarPassword($admin, $PasswordDemo)) {
    throw "Falló la comprobación de contraseña del administrador."
}

$ultimaSolicitud = $bllSolicitud.ObtenerUltima($lucia.IdUsuarioExterno)
if ($null -eq $ultimaSolicitud -or $ultimaSolicitud.Estado -ne "PendienteRevision") {
    throw "Lucía no quedó con la solicitud de gestora pendiente esperada."
}

Write-Host "Usuarios externos e internos preparados correctamente. Las contraseñas se verificaron mediante Seguridad y los roles contra sus permisos y menú reales." -ForegroundColor Cyan
