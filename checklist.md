# Checklist

## 0. 기반
- [x] 요구사항 확인 (자녀 PC 관리, 클라우드 서버, 1~2대)
- [x] plan / checklist / context-notes 작성
- [x] 솔루션 + 5개 프로젝트 생성, 빌드 확인

## 1. 공통 계약 (Latch.Shared)
- [x] 정책 DTO (`LockState`, `BlockedApps`, `BlockedDomains`)
- [x] 감시 DTO (`ConnectionSnapshot`)
- [x] 직렬화 단위 테스트

## 2. 서버 (Latch.Server)
- [x] EF Core + SQLite (Parent, Device, Policy)
- [x] 부모 로그인 (쿠키 인증, 비밀번호 해시)
- [x] 페어링 코드 발급 → 디바이스 토큰 교환 API
- [x] SignalR 허브 (디바이스 토큰 인증, 정책 push, 감시 데이터 수신)
- [x] 웹 UI: 디바이스 목록 / 잠금 토글 / 차단 목록 편집 / 연결 목록

## 3. 에이전트 (Latch.Agent)
- [x] Worker Service + Windows 서비스 등록
- [x] 페어링 + 토큰 저장 (ProgramData ACL 보호)
- [x] SignalR 클라이언트 (자동 재연결)
- [x] 마지막 정책 로컬 저장/복원
- [x] 방화벽 앱 차단 적용/해제
- [x] hosts 도메인 차단 적용/해제
- [x] TCP 연결 수집 + 전송
- [x] 사용자 세션에 LockUI 실행/감시

## 4. 잠금 화면 (Latch.LockUI)
- [x] 전체화면 topmost 창 + 안내 문구
- [x] 저수준 키보드/마우스 훅
- [x] 트레이 아이콘 (관리 중 표시)

## 5. 마무리
- [x] 로컬 통합 테스트 (서버 + 에이전트 1대, 빈 정책·잠금 push까지)
- [ ] 실기 잠금 화면·앱/사이트 차단 테스트 (자녀 PC 또는 표준 계정)
- [x] 설치 가이드 README + 설치/제거 스크립트
- [x] 호스팅 결정 → UGREEN DXP2800 Docker
- [x] Dockerfile (linux/amd64) + docker-compose.yml (`/data` 볼륨) — 빌드·컨테이너 실행·재시작 후 유지 확인
- [x] ForwardedHeaders 설정 (리버스 프록시 뒤 HTTPS 인식)
- [x] NAS 배포 (deploy.sh, 192.168.0.200:8085 기동·LAN 접속 확인)
- [x] nginx(nas.conf) 연결 + DNS(latch.deitrihi.dev) + HTTPS 확인
- [ ] WebSocket 실연결 확인 (자녀 PC 에이전트 설치 시)
