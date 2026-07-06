param(
    [string]$WorkspaceFolder,
    [string]$RepoName = "Himesamanoyume/MiyakoCarryService",
    [string]$PluginVtReportFile = "plugin_vt_report_url.txt"
    [string]$FikaVtReportFile = "fika_vt_report_url.txt"
)

$ErrorActionPreference = "Stop"

$ver = Get-Content "$WorkspaceFolder\version.txt" | Select-Object -First 1
if (-not $ver) {
    Write-Host "Error: Could not read version from version.txt"
    exit 1
}

$pluginZipPath = Join-Path $WorkspaceFolder "MiyakoCarryService-$ver.zip"
if (-not (Test-Path $pluginZipPath)) {
    Write-Host "Error: Release zip not found at $pluginZipPath"
    exit 1
}

$fikaZipPath = Join-Path $WorkspaceFolder "MiyakoCarryServiceFika-$ver.zip"
if (-not (Test-Path $fikaZipPath)) {
    Write-Host "Error: Release zip not found at $fikaZipPath"
    exit 1
}

$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Host "Error: GITHUB_TOKEN environment variable not found"
    exit 1
}

$pluginVtUrl = ""
$fikaVtUrl = ""

$pluginVtReportPath = Join-Path $WorkspaceFolder $PluginVtReportFile
if (Test-Path $pluginVtReportPath) {
    $pluginVtUrl = Get-Content $pluginVtReportPath | Select-Object -First 1
    # Remove-Item $pluginVtReportPath -Force
    # Write-Host "VT report URL loaded and temp file cleaned up."
} else {
    Write-Host "Warning: VT report file not found, releasing without scan link."
}

$fikaVtReportPath = Join-Path $WorkspaceFolder $FikaVtReportFile
if (Test-Path $fikaVtReportPath) {
    $fikaVtUrl = Get-Content $fikaVtReportPath | Select-Object -First 1
    # Remove-Item $fikaVtReportPath -Force
    # Write-Host "VT report URL loaded and temp file cleaned up."
} else {
    Write-Host "Warning: VT report file not found, releasing without scan link."
}

$bodyText = ""
if ($pluginVtUrl) {
    $bodyText += "`n`nPlugin VT: $pluginVtUrl"
}

if ($fikaVtUrl) {
    $bodyText += "`n`nFika Addon VT: $fikaVtUrl"
}

try {
    $headers = @{
        Authorization = "Bearer $token"
        Accept        = "application/vnd.github+json"
    }

    $releaseBody = @{
        tag_name               = "v$ver"
        name                   = "MiyakoCarryService v$ver"
        draft                  = $true
        body                   = $bodyText
    } | ConvertTo-Json

    $apiUri = "https://api.github.com/repos/$RepoName/releases"
    
    Write-Host "Creating draft release for v$ver..."
    $releaseRes = Invoke-RestMethod -Uri $apiUri -Method Post -Headers $headers -Body $releaseBody -ContentType "application/json; charset=utf-8"
    Write-Host "Draft release created: $($releaseRes.html_url)"

    $uploadUrl = $releaseRes.upload_url.Replace("{?name,label}", "")

    $uploadHeaders = @{
        Authorization = "Bearer $token"
    }

    $pluginFileName = [System.IO.Path]::GetFileName($pluginZipPath)
    Write-Host "Uploading $pluginFileName to release..."
    $pluginFileBytes = [System.IO.File]::ReadAllBytes($pluginZipPath)
    
    $pluginUploadRes = Invoke-RestMethod -Uri "$uploadUrl`?name=$pluginFileName" `
        -Method Post `
        -Headers $uploadHeaders `
        -Body $pluginFileBytes `
        -ContentType "application/zip"

    Write-Host "Asset uploaded successfully: $($pluginUploadRes.browser_download_url)"

    $fikaFileName = [System.IO.Path]::GetFileName($fikaZipPath)
    Write-Host "Uploading $fikaFileName to release..."
    $fikaFileBytes = [System.IO.File]::ReadAllBytes($fikaZipPath)

    $fikaUploadRes = Invoke-RestMethod -Uri "$uploadUrl`?name=$fikaFileName" `
        -Method Post `
        -Headers $uploadHeaders `
        -Body $fikaFileBytes `
        -ContentType "application/zip"

    Write-Host "Asset uploaded successfully: $($fikaUploadRes.browser_download_url)"
    Write-Host "`nAll done! Draft release is ready for review.`n"

    $pluginFinalDownloadUrl = "https://github.com/$RepoName/releases/download/v$ver/$pluginFileName"
    $fikaFinalDownloadUrl = "https://github.com/$RepoName/releases/download/v$ver/$fikaFileName"

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Plugin Download URL : $pluginFinalDownloadUrl"
    Write-Host "Fika Addon Download URL : $fikaFinalDownloadUrl"
    if ($pluginVtUrl) {
        Write-Host "Plugin VT Report    : $pluginVtUrl"
    } else {
        Write-Host "Plugin VT Report    : (Not available)" -ForegroundColor Yellow
    }
    if ($fikaVtUrl) {
        Write-Host "Fika Addon VT Report    : $fikaVtUrl"
    } else {
        Write-Host "Fika Addon VT Report    : (Not available)" -ForegroundColor Yellow
    }
    Write-Host "Draft Page   : $($releaseRes.html_url)"
    Write-Host "Forge Page   : https://forge.sp-tarkov.com/mod/2709/miyako-carry-service"
    Write-Host "`========================================" -ForegroundColor Cyan

} catch {
    Write-Host "GitHub API Error: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        Write-Host "Response Body: $($reader.ReadToEnd())"
    }
    exit 1
}