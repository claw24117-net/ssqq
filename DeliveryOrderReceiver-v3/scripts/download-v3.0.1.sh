#!/usr/bin/env bash
# DeliveryOrderReceiver v3.0.1 다운로드 (Mac/Linux 테스트용)
#
# 동일 흐름의 PowerShell 버전: Download-DOR-v3.0.1.ps1
#
# 사용법:
#   ./download-v3.0.1.sh                  # 대화형 입력
#   EMAIL=foo@bar PASSWORD=xxx ./download-v3.0.1.sh
#   ./download-v3.0.1.sh /tmp/v3.0.1.exe  # 다른 경로에 저장

set -euo pipefail

SERVER="${SERVER:-https://agent.zigso.kr}"
VERSION="3.0.1"
EXPECTED_SHA="e95d58351e0e31ebba6390f0ff47caf7094e274d1949c245daeedc6518843895"
EXPECTED_SIZE=71808727
OUT_FILE="${1:-./DeliveryOrderReceiver-v3.0.1.exe}"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
yel()   { printf '\033[33m%s\033[0m\n' "$*"; }
cyan()  { printf '\033[36m%s\033[0m\n' "$*"; }

fail() { red "[실패] $*"; exit 1; }

cyan "DeliveryOrderReceiver v${VERSION} 다운로드 도구"
echo "서버: $SERVER"
echo "저장: $OUT_FILE"

# ---- 1. 로그인 ----
cyan ""
cyan "=== 1단계 — zigso.kr 로그인 ==="
if [ -z "${EMAIL:-}" ]; then
  read -r -p "이메일: " EMAIL
fi
if [ -z "${PASSWORD:-}" ]; then
  read -r -s -p "패스워드: " PASSWORD
  echo
fi

LOGIN_BODY=$(printf '{"email":"%s","password":"%s"}' "$EMAIL" "$PASSWORD")
LOGIN_RESP=$(curl -sS -X POST "$SERVER/v2/agent/auth/login" \
  -H 'Content-Type: application/json' \
  -d "$LOGIN_BODY") || fail "로그인 요청 실패"

# 메모리에서 즉시 패스워드 폐기
PASSWORD=""
LOGIN_BODY=""

# data.userSessionToken 추출 (jq 있으면 사용, 없으면 grep)
if command -v jq >/dev/null 2>&1; then
  TOKEN=$(echo "$LOGIN_RESP" | jq -r '.data.userSessionToken // empty')
else
  TOKEN=$(echo "$LOGIN_RESP" | grep -o '"userSessionToken"[[:space:]]*:[[:space:]]*"[^"]*"' | sed 's/.*"\([^"]*\)"$/\1/')
fi

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  ERR=$(echo "$LOGIN_RESP" | head -c 300)
  fail "응답에 userSessionToken 없음. 응답: $ERR"
fi
green "[OK] 로그인 성공"

# ---- 2. manifest 확인 ----
cyan ""
cyan "=== 2단계 — v${VERSION} manifest 확인 ==="
MANIFEST=$(curl -sS -H "Authorization: Bearer $TOKEN" \
  "$SERVER/api/windows-agent/download?version=${VERSION}&kind=manifest.json") || fail "manifest 조회 실패"
echo "$MANIFEST" | head -c 500
echo

if command -v jq >/dev/null 2>&1; then
  echo "$MANIFEST" | jq '. | {version: (.data.version // .version), channel: (.data.channel // .channel), active: (.data.active // .active), publishedAt: (.data.publishedAt // .publishedAt), notes: (.data.notes // .notes)}'
fi

# ---- 3. exe 다운로드 ----
cyan ""
cyan "=== 3단계 — exe 다운로드 (~68 MB) ==="
OUT_DIR=$(dirname "$OUT_FILE")
mkdir -p "$OUT_DIR"

curl -sS -H "Authorization: Bearer $TOKEN" \
  -o "$OUT_FILE" \
  -w 'HTTP %{http_code} (size %{size_download} bytes, %{time_total}s)\n' \
  "$SERVER/api/windows-agent/download?version=${VERSION}&kind=exe" || fail "다운로드 실패"

ACTUAL_SIZE=$(stat -f%z "$OUT_FILE" 2>/dev/null || stat -c%s "$OUT_FILE")
green "[OK] 저장 완료 — $OUT_FILE ($ACTUAL_SIZE bytes)"
if [ "$ACTUAL_SIZE" != "$EXPECTED_SIZE" ]; then
  yel "[경고] 크기 불일치 — 기대 $EXPECTED_SIZE, 실제 $ACTUAL_SIZE"
fi

# ---- 4. SHA256 검증 ----
cyan ""
cyan "=== 4단계 — SHA256 검증 ==="
if command -v shasum >/dev/null 2>&1; then
  ACTUAL_SHA=$(shasum -a 256 "$OUT_FILE" | awk '{print $1}')
elif command -v sha256sum >/dev/null 2>&1; then
  ACTUAL_SHA=$(sha256sum "$OUT_FILE" | awk '{print $1}')
else
  fail "shasum/sha256sum 도구 없음"
fi

echo "기대: $EXPECTED_SHA"
echo "실제: $ACTUAL_SHA"
if [ "$ACTUAL_SHA" = "$EXPECTED_SHA" ]; then
  green "[OK] SHA256 일치 — 파일 무결성 검증 통과"
else
  fail "SHA256 불일치. 파일 손상 또는 중간자 공격 가능성. 폐기 권장."
fi

cyan ""
cyan "=== 완료 ==="
green "다운로드 완료: $OUT_FILE"
echo
yel "주의 사항:"
echo "  1. 이건 v3.0.1 (git 트래킹 빌드, 실기기 미테스트)"
echo "  2. 매장 PC에 설치하기 전 단일 PC에서 동작 검증 필요"
echo "  3. v2.0.4 config (config.json) 와 호환되지 않음 (DPAPI 마이그레이션 미구현)"
echo "  4. 별도 폴더에 설치하고 새로 로그인할 것"
echo
