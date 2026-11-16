# This script runs only on a GitHub-hosted Windows runner.
# It does not connect to any developer's SQL Server.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($env:GITHUB_ACTIONS -ne 'true') {
    throw 'CI-only script. Do not run it on a developer machine.'
}

$sqlPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'db\init\DongNamTrip_Full.sql'
if (-not (Test-Path -LiteralPath $sqlPath)) {
    throw "Missing $sqlPath. Commit the team's DongNamTrip_Full.sql to db/init/."
}
$sqlText = [System.IO.File]::ReadAllText($sqlPath, [System.Text.Encoding]::UTF8)
if ([string]::IsNullOrWhiteSpace($sqlText)) {
    throw 'db/init/DongNamTrip_Full.sql is empty.'
}

Add-Type -AssemblyName System.Data

$localDbCommand = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
$localDbExe = $null
if ($null -ne $localDbCommand) {
    $localDbExe = $localDbCommand.Source
}
else {
    foreach ($versionFolder in @('170', '160', '150')) {
        $candidate = Join-Path $env:ProgramFiles "Microsoft SQL Server\$versionFolder\Tools\Binn\SqlLocalDB.exe"
        if (Test-Path -LiteralPath $candidate) {
            $localDbExe = $candidate
            break
        }
    }
}
if ([string]::IsNullOrWhiteSpace($localDbExe)) {
    throw 'SqlLocalDB.exe is missing. Use the windows-2022 runner image.'
}

$instance = 'DongNamTripCI'
& $localDbExe create $instance -s
if ($LASTEXITCODE -ne 0) {
    throw 'Could not create/start the isolated LocalDB instance.'
}

# This server name is intentionally fixed to the CI-only LocalDB instance.
$masterConnection = 'Server=(localdb)\DongNamTripCI;Database=master;Integrated Security=True;Encrypt=False;Connect Timeout=10;'
$connection = [System.Data.SqlClient.SqlConnection]::new($masterConnection)
try {
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        try {
            $connection.Open()
            break
        }
        catch {
            if ($attempt -eq 6) { throw }
            Start-Sleep -Seconds 2
        }
    }

    # GO is an SSMS/sqlcmd batch separator, not a SQL Server statement.
    # The shared DongNamTrip_Full.sql uses standalone GO lines.
    $batches = [regex]::Split($sqlText, '(?im)^[ \t]*GO[ \t]*(?:--[^\r\n]*)?\r?$')
    foreach ($batch in $batches) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $command = $connection.CreateCommand()
        try {
            $command.CommandTimeout = 180
            $command.CommandText = $batch
            [void]$command.ExecuteNonQuery()
        }
        finally {
            $command.Dispose()
        }
    }

    $connection.ChangeDatabase('DongNamTrip')
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = 'SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0;'
        $tableCount = [int]$command.ExecuteScalar()
        $command.CommandText = 'SELECT COUNT(*) FROM dbo.VAITRO;'
        $roleCount = [int]$command.ExecuteScalar()
        $command.CommandText = 'SELECT COUNT(*) FROM dbo.NGONNGU;'
        $languageCount = [int]$command.ExecuteScalar()
        if ($tableCount -ne 25 -or $roleCount -ne 6 -or $languageCount -lt 1) {
            throw 'CI database must contain 25 tables, 6 roles, and language seed data.'
        }
    }
    finally {
        $command.Dispose()
    }
}
finally {
    $connection.Dispose()
}

Write-Host 'CI LocalDB initialized: 25 tables, 6 roles, and language seed data.'
