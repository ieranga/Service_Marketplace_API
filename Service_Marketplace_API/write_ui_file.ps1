param(
    [Parameter(Mandatory=$true)]
    [string]$RelativePath,
    [Parameter(Mandatory=$true)]
    [string]$Content
)

$baseDir = "F:\Program Files\My pro\project\Developments\Service Marketplace system\Service_Marketplace_UI\Service_Marketplace_UI"
$fullPath = Join-Path $baseDir $RelativePath

$parentDir = Split-Path $fullPath -Parent
if (-not (Test-Path $parentDir)) {
    New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
}

[System.IO.File]::WriteAllText($fullPath, $Content, [System.Text.Encoding]::UTF8)
Write-Host "Wrote: $fullPath"
