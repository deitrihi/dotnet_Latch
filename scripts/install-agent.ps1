# 자녀 PC에 Latch 에이전트를 Windows 서비스로 설치하고 서버와 페어링 (관리자 PowerShell에서 실행)
# 사용: .\install-agent.ps1 -ServerUrl https://latch.example.com -Code ABCD2345
#       업데이트만 할 때는 -Code 생략 (기존 페어링 유지)
param(
    [Parameter(Mandatory)][string]$ServerUrl,
    [string]$Code
)
$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw '관리자 권한 PowerShell에서 실행하세요.'
}

$dest = Join-Path $env:ProgramFiles 'Latch'
$service = Get-Service LatchAgent -ErrorAction SilentlyContinue
if ($service) { Stop-Service LatchAgent -Force }
Get-Process Latch.LockUI -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $PSScriptRoot '*') $dest -Recurse -Force

if ($Code) {
    & (Join-Path $dest 'Latch.Agent.exe') pair $ServerUrl $Code
    if ($LASTEXITCODE) { throw '페어링 실패 — 코드가 만료되지 않았는지, 서버 주소가 맞는지 확인하세요.' }
}

if (-not $service) {
    sc.exe create LatchAgent binPath= "`"$dest\Latch.Agent.exe`"" start= auto DisplayName= "Latch Agent" | Out-Null
    sc.exe description LatchAgent "Latch 자녀 PC 관리 에이전트" | Out-Null
    # 비정상 종료 시 5초 후 자동 재시작
    sc.exe failure LatchAgent reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null
}
Start-Service LatchAgent
Write-Host "설치 완료: $dest (서비스 LatchAgent 실행 중)"
