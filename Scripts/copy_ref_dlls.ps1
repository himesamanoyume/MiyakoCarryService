param(
    [string]$WorkspaceFolder,
    [string]$GameRoot = "D:\OtherGames\SPT4.0.X\Client-4.0.X"
)

$ErrorActionPreference = "Stop"

$refDir = Join-Path $WorkspaceFolder "Ref"
if (-not (Test-Path $refDir)) {
    New-Item -ItemType Directory -Path $refDir -Force | Out-Null
}

$dllRelativePaths = @(
    "BepInEx\plugins\spt\ConfigurationManager\ConfigurationManager.dll"
    "BepInEx\plugins\spt\spt-common.dll"
    "BepInEx\plugins\spt\spt-core.dll"
    "BepInEx\plugins\spt\spt-custom.dll"
    "BepInEx\plugins\spt\spt-debugging.dll"
    "BepInEx\plugins\spt\spt-reflection.dll"
    "BepInEx\plugins\spt\spt-singleplayer.dll"
    "BepInEx\core\0Harmony.dll"
    "BepInEx\core\BepInEx.dll"
    "EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"
    "EscapeFromTarkov_Data\Managed\CommonExtensions.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.CoreModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.IMGUIModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.PhysicsModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.TextRenderingModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.AIModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.AssetBundleModule.dll"
    "EscapeFromTarkov_Data\Managed\Unity.TextMeshPro.dll"
    "EscapeFromTarkov_Data\Managed\Comfort.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.InputLegacyModule.dll"
    "EscapeFromTarkov_Data\Managed\Sirenix.Serialization.dll"
    "EscapeFromTarkov_Data\Managed\DissonanceVoip.dll"
    "EscapeFromTarkov_Data\Managed\Comfort.Unity.dll"
    "EscapeFromTarkov_Data\Managed\Newtonsoft.Json.dll"
    "EscapeFromTarkov_Data\Managed\ItemComponent.Types.dll"
)

$copiedCount = 0
$missingFiles = @()

foreach ($relativePath in $dllRelativePaths) {
    $fullPath = Join-Path $GameRoot $relativePath
    if (Test-Path $fullPath) {
        Copy-Item $fullPath $refDir -Force
        Write-Host "Copied: $relativePath"
        $copiedCount++
    } else {
        Write-Host "Warning: $relativePath not found"
        $missingFiles += $relativePath
    }
}

Write-Host "`nCopy complete: $copiedCount/$($dllRelativePaths.Count) files copied to $refDir"

if ($missingFiles.Count -gt 0) {
    Write-Host "`nMissing files:" -ForegroundColor Yellow
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}