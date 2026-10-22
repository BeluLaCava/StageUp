param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$webConfig = Join-Path $repo "StageUp.UI\Web.config"
$privateConfig = Join-Path $repo "StageUp.UI\AppSettings.private.config"

if (-not (Test-Path -LiteralPath $webConfig)) {
    throw "No se encontró StageUp.UI\Web.config."
}

if (-not (Test-Path -LiteralPath $privateConfig)) {
    throw "Falta StageUp.UI\AppSettings.private.config. La billetera necesita la clave AES privada de esta instalación."
}

[xml]$privateXml = Get-Content -LiteralPath $privateConfig -Raw
$claveNodo = $privateXml.appSettings.add | Where-Object { $_.key -eq "ClaveEncriptacionSimetrica" } | Select-Object -First 1
$claveEntorno = [Environment]::GetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION", "Process")
$claveMaestra = if (-not [string]::IsNullOrWhiteSpace($claveEntorno)) {
    $claveEntorno
} elseif ($null -ne $claveNodo) {
    [string]$claveNodo.value
} else {
    $null
}

if ([string]::IsNullOrWhiteSpace($claveMaestra) -or $claveMaestra.StartsWith("COMPLETAR_")) {
    $bytesClave = New-Object byte[] 32
    $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generador.GetBytes($bytesClave)
        $claveMaestra = [Convert]::ToBase64String($bytesClave)
    }
    finally {
        $generador.Dispose()
        [Array]::Clear($bytesClave, 0, $bytesClave.Length)
    }

    if ($null -eq $claveNodo) {
        $claveNodo = $privateXml.CreateElement("add")
        [void]$claveNodo.SetAttribute("key", "ClaveEncriptacionSimetrica")
        [void]$privateXml.appSettings.AppendChild($claveNodo)
    }

    [void]$claveNodo.SetAttribute("value", $claveMaestra)
    $privateXml.Save($privateConfig)
    Write-Host "Se generó la clave AES-256 privada de esta instalación." -ForegroundColor Cyan
}

try {
    $bytesValidacion = [Convert]::FromBase64String($claveMaestra)
    if ($bytesValidacion.Length -ne 32) {
        throw "La clave configurada no tiene 32 bytes."
    }
}
catch {
    throw "ClaveEncriptacionSimetrica no es una clave AES-256 válida: $($_.Exception.Message)"
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
    throw "Primero compilá StageUp.sln en $Configuration. Faltan ensamblados necesarios para usar la BLL."
}

$claveAnterior = [Environment]::GetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION", "Process")

try {
    [Environment]::SetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION", $claveMaestra, "Process")
    Add-Type -AssemblyName System.Configuration

    [xml]$webXml = Get-Content -LiteralPath $webConfig -Raw
    $conexionNodo = $webXml.configuration.connectionStrings.add |
        Where-Object { $_.name -eq "StageUpConnectionString" } |
        Select-Object -First 1
    if ($null -eq $conexionNodo -or [string]::IsNullOrWhiteSpace([string]$conexionNodo.connectionString)) {
        throw "No se pudo leer StageUpConnectionString desde StageUp.UI\Web.config."
    }

    # ConfigurationManager lee la configuración del proceso anfitrión
    # (powershell.exe), no la del proyecto web. Se incorpora en memoria la
    # misma cadena del Web.config para que DAL y BLL trabajen sin duplicarla.
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

    $configuracionConexion = [System.Configuration.ConfigurationManager]::ConnectionStrings["StageUpConnectionString"]
    if ($null -eq $configuracionConexion -or [string]::IsNullOrWhiteSpace($configuracionConexion.ConnectionString)) {
        throw "No se pudo leer StageUpConnectionString desde StageUp.UI\Web.config."
    }

    $conexion = New-Object System.Data.SqlClient.SqlConnection $configuracionConexion.ConnectionString
    try {
        $conexion.Open()
        $comando = $conexion.CreateCommand()
        $comando.CommandText = @"
SELECT idUsuarioExterno
FROM dbo.UsuarioExterno
WHERE correoElectronico = N'ana.solicitante@stageup.test'
  AND activo = 1
  AND estadoCuenta = N'Activa';
"@
        $idsAna = @()
        $lector = $comando.ExecuteReader()
        try {
            while ($lector.Read()) {
                $idsAna += [int]$lector.GetInt32(0)
            }
        }
        finally {
            $lector.Dispose()
        }
    }
    finally {
        $conexion.Dispose()
    }

    if ($idsAna.Count -ne 1) {
        throw "No se encontró una única cuenta activa para Ana Solicitante."
    }

    $idAna = [int]$idsAna[0]
    $billetera = New-Object StageUp.BLL.BLL_Billetera
    $existentesAntes = @($billetera.Listar($idAna))
    $billeteraEstabaVacia = $existentesAntes.Count -eq 0
    $aliasMastercard = "Mastercard producci$([char]0x00F3)n"
    $aliasMastercardMalCodificado = "Mastercard producci$([char]0x00C3)$([char]0x00B3)n"

    $tarjetas = @(
        [PSCustomObject]@{
            Numero = "4111111111111111"
            Titular = "ANA SOLICITANTE"
            Mes = 12
            Anio = 2030
            Alias = "Visa personal"
            Marca = "Visa"
            Ultimos = "1111"
            Preferida = $true
        },
        [PSCustomObject]@{
            Numero = "5555555555554444"
            Titular = "ANA SOLICITANTE"
            Mes = 9
            Anio = 2031
            Alias = $aliasMastercard
            Marca = "Mastercard"
            Ultimos = "4444"
            Preferida = $false
        },
        [PSCustomObject]@{
            Numero = "378282246310005"
            Titular = "ANA SOLICITANTE"
            Mes = 6
            Anio = 2032
            Alias = "American Express eventos"
            Marca = "American Express"
            Ultimos = "0005"
            Preferida = $false
        }
    )

    foreach ($tarjetaDemo in $tarjetas) {
        $existentes = @($billetera.Listar($idAna))
        $yaExiste = $existentes | Where-Object {
            $_.Marca -eq $tarjetaDemo.Marca -and $_.UltimosDigitos -eq $tarjetaDemo.Ultimos
        } | Select-Object -First 1

        if ($null -ne $yaExiste) {
            if ($yaExiste.Alias -eq $aliasMastercardMalCodificado) {
                $correccionAlias = $billetera.CambiarAlias($idAna, $yaExiste.IdTarjetaGuardada, $aliasMastercard)
                if (-not $correccionAlias.Exitoso) {
                    throw ("No se pudo corregir el alias de Mastercard: {0}" -f $correccionAlias.Mensaje)
                }
            }

            Write-Host ("Ya existía: {0} terminada en {1}." -f $tarjetaDemo.Marca, $tarjetaDemo.Ultimos)
            continue
        }

        $datos = New-Object StageUp.BE.Entidades.DatosTarjeta
        $datos.Numero = $tarjetaDemo.Numero
        $datos.Titular = $tarjetaDemo.Titular
        $datos.MesVencimiento = $tarjetaDemo.Mes
        $datos.AnioVencimiento = $tarjetaDemo.Anio
        $datos.CodigoSeguridad = $null

        $marcarPreferida = $billeteraEstabaVacia -and [bool]$tarjetaDemo.Preferida
        $resultado = $billetera.Guardar($idAna, $datos, $tarjetaDemo.Alias, $marcarPreferida)
        if (-not $resultado.Exitoso) {
            throw ("No se pudo guardar {0} terminada en {1}: {2}" -f $tarjetaDemo.Marca, $tarjetaDemo.Ultimos, $resultado.Mensaje)
        }

        Write-Host ("Guardada: {0} terminada en {1}{2}." -f $tarjetaDemo.Marca, $tarjetaDemo.Ultimos,
            $(if ($resultado.Valor.Preferida) { " (preferida)" } else { "" })) -ForegroundColor Green
    }

    $resultadoFinal = @($billetera.Listar($idAna))
    $tarjetasDemoFinales = @($resultadoFinal | Where-Object {
        ($_.Marca -eq "Visa" -and $_.UltimosDigitos -eq "1111") -or
        ($_.Marca -eq "Mastercard" -and $_.UltimosDigitos -eq "4444") -or
        ($_.Marca -eq "American Express" -and $_.UltimosDigitos -eq "0005")
    })

    if ($tarjetasDemoFinales.Count -ne 3) {
        throw "La billetera de Ana no quedó con las tres tarjetas de demostración esperadas."
    }

    foreach ($tarjetaDemo in $tarjetas) {
        $guardada = $tarjetasDemoFinales | Where-Object {
            $_.Marca -eq $tarjetaDemo.Marca -and $_.UltimosDigitos -eq $tarjetaDemo.Ultimos
        } | Select-Object -First 1
        $codigoTemporal = if ($tarjetaDemo.Marca -eq "American Express") { "1234" } else { "123" }
        $lectura = $billetera.ObtenerDatosParaPagar($idAna, $guardada.IdTarjetaGuardada, $codigoTemporal)
        if (-not $lectura.Exitoso -or $lectura.Valor.Numero -ne $tarjetaDemo.Numero) {
            throw ("No se pudo comprobar el descifrado de {0} terminada en {1}." -f $tarjetaDemo.Marca, $tarjetaDemo.Ultimos)
        }

        $lectura.Valor.Numero = $null
        $lectura.Valor.Titular = $null
        $lectura.Valor.CodigoSeguridad = $null
        $codigoTemporal = $null
    }

    Write-Host "Billetera de Ana preparada correctamente. Se comprobó el cifrado y descifrado, y no se guardó ningún código de seguridad." -ForegroundColor Cyan
}
finally {
    [Environment]::SetEnvironmentVariable("STAGEUP_CLAVE_ENCRIPTACION", $claveAnterior, "Process")
    $claveMaestra = $null
}
