#requires -version 5.1
$ErrorActionPreference = 'Stop'
try {
    $compiler = $null
    foreach ($candidate in @("${env:ProgramFiles(x86)}\NSIS\makensis.exe", "${env:ProgramFiles}\NSIS\makensis.exe")) {
        if (Test-Path -LiteralPath $candidate) { $compiler = $candidate; break }
    }
    if (-not $compiler) {
        $command = Get-Command makensis.exe -ErrorAction SilentlyContinue
        if ($command) { $compiler = $command.Source }
    }
    if (-not $compiler) { throw 'Can cai NSIS 3 Unicode de tao bo cai. Website: https://nsis.sourceforge.io/Download' }
    & (Join-Path $PSScriptRoot 'TAO_BAN_CHAY.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Khong tao duoc ban Release.' }
    $payload = (Resolve-Path (Join-Path $PSScriptRoot 'SETUP_PAYLOAD')).Path
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in Get-ChildItem -LiteralPath $payload -File -Recurse | Sort-Object FullName) {
        $relative = $file.FullName.Substring($payload.Length + 1)
        if ($relative -eq 'AuctionArena.WinForms.exe.config') { continue }
        $escaped = $relative.Replace('$', '$$')
        $lines.Add('Delete "$INSTDIR\' + $escaped + '"')
    }
    foreach ($folder in Get-ChildItem -LiteralPath $payload -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending) {
        $relative = $folder.FullName.Substring($payload.Length + 1).Replace('$', '$$')
        $lines.Add('RMDir "$INSTDIR\' + $relative + '"')
    }
    [IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'UninstallFiles.nsh'), $lines, (New-Object Text.UTF8Encoding($true)))
    Push-Location $PSScriptRoot
    try { & $compiler '/V3' 'AuctionArena.nsi' }
    finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw 'Bien dich bo cai that bai.' }
    Write-Host 'Da tao AuctionArena_Setup.exe trong thu muc du an.' -ForegroundColor Green
}
catch { Write-Host ('LOI: ' + $_.Exception.Message) -ForegroundColor Red; exit 1 }
