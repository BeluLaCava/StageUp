param([string]$ServerInstance='localhost')
$ErrorActionPreference='Stop'
$database='StageUp_Beta2_QA_Runner_'+[guid]::NewGuid().ToString('N')
$carpeta=Join-Path ([IO.Path]::GetTempPath()) $database
$raiz=Split-Path $PSScriptRoot -Parent
$pasaron=0
function Verificar([bool]$condicion,[string]$nombre){if(!$condicion){throw "FAIL: $nombre"};$script:pasaron++;Write-Output "PASS: $nombre"}
function Consulta([string]$texto,[string]$base=$database){
    $r=& sqlcmd -S $ServerInstance -d $base -E -b -h -1 -W -Q "SET NOCOUNT ON; $texto" 2>&1
    if($LASTEXITCODE -ne 0){throw ($r -join "`n")};return ($r -join "`n").Trim()
}
function Archivo([string]$nombre,[string]$contenido){[IO.File]::WriteAllText((Join-Path $carpeta $nombre),$contenido,[Text.UTF8Encoding]::new($true))}
New-Item -ItemType Directory -Path $carpeta | Out-Null
Copy-Item -LiteralPath (Join-Path $raiz 'Database/EjecutarTodosLosScripts.ps1') -Destination $carpeta
$ejecutor=Join-Path $carpeta 'EjecutarTodosLosScripts.ps1'
try {
    Archivo '01_Destino.sql' @"
IF DB_ID(N'StageUp') IS NULL
BEGIN
 CREATE DATABASE StageUp;
END
GO
USE StageUp;
GO
IF DB_NAME()<>N'$database' THROW 51000,'Destino incorrecto; se detiene sin modificar datos.',1;
CREATE TABLE dbo.QA_Destino(id INT);
INSERT dbo.QA_Destino VALUES(1);
GO
USE master;
GO
USE [StageUp];
GO
IF DB_NAME()<>N'$database' THROW 51000,'Retorno de master incorrecto.',1;
"@
    Archivo '02_Fallo.sql' "THROW 51000,'Fallo de prueba controlado',1;"
    Archivo '03_Siguiente.sql' 'INSERT dbo.QA_Destino VALUES(3);'
    Archivo '87_MasCorreccionesYDatosDemo.sql' "THROW 51000,'La carga opcional no debe ejecutarse',1;"
    $ErrorActionPreference='Continue'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ejecutor -ServerInstance $ServerInstance -Database $database 2>&1 | Out-String | Write-Output
    $codigo=$LASTEXITCODE
    $ErrorActionPreference='Stop'
    Verificar ($codigo -ne 0) 'un script fallido detiene la ejecucion'
    Verificar ((Consulta 'SELECT COUNT(*) FROM dbo.QA_Destino;') -eq '1') 'USE StageUp se adapta al destino solicitado'
    Verificar ((Consulta "SELECT COUNT(*) FROM dbo._ScriptsEjecutados WHERE nombreScript=N'01_Destino.sql';") -eq '1') 'se registra en la misma base'
    Verificar ((Consulta "SELECT COUNT(*) FROM dbo._ScriptsEjecutados WHERE nombreScript=N'02_Fallo.sql';") -eq '0') 'el fallido no se registra'
    Verificar ((Consulta "SELECT COUNT(*) FROM dbo._ScriptsEjecutados WHERE nombreScript=N'03_Siguiente.sql';") -eq '0') 'no se ejecuta el siguiente tras un fallo'
    Archivo '02_Fallo.sql' 'INSERT dbo.QA_Destino VALUES(2);'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ejecutor -ServerInstance $ServerInstance -Database $database
    Verificar ($LASTEXITCODE -eq 0) 'el reintento completa los pendientes'
    Verificar ((Consulta 'SELECT COUNT(*) FROM dbo.QA_Destino;') -eq '3') 'el reintento no repite el primer script'
    Verificar ((Consulta "SELECT COUNT(*) FROM dbo._ScriptsEjecutados WHERE nombreScript LIKE N'87%';") -eq '0') 'el demo opcional no se registra como aplicado'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ejecutor -ServerInstance $ServerInstance -Database $database
    Verificar ($LASTEXITCODE -eq 0 -and (Consulta 'SELECT COUNT(*) FROM dbo.QA_Destino;') -eq '3') 'sin pendientes no cambia datos'
    Write-Output "$pasaron comprobaciones del ejecutor aprobadas."
}
finally {
    if($database -notmatch '^StageUp_Beta2_QA_Runner_[a-f0-9]{32}$'){throw 'Nombre QA inesperado.'}
    $null=Consulta "IF DB_ID(N'$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END;" 'master'
}
