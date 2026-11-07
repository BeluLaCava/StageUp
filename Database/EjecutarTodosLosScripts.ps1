param(
    [string]$ServerInstance = '.\SQLEXPRESS',
    [ValidatePattern('^[A-Za-z0-9_][A-Za-z0-9_ -]{0,127}$')][string]$Database = 'StageUp',
    [switch]$MarcarComoAplicados,
    [switch]$IncluirDatosDemo87
)

$ErrorActionPreference = 'Stop'
$null = Get-Command sqlcmd -ErrorAction Stop
$nombreSql = $Database.Replace("'", "''")
$identificadorSql = '[' + $Database.Replace(']', ']]') + ']'

function Invoke-Sql {
    param([string]$Consulta, [string]$Db = $Database, [switch]$Escalar)
    $salida = & sqlcmd -S $ServerInstance -d $Db -E -I -b -h -1 -W -Q $Consulta 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Falló la consulta contra '$Db': $($salida -join [Environment]::NewLine)"
    }
    if ($Escalar) { return (($salida | Where-Object { ![string]::IsNullOrWhiteSpace([string]$_) }) -join "`n").Trim() }
}

function Adaptar-Destino {
    param([string]$Contenido)
    $contenidoFinal = [regex]::Replace($Contenido, '(?im)^(\s*USE\s+)(?:\[StageUp\]|StageUp)(\s*;?\s*)$', [Text.RegularExpressions.MatchEvaluator]{ param($m) $m.Groups[1].Value + $identificadorSql + $m.Groups[2].Value })
    $contenidoFinal = [regex]::Replace($contenidoFinal, '(?i)\bDB_ID\(\s*N?''StageUp''\s*\)', [Text.RegularExpressions.MatchEvaluator]{ param($m) "DB_ID(N'$nombreSql')" })
    $contenidoFinal = [regex]::Replace($contenidoFinal, '(?im)^(\s*CREATE\s+DATABASE\s+)(?:\[StageUp\]|StageUp)(\s*;?\s*)$', [Text.RegularExpressions.MatchEvaluator]{ param($m) $m.Groups[1].Value + $identificadorSql + $m.Groups[2].Value })
    return $contenidoFinal
}

Invoke-Sql -Db master -Consulta "IF DB_ID(N'$nombreSql') IS NULL CREATE DATABASE $identificadorSql;"
$control = Invoke-Sql -Escalar -Consulta "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'dbo._ScriptsEjecutados',N'U') IS NOT NULL THEN 1 WHEN EXISTS(SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) THEN 2 ELSE 0 END;"
if ($control -eq '2' -and !$MarcarComoAplicados) {
    throw "La base '$Database' ya tiene tablas pero no tiene registro de scripts. Revisá cuáles fueron aplicados antes de inicializar el registro; no se ejecutó ningún script."
}
Invoke-Sql "IF OBJECT_ID(N'dbo._ScriptsEjecutados',N'U') IS NULL CREATE TABLE dbo._ScriptsEjecutados(nombreScript NVARCHAR(255) NOT NULL PRIMARY KEY,fechaEjecucion DATETIME NOT NULL DEFAULT(GETDATE()));"

$scripts = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.sql' |
    Where-Object { $_.Name -match '^\d+_' -and ($IncluirDatosDemo87 -or $_.Name -ne '87_MasCorreccionesYDatosDemo.sql') } |
    Sort-Object @{ Expression = { [int]($_.Name -replace '^(\d+)_.*','$1') } }, Name)
if ($scripts.Count -eq 0) { throw 'No se encontraron scripts numerados.' }
if (!$IncluirDatosDemo87 -and (Test-Path -LiteralPath (Join-Path $PSScriptRoot '87_MasCorreccionesYDatosDemo.sql'))) {
    Write-Host 'El script 87 contiene datos de demostración opcionales. No se ejecuta ni se marca como aplicado sin -IncluirDatosDemo87.'
}

$pendientes = @()
foreach ($script in $scripts) {
    $nombre = $script.Name.Replace("'", "''")
    $aplicado = Invoke-Sql -Escalar -Consulta "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo._ScriptsEjecutados WHERE nombreScript=N'$nombre';"
    if ($aplicado -notin @('0','1')) { throw "No se pudo verificar el estado de $($script.Name)." }
    if ($aplicado -eq '0') { $pendientes += $script }
}

foreach ($script in $pendientes) {
    $nombre = $script.Name.Replace("'", "''")
    if (!$MarcarComoAplicados) {
        Write-Host "Ejecutando $($script.Name) en $Database..."
        $temporal = Join-Path ([IO.Path]::GetTempPath()) ('StageUp-script-' + [Guid]::NewGuid().ToString('N') + '.sql')
        try {
            $contenido = Adaptar-Destino ([IO.File]::ReadAllText($script.FullName, [Text.Encoding]::UTF8))
            [IO.File]::WriteAllText($temporal, $contenido, [Text.UTF8Encoding]::new($true))
            & sqlcmd -S $ServerInstance -d $Database -E -I -i $temporal -f 65001 -b
            if ($LASTEXITCODE -ne 0) { throw "Falló $($script.Name). No se registró como aplicado. Revisá el error antes de reintentar." }
        }
        finally {
            if (Test-Path -LiteralPath $temporal) { Remove-Item -LiteralPath $temporal -Force }
        }
    }
    Invoke-Sql "IF NOT EXISTS(SELECT 1 FROM dbo._ScriptsEjecutados WHERE nombreScript=N'$nombre') INSERT dbo._ScriptsEjecutados(nombreScript) VALUES(N'$nombre');"
    Write-Host "Registrado: $($script.Name) en $Database."
}
Write-Host "Finalizado: $($pendientes.Count) scripts registrados en '$Database'."
