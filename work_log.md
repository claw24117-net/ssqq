# 작업 로그 (DeliveryOrderReceiver)

> 본 파일은 git 트래킹 기준 작업 로그.
> **운영(prod) 직접 패치는 `ssqq/ops_drift_log.md`에 동시 기록**.
> 매 entry 형식은 `ssqq/.session-format.md` 템플릿을 따름.

## 세션 시작 체크리스트

1. 본 파일 최근 entry 읽기 (마지막 세션이 뭐 했는지)
2. `ssqq/ops_drift_log.md` 읽기 (현재 drift 상태)
3. `git status` / `git log --oneline -5` (브랜치 + ahead/behind 확인)
4. 작업 범위가 `ssqq/` 경계 내인지 확인
5. `.session-format.md` 템플릿을 염두에 두고 entry 초안 잡기

## 세션 종료 체크리스트

1. 이 파일에 오늘 세션 entry 추가했나? (템플릿 준수)
2. drift 발생했으면 `ops_drift_log.md`에도 entry 추가했나?
3. `git status` clean? 커밋 완료? (Conventional Commits: `feat/fix/docs/refactor/build`)
4. `git push` 필요 여부 판단 (허락된 경우만 실행)
5. entry 끝에 "다음 세션용 현재 상태" 한 줄 적었나?

## 경계 규칙 (절대)

- `ssqq/` 폴더 밖 변경 금지 (v2 `DeliveryOrderReceiver/`, `server/`, `claude-1/` 루트)
- `main` 브랜치 직접 커밋 금지
- `git push --force` 금지 (fast-forward만)
- 운영 직접 패치는 PM 명시 허락 + `ops_drift_log.md` 기록 의무

---

### 2026-04-08 (오전, v3.0.1 정합화)

1. v3.0.1 정합화 분석:
   - 코드 현 상태 검증 (Forms/MainForm.cs 1671줄, Services/AuthService.cs, Models/LoginConfig.cs, Services/OrderStorageService.cs)
   - BUG-001/002/003/004 / W-TIME / W-RETRY 모두 git 코드에서 처리 완료 확인
     · BUG-001: `_config.Token = ""`은 LogoutButton_Click(L1456) 한 곳뿐, LoginButton_Click(L726)에는 없음
     · BUG-004: AuthService.cs:55-69에서 `data.userSessionToken` 경로로 추출
     · W-TIME: MainForm.cs:884에서 `DateTime.UtcNow.ToString("o")` 정상 UTC
   - 진짜 미수정: B-4/B-5(DPAPI), C-3/C-4(파일 잠금), PM 미테스트(재부팅/자동재로그인/포트관리)
     · LoginConfig.cs:11 Token, :20 Password 모두 평문
     · OrderStorageService.cs Save() L52 / UpdateStatus() L81 — File.WriteAllText + File.Replace, FileShare 미설정
2. 기획서 정합화:
   - dev_progress.md 상태 라벨 단일화 (BUG-001 두 번 표기 → "해결됨 (v3.0.1 git 코드 기준)" 한 곳)
   - dev_progress.md 미해결 항목을 보안 / 데이터 무결성 / PM 미테스트 / 코드 품질 4분할
   - dev_progress.md 고려사항에 4경계 추가: (a) 두 클라이언트 경계, (b) 가상프린터 off-limits, (c) 서버 측 계약 authoritative 참조, (d) 현 개발/배포 방법
   - issue_matrix.md C-1/C-2/C-5 보정, PM 지시 줄 갱신
   - issue_matrix.md 미해결 표 — 옛 'BUG-001 미수정' 행 제거, B-4/B-5/C-3/C-4 4행 추가
3. 라벨 정책:
   - git 트래킹 코드 상태 = **v3.0.1** (라벨 전용; csproj `Version`은 2.0.4 유지)
   - 운영(prod) 배포 버전과 별도 트래킹 (이전 클로드가 git 무시하고 직접 패치한 14 버전 차이 잔재)
   - 운영 직접 패치 / force push 금지 정책 명시
4. 코드/빌드/배포 변경: **0건** (PM 지시 대기 유지)
   - DeliveryOrderReceiver/ 코드: 0줄 변경
   - server/ 전체 (가상프린터 / 서버 계약 / 빌드 스크립트 / release-manifest): 0줄 변경
   - 가상프린터 단일 진실 출처 windows-agent-unified-plan-v1.md: 0줄 변경 (인용만)

**검증**: `git diff --stat` 결과 `dev_progress.md`, `issue_matrix.md`, `work_log.md` 3파일만
**커밋**: `1e0fd3e docs: v3.0.1 정합화 — 라벨 분리 트래킹 + 4경계 명시 (코드 0 변경)`

---

### 2026-04-08 (오후, v3.0.1 r2 운영 배포 세션)

**범위**: mixed (git + ops-drift)
**브랜치**: `work/v2.0.4-reconciliation`
**drift 발생**: yes → `ops_drift_log.md` 섹션 2 참조

**시작 전 상태**:
- git: `work/v2.0.4-reconciliation`, 최신 `1e0fd3e` (오전 정합화), origin 대비 +1 ahead
- ops: `/app/storage/windows-agent/releases/` = `{2.0.4/, latest.json}`, `latest.json`=v2.0.4
- `bbb-prod-web-1` page.js = 원본 (`handleDownloadV301` 없음)
- `ssqq/DeliveryOrderReceiver-v3/` 폴더 없음

**작업 내용**:
1. `ssqq/DeliveryOrderReceiver-v3/` WPF 프로젝트 27파일 작성 (.NET 8 + WPF, win-x64 self-contained)
   - `Models/`: LoginConfig(DPAPI), OrderRecord
   - `Helpers/`: DpapiHelper, FileLockHelper, EscPosParser
   - `Services/`: Auth, Upload, OrderStorage, Port, SerialReceiver, AutoStart, AdminAuth
   - `Views/`: LoginView, MainView, SettingsView, AdminPasswordDialog
   - `scripts/`: Download-DOR-v3.0.1.ps1, download-v3.0.1.sh, README.md
2. Parallels Win11 VM에 .NET 8 SDK 설치 (`C:\dotnet-sdk`, win-arm64, cross-build win-x64)
3. **r1 빌드**: `dotnet publish -c Release -r win-x64 --self-contained true` → 154MB (native DLL 분리)
4. csproj 에 `IncludeNativeLibrariesForSelfExtract=true` + `EnableCompressionInSingleFile=true` → 68.5MB single-file exe
5. **r1 서버 배포**:
   - SCP → `/tmp/dor-v3.0.1-stage/` → `docker cp` → `bbb-prod-api-1:/app/storage/windows-agent/releases/3.0.1/`
   - `manifest.json` 신설 (channel=beta, active=false)
   - `latest.json` **안 건드림** (자동 업데이트는 v2.0.4 그대로)
6. **페이지 직접 패치** (`bbb-prod-web-1:/app/apps/web/app/management/devices/download/page.js`):
   - 원본 백업 → 영속 볼륨 (`page-backups/2026-04-09/page.js.original`)
   - v3.0.1 카드 `<article>` + `handleDownloadV301()` 함수 추가 (+48줄 / -0줄)
   - r1 패치본 백업 → 영속 볼륨 (`page.js.v3.0.1-patched`, 20886 bytes)
   - 컨테이너 안 `next build` → `docker restart bbb-prod-web-1` (1차)
7. **매장 테스트 → HTTP 400 발견**
8. 서버 `agent-routes.js` 조사 → `requiredFields` 필터가 `platformId/platformStoreId`를 빈 문자열 아닌 string으로 요구 확인
9. **r2 fix**: `Services/UploadService.cs` — `(string?)null` → `"unknown"` (v2.0.4와 동일 우회)
10. **r2 재빌드**: clean + publish → 68.5MB exe (sha256 `e95d58351e0e...`)
11. **r2 서버 재배포**: 컨테이너 안 `releases/3.0.1/` exe 덮어쓰기 + manifest sha256 갱신
12. **페이지 r2 갱신**: 컨테이너 안 `sed` 로 page.js 의 sha256 + 게시일 r2로 교체 → r2 백업(`page.js.v3.0.1-patched-r2`, 20891 bytes) → `next build` → `docker restart` (2차)
13. **DB 검증**: `docker exec bbb-prod-postgres-1 psql` → `upload_jobs` 5건 `completed`, `raw_receipts` 5건 insert

**발견된 문제/이슈**:
- [해결] r1 `UploadService.cs` `platformId/platformStoreId = null` → HTTP 400. r2 에서 `"unknown"` 하드코딩 fix (`ssqq/DeliveryOrderReceiver-v3/Services/UploadService.cs:78-79`)
- [해결] 첫 빌드에서 native DLL 5개가 exe 옆에 분리됨 → `IncludeNativeLibrariesForSelfExtract` 옵션으로 single-file 화
- [잔존] `bbb-prod-web-1:page.js` drift +1 — 컨테이너 재생성 시 증발. 영속 볼륨 백업 3개 있음. `ops_drift_log.md` 섹션 2.6/2.7 복구 절차 참조
- [잔존] `Views/MainView.xaml.cs(23,32)` `MainView.LogoutRequested` 미사용 경고 `CS0067` — 기능 영향 없음, 나중에 로그아웃 버튼 추가 시 살아남
- [잔존] v3.0.1 은 v2.0.4 평문 `config.json` → DPAPI 자동 마이그레이션 미구현. 같은 폴더에 덮어쓰면 자동 재로그인 실패 (별도 폴더 설치 권장)
- [잔존] `latest.json` 은 의도적으로 v2.0.4 유지. v3.0.1 promotion 은 PM 별도 지시 시

**끝난 후 상태**:
- git: 오전 커밋 `1e0fd3e` 그대로 (이번 세션 후 추가 커밋은 P6 단계에서 들어감)
- ops:
  - `/app/storage/windows-agent/releases/3.0.1/` exe + manifest 신설 (r2, sha256 `e95d58351e0e...`)
  - `/app/storage/windows-agent/releases/latest.json` 그대로 v2.0.4
  - `/app/storage/windows-agent/page-backups/2026-04-09/` 3개 백업 파일
  - `bbb-prod-web-1:page.js` v3.0.1 카드 포함 (drift)
  - `bbb-prod-api-1` / `bbb-prod-worker-1` / `bbb-prod-postgres-1` / `bbb-prod-redis-1` 재시작 0건
  - `bbb-prod-web-1` `docker restart` 2회
- 매장: v3.0.1 r2가 실 운영, 영수증 서버 반영 중

**변경 파일** (git 기준, P6 커밋 전):
- 신규 untracked: `ssqq/DeliveryOrderReceiver-v3/**/*` (~30 파일, bin/obj 제외)
- 신규 untracked: `ssqq/ops_drift_log.md`
- 신규 untracked: `ssqq/.session-format.md`
- 수정: `ssqq/work_log.md` (이 entry 추가 + 상단 HEADER)
- 수정: `ssqq/dev_progress.md` (상태 라인 1줄 추가)

**검증**:
- SHA256 3곳 일치: 로컬 빌드 / 호스트 `/tmp/dor-v3.0.1-stage/` / 컨테이너 `releases/3.0.1/` → `e95d58351e0e31ebba6390f0ff47caf7094e274d1949c245daeedc6518843895`
- DB 쿼리: `SELECT * FROM upload_jobs WHERE upload_type='receipt_raw' ORDER BY received_at DESC LIMIT 5` → 5/5 `completed`
- `SELECT platform_id, platform_store_id FROM raw_receipts ORDER BY created_at DESC LIMIT 5` → 모두 `unknown`/`unknown`, `site_id=site_001`, `port=COM15`
- 운영 경계 0 변경 확인: `latest.json` = `{"version":"2.0.4",...}`, `releases/2.0.4/` sha256 `24d8109c...1dd3fd`, bbb-prod-api-1 Up 4 days healthy
- 신 `meybOCiMU5SGkXAaBPRrt` BUILD_ID, 새 chunk `page-64acfe5f7d0af961.js` 에 v3.0.1 / `e95d58351e0e` / `r2` 키워드 확인

**커밋**: (P6 단계에서 아래 4개로 분할 예정)
- `feat(v3.0.1): import WPF client source as deployed to prod (r2)`
- `docs(ops): add ops_drift_log.md — 2026-04-08 page.js direct patch (drift +1)`
- `docs(v3.0.1): record 2026-04-08 afternoon deploy session + CHANGELOG`
- `docs: add session log format + work_log header template`

**다음 세션용 현재 상태 (한 줄)**:
v3.0.1 r2 운영 beta 격리 배포 중, 매장 동작 확인, page.js drift +1 기록됨, git import 대기 (P6 커밋+push로 완료 예정).

---

### 2026-04-09 (v3.0.1 r2 운영 1일차 검증)

**범위**: git-only (문서만)
**브랜치**: `work/v2.0.4-reconciliation`
**drift 발생**: no

**시작 전 상태**:
- git: `320ef03` (ops-drift + session-format 커밋 완료), origin 동기화
- ops: v3.0.1 r2 운영 중 (어제 배포), com0com COM19 ↔ COM20 사용
- 매장 질문: v3.0.1의 포트 설정 방법 (v2.0.4와 다른가?)

**작업 내용**:
1. 사용자 질문: "가상포트 COM19/COM20 이미 있는데 v3.0.1 설정 방법?"
2. v3.0.1 코드 re-read:
   - `Views/MainView.xaml.cs:41-50` — `SerialPort.GetPortNames()` 자동 스캔 → 드롭다운
   - `Views/SettingsView.xaml.cs:53-76` — `CreatePortButton_Click` 은 `setupc install` (새 포트 생성용)
3. 답변: 설정 창 불필요. 메인 화면에서 COM19/COM20 중 하나 선택 → 수신 시작
4. 사용자 확인: "수신 됐어" → 포트 선택 성공
5. DB 검증 쿼리 실행:
   - upload_jobs 최근 15건 (receipt_raw) 전부 `completed`
   - 오늘 누적 58건 completed / 0 failed
   - raw_receipts 내용 확인: 배민 / 쿠팡이츠 / **요기요** 3개 플랫폼 정상 수신

**발견된 문제/이슈**:
- [관찰] v3.0.1 설정 창에 "기존 포트 등록" 버튼 없음 — 이미 존재하는 com0com 쌍을 config.CreatedPortA/B에 등록하는 UI 부재. 현재는 config에 안 저장돼도 메인 화면에서 LastPort로 저장됨 (동작 무관). 향후 r3에서 "포트 등록" 버튼 추가 후보.
- [해결] 사용자 혼동: 설정 창이 "포트 생성 전용"이라는 걸 몰라서 기존 포트 등록 방법을 찾음 → 설명으로 해소
- [관찰] 플랫폼 ID/store ID는 여전히 모두 "unknown" (r2 fix 하드코딩). 향후 ESC/POS 텍스트 첫 줄에서 "쿠팡이츠 주문"/"배달의민족 주문"/"요기요 주문" 추출해서 채울 수 있음 — 별도 트랙

**끝난 후 상태**:
- git: 이 entry 추가 외에는 변경 없음
- ops: v3.0.1 r2 계속 운영, 매장에서 3개 플랫폼 지속 수신
- 누적 통계: 58 completed / 0 failed (UTC 2026-04-08 15:00 ~ 04-09 12:35)
- 5~30분 간격 실시간 수신

**변경 파일** (git 기준):
- `ssqq/work_log.md` (이 entry 추가만)

**검증**:
- `docker exec bbb-prod-postgres-1 psql -U bbb -d bbb -c "SELECT COUNT(*) ... FROM upload_jobs WHERE upload_type='receipt_raw' AND received_at >= '2026-04-08T15:00:00Z'"` → 58/58 completed, 0 failed
- raw_receipts 샘플 15건: 배민 동삭동/세교동/통복동/평택동, 쿠팡이츠 211XDH/0C6PYJ/08G7S7/..., 요기요 #9381, 매장천사 테스트 1건
- [해결] r2 UploadService.cs fix (`platformId = "unknown"`) 실전 검증 완료 — 24시간 0 failure

**커밋**: (pending — 이 entry 추가 후 단일 커밋 예정)
- `docs: 2026-04-09 v3.0.1 r2 1일차 운영 검증 (58/58 completed)`

**다음 세션용 현재 상태 (한 줄)**:
v3.0.1 r2 운영 1일차, 매장 3개 플랫폼(배민/쿠팡이츠/요기요) 58/58 무결점. B-4/B-5/C-3/C-4 처음부터 박은 fix 실전 검증 OK. 향후 개선 후보: platformId 자동 추출, 설정 창 "기존 포트 등록" 버튼.

---

### 2026-04-10 (v3.0.1 r3 — 클라이언트 2-bug fix)

**범위**: mixed (git + ops 배포, drift 신규 0)
**브랜치**: `work/v2.0.4-reconciliation`
**drift 발생**: no (download page sha256 갱신은 §2 in-place, hub page 패치 안 함)

**시작 전 상태**:
- git: `e1442f4` (r2 1일차 검증 entry), origin 동기화
- ops: `releases/3.0.1/` = r2 (sha256 `e95d58351e0e...`), latest.json=v2.0.4 (그대로), bbb-prod-web-1 page.js download 카드 = r2 drift
- 사용자 매장 발견 3가지 이슈:
  1. 관리자 비밀번호 매번 setup mode 진입 (의도는 1회만)
  2. 주문 내역 한 행이 영수증 줄 수만큼 세로로 키짐
  3. 서버 웹 페이지에서 영수증 시각이 재전송 시각으로 표시

**작업 내용**:
1. 진단 — Phase 1 Explore agent + 직접 read:
   - Bug 1: `AdminAuthService.PromptForAccess()` 가 매번 `LoginConfig.Load()` 로 별개 인스턴스 B 만듦. MainWindow 의 stale 인스턴스 A 가 [수신 시작] 시 `_config.Save()` 호출하면서 빈 `AdminPasswordEncrypted` 로 디스크 덮어씀. → race
   - Bug 2: `MainView.xaml` DataGrid 에 `RowHeight` 미설정. `Content` 컬럼이 multi-line ESC/POS 텍스트를 그대로 렌더링.
   - Bug 3: 클라이언트 코드는 정상. `capturedAt = order.ReceivedAt` 정확히 송신. DB 638/638 `captured_at NOT NULL`. server `/orders/hub/page.js:265` 가 `formatTimestamp(r.created_at)` 표시.
2. 사용자 결정: Bug 1, 2 만 r3 로 fix. Bug 3 은 hub page 직접 패치 회피 (drift +1 안 만들기) → [잔존].
3. 클라이언트 코드 수정 (3 파일):
   - `Services/AdminAuthService.cs`: `LoginConfig` 생성자 주입, 자체 `Load()` 2건 제거
   - `MainWindow.xaml.cs`: `_adminAuth = new AdminAuthService(_config)` (1줄)
   - `Views/MainView.xaml`: `RowHeight="28"` + 내용 컬럼 `DataGridTemplateColumn` (TextBlock NoWrap+Ellipsis+ToolTip)
4. `CHANGELOG.md`: r3 섹션 신설 (sha256, 변경 내역, 잔존 이슈)
5. Parallels 빌드 r3:
   - clean → `dotnet publish -c Release -r win-x64 --self-contained true`
   - sha256: `9471b1ec3ad44746985d2b620b4034ce176ea4f8d188c60895715bf5946473ab`
   - size: 71,808,979 bytes (r2 +252)
6. 운영 r3 배포:
   - r2 백업 → 영속 볼륨 (`page-backups/2026-04-10/DeliveryOrderReceiver-v3.0.1-r2.exe.backup` + `manifest-r2.json.backup`)
   - SCP → host staging → docker cp → 권한
   - 3곳 sha256 일치 검증 (로컬/호스트/컨테이너)
   - manifest.json 갱신 (channel=beta, active=false 유지, sha256/size/notes r3)
   - latest.json 0 변경 재확인
7. download page sha256 리터럴 갱신 (기존 §2 drift in-place):
   - sed 2회 (sha256, 게시일)
   - r3 패치본 영속 백업 (`devices-download-page.js.v3.0.1-patched-r3`)
   - Next.js 재빌드 → 새 BUILD_ID `2JAKlHGTMA5dWgPgVN-an`
   - `docker restart bbb-prod-web-1` → 22초 healthy
8. ops_drift_log.md §3 추가 (r3 배포, r2 백업, hub page 미진행)
9. 본 work_log entry 작성

**발견된 문제/이슈**:
- [해결] Bug 1 — AdminAuthService DI fix (stale instance race)
- [해결] Bug 2 — DataGrid RowHeight 28 + DataGridTemplateColumn
- [잔존] Bug 3 — server `/orders/hub/page.js:265` 1줄 fix 가능하지만 사용자 결정으로 r3 에서 회피. 서버 이미지 정식 리빌드 시 처리. 우회: 행 클릭 후 상세 패널 `Captured At` 확인.
- [잔존] `MainView.LogoutRequested` 미사용 경고 CS0067 (기능 영향 없음)
- [잔존] v2.0.4 평문 config → v3 DPAPI 자동 마이그레이션 미구현

**끝난 후 상태**:
- git: 본 entry 추가 후 4 커밋 추가 예정 (R8)
- ops:
  - `releases/3.0.1/exe` = r3 (sha256 `9471b1ec3ad4...`)
  - `releases/3.0.1/manifest.json` = r3 메타
  - `latest.json` = v2.0.4 그대로
  - `bbb-prod-web-1:page.js` = r3 sha256 표시 (drift 신규 0, 기존 §2 in-place 갱신)
  - 영속 볼륨에 r2 백업 + r3 패치본 백업 보존
  - 컨테이너 재시작: bbb-prod-web-1 1회만 (다른 모두 재시작 0)
- 매장: r3 다운로드 + 검증은 사용자 단계 (R9 후)

**변경 파일** (git 기준):
- `ssqq/DeliveryOrderReceiver-v3/Services/AdminAuthService.cs` (수정)
- `ssqq/DeliveryOrderReceiver-v3/MainWindow.xaml.cs` (수정)
- `ssqq/DeliveryOrderReceiver-v3/Views/MainView.xaml` (수정)
- `ssqq/DeliveryOrderReceiver-v3/CHANGELOG.md` (수정)
- `ssqq/ops_drift_log.md` (수정 — §3 추가)
- `ssqq/work_log.md` (수정 — 본 entry)

**검증**:
- sha256 3곳 일치 검증 (로컬 빌드 / 호스트 staging / 컨테이너 release): `9471b1ec3ad44746985d2b620b4034ce176ea4f8d188c60895715bf5946473ab`
- `latest.json` `{"version":"2.0.4",...}` 그대로
- v2.0.4 sha256 `24d8109c...1dd3fd` 그대로
- 컨테이너 헬스: bbb-prod-web-1 22초 healthy (재시작), 다른 모두 4-11일 healthy 그대로
- Next.js 새 BUILD_ID: `2JAKlHGTMA5dWgPgVN-an`
- download page 안에 r3 sha256 (`9471b1ec`) + 게시일 r3 표기 확인 (line 532, 535)
- 매장 검증 (R9 후 사용자 단계): 관리자 비밀번호 영속화, DataGrid 행 28px

**커밋**: (R8 단계에서 4개로 분할 예정)
- `fix(v3.0.1): inject LoginConfig into AdminAuthService (bug 1)`
- `fix(v3.0.1): DataGrid RowHeight + single-line content cell (bug 2)`
- `docs(v3.0.1): CHANGELOG r3 — bug 1+2 fix, sha256, bug 3 deferred`
- `docs: r3 deploy session — ops_drift_log §3 + work_log entry`

**다음 세션용 현재 상태 (한 줄)**:
v3.0.1 r3 운영 배포 완료, sha256 `9471b1ec3ad4...`. Bug 1, 2 fix. Bug 3 (hub page) 잔존. drift 신규 0. 매장 r3 다운로드 + 영수증 검증 대기.
