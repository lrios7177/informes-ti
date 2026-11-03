param([Parameter(Mandatory=$true)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\InformesTI'
$dataDir = Join-Path $env:LOCALAPPDATA 'InformesTI'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\InformesTI'
$startLink = Join-Path ([Environment]::GetFolderPath('Programs')) 'Informes TI\Informes TI.lnk'
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Informes TI.lnk'
$preexisting = @()
if (Test-Path $installDir) { $preexisting += 'carpeta de instalación' }
if (Test-Path $dataDir) { $preexisting += 'carpeta de datos' }
if (Test-Path $registryPath) { $preexisting += 'registro de desinstalación' }
if ($preexisting.Count -gt 0) {
    throw ('Esta prueba exige un usuario de Windows limpio. Se encontró: ' + ($preexisting -join ', ') + '. No se tocarán datos preexistentes.')
}
$marker = Join-Path $dataDir 'persistencia-sintetica.txt'
try {
    $process = Start-Process -FilePath $InstallerPath -ArgumentList '/S' -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Instalación fallida: $($process.ExitCode)" }
    foreach ($path in @("$installDir\InformesTI.exe", "$installDir\Desinstalar.exe", $startLink, $desktopLink, $registryPath)) {
        if (-not (Test-Path $path)) { throw "No se creó: $path" }
    }
    New-Item -ItemType Directory -Path $dataDir | Out-Null
    'DATOS SINTETICOS - prueba de conservación' | Set-Content $marker
    $process = Start-Process -FilePath $InstallerPath -ArgumentList '/S' -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Test-Path $marker)) { throw 'La reinstalación no conservó los datos.' }
    Start-Process -FilePath "$installDir\Desinstalar.exe" -ArgumentList '/S' -Wait | Out-Null
    $deadline = (Get-Date).AddSeconds(30)
    while ((Test-Path "$installDir\InformesTI.exe") -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250 }
    foreach ($path in @("$installDir\InformesTI.exe", $startLink, $desktopLink, $registryPath)) {
        if (Test-Path $path) { throw "No se desinstaló: $path" }
    }
    if (-not (Test-Path $marker)) { throw 'La desinstalación eliminó los datos del usuario.' }
    Write-Host 'Instalador OK: instalación, accesos, reinstalación, desinstalación y conservación de datos sintéticos.'
} finally {
    # Solo se limpia el directorio sintético creado en esta ejecución.
    if (Test-Path $dataDir) { Remove-Item $dataDir -Recurse -Force }
}
