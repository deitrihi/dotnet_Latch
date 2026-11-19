#!/usr/bin/env bash
# Latch 서버를 NAS로 전송하고 docker compose로 빌드·기동하는 배포 스크립트
# 접속 정보는 deploy.config(git 제외, deploy.config.example 참고)에서 읽는다.
#
# 사용법
#   cp deploy.config.example deploy.config   # 최초 1회, 값 채우기
#   ./deploy.sh                              # 또는 deploy.bat 더블클릭
#
# Windows에서는 Git Bash로 실행한다 (WSL은 ~/.ssh 경로가 달라 키 인증이 깨진다).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="$SCRIPT_DIR/deploy.config"

if [ ! -f "$CONFIG_FILE" ]; then
  echo "deploy.config가 없습니다. 먼저 만들고 값을 채워주세요." >&2
  echo "  cp deploy.config.example deploy.config" >&2
  exit 1
fi
# shellcheck disable=SC1090
source "$CONFIG_FILE"

: "${DEPLOY_HOST:?deploy.config에 DEPLOY_HOST를 채워주세요}"
: "${DEPLOY_USER:?deploy.config에 DEPLOY_USER를 채워주세요}"
: "${DEPLOY_PATH:?deploy.config에 DEPLOY_PATH를 채워주세요}"
DEPLOY_PORT="${DEPLOY_PORT:-22}"
REMOTE="$DEPLOY_USER@$DEPLOY_HOST"

# SSH 키 인증만 사용. BatchMode=yes라 키 인증이 안 되면 비밀번호를 묻지 않고 바로 실패한다.
SSH_OPTS=(-p "$DEPLOY_PORT" -o BatchMode=yes)
[ -n "${DEPLOY_KEY:-}" ] && SSH_OPTS+=(-i "${DEPLOY_KEY/#\~/$HOME}")
SSH=(ssh "${SSH_OPTS[@]}" "$REMOTE")

# 서버에 안 보낼 것: 빌드 산출물, 로컬 데이터, 에이전트 게시본, 테스트, 배포 자격증명
EXCLUDES=(.git .vs .claude deploy.config claude_logs publish tests bin obj data)

echo "==> 1/4 로컬 빌드 검증"
# 전송 전에 빌드가 깨졌는지 먼저 확인. docker가 안 돌고 있으면 건너뛴다 (실제 빌드는 NAS에서).
if docker info 2>&1 | grep -qi "Server Version"; then
  (cd "$SCRIPT_DIR" && docker compose build)
else
  echo "[안내] 로컬 docker가 동작하지 않아 빌드 검증을 건너뜁니다." >&2
fi

echo "==> 2/4 [$REMOTE:$DEPLOY_PATH] 코드 전송"
"${SSH[@]}" "mkdir -p '$DEPLOY_PATH'"

if [ -n "${DEPLOY_RSYNC_TARGET:-}" ] && command -v rsync >/dev/null 2>&1; then
  # NAS "Rsync 백업 서비스"(데몬 모드): 증분 전송. UGREEN에서 SSH 위 rsync가 거부되는 문제를 피한다.
  echo "[안내] rsync 데몬(포트 ${DEPLOY_RSYNC_PORT:-873})으로 전송: $DEPLOY_RSYNC_TARGET" >&2
  RSYNC_EXCLUDES=()
  for p in "${EXCLUDES[@]}"; do RSYNC_EXCLUDES+=(--exclude "$p"); done
  RSYNC_PASSWORD="${DEPLOY_RSYNC_PASSWORD:?deploy.config에 DEPLOY_RSYNC_PASSWORD를 채워주세요}" \
    rsync -avz --delete --port="${DEPLOY_RSYNC_PORT:-873}" "${RSYNC_EXCLUDES[@]}" \
    "$SCRIPT_DIR/" "$DEPLOY_USER@$DEPLOY_HOST::$DEPLOY_RSYNC_TARGET/"
else
  echo "[안내] tar로 전송합니다." >&2
  TAR_EXCLUDES=()
  for p in "${EXCLUDES[@]}"; do TAR_EXCLUDES+=(--exclude="$p"); done
  tar czf - "${TAR_EXCLUDES[@]}" -C "$SCRIPT_DIR" . | "${SSH[@]}" "tar xzf - -C '$DEPLOY_PATH'"
fi

echo "==> 3/4 NAS에서 docker compose 빌드 + 기동"
# UGOS Docker 프로젝트가 만든 docker-compose.yaml이 있으면 compose가 그쪽을 우선하므로 파일을 명시한다
"${SSH[@]}" "cd '$DEPLOY_PATH' && docker compose -f docker-compose.yml up -d --build"

echo "==> 4/4 상태 확인"
"${SSH[@]}" "cd '$DEPLOY_PATH' && docker compose -f docker-compose.yml ps"

echo "완료. 로그 확인: ssh -p $DEPLOY_PORT $REMOTE \"cd $DEPLOY_PATH && docker compose -f docker-compose.yml logs -f\""
