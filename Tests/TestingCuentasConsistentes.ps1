param(
    [Parameter(Mandatory=$true)][ValidatePattern('^StageUp_Beta2_QA_[A-Za-z0-9_]+$')][string]$Database,
    [string]$ServerInstance = 'localhost'
)
$ErrorActionPreference='Stop'

$repo=Split-Path $PSScriptRoot -Parent
$bin=Join-Path $repo 'StageUp.UI\bin'
$salida=Join-Path ([IO.Path]::GetTempPath()) ('StageUp_Beta2_Tests_'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $salida | Out-Null
Copy-Item (Join-Path $bin 'StageUp.*.dll') $salida
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$b=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$b['Data Source']=$ServerInstance; $b['Initial Catalog']=$Database; $b['Integrated Security']=$true; $b['TrustServerCertificate']=$true
$cadena=[Security.SecurityElement]::Escape($b.ConnectionString)
foreach($test in @('TestingCuentasConsistentes')) {
    $exe=Join-Path $salida ($test+'.exe')
    $refs=@('BE','BLL','DAL','MPP','Seguridad','Servicios','UI') | ForEach-Object { '/r:'+(Join-Path $salida ('StageUp.'+$_+'.dll')) }
    & $csc /nologo ('/out:'+$exe) @refs /r:System.Configuration.dll /r:System.Data.dll /r:System.Web.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll (Join-Path $PSScriptRoot ($test+'.cs'))
    if($LASTEXITCODE -ne 0){throw 'No se pudo compilar la prueba.'}
    $config=@"
<configuration><connectionStrings><add name="StageUpConnectionString" connectionString="$cadena" /></connectionStrings><appSettings>
<add key="PerfilCompletoHabilitado" value="true"/><add key="EspaciosFichaCompletaHabilitada" value="true"/><add key="SmtpHost" value="127.0.0.1"/><add key="SmtpPuerto" value="1"/><add key="SmtpUsarSsl" value="false"/>
<add key="SmtpUsuario" value="qa"/><add key="SmtpPassword" value="qa"/><add key="CorreoRemitente" value="qa@example.test"/><add key="NombreRemitente" value="QA aislado"/>
</appSettings></configuration>
"@
    [IO.File]::WriteAllText($exe+'.config',$config,[Text.UTF8Encoding]::new($false))
    & $exe
    if($LASTEXITCODE -ne 0){throw "Fallaron las pruebas de $test."}
}
Write-Output 'Pruebas de negocio completadas únicamente en la copia QA.'
