# Publication Android MAUI
# Génère un APK signé, crée le fichier SHA256, vérifie la signature et l'alignement.
#
# Utilisation
# -----------
# .\publish-android.ps1                 -> publication simple
# .\publish-android.ps1 -Clean          -> nettoyage complet avant publication
# .\publish-android.ps1 -Clean -Install -> installation directe sur le téléphone après génération

param(
  [switch]$Clean,
  [switch]$Install
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectPath = Join-Path $PSScriptRoot "PB BZH Mobile Lab\PB BZH Mobile Lab.csproj"
$KeystorePath = Join-Path $PSScriptRoot "pb-bzh-mobile-lab.keystore"
$KeyAlias = "pb-bzh-mobile-lab"
$TargetFramework = "net10.0-android"
$Configuration = "Release"

if (!(Test-Path $ProjectPath)) {
  throw "Projet introuvable : $ProjectPath"
}

if (!(Test-Path $KeystorePath)) {
  throw "Keystore introuvable : $KeystorePath"
}

[xml]$ProjectXml = Get-Content $ProjectPath -Raw

$ApplicationId =
  ($ProjectXml.Project.PropertyGroup.ApplicationId | Where-Object { $_ } | Select-Object -Last 1)

$DisplayVersion =
  ($ProjectXml.Project.PropertyGroup.ApplicationDisplayVersion | Where-Object { $_ } | Select-Object -Last 1)

if ([string]::IsNullOrWhiteSpace($ApplicationId)) {
  throw "ApplicationId introuvable dans le csproj."
}

if ([string]::IsNullOrWhiteSpace($DisplayVersion)) {
  $DisplayVersion = "0.0.0"
}

$OutputName = "PB_BZH_License_Generator-$DisplayVersion.apk"
$ReleaseDir = Join-Path $PSScriptRoot "ReleaseAndroid"
$FinalApkPath = Join-Path $ReleaseDir $OutputName
$HashPath = "$FinalApkPath.sha256.txt"

Write-Host "Projet       : $ProjectPath"
Write-Host "Application  : $ApplicationId"
Write-Host "Version      : $DisplayVersion"
Write-Host "Keystore     : $KeystorePath"
Write-Host ""

$StorePassSecure = Read-Host "Mot de passe du keystore" -AsSecureString
$KeyPassSecure = Read-Host "Mot de passe de la cle" -AsSecureString

$StorePass =
  [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($StorePassSecure))

$KeyPass =
  [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($KeyPassSecure))

if ($Clean) {
  Write-Host ""
  Write-Host "Nettoyage bin/obj..."
  Remove-Item -Recurse -Force (Join-Path (Split-Path $ProjectPath) "bin") -ErrorAction SilentlyContinue
  Remove-Item -Recurse -Force (Join-Path (Split-Path $ProjectPath) "obj") -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Publication APK signee..."

$PublishArgs = @(
  "publish",
  $ProjectPath,
  "-f", $TargetFramework,
  "-c", $Configuration,
  "-p:AndroidPackageFormats=apk",
  "-p:AndroidKeyStore=true",
  "-p:AndroidSigningKeyStore=$KeystorePath",
  "-p:AndroidSigningKeyAlias=$KeyAlias",
  "-p:AndroidSigningKeyPass=$KeyPass",
  "-p:AndroidSigningStorePass=$StorePass"
)

& dotnet @PublishArgs

if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish a echoue."
}

$ProjectDir = Split-Path $ProjectPath
$ApkCandidates = Get-ChildItem `
  -Path (Join-Path $ProjectDir "bin\$Configuration\$TargetFramework") `
  -Recurse `
  -Filter "*.apk" |
  Sort-Object LastWriteTime -Descending

if ($ApkCandidates.Count -eq 0) {
  throw "Aucun APK trouve apres publication."
}

$GeneratedApk = $ApkCandidates[0].FullName

New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
Copy-Item -Force $GeneratedApk $FinalApkPath

$Hash = Get-FileHash $FinalApkPath -Algorithm SHA256
$Hash.Hash | Out-File $HashPath -Encoding UTF8

Write-Host ""
Write-Host "APK genere :"
Write-Host $FinalApkPath
Write-Host ""
Write-Host "SHA256 :"
Write-Host $Hash.Hash
Write-Host ""

$BuildToolsRoot = "C:\Program Files (x86)\Android\android-sdk\build-tools"

if (Test-Path $BuildToolsRoot) {
  $LatestBuildTools =
    Get-ChildItem $BuildToolsRoot -Directory |
    Sort-Object Name -Descending |
    Select-Object -First 1

  if ($LatestBuildTools) {
    $ApkSigner = Join-Path $LatestBuildTools.FullName "apksigner.bat"
    $ZipAlign = Join-Path $LatestBuildTools.FullName "zipalign.exe"

    if (Test-Path $ApkSigner) {
      Write-Host "Verification signature APK..."
      & $ApkSigner verify --verbose --print-certs $FinalApkPath
    }

    if (Test-Path $ZipAlign) {
      Write-Host ""
      Write-Host "Verification alignement APK..."
      & $ZipAlign -c -v 4 $FinalApkPath
    }
  }
}

if ($Install) {
  $Adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"

  if (!(Test-Path $Adb)) {
    throw "adb introuvable : $Adb"
  }

  Write-Host ""
  Write-Host "Desinstallation ancienne version..."
  & $Adb uninstall $ApplicationId

  Write-Host ""
  Write-Host "Installation APK..."
  & $Adb install $FinalApkPath
}

Write-Host ""
Write-Host "Publication terminee."