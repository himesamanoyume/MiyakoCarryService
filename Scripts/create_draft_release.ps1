param(
    [string]$WorkspaceFolder = (Split-Path -Parent $PSScriptRoot),
    [string]$RepoName = "Himesamanoyume/MiyakoCarryService",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "artifact-manifest.ps1")

function ConvertTo-McsTrimmedLines {
    param(
        [string[]]$RawLines
    )

    $trimmed = New-Object System.Collections.Generic.List[string]
    foreach ($line in $RawLines) {
        $trimmed.Add($line.TrimEnd())
    }

    while ($trimmed.Count -gt 0 -and $trimmed[0].Trim().Length -eq 0) {
        $trimmed.RemoveAt(0)
    }

    while ($trimmed.Count -gt 0 -and $trimmed[$trimmed.Count - 1].Trim().Length -eq 0) {
        $trimmed.RemoveAt($trimmed.Count - 1)
    }

    return $trimmed.ToArray()
}

function Get-McsChangeLogNotes {
    param(
        [string]$ChangeLogPath,
        [string]$Ver
    )

    $lines = Get-Content -Path $ChangeLogPath -Encoding UTF8

    $startIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^#{1,6}\s+\S') {
            continue
        }

        $headingText = ($lines[$i] -replace '^#{1,6}\s+', '').Trim()
        if ($headingText -eq $Ver) {
            $startIndex = $i
            break
        }
    }

    if ($startIndex -lt 0) {
        return $null
    }

    $chinese = New-Object System.Collections.Generic.List[string]
    $english = New-Object System.Collections.Generic.List[string]
    $inEnglish = $false

    for ($i = $startIndex + 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        if ($line -match '^#{1,6}\s+\S') {
            break
        }

        if ($line.Trim() -eq '---') {
            $inEnglish = $true
            continue
        }

        if ($inEnglish) {
            $english.Add($line)
        }
        else {
            $chinese.Add($line)
        }
    }

    return @{
        Chinese = ConvertTo-McsTrimmedLines -RawLines $chinese.ToArray()
        English = ConvertTo-McsTrimmedLines -RawLines $english.ToArray()
    }
}

function ConvertTo-McsGithubReleaseBody {
    param(
        [string[]]$ChineseLines,
        [string[]]$EnglishLines,
        [string]$VtText
    )

    $bodyLines = New-Object System.Collections.Generic.List[string]
    $bodyLines.Add('宫子是我老婆')
    $bodyLines.Add('')
    foreach ($line in $ChineseLines) {
        $bodyLines.Add($line)
    }

    if ($EnglishLines.Count -gt 0) {
        $bodyLines.Add('')
        $bodyLines.Add('---')
        $bodyLines.Add('')
        $bodyLines.Add('Miyako is my waifu.')
        $bodyLines.Add('')
        foreach ($line in $EnglishLines) {
            $bodyLines.Add($line)
        }
    }

    $bodyText = $bodyLines -join "`n"
    if (-not [string]::IsNullOrEmpty($VtText)) {
        $bodyText += "`n`n$VtText"
    }

    return $bodyText
}

function ConvertTo-McsForgeText {
    param(
        [string[]]$ChineseLines,
        [string[]]$EnglishLines
    )

    $forgeLines = New-Object System.Collections.Generic.List[string]
    $forgeLines.Add('# {.tabset}')
    $forgeLines.Add('')

    if ($EnglishLines.Count -gt 0) {
        $forgeLines.Add('## English')
        $forgeLines.Add('')
        $forgeLines.Add('Miyako is my waifu.')
        $forgeLines.Add('')
        $forgeLines.Add('---')
        $forgeLines.Add('')
        foreach ($line in $EnglishLines) {
            $forgeLines.Add($line)
        }

        if ($ChineseLines.Count -gt 0) {
            $forgeLines.Add('')
        }
    }

    if ($ChineseLines.Count -gt 0) {
        $forgeLines.Add('## 中文')
        $forgeLines.Add('')
        $forgeLines.Add('宫子是我老婆')
        $forgeLines.Add('')
        $forgeLines.Add('---')
        $forgeLines.Add('')
        foreach ($line in $ChineseLines) {
            $forgeLines.Add($line)
        }
    }

    return ($forgeLines -join "`n")
}

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

$changeLogPath = Join-Path $WorkspaceFolder "CHANGELOG.md"
if (-not (Test-Path $changeLogPath)) {
    Write-Host "Error: CHANGELOG.md not found at $changeLogPath"
    exit 1
}

$notes = Get-McsChangeLogNotes -ChangeLogPath $changeLogPath -Ver $ver
if ($null -eq $notes) {
    Write-Host "Error: Could not find changelog section for version $ver in CHANGELOG.md"
    exit 1
}

$vtLines = New-Object System.Collections.Generic.List[string]
foreach ($artifact in $artifacts) {
    $reportPath = Join-Path $WorkspaceFolder $artifact.ReportFile
    if (Test-Path $reportPath) {
        $vtUrl = Get-Content $reportPath | Select-Object -First 1
        $vtLines.Add("$($artifact.Name) VT: $vtUrl")
    }
    else {
        Write-Host "Warning: VT report file not found for $($artifact.Name), releasing without scan link."
    }
}
$vtText = $vtLines -join "`n`n"

$bodyText = ConvertTo-McsGithubReleaseBody -ChineseLines $notes.Chinese -EnglishLines $notes.English -VtText $vtText

$forgeText = ConvertTo-McsForgeText -ChineseLines $notes.Chinese -EnglishLines $notes.English
$forgeTemplatePath = Join-Path $WorkspaceFolder "template.txt"
$forgeTextCrlf = $forgeText.Replace("`n", "`r`n")
[System.IO.File]::WriteAllText($forgeTemplatePath, $forgeTextCrlf, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Forge release text written to $forgeTemplatePath"

if ($DryRun) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Dry run - release body preview (draft will NOT be created):" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $bodyText
    exit 0
}

$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Host "Error: GITHUB_TOKEN environment variable not found"
    exit 1
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