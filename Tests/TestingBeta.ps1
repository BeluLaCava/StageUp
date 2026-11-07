param([string]$BinPath = (Join-Path $PSScriptRoot '..\StageUp.UI\bin'))
$ErrorActionPreference = 'Stop'
$BinPath = (Resolve-Path -LiteralPath $BinPath).Path
foreach ($assembly in @('BE','DAL','Seguridad','MPP','Servicios','BLL','UI')) {
    $null = [Reflection.Assembly]::LoadFrom((Join-Path $BinPath "StageUp.$assembly.dll"))
}
$passed = 0
function Check([bool]$condition, [string]$test) {
    if (!$condition) { throw "FAILED: $test" }
    $script:passed++
    Write-Output "PASS: $test"
}

foreach ($email in @('persona@example.test', "o'connor@example.test", 'ana+reservas@example.com', 'ana.apellido@sub.example.com')) {
    Check ([StageUp.Seguridad.ValidacionContacto]::EsCorreoValido($email)) "valid email: $email"
}
foreach ($email in @('a@b..com','a@-dominio.test','a..b@example.test','a.@example.test','no-email',"ana@example.test`r`nBcc:otra@example.test",('a'*65+'@example.test'),('a'*301))) {
    Check (![StageUp.Seguridad.ValidacionContacto]::EsCorreoValido($email)) 'invalid email rejected'
}
foreach ($phone in @('+54 11 5555-5555','(011) 4444-5555','123456')) {
    Check ([StageUp.Seguridad.ValidacionContacto]::EsTelefonoValido($phone)) "valid phone: $phone"
}
foreach ($phone in @('++++++','------','()()()','12345','abc123','123456(',')123456',('1'*31),"123456`n")) {
    Check (![StageUp.Seguridad.ValidacionContacto]::EsTelefonoValido($phone)) 'invalid phone rejected'
}
$bll = [StageUp.BLL.BLL_UsuarioExterno]::new()
$result = $bll.Registrar(('a'*201),'Apellido','qa@example.test','StageUp2026','StageUp2026',$true,$true,$null)
Check (!$result.Exitoso -and $result.Mensaje.Contains('200')) 'registration checks name length before persistence'
$result = $bll.Registrar('Nombre','Apellido',('a'*301),'StageUp2026','StageUp2026',$true,$true,$null)
Check (!$result.Exitoso -and $result.CodigoAlternativo -eq 'A2') 'registration checks email length before persistence'
$result = $bll.Registrar('Nombre','Apellido','qa@example.test','StageUp2026','StageUp2026',$true,$true,'++++++')
Check (!$result.Exitoso -and $result.CodigoAlternativo -eq 'A6') 'registration rejects invalid phone before persistence'
$result = $bll.Registrar("O'Connor",'Apellido','qa@example.test','StageUp2026','different',$true,$true,$null)
Check (!$result.Exitoso -and $result.CodigoAlternativo -eq 'A4') 'legitimate apostrophe reaches normal password validation'
$method = [StageUp.DAL.Conexion].GetMethod('AgregarParametros', [Reflection.BindingFlags]'NonPublic,Static')
foreach ($value in @("O'Connor", "' OR '1'='1", "'; DROP TABLE UsuarioExterno; --")) {
    $command = [System.Data.SqlClient.SqlCommand]::new('sp_NotExecuted')
    $command.CommandType = [System.Data.CommandType]::StoredProcedure
    $null = $method.Invoke($null, @($command, @{ '@texto' = $value }))
    Check ($command.CommandText -eq 'sp_NotExecuted' -and $command.Parameters['@texto'].Value -ceq $value) 'SQL values remain parameters; command unchanged'
    $command.Dispose()
}
Write-Output "$passed checks passed. No database commands were executed."
