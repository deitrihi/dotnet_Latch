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

## 구현 (MVP)
- 요청: 구현 시작 (리버스 프록시 = Nginx Proxy Manager)
- 커밋: d8a544b 골격 → 607ad9d Shared → bc5b123 Server → 6a02bda Agent → 6d65a25 LockUI → 7eca0ef 버그 수정 → 92205fb 배포
- 테스트 25개 통과. 개발 PC에서 페어링·온라인·연결 보고·잠금 push 통합 확인 후 정리
- 결정: 토큰 보호는 ProgramData ACL, 관리자 세션은 잠금 제외(비상 해제), 재연결 지수 백오프, Docker 명명된 볼륨
- 발견/수정: 빈 정책에서 hosts 재작성, pair 연결 실패 시 크래시

## 미해결
- (해결) Docker 이미지 빌드·실행 검증 완료
- 실기 잠금·차단 테스트 (표준 계정)
- NPM 연결 + WebSocket 확인


## 배포 스크립트
- 요청: dho/dbsql deploy.config 참고해 deploy.bat 작성
- 행동: deploy.bat/deploy.sh/deploy.config.example 추가, deploy.config는 git 제외 (f4f9269)
- 발견: NAS에 UGOS가 만든 docker-compose.yaml(root 소유)이 우선 적용됨 → -f docker-compose.yml 명시
- 배포 실행: NAS 빌드·기동 성공, LAN에서 /Setup 200·허브 401 확인, 재시작 0회

## nginx 연결
- nas.conf에 latch.deitrihi.dev 블록(사용자 추가) → nginx -t 통과, reload
- NAS 내부 8443 경유: latch 302(https 리다이렉트 정상)·허브 401, dho·wiki 정상
- 미해결: DNS latch CNAME 미등록 (외부 접속 불가), 초기 비밀번호 설정

## 외부 공개
- DNS latch CNAME 반영 확인, https://latch.deitrihi.dev 외부 접속·와일드카드 인증서 검증 통과
- 미해결: 초기 비밀번호 설정(사용자), WebSocket 실연결은 에이전트 설치 시 확인
