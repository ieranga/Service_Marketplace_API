$file = "F:\Program Files\My pro\project\Developments\Service Marketplace system\Service_Marketplace_UI\Service_Marketplace_UI\angular.json"
$content = Get-Content -Path $file -Raw | ConvertFrom-Json

# 1. Update serve options
if (-not $content.projects.Service_Marketplace_UI.architect.serve.options) {
    $content.projects.Service_Marketplace_UI.architect.serve | Add-Member -MemberType NoteProperty -Name "options" -Value ([PSCustomObject]@{})
}
$content.projects.Service_Marketplace_UI.architect.serve.options | Add-Member -MemberType NoteProperty -Name "allowedHosts" -Value $true -Force

# 2. Update build security allowedHosts
if ($content.projects.Service_Marketplace_UI.architect.build.options.security) {
    $content.projects.Service_Marketplace_UI.architect.build.options.security.allowedHosts = @("localhost", "127.0.0.1", "192.168.8.140", ".local")
}

$content | ConvertTo-Json -Depth 25 | Set-Content -Path $file -Encoding utf8
Write-Host "angular.json updated successfully with serve and build allowedHosts"
