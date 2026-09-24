# RepararTildes.ps1
#
# Repara datos que ya quedaron guardados con acentos/enies corrompidos
# (por ejemplo "LÃ³pez" en vez de "López", "AcÃºstica" en vez de "Acústica")
# a causa de un bug en EjecutarTodosLosScripts.ps1: le faltaba indicarle a
# sqlcmd que lea los archivos .sql como UTF-8, entonces los interpretaba con
# la codificacion por defecto de Windows (Windows-1252) y guardaba en la base
# una version corrompida de cualquier texto con tildes o enies.
#
# Ese bug de raiz YA esta corregido en este mismo paquete (se le agrego el
# flag "-f 65001" a la linea de sqlcmd en EjecutarTodosLosScripts.ps1), asi
# que los PROXIMOS scripts que corras van a guardar el texto bien. Pero ese
# arreglo no repara solo los datos que ya quedaron mal guardados en tu base
# actual — para eso esta este script separado.
#
# QUE HACE: recorre todas las columnas de texto (NVARCHAR/NTEXT) de las
# tablas que tienen una clave primaria de una sola columna (para poder
# corregir fila por fila con seguridad, sin tocar tablas con clave compuesta
# o sin clave). Para cada valor de texto, prueba si "tiene cara" de estar
# corrompido: agarra el texto tal como esta guardado, lo reinterpreta como si
# fueran bytes Windows-1252, y despues intenta decodificar esos bytes como
# UTF-8 en modo estricto (sin tolerar errores). Si esa decodificacion anda
# bien Y da un texto DISTINTO al original, es casi seguro que el original
# estaba corrompido y el resultado es el texto real (por ejemplo "López").
# Si la decodificacion falla, quiere decir que ese texto nunca tuvo este
# problema, y se deja completamente intacto.
#
# Este chequeo es seguro incluso para columnas que guardan datos encriptados
# en Base64 (como el telefono): un texto Base64 es puro ASCII, y un ASCII
# puro da exactamente el mismo resultado en la ida y vuelta, asi que nunca se
# marca como "distinto" y nunca se toca.
#
# USO RECOMENDADO — primero en modo simulacion, sin escribir nada en la base:
#   .\RepararTildes.ps1 -WhatIf
#
# Repasa la lista de cambios que te muestra. Si tiene sentido (deberias ver
# cosas como "LÃ³pez" -> "López"), corré el arreglo real:
#   .\RepararTildes.ps1
#
# Si tu instancia de SQL Server no es ".\SQLEXPRESS":
#   .\RepararTildes.ps1 -ServerInstance "BELUENVY\SQLEXPRESS01" -WhatIf
#
# Requiere PowerShell con acceso a System.Data.SqlClient (viene incluido en
# Windows / .NET Framework, no hace falta instalar nada aparte).

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

# 1) Tablas con exactamente una columna de clave primaria (las unicas que este
#    script puede reparar fila por fila con seguridad).
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
                # No decodifico como UTF-8 valido -> este texto no estaba
                # corrompido de esta forma, se deja exactamente como esta.
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
