param(
    [string]$WorkspaceFolder,
    [string]$RepoName = "Himesamanoyume/MiyakoCarryService"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "artifact-manifest.ps1")

$ver = Get-Content "$WorkspaceFolder\version.txt" | Select-Object -First 1
if (-not $ver) {
    Write-Host "Error: Could not read version from version.txt"
    exit 1
}

$artifacts = Get-McsArtifacts $ver

foreach ($artifact in $artifacts) {
    $zipPath = Join-Path $WorkspaceFolder $artifact.ZipFile
    if (-not (Test-Path $zipPath)) {
        Write-Host "Error: Release zip not found at $zipPath"
        exit 1
    }
}

$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Host "Error: GITHUB_TOKEN environment variable not found"
    exit 1
}

$bodyText = ""
foreach ($artifact in $artifacts) {
    $reportPath = Join-Path $WorkspaceFolder $artifact.ReportFile
    if (Test-Path $reportPath) {
        $vtUrl = Get-Content $reportPath | Select-Object -First 1
        $bodyText += "`n`n$($artifact.Name) VT: $vtUrl"
    } else {
        Write-Host "Warning: VT report file not found for $($artifact.Name), releasing without scan link."
    }
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

    foreach ($artifact in $artifacts) {
        $zipPath = Join-Path $WorkspaceFolder $artifact.ZipFile
        $fileName = [System.IO.Path]::GetFileName($zipPath)
        Write-Host "Uploading $fileName to release..."
        $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)

        $uploadRes = Invoke-RestMethod -Uri "$uploadUrl`?name=$fileName" `
            -Method Post `
            -Headers $uploadHeaders `
            -Body $fileBytes `
            -ContentType "application/zip"

        Write-Host "Asset uploaded successfully: $($uploadRes.browser_download_url)"
    }

    Write-Host "`nAll done! Draft release is ready for review.`n"

    Write-Host "========================================" -ForegroundColor Cyan
    foreach ($artifact in $artifacts) {
        $fileName = [System.IO.Path]::GetFileName($artifact.ZipFile)
        $downloadUrl = "https://github.com/$RepoName/releases/download/v$ver/$fileName"
        Write-Host "$($artifact.Name) Download URL : $downloadUrl"

        $reportPath = Join-Path $WorkspaceFolder $artifact.ReportFile
        if (Test-Path $reportPath) {
            $vtUrl = Get-Content $reportPath | Select-Object -First 1
            Write-Host "$($artifact.Name) VT Report    : $vtUrl"
        } else {
            Write-Host "$($artifact.Name) VT Report    : (Not available)" -ForegroundColor Yellow
        }
    }
    Write-Host "Draft Page   : $($releaseRes.html_url)"
    Write-Host "Forge Page   : https://sp-mod.com/mod/2709/miyako-carry-service"
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