# Latch 에이전트를 중지하고 방화벽·hosts 차단을 정리한 뒤 제거 (관리자 PowerShell에서 실행)
$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw '관리자 권한 PowerShell에서 실행하세요.'
}

$dest = Join-Path $env:ProgramFiles 'Latch'
# 설치 폴더 안에서 실행해도 폴더를 지울 수 있도록 작업 위치를 옮긴다
Set-Location $env:TEMP

if (Get-Service LatchAgent -ErrorAction SilentlyContinue) {
    Stop-Service LatchAgent -Force
    sc.exe delete LatchAgent | Out-Null
}
Get-Process Latch.LockUI -ErrorAction SilentlyContinue | Stop-Process -Force

$agent = Join-Path $dest 'Latch.Agent.exe'
if (Test-Path $agent) { & $agent reset }

$data = Join-Path $env:ProgramData 'Latch'
Remove-Item $dest, $data -Recurse -Force -ErrorAction SilentlyContinue

$left = @($dest, $data) | Where-Object { Test-Path $_ }
if ($left) {
    Write-Warning "다음 폴더를 지우지 못했습니다 (사용 중인 파일). 재부팅 후 직접 삭제하세요: $($left -join ', ')"
} else {
    Write-Host '제거 완료. 웹 화면에서도 이 PC 등록을 삭제하세요.'
}
