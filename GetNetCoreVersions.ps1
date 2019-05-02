Write-Host Path:
(Get-Command dotnet).Path

Write-Host

Write-Host Runtimes:
(dir (Get-Command dotnet).Path.Replace('dotnet.exe', 'shared\Microsoft.NETCore.App')).Name

Write-Host

Write-Host SDK:
(dir (Get-Command dotnet).Path.Replace('dotnet.exe', 'sdk')).Name