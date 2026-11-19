# CHANGELOG

## [미커밋]

- src/Latch.Agent/Enforcement/HostsFile.cs — 차단 목록이 비고 관리 구역이 없으면 hosts 파일을 건드리지 않도록 수정
- src/Latch.Agent/Commands.cs — pair 시 서버 연결 실패를 예외 대신 메시지로 안내
- src/Latch.Server/Pages/Device.cshtml — 하위 도메인은 따로 추가해야 한다는 안내 추가
- tests/Latch.Tests/AgentTests.cs — hosts 무변경 테스트 추가

## 2026-11-19 | 6d65a25

- src/Latch.LockUI/Program.cs — 세션당 단일 실행 트레이 앱 진입점
- src/Latch.LockUI/TrayContext.cs — 관리 중 트레이 표시, 상태 파일 감시로 잠금 화면 전환
- src/Latch.LockUI/LockForm.cs — 모니터별 전체화면 잠금 창 (사용자 닫기 차단)
- src/Latch.LockUI/InputBlocker.cs — 저수준 키보드·마우스 훅으로 입력 차단
- src/Latch.LockUI/UserCheck.cs — 관리자 계정 판별 (비상 해제용으로 잠금 제외)
- src/Latch.LockUI/Form1* — 템플릿 폼 제거
- checklist.md, context-notes.md — 4단계 완료 및 결정 기록

## 2026-11-19 | 6a02bda

- src/Latch.Shared/LocalUiState.cs — 에이전트↔LockUI 로컬 잠금 상태 파일 계약
- src/Latch.Agent/AgentStore.cs — 권한 제한된 ProgramData에 설정·정책·UI 상태 저장
- src/Latch.Agent/Enforcement/HostsFile.cs — hosts 관리 구역으로 도메인 차단 + DNS 캐시 비우기
- src/Latch.Agent/Enforcement/FirewallBlocker.cs — 방화벽 COM API로 앱 아웃바운드 차단 규칙 동기화
- src/Latch.Agent/Enforcement/LockUiLauncher.cs — 활성 사용자 세션에 LockUI 실행·감시
- src/Latch.Agent/Monitoring/TcpConnections.cs — 프로세스별 외부 TCP 연결 수집
- src/Latch.Agent/PolicyEnforcer.cs, AgentWorker.cs — 정책 적용, 서버 연결 유지·재시도, 주기 보고
- src/Latch.Agent/Commands.cs, Program.cs — pair/reset 명령, Windows 서비스 호스트
- tests/Latch.Tests/AgentTests.cs — hosts·방화벽 대상 계산·TCP 파싱 테스트
- checklist.md, context-notes.md — 3단계 진행 및 결정 기록

## 2026-11-19 | bc5b123

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
