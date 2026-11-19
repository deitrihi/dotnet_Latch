# 2026-11-19 | 자녀-PC-관리-설계

## 요청
- Windows PC 입력 차단 / 네트워크 감시·차단 프로그램과 웹 제어 사이트 제작

## 행동
- 요구사항 질문 → 자녀 PC 관리, 클라우드 서버, 1~2대, MVP(잠금 화면·앱/도메인 차단·네트워크 감시)
- `2026-11-19` 브랜치 생성
- plan.md, checklist.md, context-notes.md, CHANGELOG.md 작성

## 결정
- 에이전트 아웃바운드 SignalR 연결 (포트포워딩 불필요)
- Windows 서비스 + 사용자 세션 LockUI 분리
- 앱 차단은 Windows 방화벽, 도메인 차단은 hosts 파일로 시작
- 자녀 PC에 관리 중 표시(투명성), 은닉 기능 없음

## 호스팅 변경
- 요청: 서버를 NAS(UGREEN DXP2800) Docker에 올리고 싶음
- 결정: ASP.NET Core 유지, linux/amd64 이미지 + docker-compose, 기존 리버스 프록시 재사용
- plan.md / checklist.md / context-notes.md 갱신

## 미해결
- NAS에서 쓰는 리버스 프록시 종류 확인
- 구현 착수 승인 대기
