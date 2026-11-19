# CHANGELOG

## [미커밋]

- src/Latch.Server/Data/LatchDb.cs — 부모 계정·디바이스·페어링 코드 SQLite 모델
- src/Latch.Server/Services/* — 페어링·토큰 해시·정책 push 서비스, 디바이스 토큰 인증 핸들러, 온라인 추적
- src/Latch.Server/Hubs/DeviceHub.cs — 에이전트용 SignalR 허브 (정책 조회, 연결 보고)
- src/Latch.Server/Program.cs — DB·인증·레이트리밋·ForwardedHeaders·허브 구성
- src/Latch.Server/Pages/* — 초기 설정, 로그인, 디바이스 목록·잠금·차단 목록·연결 조회 화면
- src/Latch.Server/wwwroot/site.css — 모바일 우선 스타일
- src/Latch.Agent/Latch.Agent.csproj — SignalR 클라이언트·WindowsServices 패키지 추가, Hosting 10.0.15
- tests/Latch.Tests/ServerTests.cs — 페어링·허브 인증·정책 push·로그인 요구 통합 테스트
- checklist.md — 2단계 완료 표시
- context-notes.md — 서버 구현 결정 기록

## 2026-11-19 | 607ad9d

- src/Latch.Shared/Contracts.cs — 정책·연결 감시·페어링 메시지와 허브 이름 정의
- src/Latch.Shared/PolicyRules.cs — 도메인·앱 차단 항목 정규화 규칙
- tests/Latch.Tests/SharedTests.cs — 정규화·직렬화 테스트
- checklist.md — 1단계 완료 표시

## 2026-11-19 | d8a544b

- Latch.slnx, src/*, tests/Latch.Tests — 솔루션과 5개 프로젝트 생성 (Agent/Tests는 net10.0-windows)
- .gitignore — dotnet 기본 gitignore 추가
- checklist.md — 0단계 완료 표시
- context-notes.md — 리버스 프록시(Nginx Proxy Manager) 기록

## 2026-11-19 | 53a9e9d

- plan.md — 배포 대상을 클라우드에서 NAS(UGREEN DXP2800) Docker로 변경
- checklist.md — Docker 배포 작업 항목 추가
- context-notes.md — 호스팅 변경 결정과 이유 기록
- claude_logs/자녀-PC-관리-설계.md — 호스팅 변경 내용 추가

## 2026-11-19 | fed4c13

- plan.md — 자녀 PC 원격 관리(Latch) 아키텍처 및 MVP 계획 작성
- checklist.md — MVP 작업 체크리스트 작성
- context-notes.md — 요구사항 및 설계 결정 기록
- claude_logs/자녀-PC-관리-설계.md — 세션 로그 생성
