# 자녀 PC용 에이전트와 LockUI를 하나의 폴더로 게시 (self-contained — 자녀 PC에 .NET 설치 불필요)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'publish\agent'

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
foreach ($project in 'Latch.Agent', 'Latch.LockUI') {
    dotnet publish (Join-Path $root "src\$project") -c Release -r win-x64 --self-contained -o $out
    if ($LASTEXITCODE) { throw "$project 게시 실패" }
}
Copy-Item (Join-Path $PSScriptRoot 'install-agent.ps1'), (Join-Path $PSScriptRoot 'install-agent.bat'), (Join-Path $PSScriptRoot 'uninstall-agent.bat'), (Join-Path $PSScriptRoot 'uninstall-agent.ps1') $out

Write-Host "완료: $out — 이 폴더를 자녀 PC로 복사한 뒤 install-agent.bat 을 더블클릭하세요."
