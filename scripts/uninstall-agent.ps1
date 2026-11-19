# Latch 에이전트를 중지하고 방화벽·hosts 차단을 정리한 뒤 제거 (관리자 PowerShell에서 실행)
$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw '관리자 권한 PowerShell에서 실행하세요.'
}

$dest = Join-Path $env:ProgramFiles 'Latch'
if (Get-Service LatchAgent -ErrorAction SilentlyContinue) {
    Stop-Service LatchAgent -Force
    sc.exe delete LatchAgent | Out-Null
}
Get-Process Latch.LockUI -ErrorAction SilentlyContinue | Stop-Process -Force

$agent = Join-Path $dest 'Latch.Agent.exe'
if (Test-Path $agent) { & $agent reset }

Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:ProgramData 'Latch') -Recurse -Force -ErrorAction SilentlyContinue
Write-Host '제거 완료. 웹 화면에서도 이 PC 등록을 삭제하세요.'
