# 윈도우 프로그램 재분석 + 기획서 정합화

## Context

PM가 "윈도우 프로그램이 계속 문제가 많다"고 느끼는 이유는, **코드/배포 상태와 기획서 기록이 어긋나** 있기 때문이다. v2.0.4는 이미 빌드+서버 배포까지 끝났는데(work_log #115-117, 커밋 `77f4961`), `dev_progress.md`/`issue_matrix.md`는 곳곳이 옛 상태를 유지하고 있다. 동시에 두 개의 윈도우 클라이언트(매장천사 직결용 `DeliveryOrderReceiver`, zigso.kr 다운로드용 `windows-agent`)가 같은 서버 API를 공유하지만 둘의 경계가 어디 기획서에도 또렷하게 명시돼 있지 않다.

이번 작업의 결과물은 **새 기획서가 아니라**(PM 규칙: "기획서는 추가/수정/삭제로만 관리"), 기존 세 파일(`dev_progress.md`, `issue_matrix.md`, `work_log.md`)을 코드 진실 기준으로 다시 맞추는 것 + (a) 두 클라이언트 경계, (b) 가상프린터 운영 경계, (c) 서버 측 계약 authoritative 참조, (d) 현 개발/배포 방법 문서화 네 축을 `dev_progress.md`에 박는 것이다. **코드/빌드/배포/설정/서버는 일체 건드리지 않는다** (2026-04-03 PM 빌드 금지 지시 유지, 가상프린터 운영 보호, v2.0.4 서버 계약 호환성 검증 완료).

### 특히 주의할 세 가지 (탐색으로 재확인)

1. **가상프린터는 절대 건드리지 않는다** — 운영 매장에서 기존 실제 프린터와 종이 출력이 돌고 있다. `server/agents/windows-agent/CaptureAdapters.cs`의 `ReceiptPrinterCaptureAdapter`는 이미 MSI 기본 SourceType으로 들어가 있지만 **실제 드라이버/스풀 캡처/미러 재전달은 Phase 4 미구현**이다. `server/docs/architecture/windows-agent-unified-plan-v1.md:240-241`의 **금지 조항**(실제 프린터 제거 금지, 종이 출력 끊기는 구조 금지)이 이미 단일 진실 출처다. 본 작업은 이 영역 코드를 읽지도 수정하지도 인용문 외 새로 기획하지도 않는다.
2. **서버 계약은 이미 정합** — 탐색 결과 DOR v2.0.4 코드와 `server/apps/api/src/routes/agent-routes.js` + `server/apps/api/src/server.js` 사이에 **불일치 0건**. login 응답 `data.userSessionToken`/`data.sites[]`, receipt-raw dual auth(Bearer 우선 → user session fallback), `Idempotency-Key` 헤더, body 필드 전부 일치. 본 작업은 서버 코드를 변경하지 않고, 기획서에 authoritative 서버 파일 경로만 박는다.
3. **현 개발 방법은 기획서에 없다** — DOR은 빌드 스크립트 없이 `prlctl exec "Windows 11" powershell` + `dotnet publish`로 빌드하고 SSH로 `zigso.kr:/app/storage/windows-agent/releases/{version}/`에 직접 올린다. Windows-agent는 `scripts/publish-win-x64.ps1`+`scripts/build-msi.ps1`로 빌드한다. 서버는 `docker-build-strict.sh` → `upload-gate-strict.sh` → `docker-deploy-strict.sh` → `safe-deploy.sh`(auto-rollback) gate가 있고 `server/docs/release-manifest/`에 per-release `.md`+`.json` 쌍을 남긴다. DOR은 release-manifest 엔트리가 없다. 이 상태를 기획서에 있는 그대로 기록만 한다(개선은 PM 지시 대기).

## 코드 검증으로 드러난 사실 (드래프트와 차이)

| 항목 | 드래프트 주장 | 코드/git 실제 |
|------|--------------|--------------|
| 현재 버전 | v2.0.2 → v2.0.4 (코드 완성, 빌드 대기) | **v2.0.4 빌드+배포 완료** (커밋 `77f4961`, work_log #115-117) |
| BUG-001 (로그인 직후 토큰 삭제) | "코드 수정됐으나 빌드 미완" | **v2.0.3에서 수정+배포 완료**. `MainForm.cs:726-780` LoginButton_Click에 `_config.Token = ""` 라인 없음 |
| BUG-001 위치 | "MainForm.cs:479-482" | 해당 라인은 포트 UI 라벨 코드. 원래 버그는 750~753 라인이었고 이미 제거됨 |
| BUG-004 (토큰 추출) | 언급 없음 | **v2.0.4 핵심 수정**. `AuthService.cs:55-69`가 `data.data.userSessionToken` 경로로 변경됨 (커밋 `8f363c7`) |
| 시간대 버그 | "현재 코드가 KST를 Z로 기록 중" | **이미 수정됨**. `MainForm.cs:884`는 `DateTime.UtcNow.ToString("o")` (정상 UTC) — 옛 버그는 `DateTime.Now`였고 v2.0.3에서 W-TIME로 수정 |
| Password 평문 저장 | 미수정 | **확인됨, 진짜 미수정**. `LoginConfig.cs:20`에 `string Password`, `Save()`는 단순 `JsonSerializer.Serialize(this)` |
| 로그아웃 시 `_config.Token = ""` (`MainForm.cs:1456`) | 언급 없음 | 정상 동작 (LogoutButton_Click 내부) |
| `MainForm.cs` 줄 수 | 언급 없음 | 1671 줄 |

## 추가로 발견된 정합성 깨짐

1. **`issue_matrix.md` 자기모순**: 332행은 BUG-001을 "✅ v2.0.3 수정"으로 처리했지만, 같은 파일 407행 C-1(`MainForm.cs:750-753`)은 여전히 `[ ]` 미완 표기.
2. **`dev_progress.md:382` 표기 혼선**: "[x] BUG-001: ... if 블록 제거 (MainForm.cs:750-753)"로 수정 완료 표시는 됐지만, 같은 파일 425행은 다시 "v2.0.3에서 수정 완료 — 빌드 + 서버 배포 완료"라고 적혀 있어 BUG-001 항목이 파일 내에서 상태 라벨 두 번 등장 → 한 번만 정리 필요.
3. **PM 빌드 금지 vs 실제 배포 이력**: `dev_progress.md:595`는 "2026-04-03 PM 빌드 금지" 지시를 명시하지만, work_log #115-117은 같은 날 v2.0.4를 빌드+배포 완료. 이 모순을 기획서가 명시적으로 해소해야 한다(예: "금지는 v2.0.4 배포 이후로 발효" 또는 "v2.0.4는 PM 직접 승인" 중 사실 확인 필요).
4. **두 클라이언트 경계 부재**: `dev_progress.md`는 DeliveryOrderReceiver 한쪽만 다루고, `windows-agent`는 `server/docs/architecture/windows-agent-unified-plan-v1.md`에 분리. 두 문서를 잇는 한 줄짜리 경계 정의가 없음.
5. **실제 미수정 항목**: B-4(token 평문), B-5(password 평문), C-3/C-4(JSON 파일 동시 쓰기 잠금 없음). 모두 issue_matrix.md에 박혀 있지만 dev_progress.md "미해결" 섹션에는 빠져 있음.

## 작업 흐름 (다이어그램)

```
 [read-only 진실 출처들]                                     [편집 대상 = 문서 3건만]
 ─────────────────────────                                   ───────────────────────
  git log + dev_progress/issue_matrix/work_log (v2.0.4 배포)
  DOR 코드 (MainForm/AuthService/LoginConfig/OrderStorage)
  windows-agent 코드 (CaptureAdapters/AgentRuntime/Setup)  ┐
  가상프린터 단일진실: unified-plan-v1.md §8-3/§8-6/§8-7    ├── 인용만, 수정 X
  서버 계약: agent-routes.js / server.js / contracts.js    │
  DB 계약: postgres-store.js                                │
  빌드/배포: docker-build-strict.sh / safe-deploy.sh /      │
              release-manifest/                             ┘
              │
              │  (read-only 비교 → 차이 추출)
              ▼
  ┌──────────────────────────────────────────────────────────┐
  │ claude-1/dev_progress.md                                 │
  │   • 7행 상태 줄 재작성                                    │
  │   • 372~451행 "알려진 문제점" 라벨 단일화 (한 항목 = 한   │
  │     상태) — BUG-001/002/003 미해결 섹션에서 제거          │
  │   • "해결됨 (v2.0.4)" 소제목 + BUG-004/W-RETRY 추가       │
  │   • "미해결 — 보안 / 데이터 무결성 / PM 미테스트" 3분할  │
  │   • "고려사항" 577행 부근에 다음 4개 항목 끼워넣기:       │
  │       (a) 두 클라이언트 경계                              │
  │       (b) 가상프린터 off-limits (인용만)                  │
  │       (c) 서버 계약 authoritative 파일 목록               │
  │       (d) 현 개발 방법 (빌드 호스트/배포 경로/gate/       │
  │           release-manifest 상태) — 기록만, 개선 X         │
  │   • 595행 빌드 금지 줄 모순 해소                          │
  │   • 변경 이력 1줄                                         │
  │                                                          │
  │ claude-1/issue_matrix.md                                 │
  │   • 407 C-1, 408 C-2, 411 C-5 상태 보정                  │
  │   • 413 PM 지시 줄 수정                                  │
  │   • 718 미해결 표에서 옛 BUG-001 행 제거, B-4/B-5/       │
  │     C-3/C-4 행 추가                                       │
  │                                                          │
  │ claude-1/work_log.md                                     │
  │   • 2026-04-08 정합화 분석 블록 1개 추가                 │
  └──────────────────────────────────────────────────────────┘
              │
              ▼
    PR 생성 (코드/서버/가상프린터/빌드 스크립트/manifest 변경 0)
    — `git diff --stat` 결과는 세 문서에만 나타나야 함
```

## 갱신 대상 파일과 변경점

> 모든 경로는 이 워크트리 기준 (`/home/user/repo/...`).
> 코드는 한 줄도 손대지 않는다 — `claude-1/DeliveryOrderReceiver/`, `server/agents/windows-agent/` 모두 read-only.

### 1. `claude-1/dev_progress.md`

**(a) 7행 — 상태 줄 정리**
- 현재: `상태: v2.0.4 배포 완료, 기존 서버 업그레이드 진행 중`
- 변경: `상태: v2.0.4 배포 완료. 잔여 미테스트(P2) + 보안 잔여(B-4/B-5) + 동시쓰기(C-3/C-4) 처리 PM 지시 대기`

**(b) 372~451행 "알려진 문제점" 섹션 정리** — 한 항목 한 상태 원칙
- "해결됨 (v2.0.3)" + "해결됨 (v2.0.4)" 두 소제목으로 분리해서 각 항목의 최종 라벨이 한 곳에만 나오도록 한다.
- BUG-001(405~425행): "미해결 — 긴급 버그" 하위에서 빼고 "해결됨 (v2.0.3)" 표로 옮긴다. 한 줄 요약: `[x] BUG-001: 자동 로그인 미체크 시 토큰 삭제 if 블록 제거 (MainForm.cs LoginButton_Click)`. 상세 코드 블록은 **삭제** — issue_matrix C-1이 단일 진실 출처가 된다.
- BUG-002/BUG-003: 동일 처리. issue_matrix.md 332~334행이 이미 "BUG-004 수정으로 해결" / "재전송 버튼 검증 완료"로 정리한 것과 일치시킨다.
- BUG-004 신규 항목 추가(`### 해결됨 (v2.0.4)` 표 안):
  ```
  - [x] BUG-004: AuthService.cs 토큰 추출 필드명 수정 (data.userSessionToken, data 객체 내부 추출)
  - [x] W-RETRY: 재전송 실패 시 lastError 표시
  ```

**(c) 미해결 신규 섹션 추가** — 372행 "미해결 — 긴급 버그"를 다음 3개로 재구성
```
### 미해결 — 보안 (PM 빌드 지시 대기)
- [ ] B-4: config.json `token` 평문 저장 (LoginConfig.cs)
       방향: Windows DPAPI(`ProtectedData.Protect`, scope=CurrentUser)
- [ ] B-5: config.json `password` 평문 저장 (LoginConfig.cs:20, Save():46-63)
       방향: 동일. 단일 사용자 PC 한정 복호화 — 매장 공용 PC 시나리오 적합

### 미해결 — 데이터 무결성 (PM 빌드 지시 대기)
- [ ] C-3: 주문 JSON 동시 쓰기 잠금 없음
       위치: OrderStorageService.cs Save():52-76, UpdateStatus():81-101
       현재: File.WriteAllText + File.Replace, FileShare 미설정
- [ ] C-4: config.json 동시 쓰기 잠금 없음 (LoginConfig.cs Save())

### 미해결 — PM 미테스트 (실기기 필요)
- [ ] 재부팅 자동 시작 (W-701~702, W-1101~1105)
- [ ] 자동 재로그인 + 만료 토큰 흐름 (W-104, W-1205)
- [ ] 포트 관리/삭제 실기기 (W-203,208,301~305)
- [ ] v2.0.1 신규 기능 실사용 (W-1201~1207)
```

**(d) "고려사항" 577행 부근에 네 개 항목을 기존 글머리표 안에 끼워 넣는다** (새 섹션 아님)

(d-1) 두 클라이언트 경계:
```
- **두 클라이언트 경계 (단일 진실 출처)**:
  - DeliveryOrderReceiver (`claude-1/DeliveryOrderReceiver/`):
      C# .NET 8 WinForms (`net8.0-windows`, `PublishSingleFile`+`SelfContained` win-x64,
      DeliveryOrderReceiver.csproj 기준), com0com 가상 COM 직결, 매장천사 운영 매장 전용,
      단일 창 GUI, 매장 운영자 PC 1대 1 설치
  - Windows Agent (`server/agents/windows-agent/`):
      C# .NET 백그라운드 서비스(`WindowsAgentBackgroundService`), MSI 배포,
      file_watch/serial/receipt_printer 3축 (CaptureAdapters.cs),
      zigso.kr 다운로드 + site_owner/org_operator 권한
      (`/management/devices/download`, unified-plan-v1.md §8-12)
  - 공유 서버 API: `POST /v2/agent/auth/login`, `POST /v2/agent/uploads/receipt-raw`
    (자세한 파일/라인은 아래 (d-3) 서버 계약 항목 참조)
  - 합치지 않는다. windows-agent 전체 기획은
    `server/docs/architecture/windows-agent-unified-plan-v1.md` 참조.
```

(d-2) 가상프린터 off-limits — **이 플랜의 핵심 보호 경계**:
```
- **가상프린터 운영 경계 (절대 건드리지 않음, 인용만)**:
  - 구현 상태: `ReceiptPrinterCaptureAdapter` (server/agents/windows-agent/CaptureAdapters.cs)
    가 MSI에 실려 있고 `AgentSettings.CaptureSettings.SourceType` 기본값이
    `"receipt_printer"` (AgentSettings.cs)이며 `AgentRuntime` fallback이
    여기로 라우팅된다. 그러나 **실제 Windows 가상 프린터 드라이버 설치/스풀 캡처/
    실제 프린터 재전달은 Phase 4 미구현**이다
    (unified-plan-v1.md §8-3 "현재 상태 메모" 245-246행).
  - 운영 금지 (unified-plan-v1.md §8-3 236-241행, §8-7 318-321행/340-344행):
      · 실제 프린터 제거 금지
      · 기존 프린터 포트 임의 변경 금지
      · 종이 출력이 끊기는 단일 가상 프린터 강제 전환 금지
      · 운영 중 POS의 COM19 사전 검증 없이 변경 금지
      · 실매장에서 즉시 엔진 서비스 자동등록 금지
  - 이 기획서는 가상프린터 관련 코드/설정/UX/배포를 다루지 않는다.
    단일 진실 출처는 `windows-agent-unified-plan-v1.md` §8-1~§8-11이며,
    모든 업데이트는 거기서만 이루어진다.
```

(d-3) 서버 측 계약 (authoritative 참조, 수정 X):
```
- **서버 측 계약 정합성 (2026-04-08 탐색 확인, 수정 불필요)**:
  v2.0.4 DeliveryOrderReceiver ↔ 서버는 **완전 호환**. 서버 코드 변경 필요 없음.
  아래 파일들은 참조 전용 (단일 진실 출처):
  - `server/apps/api/src/routes/agent-routes.js`
      · POST /v2/agent/auth/login (145-240행) — 응답: `data.userSessionToken`,
        `data.user`, `data.organizations`, `data.sites[]` (86-96행 buildAuthSessionPayload)
      · POST /v2/agent/devices/register (455-613행) — Bearer 필수
      · POST /v2/agent/uploads/receipt-raw (790-1060행) — dual auth:
        1) `requireDeviceApiKeyFromRequest` (우선)
        2) 실패 시 `requireUserSessionFromRequest` + 가상 디바이스 자동생성(812-872행)
        필수: `Authorization: Bearer {token}`, `Idempotency-Key: {hash}` (894-910행),
        body: eventId/siteId/platformId/platformStoreId/capturedAt/rawChecksum/
             decodedText/port (911-931행)
      · POST /v2/agent/heartbeat (714-788행), GET /v2/agent/bootstrap (615-712행)
      · 로그인 rate limit (32-44행, 5분간 5회/30초 잠금)
  - `server/apps/api/src/server.js`
      · requireUserSessionFromRequest (578-605행)
      · requireDeviceApiKeyFromRequest (607-640행)
      · validateLoginBody (324-346행)
      · bodyLimit 1MB (FastifyAdapter)
  - `server/packages/contracts/src/contracts.js`
      · wrapDataEnvelope (157-164행) — `{data, meta:{requestId}}` 래핑
  - `server/packages/db/src/postgres-store.js`
      · recordAuditLog / listManagementAuditLogs (8858-8960행)
      · scrypt 해싱 (2606-2809행)
      · updateUploadJobStatus (9286-9293행)
  - DOR 측 매칭 코드 (이미 호환):
      · Services/AuthService.cs:55-69 — `data.userSessionToken` 경로로 이미 수정(BUG-004)
      · Services/UploadService.cs — Bearer + Idempotency-Key로 이미 송신
  - 기획서에 이 목록을 박는 이유: 향후 DPAPI(B-4/B-5) 또는 파일 잠금(C-3/C-4)을
    구현할 때 이 요청/응답 shape을 깨지 않도록 reviewer가 한눈에 확인 가능해야 함.
```

(d-4) 현 개발 방법 (있는 그대로 기록만, 개선 X):
```
- **현 개발/배포 방법 (2026-04-08 기준, PM 지시 전까지 변경 금지)**:
  - DeliveryOrderReceiver 빌드:
      · 빌드 호스트: Mac mini → Parallels Windows 11 VM (`prlctl exec "Windows 11" powershell`)
      · 빌드 명령: `dotnet publish -c Release -r win-x64 --self-contained true`
      · csproj: net8.0-windows / WinExe / PublishSingleFile=true / SelfContained=true
      · 산출물: self-contained exe 약 154MB
      · **빌드 스크립트 없음** (windows-agent와 달리 .ps1 미존재)
      · **release-manifest 엔트리 없음** (server/docs/release-manifest/ 체계 미연결)
      · 배포: SSH `min@zigso.kr` → `/app/storage/windows-agent/releases/{version}/`
              + `manifest.json`, `latest.json` 수동 갱신
  - Windows Agent 빌드 (참고, 본 작업 대상 아님):
      · `server/agents/windows-agent/scripts/publish-win-x64.ps1`
      · `server/agents/windows-agent/scripts/build-msi.ps1` (WiX v4+)
      · `server/agents/windows-agent/installer/Product.wxs`
  - 서버 빌드/배포 (참고, 본 작업 대상 아님):
      · `server/scripts/docker-build-strict.sh` (semver 강제, latest 금지, 룰 체크)
      · `server/scripts/docker-deploy-strict.sh` (version_gt, 건강성 180초, api-config 확인)
      · `server/scripts/upload-gate-strict.sh` (폐쇄형 검증 게이트)
      · `server/scripts/safe-deploy.sh` (docker cp + `node --check` + auto rollback from backup)
      · `server/docs/release-manifest/` (per-release `{TAG}.md` + `{TAG}.json`,
         README에 필수 필드 명시)
  - PM 검증 루프: 배포 후 PM이 실기기에서 수동 테스트 → work_log.md에 결과 기록
  - 현재 금지/대기: v2.0.4 배포(2026-04-03) 이후 신규 빌드·배포·가상프린터 구현
    모두 PM 별도 지시 대기. 본 작업은 문서만 갱신.
```

**(e) 595행 빌드 금지 줄 보강** — 모순 해소
- 현재: `**윈도우 프로그램 빌드/배포 금지**: PM 별도 지시 있기 전까지 ... (2026-04-03 PM 지시)`
- 변경: `**윈도우 프로그램 빌드/배포 금지**: v2.0.4 배포(2026-04-03) 이후로 PM 별도 지시 있기 전까지 빌드/배포 금지. v2.0.4가 운영 버전.`

**(f) "변경 이력" 표 619행 다음에 1줄 추가**
```
| 2026-04-08 | v2.0.4 정합화 — 기획서/이슈매트릭스 상태 라벨 단일화, 두 클라이언트 경계 명시 | 에이전트 |
```

### 2. `claude-1/issue_matrix.md`

**(a) 407행 C-1 상태 보정** — 자기모순 해소
- 현재: `| C-1 | BUG-001 토큰 삭제 버그 | ...삭제 | MainForm.cs:750-753 | [ ] |`
- 변경: `| C-1 | BUG-001 토큰 삭제 버그 | ...삭제 | MainForm.cs LoginButton_Click | [x] v2.0.3 수정+배포 |`

**(b) 408행 C-2 보정** — issue_matrix 333행이 이미 BUG-002 해결로 처리했으므로 일관화
- 변경: `| C-2 | 로컬→서버 재전송 테스트 | ... | MainForm.cs RetryUploadBtn_Click | [x] BUG-004 수정 후 동작 |`

**(c) 411행 C-5 보정**
- 변경: `| C-5 | v2.0.3 빌드+배포 | ... | [x] v2.0.3 + v2.0.4 빌드+배포 완료 |`

**(d) 413행 PM 지시 줄 수정**
- 현재: `**⚠ PM 지시: 윈도우 프로그램 빌드/배포 금지. PM 별도 지시 시 진행.**`
- 변경: `**⚠ PM 지시: v2.0.4 배포 이후 신규 빌드/배포 금지. C-3/C-4(파일 잠금) + B-4/B-5(DPAPI)는 PM 지시 시 진행.**`

**(e) 718행 미해결 표 — 윈도우 항목 갱신**
- 82번 행 `BUG-001 미수정 — 윈도우 서버 업로드 0건` → 삭제 (이미 fixed). 그 자리에 다음 4건 추가:
  ```
  | 82  | B-4 token 평문 저장        | 보안 | DPAPI 적용 | [ ] |
  | 82a | B-5 password 평문 저장      | 보안 | DPAPI 적용 | [ ] |
  | 82b | C-3 주문 JSON 동시쓰기 잠금 | 데이터 | FileShare.None | [ ] |
  | 82c | C-4 config.json 동시쓰기 잠금| 데이터 | FileShare.None | [ ] |
  ```
  (행 번호 체계는 원본 표가 사용하는 방식에 맞춘다 — 현재는 단순 일련번호이므로 본 작업에서는 기존 82번을 위 항목으로 교체)

### 3. `claude-1/work_log.md`

마지막 항목 다음에 1개 블록 추가:
```
### 2026-04-08
N. v2.0.4 정합화 분석:
  - 코드 현 상태 검증 (MainForm.cs 1671줄, AuthService.cs, LoginConfig.cs, OrderStorageService.cs)
  - BUG-001/002/003/004/W-TIME/W-RETRY 모두 v2.0.3~v2.0.4에서 처리 완료 확인
  - 진짜 미수정: B-4/B-5(DPAPI), C-3/C-4(파일 잠금), PM 미테스트 (재부팅/자동재로그인/포트관리)
  - dev_progress.md 상태 라벨 단일화 (BUG-001 두 번 표기 → 한 번)
  - issue_matrix.md C-1/C-2/C-5 보정 (332~336행 요약과 405~411행 표 일관화)
  - 두 클라이언트 경계 (DeliveryOrderReceiver vs windows-agent) dev_progress.md 고려사항에 한 항목 추가
  - 코드/빌드/배포 변경 0건 (PM 지시 대기 유지)
```

### 4. 손대지 않는 파일 (off-limits)

**코드 — 한 줄도 안 건드림**
- `claude-1/DeliveryOrderReceiver/**`
- `server/agents/windows-agent/**` (특히 `CaptureAdapters.cs`, `AgentRuntime.cs`, `WindowsAgentSetupRunner.cs`, `AgentSettings.cs` — 가상프린터 SourceType 기본값/fallback 경로 건드리지 않음)
- `server/apps/api/src/**` (routes/agent-routes.js, server.js 등 서버 계약 — 호환성 검증 완료)
- `server/packages/**` (contracts, db, modules)
- `server/agents/pc-uploader/**` (계약 검증용 Node 브리지 — unified-plan §1-1 근거)

**빌드/배포 스크립트 — 한 줄도 안 건드림**
- `server/scripts/docker-build-strict.sh`, `docker-deploy-strict.sh`, `safe-deploy.sh`, `upload-gate-strict.sh`
- `server/Dockerfile`, `docker-compose.*.yml`
- `server/agents/windows-agent/scripts/*.ps1`
- `server/agents/windows-agent/installer/Product.wxs`

**가상프린터 단일 진실 문서 — 읽기만, 수정/중복 서술 금지**
- `server/docs/architecture/windows-agent-unified-plan-v1.md` (§8-1 ~ §8-11 — 특히 §8-3 236-241행, §8-7 318-321/340-344행, §8-6 Phase 4 289-298행)
- `server/docs/architecture/windows-agent-input-contract-v1.md` (§2-1 54행 — `receipt_printer` 3축 계약)
- `server/docs/architecture/windows-agent-installer-product-spec-v1.md`
- `server/docs/architecture/windows-agent-installer-screen-spec-v1.md`
- `server/docs/architecture/windows-agent-installer-ux-team-plan-v1.md`
- `server/docs/architecture/windows-agent-server-web-exposure-spec-v1.md`
- `server/docs/architecture/windows-agent-web-download-plan-v1.md`

이유: 이 문서들은 이미 단일 진실 출처다. `dev_progress.md`는 인용/포인터만 남기고 내용을 복제하지 않는다 — 복제는 다음 번에 또 정합성이 깨지는 원인이 된다. 드래프트의 "windows-agent-unified-plan-v1.md Phase 진행 상황 메모 갱신" 항목은 **삭제**한다 (변경 사유 없음, 갱신 = 노이즈).

**Release manifest — 이번에 새로 만들지 않음**
- `server/docs/release-manifest/` 디렉토리에 DOR v2.0.4 엔트리를 새로 만들지 **않는다**. 현재 체계는 서버 이미지 중심이며, DOR 엔트리 생성은 release-manifest README 필수 필드(commit_sha, image_digest 등) 해석을 새로 정의해야 하는 별도 트랙. PM 지시 없이 만들면 기존 체계 정합성을 깬다.

## 검증 방법

1. **편집 후 재검증** — 편집한 세 파일 각각에 대해 Read로 변경 부분(±5줄)을 다시 읽어 라벨 단일화 확인.

2. **상호 참조 grep (정합성)**:
   ```
   Grep "BUG-001"     in claude-1/dev_progress.md   → "[x]" 표기만, "[ ]" 없어야 함
   Grep "BUG-001"     in claude-1/issue_matrix.md   → C-1 [x], 332행 ✅, 모순 없음
   Grep "DateTime\."  in DeliveryOrderReceiver/Forms/MainForm.cs → "DateTime.UtcNow" 한 건만
                                                                   (수정 전후 동일 — 확인용)
   Grep "B-4\|B-5\|C-3\|C-4" in claude-1/dev_progress.md → "미해결 — 보안 / 데이터 무결성" 섹션
   Grep "receipt_printer\|ReceiptPrinterCaptureAdapter\|가상프린터" in claude-1/dev_progress.md
          → "가상프린터 운영 경계" 항목 1곳만, 규칙 복제 없음
   Grep "agent-routes\.js\|requireUserSessionFromRequest" in claude-1/dev_progress.md
          → "서버 측 계약 정합성" 항목에 등장
   Grep "prlctl\|publish-win-x64\|release-manifest" in claude-1/dev_progress.md
          → "현 개발/배포 방법" 항목에 등장
   ```

3. **off-limits 영역 변경 0 검증** — 모든 코드/스크립트/가상프린터 문서가 0줄 변경인지 확인:
   ```
   git diff --stat -- \
     claude-1/DeliveryOrderReceiver/ \
     server/agents/ \
     server/apps/ \
     server/packages/ \
     server/scripts/ \
     server/Dockerfile \
     server/docker-compose.*.yml \
     server/docs/architecture/windows-agent-*.md \
     server/docs/release-manifest/
   ```
   결과가 **완전히 비어 있어야** 함. 한 줄이라도 나오면 중단하고 복원.

4. **편집 영역 확인** — 변경이 정확히 세 파일에만 있는지:
   ```
   git diff --stat
   ```
   결과에 `claude-1/dev_progress.md`, `claude-1/issue_matrix.md`, `claude-1/work_log.md` 세 줄만 나와야 함.

5. **PR 생성** — 제목: `docs: v2.0.4 정합화 — 상태 단일화 + 두 클라이언트/가상프린터/서버계약/개발방법 경계 명시`. 본문 체크리스트:
   - [ ] 코드 변경 없음 (DOR, windows-agent, 서버 apps/packages, 스크립트, Dockerfile)
   - [ ] 가상프린터 관련 코드/문서 0 변경 (운영 프린터 보호)
   - [ ] 서버 계약 파일 0 변경 (호환성 검증 완료)
   - [ ] 빌드 스크립트/릴리즈 매니페스트 0 변경
   - [ ] `dev_progress.md` BUG-001/002/003/004 상태 라벨 중복 제거
   - [ ] `issue_matrix.md` C-1/C-2/C-5 보정, 332~336 요약과 C섹션 일관
   - [ ] `dev_progress.md` 고려사항에 (a)~(d) 네 경계 항목 추가
   - [ ] `work_log.md` 2026-04-08 분석 블록 1건
   - [ ] PR 본문에 "코드/서버/가상프린터 0 변경, 문서만" 한 줄 명시

## 핵심 파일 경로 (참조용)

**편집 대상 (3건)**
- `/home/user/repo/claude-1/dev_progress.md`
- `/home/user/repo/claude-1/issue_matrix.md`
- `/home/user/repo/claude-1/work_log.md`

**DOR 코드 참조 (read-only)**
- `/home/user/repo/claude-1/DeliveryOrderReceiver/Forms/MainForm.cs` (1671줄)
- `/home/user/repo/claude-1/DeliveryOrderReceiver/Models/LoginConfig.cs`
- `/home/user/repo/claude-1/DeliveryOrderReceiver/Services/AuthService.cs`
- `/home/user/repo/claude-1/DeliveryOrderReceiver/Services/UploadService.cs`
- `/home/user/repo/claude-1/DeliveryOrderReceiver/Services/OrderStorageService.cs`
- `/home/user/repo/claude-1/DeliveryOrderReceiver/DeliveryOrderReceiver.csproj`

**가상프린터 단일 진실 (read-only, 인용만)**
- `/home/user/repo/server/docs/architecture/windows-agent-unified-plan-v1.md` §8-3(236-246행), §8-6(289-298행), §8-7(318-344행)
- `/home/user/repo/server/docs/architecture/windows-agent-input-contract-v1.md` §2-1(54행)

**서버 계약 참조 (read-only, 호환성 검증 완료)**
- `/home/user/repo/server/apps/api/src/routes/agent-routes.js` (145-240 login, 455-613 device register, 790-1060 receipt-raw, 32-44 rate-limit)
- `/home/user/repo/server/apps/api/src/server.js` (324-346 validateLoginBody, 578-605 user session, 607-640 device api key)
- `/home/user/repo/server/packages/contracts/src/contracts.js` (157-164 wrapDataEnvelope)
- `/home/user/repo/server/packages/db/src/postgres-store.js` (2606-2809 scrypt, 8858-8960 audit, 9286-9293 upload job status)

**빌드/배포 참조 (read-only)**
- `/home/user/repo/server/agents/windows-agent/scripts/publish-win-x64.ps1`
- `/home/user/repo/server/agents/windows-agent/scripts/build-msi.ps1`
- `/home/user/repo/server/agents/windows-agent/installer/Product.wxs`
- `/home/user/repo/server/scripts/docker-build-strict.sh`
- `/home/user/repo/server/scripts/docker-deploy-strict.sh`
- `/home/user/repo/server/scripts/safe-deploy.sh`
- `/home/user/repo/server/scripts/upload-gate-strict.sh`
- `/home/user/repo/server/docs/release-manifest/README.md`

**Git**
- 레포: `git@github.com:claw24117-net/ssa.git`
- 브랜치: `main`
- 시작 커밋: `6a2b0e1` (docs: 페이지 정상화 Phase 3~5 완료 + issue_matrix 재집계)
- 관련 과거 커밋: `77f4961` (v2.0.4 빌드+배포), `8f363c7` (v2.0.4 BUG-004 수정)

## 작업 범위 밖 (명시적 제외)

다음은 본 작업에서 **절대로 하지 않는다**. 필요한 것이 있다면 PM 별도 지시를 받는다.

**코드/빌드/배포**
- 윈도우 코드 수정 (DOR, windows-agent 모두) — 2026-04-03 PM 빌드 금지 유지
- 윈도우 빌드 실행 (`dotnet publish`, `publish-win-x64.ps1`, `build-msi.ps1`)
- 윈도우 배포 (`zigso.kr:/app/storage/windows-agent/releases/`, `manifest.json`, `latest.json` 갱신)
- DPAPI(B-4/B-5) / FileShare(C-3/C-4) 실제 구현 — 별도 트랙, PM 지시 시
- 서버 코드 수정 (agent-routes.js, server.js, contracts.js, postgres-store.js) — 호환성 검증 완료, 변경 사유 없음
- 서버 이미지 빌드/배포 (docker-build-strict.sh, docker-deploy-strict.sh)
- safe-deploy.sh 실행

**가상프린터 (운영 보호)**
- `ReceiptPrinterCaptureAdapter` 수정
- `AgentSettings.CaptureSettings.SourceType` 기본값 변경
- `AgentRuntime` fallback 경로 수정
- `WindowsAgentSetupRunner` 설정 UI 변경
- 실제 Windows 가상 프린터 드라이버 설치/스풀 캡처/미러 재전달 구현
- 운영 매장에서 실제 프린터/포트 변경 시도
- `unified-plan-v1.md` §8-* 내용 복제·재작성
- Phase 4 일정 당기기

**기획서 체계**
- 신규 기획서 파일 생성 (PM 규칙: "추가/수정/삭제로만 관리")
- `windows-agent-unified-plan-v1.md` "Phase 진행 상황 메모" 갱신 (드래프트에 있던 항목 — 변경 사유 없음)
- `server/docs/release-manifest/`에 DOR 엔트리 신규 작성 (별도 트랙)
- Windows Agent 설치 UX 재설계 (별도 트랙, `windows-agent-installer-*.md`에서 관리)

**테스트**
- 실기기 테스트 (재부팅 자동 시작, 자동 재로그인, 포트 관리) — PM 윈도우 접근 시
- 서버 curl 재테스트 — v2.0.4 배포 후 이미 완료 (work_log 84행 "curl 테스트 3건 성공")
