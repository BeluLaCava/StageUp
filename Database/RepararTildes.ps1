

param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$Database = "StageUp",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Data

$connectionString = "Server=$ServerInstance;Database=$Database;Integrated Security=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()

Write-Host "Conectado a '$Database' en '$ServerInstance'." -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "Modo -WhatIf: solo se va a MOSTRAR lo que se repararia, no se escribe nada." -ForegroundColor Cyan
}

$consultaTablas = @"
SELECT
    t.name AS TableName,
    pkcol.name AS PkColumnName
FROM sys.tables t
CROSS APPLY (
    SELECT TOP 1 c.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    INNER JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = t.object_id AND i.is_primary_key = 1
    ORDER BY ic.key_ordinal
) pkcol(name)
WHERE t.name <> N'_ScriptsEjecutados'
  AND (
        SELECT COUNT(*)
        FROM sys.indexes i2
        INNER JOIN sys.index_columns ic2 ON ic2.object_id = i2.object_id AND ic2.index_id = i2.index_id
        WHERE i2.object_id = t.object_id AND i2.is_primary_key = 1
      ) = 1
"@

$tablasCmd = New-Object System.Data.SqlClient.SqlCommand $consultaTablas, $connection
$tablasReader = $tablasCmd.ExecuteReader()
$tablas = @()
while ($tablasReader.Read()) {
    $tablas += [PSCustomObject]@{
        Tabla     = $tablasReader["TableName"]
        ColumnaPk = $tablasReader["PkColumnName"]
    }
}
$tablasReader.Close()

Write-Host "Se encontraron $($tablas.Count) tabla(s) con clave primaria simple para revisar.`n" -ForegroundColor Cyan

$cp1252 = [System.Text.Encoding]::GetEncoding(1252, [System.Text.EncoderExceptionFallback]::new(), [System.Text.DecoderExceptionFallback]::new())
$utf8Estricto = New-Object System.Text.UTF8Encoding($false, $true)

$totalFilasCambiadas = 0
$totalValoresCambiados = 0

foreach ($tablaInfo in $tablas) {
    $tabla = $tablaInfo.Tabla
    $columnaPk = $tablaInfo.ColumnaPk

    $consultaColumnas = @"
SELECT c.name AS ColumnName
FROM sys.columns c
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.$tabla')
  AND ty.name IN (N'nvarchar', N'ntext')
  AND c.name <> N'$columnaPk'
"@
    $colCmd = New-Object System.Data.SqlClient.SqlCommand $consultaColumnas, $connection
    $colReader = $colCmd.ExecuteReader()
    $columnasTexto = @()
    while ($colReader.Read()) { $columnasTexto += $colReader["ColumnName"] }
    $colReader.Close()

    if ($columnasTexto.Count -eq 0) { continue }

    $listaColumnas = ($columnasTexto | ForEach-Object { "[$_]" }) -join ", "
    $consultaFilas = "SELECT [$columnaPk], $listaColumnas FROM dbo.[$tabla]"
    $filasCmd = New-Object System.Data.SqlClient.SqlCommand $consultaFilas, $connection
    $filasReader = $filasCmd.ExecuteReader()

    $filasAReparar = @()

    while ($filasReader.Read()) {
        $idValor = $filasReader[$columnaPk]
        $cambios = @{}

        foreach ($columna in $columnasTexto) {
            $valor = $filasReader[$columna]
            if ($valor -eq [DBNull]::Value -or [string]::IsNullOrEmpty($valor)) { continue }

            try {
                $bytes = $cp1252.GetBytes([string]$valor)
                $reparado = $utf8Estricto.GetString($bytes)
                if ($reparado -ne $valor) {
                    $cambios[$columna] = $reparado
                }
            } catch {

            }
        }

        if ($cambios.Count -gt 0) {
            $filasAReparar += [PSCustomObject]@{ Id = $idValor; Cambios = $cambios }
        }
    }
    $filasReader.Close()

    if ($filasAReparar.Count -eq 0) { continue }

    Write-Host "Tabla dbo.$tabla : $($filasAReparar.Count) fila(s) con texto para reparar." -ForegroundColor Yellow

    foreach ($fila in $filasAReparar) {
        foreach ($columna in $fila.Cambios.Keys) {
            Write-Host "  [$columnaPk=$($fila.Id)] $columna -> '$($fila.Cambios[$columna])'"
        }

        if (-not $WhatIf) {
            foreach ($columna in $fila.Cambios.Keys) {
                $updateSql = "UPDATE dbo.[$tabla] SET [$columna] = @valor WHERE [$columnaPk] = @id"
                $updateCmd = New-Object System.Data.SqlClient.SqlCommand $updateSql, $connection
                [void]$updateCmd.Parameters.AddWithValue("@valor", $fila.Cambios[$columna])
                [void]$updateCmd.Parameters.AddWithValue("@id", $fila.Id)
                [void]$updateCmd.ExecuteNonQuery()
                $totalValoresCambiados++
            }
            $totalFilasCambiadas++
        }
    }
    Write-Host ""
}

$connection.Close()

if ($WhatIf) {
    Write-Host "(Modo -WhatIf: no se escribio nada. Volve a correr sin -WhatIf para aplicar estos cambios.)" -ForegroundColor Cyan
} else {
    Write-Host "Listo: se repararon $totalValoresCambiados valor(es) de texto en $totalFilasCambiadas fila(s)." -ForegroundColor Green
}
