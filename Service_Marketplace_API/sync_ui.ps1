$staging = "F:\Program Files\My pro\project\Developments\Service Marketplace system\Service_Marketplace_API\Service_Marketplace_API\Service_Marketplace_API\ui_staging"
$dest = "F:\Program Files\My pro\project\Developments\Service Marketplace system\Service_Marketplace_UI\Service_Marketplace_UI"

Get-ChildItem -Path $staging -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($staging.Length + 1)
    $targetFile = Join-Path $dest $rel
    $targetDir = Split-Path $targetFile -Parent
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }
    Copy-Item $_.FullName $targetFile -Force
    Write-Host "Synced: $rel"
}

# Clean up removed features
$oldProvider = Join-Path $dest "src\app\features\provider"
if (Test-Path $oldProvider) {
    Remove-Item -Recurse -Force $oldProvider
    Write-Host "Removed obsolete: $oldProvider"
}

$oldReceiver = Join-Path $dest "src\app\features\receiver"
if (Test-Path $oldReceiver) {
    Remove-Item -Recurse -Force $oldReceiver
    Write-Host "Removed obsolete: $oldReceiver"
}

