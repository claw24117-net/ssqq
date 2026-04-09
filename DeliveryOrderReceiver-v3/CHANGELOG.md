# DeliveryOrderReceiver-v3 CHANGELOG

v3.0.1은 v2.0.4의 알려진 버그들을 **처음부터 fix한 상태**로 재구현한 별도 프로젝트 (C# .NET 8 + WPF).
기존 `ssqq/DeliveryOrderReceiver/` (v2.0.4 소스)는 한 줄도 건드리지 않음.

---

## v3.0.1 r2 — 2026-04-08 (현재 운영 배포)

### 빌드 메타

| 항목 | 값 |
|---|---|
| sha256 | `e95d58351e0e31ebba6390f0ff47caf7094e274d1949c245daeedc6518843895` |
| size | 71,808,727 bytes (~68.5 MB, single-file with IncludeNativeLibrariesForSelfExtract) |
| 빌드 시각 | 2026-04-09T02:52:54Z |
| 빌드 환경 | Parallels Win11 VM, .NET 8 SDK 8.0.419 (host: win-arm64, target: win-x64) |
| 빌드 명령 | `dotnet publish -c Release -r win-x64 --self-contained true` |
| csproj Version | 3.0.1 (`DeliveryOrderReceiver-v3.csproj`) |

### 변경 내역 (r1 대비)

- **fix**: `Services/UploadService.cs` — `platformId` / `platformStoreId` 를 `(string?)null` 에서 `"unknown"` 으로 하드코딩 변경 (v2.0.4 `ssqq/Services/UploadService.cs:40-41` 과 동일 우회). 서버 `agent-routes.js` 의 `requiredFields` 검증이 빈 문자열 아닌 string 을 요구하기 때문 (`typeof !== "string" || trim() === ""` 거부).

### 운영 배포 위치

| 경로 | 내용 |
|---|---|
| `bbb-prod-api-1:/app/storage/windows-agent/releases/3.0.1/DeliveryOrderReceiver-v3.0.1.exe` | 본 exe |
| `bbb-prod-api-1:/app/storage/windows-agent/releases/3.0.1/manifest.json` | `{channel:"beta", active:false, ...}` |

### 격리 정책

- `latest.json` 은 건드리지 않음 → 매장 PC 자동 업데이트는 **여전히 v2.0.4** 그대로
- `channel=beta, active=false` → `/api/windows-agent/latest` 엔드포인트에 안 노출
- `/management/devices/download` 페이지에는 별도 v3.0.1 (베타) 카드가 drift 상태로 추가되어 있음 (`ops_drift_log.md` 섹션 2 참조)

### 검증 (2026-04-09T03:14 UTC)

- `raw_receipts` 5건 insert (site_001, platform_id=unknown, port=COM15)
- `upload_jobs` 5건 `completed`
- 영수증 내용: 배민 세교동 T2BW0000AAEG, 쿠팡이츠 0029YW / 2H7RY9 등 — 실 매장 데이터

---

## v3.0.1 r1 — 2026-04-08 (r2로 대체됨, 흔적 없음)

### 빌드 메타

| 항목 | 값 |
|---|---|
| sha256 | `23f7345650fa84e4dfc508e167b56373babf5734565bb17153538f4518410613` |
| size | 71,808,718 bytes |
| 빌드 시각 | 2026-04-08 (세션 초반) |

### 알려진 버그 (r2에서 fix)

- `Services/UploadService.cs` — `platformId` / `platformStoreId` 를 `null` 로 송신
- 결과: 서버가 HTTP 400 반환 (`"platformId is required"`, `"platformStoreId is required"`)
- 매장에서 모든 receipt-raw 업로드 실패

### 상태

- 운영 서버에 r1 파일 **더 이상 존재하지 않음** (r2로 덮어써짐)
- 영속 볼륨에도 백업 없음
- 이 entry는 히스토리 기록용

---

## 처음부터 반영된 fix (v2.0.4 대비, r1/r2 공통)

v2.0.4 의 알려진 bug 중 v3 에 처음부터 fix 로 박힌 항목:

| ID | 내용 | v3 위치 |
|---|---|---|
| BUG-001 | LoginButton 에서 `_config.Token = ""` 절대 호출 안 함 | `Views/LoginView.xaml.cs` LoginButton_Click |
| BUG-004 | `data.userSessionToken` 경로로 토큰 추출 | `Services/AuthService.cs:55-69` (LoginAsync) |
| W-TIME | 모든 timestamp `DateTime.UtcNow.ToString("o")` (UTC) | `Models/OrderRecord.cs`, `Services/SerialReceiverService.cs` |
| W-RETRY | 재전송 실패 시 `lastError` 표시 | `Models/OrderRecord.cs:LastError` + `Views/MainView` |
| B-4 | `config.json` `token` DPAPI 암호화 | `Helpers/DpapiHelper.cs` + `Models/LoginConfig.cs:Token` |
| B-5 | `config.json` `password` DPAPI 암호화 | `Models/LoginConfig.cs:Password` (동일 메커니즘) |
| C-3 | 주문 JSON `FileShare.None` + tmp + atomic replace | `Helpers/FileLockHelper.cs` + `Services/OrderStorageService.cs` |
| C-4 | `config.json` 동일 잠금 | `Helpers/FileLockHelper.cs` + `Models/LoginConfig.cs:Save` |
| 품질 | 관리자 비밀번호 하드코딩 `"0000"` 제거, DPAPI 저장 | `Services/AdminAuthService.cs` + `Views/AdminPasswordDialog` |
| D-7 | 시작 시 `Pending` → `Failed` 변환 | `Services/OrderStorageService.cs:SweepStaleOnStartup` |
| 401 retry | UploadService 401 시 자동 재로그인 1회 | `Services/UploadService.cs:UploadAsync` |

## 미적용 / 잔존 이슈

- `MainView.LogoutRequested` 이벤트 미사용 → 빌드 경고 `CS0067` (기능 영향 없음)
- v2.0.4 평문 `config.json` → v3 DPAPI 자동 마이그레이션 **미구현**
  - 같은 폴더에 덮어쓰면 자동 재로그인 실패
  - 별도 폴더 설치 권장 (README.md 참조)
- 실기기 매장천사 연동 시험 → 매장 1곳에서 r2 동작 확인했으나 전면 확장 전 PM 검증 필요

## 관련 파일

- `ssqq/DeliveryOrderReceiver-v3/README.md` — 프로젝트 개요, 빌드 가이드
- `ssqq/DeliveryOrderReceiver-v3/scripts/README.md` — 매장 PC 다운로드 스크립트
- `ssqq/ops_drift_log.md` — 운영 배포 drift 상세 (page.js 직접 패치 등)
- `ssqq/work_log.md` — 세션 로그
