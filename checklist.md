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
- [ ] Worker Service + Windows 서비스 등록
- [ ] 페어링 + 토큰 DPAPI 저장
- [ ] SignalR 클라이언트 (자동 재연결)
- [ ] 마지막 정책 로컬 저장/복원
- [ ] 방화벽 앱 차단 적용/해제
- [ ] hosts 도메인 차단 적용/해제
- [ ] TCP 연결 수집 + 전송
- [ ] 사용자 세션에 LockUI 실행/감시

## 4. 잠금 화면 (Latch.LockUI)
- [ ] 전체화면 topmost 창 + 안내 문구
- [ ] 저수준 키보드/마우스 훅
- [ ] 트레이 아이콘 (관리 중 표시)

## 5. 마무리
- [ ] 로컬 통합 테스트 (서버 + 에이전트 1대)
- [ ] 설치 가이드 README
- [x] 호스팅 결정 → UGREEN DXP2800 Docker
- [ ] Dockerfile (linux/amd64) + docker-compose.yml (`/data` 볼륨)
- [ ] ForwardedHeaders 설정 (리버스 프록시 뒤 HTTPS 인식)
- [ ] NAS 리버스 프록시에 연결 + WebSocket 동작 확인
