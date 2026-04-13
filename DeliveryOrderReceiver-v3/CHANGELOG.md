# 배달 주문 수신기 — 작업내역서

v2.0.4의 알려진 버그들을 **처음부터 fix한 상태**로 재구현한 별도 프로젝트 (C# .NET 8 + WPF).
기존 `ssqq/DeliveryOrderReceiver/` (v2.0.4 소스)는 한 줄도 건드리지 않음.

---

## v3.0.3 — 2026-04-13 (현재 운영 최신)

### 빌드 정보

| 항목 | 값 |
|---|---|
| sha256 | `82fe90709cf866cb7add278c1f1b905548888bf71c0dddd64094f2ab73bd92ff` |
| 크기 | 71,810,864 bytes (~68.5 MB) |
| 파일명 | `DeliveryOrderReceiver-v3.0.3.exe` |
| 빌드 환경 | Parallels Win11 VM, .NET 8 SDK 8.0.419 (win-arm64 → win-x64 cross-build) |
| 서버 위치 | `/app/storage/windows-agent/releases/3.0.3/` |
| 격리 | channel=beta, active=false, `latest.json`=v2.0.4 유지 |

### 변경 내역

**1. COM 포트 끊김 근본 fix — SerialPort 설정을 v2.0.4와 동일하게 복원**

v2.0.4는 같은 환경에서 끊김 없이 동작했는데 v3.0.1부터 끊겼음. 원인 분석 결과:

| 설정 | v2.0.4 (끊김 없음) | v3.0.1~3.0.2 (끊김 발생) | v3.0.3 (fix) |
|---|---|---|---|
| ReadTimeout | -1 (기본, 무한) | 1000ms | **-1 (복원)** |
| ReadBufferSize | 4096 (기본) | 8192 | **4096 (복원)** |
| ErrorReceived | 구독함 | **없음** | **구독 추가** |
| idle timeout | 500ms | 800ms | **500ms (복원)** |

- `ReadTimeout > 0` 설정 시 .NET SerialPort 내부 스레드 풀 동작이 달라져서 장시간 후 `DataReceived` 이벤트가 silent하게 멈추는 알려진 이슈
- `ErrorReceived` 미구독 시 하드웨어 에러(overrun, framing)가 내부에 쌓여 포트 이상 동작
- v3.0.2의 heartbeat 30초 자동 재연결은 그대로 유지 (이중 방어)

**2. 날짜별 조회 기능**

- 메인 화면에 DatePicker 추가 (KST 기준)
- 날짜 선택 시 해당 날짜 주문 파일 로드
- "총 N건" 건수 표시
- 이전 날짜 주문 조회 가능 (기존에는 오늘만 표시)

**3. 시간 내림차순 정렬**

- 최신 주문이 항상 맨 위
- 기존 `Seq` (순번) 기준 → `ReceivedAtUtc` (시간) 기준으로 변경

### 변경 파일

| 파일 | 변경 |
|---|---|
| `Services/SerialReceiverService.cs` | SerialPort 생성자 v2.0.4 방식, ErrorReceived 추가, idle 500ms |
| `Views/MainView.xaml` | DatePicker 추가, Grid row 추가 |
| `Views/MainView.xaml.cs` | DateSelector_Changed 핸들러, ReloadOrders() 날짜별 로드 + 시간 내림차순 |
| `DeliveryOrderReceiver-v3.csproj` | Version 3.0.3 |

---

## v3.0.2 — 2026-04-13

### 빌드 정보

| 항목 | 값 |
|---|---|
| sha256 | `e7f5a3ac8ad8a326e9cb5c34e53ce2c76bd19c0663e8e45a90e2c9e14a89caea` |
| 크기 | 71,809,844 bytes |
| 파일명 | `DeliveryOrderReceiver-v3.0.2.exe` |
| 서버 위치 | `/app/storage/windows-agent/releases/3.0.2/` |

### 변경 내역

**1. 시각 표시 UTC → KST**

- `Models/OrderRecord.cs` 에 `ReceivedAtKst` computed property 추가
- `TimeZoneInfo.ConvertTimeFromUtc` → "Korea Standard Time"
- `Views/MainView.xaml` 컬럼 바인딩 `ReceivedAt` → `ReceivedAtKst`, Header "시각 (UTC)" → "시각"
- 서버 전송은 `ReceivedAt` (UTC ISO-8601) 그대로 유지

**2. COM 포트 자동 재연결 (heartbeat)**

- `Services/SerialReceiverService.cs` 에 `_heartbeatTimer` 추가 (30초)
- `CheckAndReconnect()`: `_port.IsOpen` false 감지 시 자동 Stop→Start
- `_lastPort` / `_lastBaudRate` 별도 보관 (Stop 시 초기화 방지)
- `_reconnectLock` 으로 중복 재연결 방지
- 재연결 성공/실패를 화면에 표시

### 변경 파일

| 파일 | 변경 |
|---|---|
| `Models/OrderRecord.cs` | ReceivedAtKst property 추가 |
| `Views/MainView.xaml` | 시각 컬럼 바인딩 변경 |
| `Services/SerialReceiverService.cs` | heartbeat timer + CheckAndReconnect |
| `DeliveryOrderReceiver-v3.csproj` | Version 3.0.2 |

---

## v3.0.1 — 2026-04-08~10 (최초 배포, v3.0.3으로 대체됨)

v2.0.4를 완전히 새로 작성한 최초 버전. WinForms → WPF 전환.

### 빌드 이력

| 빌드 | sha256 | 크기 | 비고 |
|---|---|---|---|
| 최종 (2026-04-10) | `9471b1ec3ad4...` | 71,808,979 | 관리자 비밀번호 DI fix + DataGrid 행 높이 fix |
| 2차 (2026-04-08) | `e95d58351e0e...` | 71,808,727 | UploadService platformId "unknown" fix |
| 1차 (2026-04-08) | `23f7345650fa...` | 71,808,718 | 최초 빌드. platformId null 버그 (HTTP 400) |

### 변경 내역 (v2.0.4 → v3.0.1)

**관리자 비밀번호 fix (최종 빌드)**
- `AdminAuthService` 에 `LoginConfig` 생성자 주입
- stale instance race 해소 (2개 인스턴스가 같은 config.json 경쟁하던 문제)

**DataGrid 행 높이 fix (최종 빌드)**
- `RowHeight="28"` 고정
- 내용 컬럼을 `DataGridTemplateColumn` (한 줄 + ellipsis + ToolTip)

**UploadService fix (2차 빌드)**
- `platformId` / `platformStoreId` 를 `null` → `"unknown"` (서버 필수 string)

### 서버 위치

| 경로 | 내용 |
|---|---|
| `/app/storage/windows-agent/releases/3.0.1/` | 최종 빌드 exe + manifest |

---

## v2.0.4 대비 처음부터 반영된 fix (전 버전 공통)

| ID | 내용 | 위치 |
|---|---|---|
| BUG-001 | 로그인 후 토큰 삭제 코드 제거 | `Views/LoginView.xaml.cs` |
| BUG-004 | 서버 응답에서 `data.userSessionToken` 경로로 토큰 추출 | `Services/AuthService.cs:55-69` |
| B-4 | config.json `token` DPAPI 암호화 (평문 저장 금지) | `Helpers/DpapiHelper.cs` + `Models/LoginConfig.cs` |
| B-5 | config.json `password` DPAPI 암호화 | 동일 |
| C-3 | 주문 JSON 동시 쓰기 잠금 (`FileShare.None` + atomic replace) | `Helpers/FileLockHelper.cs` + `Services/OrderStorageService.cs` |
| C-4 | config.json 동시 쓰기 잠금 | 동일 |
| W-TIME | 모든 timestamp UTC ISO-8601 | `Models/OrderRecord.cs`, `Services/SerialReceiverService.cs` |
| W-RETRY | 재전송 실패 시 lastError 표시 | `Models/OrderRecord.cs` + `Views/MainView` |
| D-7 | 시작 시 미완료(Pending) → 실패(Failed) 자동 변환 | `Services/OrderStorageService.cs` |
| 401 retry | 서버 401 시 자동 재로그인 1회 시도 | `Services/UploadService.cs` |
| 관리자 PW | 하드코딩 `"0000"` 제거 → 사용자 직접 설정 (DPAPI 저장) | `Services/AdminAuthService.cs` |

---

## 기술 스택

| 항목 | v2.0.4 | v3.x |
|---|---|---|
| UI | WinForms | **WPF** |
| .NET | net8.0-windows | net8.0-windows |
| 빌드 | exe + DLL (154MB) | **단일 exe** (~68.5MB, 압축) |
| 보안 | 평문 config | **DPAPI 암호화** |
| 파일 쓰기 | 잠금 없음 | **FileShare.None + atomic** |
| COM 포트 | 기본 설정 | **기본 설정 + heartbeat 30초** |

---

## 잔존 이슈

| 항목 | 설명 | 처리 시기 |
|---|---|---|
| **서버 시간 표시** | `/orders/hub` 페이지가 `created_at` (재전송 시각) 표시. `captured_at` 으로 1줄 수정 필요. 클라이언트는 정확히 전송 중. | 서버 이미지 리빌드 시 |
| **platformId 자동 추출** | 배민/쿠팡/요기요 자동 인식 안 함 (전부 "unknown") | 다음 버전 |
| **DPAPI 마이그레이션** | v2.0.4 평문 config → v3 DPAPI 자동 변환 미구현. 별도 폴더 설치만 가능. | 전 매장 promotion 전 |
| **로그아웃 버튼** | 메인 화면에 없음. 설정 화면 안에만 있음. CS0067 경고. | 다음 버전 |
| **latest.json promotion** | 현재 v2.0.4. v3.0.3으로 갱신하면 전 매장 자동 업데이트. | PM 결정 대기 |

---

## 운영 현황 (2026-04-13 기준)

| 항목 | 값 |
|---|---|
| 운영 최신 버전 | **v3.0.3** (beta 격리) |
| 매장 자동 업데이트 | v2.0.4 (latest.json 미변경) |
| 운영 매장 수 | 2곳 (매장천사 1.0 + 2.0) |
| 누적 영수증 | 287건+ completed (2026-04-08~11, 배민/쿠팡이츠/요기요) |
| 다운로드 페이지 | https://zigso.kr/management/devices/download → v3.0.3 (베타) 카드 |
| 서버 releases | 2.0.4 / 3.0.1 / 3.0.2 / 3.0.3 (4개 폴더) |

---

## 버전 이력 요약

| 버전 | 날짜 | 주요 변경 |
|---|---|---|
| **v3.0.3** | 2026-04-13 | SerialPort v2.0.4 설정 복원 (끊김 fix) + 날짜별 조회 + 시간 내림차순 |
| **v3.0.2** | 2026-04-13 | KST 시각 표시 + COM 포트 heartbeat 자동 재연결 |
| **v3.0.1** | 2026-04-08~10 | WPF 재구현 + v2.0.4 버그 전부 fix + 관리자 PW DI fix + DataGrid fix |
| v2.0.4 | 2026-04-03 | 기존 WinForms 버전 (BUG-001 fix) — 현재 다른 매장 자동 업데이트 대상 |
