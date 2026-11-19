# Latch — 자녀 PC 원격 관리

부모가 웹에서 자녀 Windows PC를 **잠그고**, **앱·사이트를 차단**하고, **네트워크 연결을 확인**하는 도구입니다.
서버는 NAS(Docker)에서, 에이전트는 자녀 PC에서 Windows 서비스로 실행됩니다.

```
[부모 브라우저] ─HTTPS─▶ Nginx Proxy Manager ─▶ Latch.Server (NAS Docker :8085)
                                                    ▲ SignalR(WebSocket)
                                    자녀 PC: Latch.Agent (서비스) + Latch.LockUI (잠금 화면·트레이)
```

자녀 PC가 서버로 먼저 접속하므로 자녀 PC 쪽 포트 개방은 필요 없습니다.

## 1. 서버 배포 (UGREEN NAS)

### 이미지 빌드

**권장: 배포 스크립트** — `deploy.config.example`을 `deploy.config`로 복사해 NAS 접속 정보를 채운 뒤
`deploy.bat`을 더블클릭(또는 Git Bash에서 `./deploy.sh`). 로컬 빌드 검증 → 소스 전송 → NAS에서
`docker compose up -d --build`까지 한 번에 합니다. SSH 키 인증과 배포 계정의 docker 그룹 권한이 필요합니다.

**A. NAS에서 직접** (SSH 접속 후, 저장소를 NAS에 복사한 폴더에서)
```sh
docker compose up -d --build
```

**B. PC에서 빌드 후 옮기기**
```sh
docker build --platform linux/amd64 -t latch-server:latest .
docker save latch-server:latest -o latch-server.tar
# NAS로 복사 후: docker load -i latch-server.tar && docker compose up -d
```

컨테이너는 `8085` 포트(HTTP)로 열립니다. 데이터(DB·키)는 `latch-data` 볼륨에 저장됩니다.

### Nginx Proxy Manager 설정
- Proxy Host 추가 → Forward: `http://<NAS-IP>:8085`
- **Websockets Support 켜기** (필수 — 에이전트 연결)
- SSL 탭: Let's Encrypt 인증서 발급, **Force SSL** 켜기

### 첫 접속
`https://<도메인>` 접속 → 부모 비밀번호 설정 → 로그인.

## 2. 자녀 PC 설치

### 게시 (개발 PC에서)
```powershell
.\scripts\publish-agent.ps1
```
`publish\agent` 폴더가 만들어집니다 (.NET 포함, 자녀 PC에 별도 설치 불필요).

### 설치 (자녀 PC, 관리자 PowerShell)
1. 웹에서 **페어링 코드 발급** (10분 유효, 1회용)
2. `publish\agent` 폴더를 자녀 PC로 복사한 뒤:
```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\install-agent.ps1 -ServerUrl https://<도메인> -Code <페어링코드>
```
`C:\Program Files\Latch`에 설치되고 `LatchAgent` 서비스가 자동 시작됩니다.
업데이트는 새 게시 폴더에서 `-Code` 없이 다시 실행하면 됩니다.

### 제거
```powershell
.\uninstall-agent.ps1
```
방화벽 규칙과 hosts 차단을 정리한 뒤 제거합니다.

## 3. 사용

| 기능 | 동작 |
|---|---|
| 잠그기 | 전체화면 잠금 + 키보드·마우스 차단. 메시지 표시 가능 |
| 앱 차단 | `Discord.exe`처럼 이름 또는 전체 경로. 해당 앱의 인터넷 연결을 방화벽으로 차단 |
| 사이트 차단 | `youtube.com` 형태. `www.` 자동 포함 |
| 네트워크 연결 | 15초마다 갱신되는 프로세스별 외부 연결 목록 |

## 꼭 알아둘 점

- **자녀 계정은 표준 사용자**여야 합니다. 관리자 계정이면 서비스를 끌 수 있습니다.
- **관리자 계정 세션은 잠기지 않습니다.** 서버나 인터넷 장애로 원격 해제가 안 될 때 부모 계정으로 로그인하면 사용할 수 있습니다.
- 인터넷이 끊겨도 **마지막 정책이 유지**됩니다.
- 자녀 PC에는 트레이 아이콘으로 **관리 중임이 표시**됩니다.
- **Ctrl+Alt+Del**은 Windows가 막을 수 없습니다. 작업 관리자로 잠금 화면을 끄면 3초 안에 다시 뜹니다.
- 사이트 차단은 hosts 파일 방식이라 **브라우저 보안 DNS(DoH)·VPN·IP 직접 접속**으로 우회될 수 있고, 하위 도메인(`m.youtube.com` 등)은 따로 추가해야 합니다.
- 앱 차단은 **인터넷 연결만** 막습니다 (앱 실행 자체는 가능).

## 개발

```sh
dotnet build Latch.slnx
dotnet test tests/Latch.Tests
dotnet run --project src/Latch.Server   # http://localhost:5xxx, 데이터는 src/Latch.Server/data
```

| 프로젝트 | 역할 |
|---|---|
| `Latch.Shared` | 서버·에이전트 공통 메시지 계약 |
| `Latch.Server` | 웹 UI, 인증, SignalR 허브 (net10.0, Linux 컨테이너) |
| `Latch.Agent` | Windows 서비스: 방화벽·hosts·연결 감시·LockUI 관리 |
| `Latch.LockUI` | 잠금 화면·트레이 아이콘 (사용자 세션) |
| `Latch.Tests` | 단위·통합 테스트 |
