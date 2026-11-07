param(
    [Parameter(Mandatory=$true)][ValidatePattern('^StageUp_Beta2_QA_[A-Za-z0-9_]+$')][string]$Database,
    [string]$ServerInstance='localhost'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$temp=Join-Path ([IO.Path]::GetTempPath()) ('StageUp_Busqueda91_'+[Guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory -Path $temp
$builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$builder['Data Source']=$ServerInstance; $builder['Initial Catalog']=$Database
$builder['Integrated Security']=$true; $builder['TrustServerCertificate']=$true
$connection=New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
$created=@()
try {
    $connection.Open()
    foreach($item in @(
        @{Script='51_RendimientoYBitacora.sql';Name='sp_RegistroActividad_Buscar'},
        @{Script='88_BusquedasYMensajeriaCompletas.sql';Name='sp_Busqueda_Interna'}
    )) {
        $text=[IO.File]::ReadAllText((Join-Path $repo ('Database\'+$item.Script)))
        $pattern='(?ims)^CREATE PROCEDURE dbo\.'+[Regex]::Escape($item.Name)+'\b.*?(?=^GO\s*$)'
        $match=[Regex]::Match($text,$pattern)
        if(-not $match.Success){throw 'No se encontro el procedimiento de referencia.'}
        $versions=@(@{Suffix='_QA_Antes91';Body=$match.Value})
        if($item.Name -eq 'sp_Busqueda_Interna') {
            $versions+=@{Suffix='_QA_DetalleCompleto';Body=$match.Value.Replace('titulo NVARCHAR(400), detalle NVARCHAR(1000)','titulo NVARCHAR(MAX), detalle NVARCHAR(MAX)')}
        }
        foreach($v in $versions){
            $name=$item.Name+$v.Suffix
            $command=$connection.CreateCommand()
            $command.CommandText="IF OBJECT_ID(N'dbo.$name',N'P') IS NOT NULL THROW 51000,'Ya existe una referencia QA; no se reemplaza.',1;"
            $null=$command.ExecuteNonQuery()
            $command.CommandText=$v.Body.Replace('CREATE PROCEDURE dbo.'+$item.Name,'CREATE PROCEDURE dbo.'+$name)
            $null=$command.ExecuteNonQuery();$created+=$name
        }
    }
    $csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $exe=Join-Path $temp 'TestingBusqueda91.exe'
    & $csc /nologo /r:System.Data.dll /r:System.Core.dll ('/out:'+$exe) (Join-Path $PSScriptRoot 'TestingBusqueda91.cs')
    if($LASTEXITCODE -ne 0){throw 'No se pudo compilar la prueba.'}
    & $exe $ServerInstance $Database
    if($LASTEXITCODE -ne 0){throw 'Difieren los resultados de las consultas. Revisar antes de aplicar.'}
} finally {
    foreach($name in $created){
        if($connection.State -eq 'Open'){
            $command=$connection.CreateCommand();$command.CommandText='DROP PROCEDURE dbo.'+$name
            $null=$command.ExecuteNonQuery()
        }
    }
    $connection.Dispose()
}
