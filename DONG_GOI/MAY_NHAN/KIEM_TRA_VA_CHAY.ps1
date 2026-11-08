#requires -version 5.1
$ErrorActionPreference = 'Stop'
try {
    $exe = Join-Path $PSScriptRoot 'AuctionArena.WinForms.exe'
    if (-not (Test-Path -LiteralPath $exe)) {
        throw 'Khong tim thay AuctionArena.WinForms.exe. Hay giai nen day du goi ban chay hoac cai lai ung dung.'
    }
    $dotNetKey = 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full'
    $release = (Get-ItemProperty -Path $dotNetKey -Name Release -ErrorAction SilentlyContinue).Release
    if (-not $release -or $release -lt 528040) {
        throw 'May chua cai .NET Framework 4.8. Cai tu Microsoft truoc khi chay Auction Arena.'
    }
    $configPath = Join-Path $PSScriptRoot 'AuctionArena.WinForms.exe.config'
    if (-not (Test-Path -LiteralPath $configPath)) {
        throw 'Thieu file AuctionArena.WinForms.exe.config.'
    }
    [xml]$config = Get-Content -LiteralPath $configPath -Encoding UTF8
    $flag = $config.SelectSingleNode('/configuration/appSettings/add[@key="ConnectionConfigured"]')
    if (($null -eq $flag) -or ($flag.GetAttribute('value') -ne 'true')) {
        Write-Host 'Lan dau su dung: chon SQL Server va kiem tra database.' -ForegroundColor Cyan
        Write-Host 'SQL Server phai duoc cai va tai khoan cua ban duoc phep truy cap.'
        # Liet ke nhung instance SQL Server dang chay tren may nay neu co.
        $candidates = @()
        $sqlServices = @(Get-Service -ErrorAction SilentlyContinue | Where-Object {
            ($_.Name -eq 'MSSQLSERVER' -or $_.Name -like 'MSSQL$*') -and $_.Status -eq 'Running'
        })
        foreach ($service in $sqlServices) {
            if ($service.Name -eq 'MSSQLSERVER') {
                $candidates += '.'
            }
            else {
                $instanceName = $service.Name.Substring(6)
                $candidates += ('.\' + $instanceName)
            }
        }
        $candidates = @($candidates | Select-Object -Unique)
        if ($candidates.Count -gt 0) {
            Write-Host ('SQL Server tren may: ' + ($candidates -join ', ')) -ForegroundColor Cyan
            $server = Read-Host ('Ten server [Enter = ' + $candidates[0] + ']')
            if ([string]::IsNullOrWhiteSpace($server)) { $server = $candidates[0] }
        }
        else {
            Write-Host 'Khong tim thay dich vu SQL Server dang chay tren may.' -ForegroundColor Yellow
            Write-Host 'Neu co SQL Server tren may khac, hay nhap ten may chu\instance.'
            Write-Host 'Neu chua cai SQL Server, can cai SQL Server Express truoc.'
            $server = Read-Host 'Ten SQL Server (vd .\SQLEXPRESS hoac MAYCHU\SQLEXPRESS)'
        }
        $server = $server.Trim()
        if ([string]::IsNullOrWhiteSpace($server)) { throw 'Ban chua chon SQL Server.' }
        if (($server -split '\\').Count -gt 2) {
            throw ('Ten SQL Server dang bi lap instance: ' + $server +
                '. Vi du dung: .\SQLEXPRESS hoac MAYCHU\SQLEXPRESS. Khi hien goi y, co the bam Enter.')
        }
        # Chạy trình cài DB ở tiến trình riêng để cửa sổ khởi động không bị thoát theo.
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'CAI_DATABASE.ps1') -Server $server -ConfigureApp
        if ($LASTEXITCODE -ne 0) {
            throw 'Chua cau hinh database thanh cong. Kiem tra thong bao ben tren.'
        }
    }
    Start-Process -FilePath $exe -WorkingDirectory $PSScriptRoot
}
catch {
    Write-Host ''
    Write-Host ('Khong the khoi dong: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
