param(
    [Parameter(Mandatory=$true)][ValidatePattern('\AStageUp_Beta2_QA_[A-Za-z0-9_]+\z')][string]$Database,
    [string]$ServerInstance = 'localhost',
    [string]$BinPath
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($BinPath)) { $BinPath = Join-Path $PSScriptRoot '..\StageUp.UI\bin' }
$BinPath = (Resolve-Path -LiteralPath $BinPath).Path
$temporal = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$salida = Join-Path $temporal ('StageUp_Beta_Tests_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $salida | Out-Null
try {
    Copy-Item (Join-Path $BinPath 'StageUp.*.dll') $salida
    $correos = Join-Path $salida 'Correos'
    New-Item -ItemType Directory -Path $correos | Out-Null
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $b['Data Source'] = $ServerInstance
    $b['Initial Catalog'] = $Database
    $b['Integrated Security'] = $true
    $b['TrustServerCertificate'] = $true
    $cadena = [Security.SecurityElement]::Escape($b.ConnectionString)
    $correoXml = [Security.SecurityElement]::Escape($correos)
    $exe = Join-Path $salida 'TestingBeta.exe'
    $config = @"
<configuration><connectionStrings><add name="StageUpConnectionString" connectionString="$cadena" /></connectionStrings><appSettings>
<add key="PerfilCompletoHabilitado" value="true"/><add key="EspaciosFichaCompletaHabilitada" value="true"/>
<add key="SmtpHost" value="127.0.0.1"/><add key="SmtpPuerto" value="1"/><add key="SmtpUsarSsl" value="false"/>
<add key="SmtpUsuario" value="qa"/><add key="SmtpPassword" value="qa"/><add key="CorreoRemitente" value="qa@example.test"/><add key="NombreRemitente" value="QA aislado"/>
<add key="CorreoPruebaDirectorio" value="$correoXml"/></appSettings><system.net><mailSettings><smtp deliveryMethod="SpecifiedPickupDirectory"><specifiedPickupDirectory pickupDirectoryLocation="$correoXml"/></smtp></mailSettings></system.net></configuration>
"@
    [IO.File]::WriteAllText($exe + '.config', $config, [Text.UTF8Encoding]::new($false))
    $refs = @('BE','DAL','Seguridad','MPP','Servicios','BLL') | ForEach-Object { '/r:' + (Join-Path $salida ('StageUp.' + $_ + '.dll')) }
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $csc /nologo ('/out:' + $exe) @refs /r:System.Configuration.dll /r:System.Data.dll (Join-Path $PSScriptRoot 'TestingBeta.cs')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar TestingBeta.' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de TestingBeta.' }
}
finally {
    if (Test-Path -LiteralPath $salida) {
        $resuelta = (Resolve-Path -LiteralPath $salida).Path
        if ($resuelta -ne $salida -or (Split-Path $resuelta -Parent).TrimEnd('\') -ne $temporal.TrimEnd('\')) {
            throw 'La carpeta temporal no coincide con el destino creado para la prueba.'
        }
        Remove-Item -LiteralPath $resuelta -Recurse -Force
    }
}
