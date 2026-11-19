# Checklist

## 0. 기반
- [x] 요구사항 확인 (자녀 PC 관리, 클라우드 서버, 1~2대)
- [x] plan / checklist / context-notes 작성
- [ ] 솔루션 + 5개 프로젝트 생성, 빌드 확인

## 1. 공통 계약 (Latch.Shared)
- [ ] 정책 DTO (`LockState`, `BlockedApps`, `BlockedDomains`)
- [ ] 감시 DTO (`ConnectionSnapshot`)
- [ ] 직렬화 단위 테스트

## 2. 서버 (Latch.Server)
- [ ] EF Core + SQLite (Parent, Device, Policy)
- [ ] 부모 로그인 (쿠키 인증, 비밀번호 해시)
- [ ] 페어링 코드 발급 → 디바이스 토큰 교환 API
- [ ] SignalR 허브 (디바이스 토큰 인증, 정책 push, 감시 데이터 수신)
- [ ] 웹 UI: 디바이스 목록 / 잠금 토글 / 차단 목록 편집 / 연결 목록

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
- [ ] 클라우드 호스팅 결정
