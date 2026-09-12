# EjecutarTodosLosScripts.ps1
#
# Corre los scripts numerados de la carpeta Database/ contra tu instancia local de
# SQL Server, pero solo los que TODAVIA NO se aplicaron. Para saber cuales ya corrieron,
# usa una tabla de control (dbo._ScriptsEjecutados) que crea sola la primera vez.
#
# PRIMER USO en una base que ya tenias armada de antes (con los scripts corridos a mano):
#   .\EjecutarTodosLosScripts.ps1 -MarcarComoAplicados
#   Esto NO ejecuta nada: solo anota en la tabla de control que esos scripts ya estan
#   aplicados, para que despues no se intenten volver a correr (y rompan cosas por los
#   DROP TABLE de scripts como el 01).
#
# USO NORMAL, de ahi en adelante (por ejemplo despues de un git pull con scripts nuevos):
#   .\EjecutarTodosLosScripts.ps1
#   Esto corre SOLO los scripts que todavia no figuran en la tabla de control, en orden.
#
# Si tu instancia de SQL Server no es ".\SQLEXPRESS", pasala como parametro:
#   .\EjecutarTodosLosScripts.ps1 -ServerInstance "BELUENVY\SQLEXPRESS01"
#
# Requiere tener "sqlcmd" disponible (viene con SQL Server / con las herramientas de
# linea de comandos de SSMS). Para comprobarlo: abri PowerShell y escribi "sqlcmd -?".

param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$Database = "StageUp",
    [switch]$MarcarComoAplicados
)

$ErrorActionPreference = "Stop"

function Invoke-Sql {
    param([string]$Consulta, [string]$Db = $Database)
    sqlcmd -S $ServerInstance -d $Db -E -Q $Consulta -b
    if ($LASTEXITCODE -ne 0) {
        throw "Fallo ejecutando una consulta de control contra '$Db'. Revisa el mensaje de sqlcmd de arriba."
    }
}

Write-Host "Verificando que la base de datos '$Database' exista en '$ServerInstance'..." -ForegroundColor Cyan
Invoke-Sql -Db "master" -Consulta "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$Database') CREATE DATABASE [$Database];"

Write-Host "Verificando tabla de control de scripts (dbo._ScriptsEjecutados)..." -ForegroundColor Cyan
Invoke-Sql "IF OBJECT_ID('dbo._ScriptsEjecutados', 'U') IS NULL BEGIN CREATE TABLE dbo._ScriptsEjecutados (nombreScript NVARCHAR(255) NOT NULL PRIMARY KEY, fechaEjecucion DATETIME NOT NULL DEFAULT (GETDATE())); END"

$scripts = Get-ChildItem -Path $PSScriptRoot -Filter "*.sql" |
    Where-Object { $_.Name -match '^\d+_' } |
    Sort-Object { [int]($_.Name -replace '^(\d+)_.*', '$1') }

if ($scripts.Count -eq 0) {
    Write-Host "No se encontraron scripts numerados (01_..., 02_..., etc.) en esta carpeta." -ForegroundColor Red
    exit 1
}

if ($MarcarComoAplicados) {
    Write-Host "`nMarcando los $($scripts.Count) scripts como ya aplicados, SIN ejecutarlos:" -ForegroundColor Magenta
    foreach ($script in $scripts) {
        Invoke-Sql "IF NOT EXISTS (SELECT 1 FROM dbo._ScriptsEjecutados WHERE nombreScript = N'$($script.Name)') INSERT INTO dbo._ScriptsEjecutados (nombreScript) VALUES (N'$($script.Name)');"
        Write-Host "  marcado: $($script.Name)"
    }
    Write-Host "`nListo. La proxima vez que corras este script SIN -MarcarComoAplicados, va a saltear todos estos y solo va a correr los que sean realmente nuevos." -ForegroundColor Green
    exit 0
}

$pendientes = @()
foreach ($script in $scripts) {
    $resultado = sqlcmd -S $ServerInstance -d $Database -E -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo._ScriptsEjecutados WHERE nombreScript = N'$($script.Name)') THEN 1 ELSE 0 END;"
    $yaAplicado = ($resultado | Select-Object -First 1).Trim() -eq "1"
    if (-not $yaAplicado) {
        $pendientes += $script
    }
}

if ($pendientes.Count -eq 0) {
    Write-Host "`nNo hay scripts nuevos: tu base ya tiene aplicados los $($scripts.Count) scripts que hay en esta carpeta." -ForegroundColor Green
    exit 0
}

Write-Host "`nScripts pendientes de aplicar ($($pendientes.Count) de $($scripts.Count)):" -ForegroundColor Cyan
$pendientes | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

foreach ($script in $pendientes) {
    Write-Host "--- Ejecutando $($script.Name) ---" -ForegroundColor Yellow
    sqlcmd -S $ServerInstance -d $Database -E -i $script.FullName -b
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nERROR ejecutando $($script.Name). Revisa el mensaje de arriba y solucionalo antes de continuar. Los scripts anteriores ya quedaron marcados como aplicados, asi que al volver a correr este archivo va a arrancar justo desde el que fallo." -ForegroundColor Red
        exit 1
    }
    Invoke-Sql "INSERT INTO dbo._ScriptsEjecutados (nombreScript) VALUES (N'$($script.Name)');"
}

Write-Host "`nListo: se aplicaron $($pendientes.Count) scripts nuevos sobre '$Database'." -ForegroundColor Green