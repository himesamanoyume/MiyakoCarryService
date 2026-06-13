param(
    [string]$WorkspaceFolder,
    [string]$RepoName = "Himesamanoyume/MiyakoCarryService",
    [string]$VtReportFile = "vt_report_url.txt"
)

$ErrorActionPreference = "Stop"

$ver = Get-Content "$WorkspaceFolder\version.txt" | Select-Object -First 1
if (-not $ver) {
    Write-Host "Error: Could not read version from version.txt"
    exit 1
}

$zipPath = Join-Path $WorkspaceFolder "MiyakoCarryService-$ver.zip"
if (-not (Test-Path $zipPath)) {
    Write-Host "Error: Release zip not found at $zipPath"
    exit 1
}

$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Host "Error: GITHUB_TOKEN environment variable not found"
    exit 1
}

$vtUrl = ""
$vtReportPath = Join-Path $WorkspaceFolder $VtReportFile
if (Test-Path $vtReportPath) {
    $vtUrl = Get-Content $vtReportPath | Select-Object -First 1
    # Remove-Item $vtReportPath -Force
    # Write-Host "VT report URL loaded and temp file cleaned up."
} else {
    Write-Host "Warning: VT report file not found, releasing without scan link."
}

$bodyText = ""
if ($vtUrl) {
    $bodyText += "`n`n$vtUrl"
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
    $fileName = [System.IO.Path]::GetFileName($zipPath)
    $uploadHeaders = @{
        Authorization = "Bearer $token"
    }

    Write-Host "Uploading $fileName to release..."
    $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
    
    $uploadRes = Invoke-RestMethod -Uri "$uploadUrl`?name=$fileName" `
        -Method Post `
        -Headers $uploadHeaders `
        -Body $fileBytes `
        -ContentType "application/zip"

    Write-Host "Asset uploaded successfully: $($uploadRes.browser_download_url)"
    Write-Host "`nAll done! Draft release is ready for review.`n"

    $finalDownloadUrl = "https://github.com/$RepoName/releases/download/v$ver/$fileName"

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Download URL : $finalDownloadUrl"
    if ($vtUrl) {
        Write-Host "VT Report    : $vtUrl"
    } else {
        Write-Host "VT Report    : (Not available)" -ForegroundColor Yellow
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