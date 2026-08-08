param(
    [string]$WorkspaceFolder
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "artifact-manifest.ps1")

$vtKey = $env:VT_API_KEY
if (-not $vtKey) {
    Write-Host "Error: VT_API_KEY environment variable not found"
    exit 1
}

function Upload-VtFile {
    param(
        [string]$ZipPath,
        [string]$ApiKey
    )

    Write-Host "Uploading $ZipPath to VirusTotal..."

    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $client.DefaultRequestHeaders.Add("x-apikey", $ApiKey)

    $fileStream = [System.IO.File]::OpenRead($ZipPath)
    $fileName = [System.IO.Path]::GetFileName($ZipPath)

    $content = New-Object System.Net.Http.MultipartFormDataContent
    $streamContent = New-Object System.Net.Http.StreamContent($fileStream)
    $streamContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/zip")
    $content.Add($streamContent, "file", $fileName)

    try {
        $uploadResponse = $client.PostAsync("https://www.virustotal.com/api/v3/files", $content).Result
        $uploadJson = $uploadResponse.Content.ReadAsStringAsync().Result

        if (-not $uploadResponse.IsSuccessStatusCode) {
            throw "Upload failed with status $($uploadResponse.StatusCode): $uploadJson"
        }

        $uploadRes = $uploadJson | ConvertFrom-Json
        $analysisId = $uploadRes.data.id
        Write-Host "Upload successful. Analysis ID: $analysisId. Waiting for scan..."
    }
    finally {
        $fileStream.Dispose()
        $content.Dispose()
    }

    Start-Sleep -Seconds 20

    $reportUrl = ""
    $maxRetries = 36
    for ($i = 0; $i -lt $maxRetries; $i++) {
        $reportResponse = $client.GetAsync("https://www.virustotal.com/api/v3/analyses/$analysisId").Result
        $reportJson = $reportResponse.Content.ReadAsStringAsync().Result
        $reportRes = $reportJson | ConvertFrom-Json

        if ($reportRes.data.attributes.status -eq "completed") {
            $sha256 = $reportRes.meta.file_info.sha256
            if (-not $sha256) {
                throw "Scan completed but SHA256 not found in response metadata."
            }
            $reportUrl = "https://www.virustotal.com/gui/file/$sha256"
            break
        }

        Write-Host "Scan still in progress... retrying in 10s ($($i+1)/$maxRetries)"
        Start-Sleep -Seconds 10
    }

    $client.Dispose()

    if (-not $reportUrl) {
        throw "Scan did not complete within the expected time."
    }

    return $reportUrl
}

try {
    $ver = Get-Content "$WorkspaceFolder\version.txt" | Select-Object -First 1
    if (-not $ver) {
        Write-Host "Error: Could not read version from version.txt"
        exit 1
    }

    $artifacts = Get-McsArtifacts $ver

    foreach ($artifact in $artifacts) {
        $zipPath = Join-Path $WorkspaceFolder $artifact.ZipFile
        if (-not (Test-Path $zipPath)) {
            Write-Host "Error: File not found at $zipPath"
            exit 1
        }

        $reportUrl = Upload-VtFile $zipPath $vtKey

        $outputPath = Join-Path $WorkspaceFolder $artifact.ReportFile
        Set-Content -Path $outputPath -Value $reportUrl -Force
        Write-Host "Scan complete! Report URL saved to $outputPath"
        Write-Host "URL: $reportUrl"
    }
} catch {
    Write-Host "VirusTotal API Error: $_"
    exit 1
}