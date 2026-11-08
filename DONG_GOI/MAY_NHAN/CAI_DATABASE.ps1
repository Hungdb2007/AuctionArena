#requires -version 5.1
param(
    [string]$Server,
    [switch]$NonInteractive,
    [switch]$ConfigureApp,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'
$databaseName = 'AuctionArenaDB'

function Write-Log([string]$message) {
    Write-Host $message
    if (-not [string]::IsNullOrWhiteSpace($LogFile)) {
        Add-Content -LiteralPath $LogFile -Value $message -Encoding UTF8
    }
}

function Run-Command($connection, [string]$sql, [int]$timeout = 60) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = $sql
        $command.CommandTimeout = $timeout
        return $command.ExecuteScalar()
    }
    finally { $command.Dispose() }
}

function Check-RequiredSchema([string]$serverConnectionString) {
    # Danh sách cột lấy từ các Entity đang dùng bởi Entity Framework 6.
    $schemaPath = Join-Path $PSScriptRoot 'SQL\SchemaColumns.tsv'
    if (-not (Test-Path -LiteralPath $schemaPath)) {
        throw 'Thieu SQL\SchemaColumns.tsv trong goi cai dat.'
    }

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder.ConnectionString = $serverConnectionString
    $builder['Initial Catalog'] = 'AuctionArenaDB'
    $connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $actual = @{}
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = @'
SELECT TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
'@
            $reader = $command.ExecuteReader()
            try {
                while ($reader.Read()) {
                    $key = ($reader.GetString(0) + '.' + $reader.GetString(1)).ToLowerInvariant()
                    $actual[$key] = $true
                }
            }
            finally { $reader.Dispose() }
        }
        finally { $command.Dispose() }
    }
    finally { $connection.Dispose() }

    $missing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($line in [System.IO.File]::ReadAllLines($schemaPath, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
        $parts = $line -split "`t"
        if ($parts.Count -ne 2) { throw 'Danh sach cot trong SQL\SchemaColumns.tsv khong hop le.' }
        $key = ($parts[0] + '.' + $parts[1]).ToLowerInvariant()
        if (-not $actual.ContainsKey($key)) { $missing.Add($parts[0] + '.' + $parts[1]) }
    }
    if ($missing.Count -gt 0) {
        $sample = ($missing | Select-Object -First 15) -join ', '
        $extra = if ($missing.Count -gt 15) { ' ...' } else { '' }
        throw ('AuctionArenaDB con thieu ' + $missing.Count +
            ' cot. Vi du: ' + $sample + $extra + '. Sao luu du lieu va chay SQL\02_DongBoCauTruc.sql trong SSMS tren dung server; khong khoi tao lai database dang su dung.')
    }
    Write-Log ('Kiem tra cau truc du lieu: ' + $actual.Count + ' cot hien co, tat ca ' +
        ([Math]::Max(0, [System.IO.File]::ReadAllLines($schemaPath).Length - 1)) + ' cot bat buoc deu day du.')
}

function Sync-PaymentAndDeliverySchema([string]$serverConnectionString) {
    $scriptPath = Join-Path $PSScriptRoot 'SQL\04_DongBoThanhToanXuatKho.sql'
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw 'Thieu SQL\04_DongBoThanhToanXuatKho.sql trong goi cai dat.'
    }
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder.ConnectionString = $serverConnectionString
    $builder['Initial Catalog'] = 'AuctionArenaDB'
    $schemaConnection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    try {
        $schemaConnection.Open()
        $command = $schemaConnection.CreateCommand()
        try {
            $command.CommandTimeout = 120
            $command.CommandText = [System.IO.File]::ReadAllText($scriptPath, [System.Text.Encoding]::UTF8)
            $null = $command.ExecuteNonQuery()
        }
        finally { $command.Dispose() }
        Write-Log 'Cau truc thanh toan va xuat kho da san sang.'
    }
    catch {
        throw ('Khong the chuan bi cau truc thanh toan va xuat kho. Server: ' + $builder.DataSource +
            '. Database: AuctionArenaDB. Can quyen ALTER khi con thieu cau truc; quan tri vien co the chay ' +
            'SQL\04_DongBoThanhToanXuatKho.sql trong SSMS. Chi tiet: ' + $_.Exception.GetBaseException().Message)
    }
    finally { $schemaConnection.Dispose() }
}

function Save-AppConnection([string]$serverName) {
    $configPath = Join-Path $PSScriptRoot 'AuctionArena.WinForms.exe.config'
    if (-not (Test-Path -LiteralPath $configPath)) {
        throw 'Khong tim thay file .exe.config de luu ten SQL Server.'
    }
    [xml]$config = Get-Content -LiteralPath $configPath -Encoding UTF8
    $cs = $config.SelectSingleNode('/configuration/connectionStrings/add[@name="AuctionArenaDB"]')
    $flag = $config.SelectSingleNode('/configuration/appSettings/add[@key="ConnectionConfigured"]')
    if (($null -eq $cs) -or ($null -eq $flag)) {
        throw 'File cau hinh thieu connectionStrings hoac ConnectionConfigured.'
    }
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source'] = $serverName
    $builder['Initial Catalog'] = 'AuctionArenaDB'
    $builder['Integrated Security'] = $true
    $builder['MultipleActiveResultSets'] = $true
    $builder['TrustServerCertificate'] = $true
    $builder['Connect Timeout'] = 8
    $builder['Application Name'] = 'AuctionArena'
    $cs.SetAttribute('connectionString', $builder.ConnectionString)
    $cs.SetAttribute('providerName', 'System.Data.SqlClient')
    $flag.SetAttribute('value', 'true')
    $config.Save($configPath)
}

try {
    Add-Type -AssemblyName System.Data
    if ($LogFile) {
        $parent = Split-Path -Path $LogFile -Parent
        if ($parent -and -not (Test-Path -LiteralPath $parent)) {
            New-Item -Path $parent -ItemType Directory -Force | Out-Null
        }
        Set-Content -LiteralPath $LogFile -Value 'AUCTION ARENA - CAU HINH DATABASE' -Encoding UTF8
    }
    if ([string]::IsNullOrWhiteSpace($Server)) {
        if ($NonInteractive) { throw 'Chua chon ten SQL Server trong bo cai.' }
        $Server = Read-Host 'Ten SQL Server (vd .\SQLEXPRESS, localhost, MAYCHU\INSTANCE)'
    }
    $Server = $Server.Trim()
    if (-not $Server) { throw 'Ten SQL Server khong hop le.' }
    # Ten instance co dang MAYCHU\TENINSTANCE, chi co mot dau gach cheo nguoc.
    $serverPart = $Server -split '\\'
    if ($serverPart.Count -gt 2 -or ($serverPart.Count -eq 2 -and
        ([string]::IsNullOrWhiteSpace($serverPart[0]) -or [string]::IsNullOrWhiteSpace($serverPart[1])))) {
        throw ('Ten SQL Server bi lap instance: ' + $Server +
            '. Chi nhap .\SQLEXPRESS hoac MAYCHU\SQLEXPRESS.')
    }

    $master = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $master['Data Source'] = $Server
    $master['Initial Catalog'] = 'master'
    $master['TrustServerCertificate'] = $true
    $master['Connect Timeout'] = 10
    $master['Application Name'] = 'AuctionArena.DatabaseSetup'

    $useWindows = $true
    if (-not $NonInteractive) {
        $choice = Read-Host 'Dung Windows Authentication? (Y/n)'
        if ($choice -match '^[nN]$') { $useWindows = $false }
    }
    $master['Integrated Security'] = $useWindows
    if (-not $useWindows) {
        $master['User ID'] = Read-Host 'Ten dang nhap SQL Server'
        $secure = Read-Host 'Mat khau SQL Server' -AsSecureString
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try { $master['Password'] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
        finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
        # Khong ghi mat khau SQL Server vao log hay cau lenh thuc thi.
        if ($ConfigureApp) {
            Write-Log 'SQL Authentication: ung dung se yeu cau luu ket noi rieng sau khi cai.'
            $ConfigureApp = $false
        }
    }

    $sqlPath = Join-Path $PSScriptRoot 'SQL\01_TaoDatabase.sql'
    if (-not (Test-Path -LiteralPath $sqlPath)) { throw 'Thieu SQL\01_TaoDatabase.sql.' }

    $connection = New-Object System.Data.SqlClient.SqlConnection($master.ConnectionString)
    try {
        try {
            $connection.Open()
        }
        catch {
            throw ('Khong ket noi duoc SQL Server ' + $Server +
                '. Kiem tra SQL Server dang chay, dung ten instance va tai khoan co quyen. Chi tiet: ' +
                $_.Exception.GetBaseException().Message)
        }
        Write-Log ('Da ket noi: ' + $Server)
        $check = $connection.CreateCommand()
        try {
            $check.CommandText = 'SELECT DB_ID(@database)'
            $null = $check.Parameters.Add('@database', [System.Data.SqlDbType]::NVarChar, 128)
            $check.Parameters['@database'].Value = $databaseName
            $found = $check.ExecuteScalar()
        }
        finally { $check.Dispose() }

        if (($null -ne $found) -and -not ($found -is [DBNull])) {
            Write-Log 'AuctionArenaDB da ton tai. Kiem tra schema truoc khi tiep tuc; khong ghi de du lieu.'
        }
        else {
            if (-not $NonInteractive) {
                $answer = Read-Host 'Chua co AuctionArenaDB. Tao database? (Y/n)'
                if ($answer -match '^[nN]$') { throw 'Da huy khoi tao database.' }
            }
            Write-Log 'Tao database AuctionArenaDB va cac bang nghiep vu...'
            $sql = [System.IO.File]::ReadAllText($sqlPath, [System.Text.Encoding]::UTF8)
            $batches = [regex]::Split($sql, '(?im)^\s*GO\s*$')
            $completed = 0
            foreach ($batch in $batches) {
                if ([string]::IsNullOrWhiteSpace($batch)) { continue }
                $command = $connection.CreateCommand()
                try {
                    $command.CommandTimeout = 180
                    $command.CommandText = $batch
                    $null = $command.ExecuteNonQuery()
                    $completed++
                }
                finally { $command.Dispose() }
            }
            Write-Log ('Da chay ' + $completed + ' dot lenh SQL.')
        }

        # Query trong master su dung ten database day du de khong phu thuoc USE cua script.
        $dbCheck = Run-Command $connection "SELECT DB_ID(N'AuctionArenaDB')"
        if (($null -eq $dbCheck) -or ($dbCheck -is [DBNull])) {
            throw 'Database khong duoc tao. Kiem tra quyen CREATE DATABASE.'
        }
        # Dùng cùng tệp SQL với ứng dụng trước khi kiểm tra toàn bộ các cột.
        Sync-PaymentAndDeliverySchema $master.ConnectionString
        Check-RequiredSchema $master.ConnectionString
        if ($ConfigureApp) {
            Save-AppConnection $Server
            Write-Log 'Da luu cau hinh ket noi Windows Authentication.'
        }
        Write-Log 'THANH CONG.'
    }
    finally { $connection.Dispose() }
    exit 0
}
catch {
    Write-Log ('LOI: ' + $_.Exception.GetBaseException().Message)
    Write-Log 'Kiem tra thong bao loi tren dung server; khong xoa hoac tao lai database dang su dung.'
    exit 1
}
