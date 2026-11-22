param(
    [string]$WorkspaceFolder,
    [string]$GameRoot = "D:\OtherGames\SPT5.0.X\SPT-5.0.X"
)

$ErrorActionPreference = "Stop"

$refDir = Join-Path $WorkspaceFolder "Ref"
if (-not (Test-Path $refDir)) {
    New-Item -ItemType Directory -Path $refDir -Force | Out-Null
}

$dllRelativePaths = @(
    "BepInEx\plugins\sptushonka\ConfigurationManager\ConfigurationManager.dll"
    "BepInEx\plugins\sptushonka\ConfigurationManager\BepInEx.KeyboardShortcut.dll"
    "BepInEx\plugins\sptushonka\SPTushonka.Common.dll"
    "BepInEx\plugins\sptushonka\SPTushonka.Core.dll"
    "BepInEx\plugins\sptushonka\SPTushonka.Custom.dll"
    "BepInEx\plugins\sptushonka\SPTushonka.Debugging.dll"
    "BepInEx\plugins\sptushonka\SPTushonka.Reflection.dll"
    "BepInEx\plugins\sptushonka\SPTushonka.Singleplayer.dll"
    "BepInEx\core\0Harmony.dll"
    "BepInEx\core\BepInEx.Core.dll"
    "BepInEx\core\BepInEx.Unity.Common.dll"
    "BepInEx\core\BepInEx.Unity.IL2CPP.dll"
    "BepInEx\core\Il2CppInterop.Common.dll"
    "BepInEx\core\Il2CppInterop.Runtime.dll"
    "BepInEx\core\SemanticVersioning.dll"
    "BepInEx\interop\Il2Cppmscorlib.dll"
    "BepInEx\interop\Il2CppSystem.dll"
    "BepInEx\interop\Il2CppSystem.Core.dll"
    "BepInEx\interop\Assembly-CSharp.dll"
    "BepInEx\interop\CommonExtensions.dll"
    "BepInEx\interop\UnityEngine.dll"
    "BepInEx\interop\UnityEngine.CoreModule.dll"
    "BepInEx\interop\UnityEngine.IMGUIModule.dll"
    "BepInEx\interop\UnityEngine.PhysicsModule.dll"
    "BepInEx\interop\UnityEngine.TextRenderingModule.dll"
    "BepInEx\interop\UnityEngine.AIModule.dll"
    "BepInEx\interop\UnityEngine.AssetBundleModule.dll"
    "BepInEx\interop\Unity.TextMeshPro.dll"
    "BepInEx\interop\Comfort.dll"
    "BepInEx\interop\UnityEngine.InputLegacyModule.dll"
    "BepInEx\interop\Sirenix.Serialization.dll"
    "BepInEx\interop\DissonanceVoip.dll"
    "BepInEx\interop\Comfort.Unity.dll"
    "BepInEx\interop\Newtonsoft.Json.dll"
    "BepInEx\interop\ItemComponent.Types.dll"
    "BepInEx\interop\Diz.Jobs.dll"
    "BepInEx\interop\Diz.Binding.dll"
    "BepInEx\interop\PlayerEnums.dll"
    "BepInEx\interop\Utils.dll"
    "BepInEx\interop\CoreServices.dll"
    "BepInEx\interop\UnityEngine.UI.dll"
    "BepInEx\interop\DOTween.dll"
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