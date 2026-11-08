#requires -version 5.1
$ErrorActionPreference = 'Stop'

try {
    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $solutionPath = Join-Path $projectRoot 'AuctionArena.sln'
    $releaseDir = Join-Path $projectRoot 'AuctionArena.WinForms\bin\Release'
    $resultZip = Join-Path $projectRoot 'AuctionArena_BanChay.zip'
    $stagingDir = Join-Path $projectRoot '_BAN_CHAY_TAM'
    $appDir = Join-Path $stagingDir 'AuctionArena'

    # Locate MSBuild installed with Visual Studio or Build Tools.
    $msbuildPath = $null
    $vswherePaths = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    )
    foreach ($item in $vswherePaths) {
        if (-not $item -or -not (Test-Path $item)) { continue }
        $vsRoot = & $item -latest -products '*' -requires Microsoft.Component.MSBuild -property installationPath
        if ($vsRoot) {
            $candidate = Join-Path $vsRoot 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path $candidate) { $msbuildPath = $candidate; break }
        }
    }
    if (-not $msbuildPath) {
        $found = Get-Command msbuild.exe -ErrorAction SilentlyContinue
        if ($found) { $msbuildPath = $found.Source }
    }
    if (-not $msbuildPath) {
        throw 'Khong tim thay MSBuild. Hay cai Visual Studio voi workload .NET desktop development.'
    }

    Write-Host '1/4 Restore NuGet...' -ForegroundColor Cyan
    & $msbuildPath $solutionPath '/t:Restore' '/p:Configuration=Release' '/p:Platform=Any CPU' '/nologo' '/v:minimal'
    if ($LASTEXITCODE -ne 0) { throw 'Restore NuGet that bai. Kiem tra ket noi Internet va NuGet packages.' }

    Write-Host '2/4 Build Release...' -ForegroundColor Cyan
    & $msbuildPath $solutionPath '/t:Rebuild' '/p:Configuration=Release' '/p:Platform=Any CPU' '/nologo' '/v:minimal'
    if ($LASTEXITCODE -ne 0) { throw 'Build Release that bai. Hay doc cac loi C# trong cua so ben tren.' }

    $exe = Join-Path $releaseDir 'AuctionArena.WinForms.exe'
    if (-not (Test-Path $exe)) { throw 'Khong tim thay AuctionArena.WinForms.exe sau khi Build.' }
    $required = @('AuctionArena.Data.dll', 'AuctionArena.Services.dll', 'EntityFramework.dll',
                  'EntityFramework.SqlServer.dll', 'Microsoft.ReportViewer.WinForms.dll', 'Microsoft.ReportViewer.Common.dll')
    foreach ($dll in $required) {
        if (-not (Test-Path (Join-Path $releaseDir $dll))) {
            throw ('Build thieu DLL: ' + $dll + '. Hay kiem tra Restore NuGet va Copy Local.')
        }
    }

    if (Test-Path $stagingDir) { Remove-Item -Recurse -Force $stagingDir }
    New-Item -ItemType Directory -Path $appDir | Out-Null
    Write-Host '3/4 Gom EXE, DLL, SQL...' -ForegroundColor Cyan
    Copy-Item -Path (Join-Path $releaseDir '*') -Destination $appDir -Recurse -Force
    # Bo file debug symbol, khong can khi chay tren may khac.
    Get-ChildItem -LiteralPath $appDir -Filter '*.pdb' -File -Recurse | Remove-Item -Force
    Copy-Item -Path (Join-Path $projectRoot 'SQL') -Destination (Join-Path $appDir 'SQL') -Recurse -Force
    Copy-Item -Path (Join-Path $PSScriptRoot 'MAY_NHAN\*') -Destination $appDir -Recurse -Force

    # New machine starts with its own SQL Server connection, never the sender's login.
    $runtimeConfigPath = Join-Path $appDir 'AuctionArena.WinForms.exe.config'
    if (-not (Test-Path $runtimeConfigPath)) { throw 'Khong tim thay file cau hinh EXE trong bin\Release.' }
    [xml]$runtimeConfig = Get-Content -LiteralPath $runtimeConfigPath -Encoding UTF8
    $setting = $runtimeConfig.SelectSingleNode('/configuration/appSettings/add[@key="ConnectionConfigured"]')
    $connection = $runtimeConfig.SelectSingleNode('/configuration/connectionStrings/add[@name="AuctionArenaDB"]')
    if (-not $setting -or -not $connection) { throw 'Thieu ConnectionConfigured hoac AuctionArenaDB trong App.config.' }
    $setting.SetAttribute('value', 'false')
    $connection.SetAttribute('connectionString', 'Data Source=.;Initial Catalog=AuctionArenaDB;Connect Timeout=4;TrustServerCertificate=True;Integrated Security=True;MultipleActiveResultSets=True;App=AuctionArena')
    $runtimeConfig.Save($runtimeConfigPath)

    # Bo cai cung lay ban Release da xoa ket noi cua may phat trien.
    $setupPayload = Join-Path $PSScriptRoot 'SETUP_PAYLOAD'
    if (Test-Path $setupPayload) { Remove-Item -Recurse -Force $setupPayload }
    New-Item -ItemType Directory -Path $setupPayload | Out-Null
    Copy-Item -Path (Join-Path $appDir '*') -Destination $setupPayload -Recurse -Force

    if (Test-Path $resultZip) { Remove-Item -Force $resultZip }
    Write-Host '4/4 Dong goi ban chay...' -ForegroundColor Cyan
    Compress-Archive -Path $appDir -DestinationPath $resultZip -CompressionLevel Optimal
    Remove-Item -Path $stagingDir -Recurse -Force
    Write-Host ''
    Write-Host ('OK: ' + $resultZip) -ForegroundColor Green
    Write-Host 'Giai nen goi ban chay va mo CHAY_AUCTION_ARENA.cmd de khoi dong.' -ForegroundColor Green
}
catch {
    Write-Host ''
    Write-Host ('LOI: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
