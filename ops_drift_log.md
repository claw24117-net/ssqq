# 운영 ↔ git drift 기록소

> 이 파일은 **git**과 **운영(prod)** 사이의 모든 어긋남을 단일 위치에 기록한다.
> 세션 시작 시 반드시 이 파일을 읽어서 drift 상태를 파악할 것.
> 새 drift 발생 시 반드시 여기 entry 추가 (커밋과 함께).

---

## 개요 (현재 drift 상태)

| 항목 | 값 |
|---|---|
| 총 drift 건수 | 15건 (14 legacy + 1 이번 세션) |
| 레거시 14건 | git v0.2.138 ↔ prod v0.2.152 (memory/project_git_prod_drift.md 참조) |
| 이번 세션 +1 | `bbb-prod-web-1:/app/apps/web/app/management/devices/download/page.js` (2026-04-08 직접 패치) |
| 원칙 | 기록만. 원복/재패치는 PM 별도 지시. force push 금지. |

---

## 1. 14버전 legacy drift (이전 세션들)

**배경**: 이전 클로드가 git을 무시하고 운영 컨테이너(`bbb-app:*`)를 직접 패치해서 `v0.2.138 → v0.2.152` 까지 14 버전이 git에 없음.

**상세**: `~/.claude/projects/-Users-min-Documents-claude-1/memory/project_git_prod_drift.md` 참조.

**현재 상태**: 이 영역은 이번 plan 범위 밖. 추적만. 복구 계획 없음.

---

## 2. 2026-04-08 — page.js 직접 패치 (drift +1, 이번 세션)

### 2-1. 변경 대상

| 항목 | 값 |
|---|---|
| 컨테이너 | `bbb-prod-web-1` |
| 경로 | `/app/apps/web/app/management/devices/download/page.js` |
| 변경 | v3.0.1 카드 `<article>` + `handleDownloadV301()` 함수 추가 |
| 라인 변동 | +48 / -0 (r1 패치), 이후 r2에서 sha256 표시 갱신 +0/-0 wise |
| 커밋에 기록? | **없음** (git이 아닌 컨테이너 내부 직접 패치) |
| 패치 ID | `dor-v3.0.1-card-v1` (page.js 주석에 명시) |

### 2-2. 실행 이력

| 순서 | 작업 | 시각 (UTC) |
|---|---|---|
| 1 | page.js 원본 백업 → 영속 볼륨 (`page.js.original`, 18916 bytes) | 2026-04-08 ~15:29 |
| 2 | r1 패치 docker cp → 컨테이너 | 2026-04-08 ~15:35 |
| 3 | r1 패치본 백업 → 영속 볼륨 (`page.js.v3.0.1-patched`, 20886 bytes) | 2026-04-08 ~15:35 |
| 4 | `next build` (컨테이너 안, web 패키지만) | 2026-04-08 ~15:36 |
| 5 | `docker restart bbb-prod-web-1` (1차) | 2026-04-08 ~15:36 |
| 6 | 매장 테스트 → HTTP 400 발견 → UploadService.cs fix → r2 재빌드 | 2026-04-08 오후 |
| 7 | page.js의 sha256 표시 + 게시일 r2로 갱신 (sed in container) | 2026-04-08 ~15:53 |
| 8 | r2 패치본 백업 → 영속 볼륨 (`page.js.v3.0.1-patched-r2`, 20891 bytes) | 2026-04-08 ~15:53 |
| 9 | `next build` (2회차) | 2026-04-08 ~15:53 |
| 10 | `docker restart bbb-prod-web-1` (2차) | 2026-04-08 ~15:54 |

### 2-3. 영속 볼륨 백업 (컨테이너 재생성 시에도 생존)

위치: `bbb-prod-api-1:/app/storage/windows-agent/page-backups/2026-04-09/` (볼륨 `bbb-prod_bbb_prod_windows_agent_storage`)

| 파일 | 크기 | 내용 |
|---|---|---|
| `page.js.original` | 18,916 bytes | 패치 전 원본 (sha256 `858f3859a91201dd978016d1cbb2a8e414b5b1bafe5ad399437113d88cef0f84`) |
| `page.js.v3.0.1-patched` | 20,886 bytes | r1 패치 (옛 sha256 표시: `23f7345650fa...`) |
| `page.js.v3.0.1-patched-r2` | 20,891 bytes | r2 패치 (새 sha256 표시: `e95d58351e0e...`) |

### 2-4. 관련 운영 배포 산출물

**`/app/storage/windows-agent/releases/3.0.1/`** (영속 볼륨, 컨테이너 재생성에도 생존)

| 파일 | 값 |
|---|---|
| `DeliveryOrderReceiver-v3.0.1.exe` | **현재 r2** — sha256 `e95d58351e0e31ebba6390f0ff47caf7094e274d1949c245daeedc6518843895`, 71,808,727 bytes, 2026-04-09T02:52:54Z |
| r1 (옛 빌드) | sha256 `23f7345650fa84e4dfc508e167b56373babf5734565bb17153538f4518410613`, 71,808,718 bytes — **r2로 덮어써짐**. 흔적 없음. |
| `manifest.json` | `{"version":"3.0.1","channel":"beta","active":false,"publishedAt":"2026-04-09T02:52:54Z", ...}` |

**`/app/storage/windows-agent/releases/latest.json`** — **그대로 v2.0.4** (의도적 미갱신)
```json
{"version":"2.0.4","channel":"stable","active":true,"publishedAt":"2026-04-03T00:00:00Z"}
```

### 2-5. 검증 스냅샷 (2026-04-09T03:14 UTC)

DB `bbb-prod-postgres-1` 쿼리 결과:

- `upload_jobs` 5건 `completed`, `upload_type = receipt_raw`, device_id `dev_9aa8864422484fbe911fceed74940b24`
- `raw_receipts` 5건, `site_id = site_001`, `platform_id = unknown`, `platform_store_id = unknown`, port = COM15
- 영수증 내용 예: 배민 세교동(배민원) T2BW0000AAEG, 쿠팡이츠 0029YW, 쿠팡이츠 2H7RY9 등

→ v3.0.1 r2가 실 매장에서 영수증 받아 서버에 정상 저장 중.

### 2-6. 위험 포인트

1. **컨테이너 재생성 시 page.js 패치 증발**
   - 트리거: `docker compose up --force-recreate`, `docker rm bbb-prod-web-1 && docker compose up`, 이미지 재빌드
   - 영향: `/management/devices/download` 페이지에서 v3.0.1 카드 사라짐. v2.0.4 카드만 남음.
   - exe 자체는 영속 볼륨이라 생존. 매장 PC는 이미 받아서 쓰고 있으면 영향 없음.
2. **Next.js BUILD_ID 변동**
   - 컨테이너 재시작마다 `.next/BUILD_ID` 바뀜
   - 브라우저 캐시한 옛 chunk 요청 시 "Failed to find Server Action" 에러 로그 잠깐 나옴 (자동 재로드하면 정상)
3. **운영 백업 정책 의존**
   - 영속 볼륨 `bbb-prod_bbb_prod_windows_agent_storage`가 유실되면 모든 복구 지점 날아감
   - 호스트 파일시스템 백업은 PM 영역, 본 plan 범위 밖

### 2-7. 복구 절차 (page.js 패치 증발 시)

**전제**: PM 별도 지시 있을 때만 수행.

```bash
ssh -i ~/.ssh/id_ed25519_zigso min@zigso.kr
docker exec bbb-prod-api-1 cat /app/storage/windows-agent/page-backups/2026-04-09/page.js.v3.0.1-patched-r2 > /tmp/page.js
docker cp /tmp/page.js bbb-prod-web-1:/app/apps/web/app/management/devices/download/page.js
docker exec -w /app/apps/web bbb-prod-web-1 sh -c 'pnpm exec next build'
docker restart bbb-prod-web-1
# 그 후 새 ops_drift_log.md entry 추가 (drift +2 or 복구 완료 기록)
```

---

## drift 원칙 (반복)

1. **새 drift 발생 시 반드시 본 파일에 entry 추가** (동일 커밋 또는 즉시 별도 커밋)
2. **force push / main 직접 푸시 절대 금지**
3. **직접 패치는 PM 명시 허락 전제**
4. **기록은 git, 실제 변경물은 운영 영속 볼륨 + 본 파일의 경로/sha256**
5. 이 파일의 entry는 **절대 삭제하지 않음** (히스토리). 복구 완료 시 상단에 "STATUS: recovered <date>" 마킹만 추가.

## 관련 파일

- `ssqq/work_log.md` — 각 세션의 작업 로그 (이 drift를 야기한 세션 entry 포함)
- `ssqq/.session-format.md` — 세션 로그 템플릿
- `ssqq/DeliveryOrderReceiver-v3/CHANGELOG.md` — v3.0.1 r1/r2 빌드 이력
- `~/.claude/projects/-Users-min-Documents-claude-1/memory/project_git_prod_drift.md` — legacy 14버전 drift 메모리
