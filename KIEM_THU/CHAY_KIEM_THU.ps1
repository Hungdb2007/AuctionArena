#requires -version 5.1
param([string]$Server)
$ErrorActionPreference = 'Stop'
$exitCode = 1
$testDatabase = 'AA_Test_' + [Guid]::NewGuid().ToString('N')
$temporary = Join-Path ([IO.Path]::GetTempPath()) $testDatabase
$masterConnection = $null
$ownsDatabase = $false
$oldConnection = $env:AUCTIONARENA_TEST_CONNECTION
try {
    Add-Type -AssemblyName System.Data
    if ([string]::IsNullOrWhiteSpace($Server)) { $Server = Read-Host 'SQL Server dung de kiem thu (Windows Authentication)' }
    if ([string]::IsNullOrWhiteSpace($Server)) { throw 'Chua nhap SQL Server.' }
    $root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $runtime = Join-Path $root 'DONG_GOI\BAN_CHAY_LOCAL\AuctionArena'
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
    if (-not (Test-Path $compiler)) { throw 'Can .NET Framework 4.8 de chay kiem thu.' }
    New-Item -ItemType Directory -Path $temporary | Out-Null
    Copy-Item (Join-Path $runtime '*.dll') $temporary
    $exe = Join-Path $temporary 'ServiceChecks.exe'
    $arguments = @('/nologo', '/target:exe', ('/out:' + $exe), '/r:System.dll', '/r:System.Core.dll', '/r:System.Data.dll', '/r:System.Xml.dll', '/r:System.ComponentModel.DataAnnotations.dll')
    foreach ($name in @('AuctionArena.Data.dll','AuctionArena.Services.dll','EntityFramework.dll','EntityFramework.SqlServer.dll')) {
        $arguments += '/r:' + (Join-Path $temporary $name)
    }
    $arguments += Join-Path $PSScriptRoot 'ServiceChecks.cs'
    & $compiler $arguments
    if ($LASTEXITCODE -ne 0) { throw 'Bien dich bo kiem thu that bai.' }
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source'] = $Server.Trim()
    $builder['Initial Catalog'] = 'master'
    $builder['Integrated Security'] = $true
    $builder['TrustServerCertificate'] = $true
    $builder['MultipleActiveResultSets'] = $true
    $builder['Connect Timeout'] = 10
    $masterConnection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $masterConnection.Open()
    $check = $masterConnection.CreateCommand()
    try {
        $check.CommandText = 'SELECT DB_ID(@name)'
        $null = $check.Parameters.Add('@name', [Data.SqlDbType]::NVarChar, 128)
        $check.Parameters['@name'].Value = $testDatabase
        $existing = $check.ExecuteScalar()
        if ($existing -isnot [DBNull] -and $null -ne $existing) { throw 'Ten database kiem thu da ton tai. Dung de bao ve du lieu.' }
    } finally { $check.Dispose() }
    $ownsDatabase = $true
    Write-Host ('Tao database tam: ' + $testDatabase)
    $sql = [IO.File]::ReadAllText((Join-Path $root 'SQL\01_TaoDatabase.sql'), [Text.Encoding]::UTF8).Replace('AuctionArenaDB', $testDatabase)
    foreach ($batch in [regex]::Split($sql, '(?im)^\s*GO\s*$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $command = $masterConnection.CreateCommand()
        try {
            $command.CommandTimeout = 180
            $command.CommandText = $batch
            $null = $command.ExecuteNonQuery()
        } finally { $command.Dispose() }
    }
    $builder['Initial Catalog'] = $testDatabase
    $env:AUCTIONARENA_TEST_CONNECTION = $builder.ConnectionString
    & $exe 2>&1 | Tee-Object -FilePath (Join-Path $PSScriptRoot 'KET_QUA_KIEM_THU.txt')
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) { throw 'Kiem thu khong dat. Xem KET_QUA_KIEM_THU.txt.' }
}
catch { Write-Host ('LOI: ' + $_.Exception.Message) -ForegroundColor Red; $exitCode = 1 }
finally {
    $env:AUCTIONARENA_TEST_CONNECTION = $oldConnection
    if ($masterConnection) { $masterConnection.Dispose() }
    [System.Data.SqlClient.SqlConnection]::ClearAllPools()
    if ($ownsDatabase -and $testDatabase -match '^AA_Test_[a-f0-9]{32}$') {
        $builder['Initial Catalog'] = 'master'
        $cleanup = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
        try {
            $cleanup.Open()
            $command = $cleanup.CreateCommand()
            try {
                $command.CommandTimeout = 60
                $command.CommandText = "IF DB_ID(N'$testDatabase') IS NOT NULL BEGIN ALTER DATABASE [$testDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$testDatabase]; END"
                $null = $command.ExecuteNonQuery()
                Write-Host ('Da xoa database tam: ' + $testDatabase)
            } finally { $command.Dispose() }
        }
        catch { Write-Host ('Khong xoa duoc database tam ' + $testDatabase + ': ' + $_.Exception.Message); $exitCode = 1 }
        finally { $cleanup.Dispose() }
    }
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
exit $exitCode
