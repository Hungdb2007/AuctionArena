#requires -Version 5.1
param([switch]$Rebuild)
$ErrorActionPreference = 'Stop'

try {
    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $runFolder = Join-Path $PSScriptRoot 'BAN_CHAY_LOCAL'
    $appFolder = Join-Path $runFolder 'AuctionArena'
    $launcher = Join-Path $appFolder 'CHAY_AUCTION_ARENA.cmd'
    $exportZip = Join-Path $projectRoot 'AuctionArena_BanChay.zip'

    if ($Rebuild -or -not (Test-Path -LiteralPath $launcher)) {
        # Giu cau hinh ket noi khi bien dich lai ma nguon.
        $oldConfig = Join-Path $appFolder 'AuctionArena.WinForms.exe.config'
        $savedConfig = $null
        if ((Test-Path -LiteralPath $oldConfig) -and $Rebuild) {
            $savedConfig = [System.IO.File]::ReadAllBytes($oldConfig)
        }

        Write-Host 'Dang bien dich va dong goi Auction Arena...' -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot 'TAO_BAN_CHAY.ps1')
        if ($LASTEXITCODE -ne 0) {
            throw 'Khong Build duoc ma nguon. Doc loi MSBuild o phia tren.'
        }
        if (-not (Test-Path -LiteralPath $exportZip)) {
            throw 'Khong tim thay ZIP ban chay sau khi Build.'
        }

        if (Test-Path -LiteralPath $runFolder) {
            Remove-Item -LiteralPath $runFolder -Recurse -Force
        }
        New-Item -Path $runFolder -ItemType Directory -Force | Out-Null
        Expand-Archive -LiteralPath $exportZip -DestinationPath $runFolder -Force

        if ($savedConfig) {
            [System.IO.File]::WriteAllBytes((Join-Path $appFolder 'AuctionArena.WinForms.exe.config'), $savedConfig)
        }
    }

    if (-not (Test-Path -LiteralPath $launcher)) {
        throw 'Thieu chuong trinh khoi dong trong ban chay.'
    }

    Write-Host 'Dang mo Auction Arena...' -ForegroundColor Green
    & $launcher
    if ($LASTEXITCODE -ne 0) { exit 1 }
    exit 0
}
catch {
    Write-Host ('LOI: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
