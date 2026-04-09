# 배달 주문 수신기 v3.0.1

## 의도

DOR(`ssqq/DeliveryOrderReceiver/`)의 v2.0.4 버그를 **처음부터 수정된 상태**로 재구현한 별도 프로젝트.

- **기존 v2.0.4 소스는 건드리지 않음** (분리 트래킹)
- **로컬 전용** — git 추가 안 함, 사용자가 테스트 후 직접 push
- **stack**: C# .NET 8 + WPF (`net8.0-windows`, `WinExe`, SelfContained, win-x64)
- **운영 배포 버전과 무관** — git 트래킹 라벨 v3.0.1

## 처음부터 반영된 버그 수정

| ID | 내용 | 위치 |
|---|---|---|
| BUG-001 | LoginButton에서 `_config.Token = ""` 절대 호출 안 함. autoLogin 플래그만 별도 설정 | `Views/LoginView.xaml.cs:LoginButton_Click` |
| BUG-004 | 로그인 응답에서 `data.userSessionToken` 경로로 토큰 추출 (data 객체 내부) | `Services/AuthService.cs:LoginAsync` |
| W-TIME | 모든 timestamp는 `DateTime.UtcNow.ToString("o")` (UTC ISO-8601) | `Services/SerialReceiverService.cs`, `Models/OrderRecord.cs` |
| W-RETRY | 재전송 실패 시 lastError 표시 | `Models/OrderRecord.cs:LastError`, `Views/MainView` |
| **B-4** | config.json `token` 평문 저장 금지 → DPAPI(CurrentUser scope) 암호화 | `Helpers/DpapiHelper.cs` + `Models/LoginConfig.cs:Token` |
| **B-5** | config.json `password` 평문 저장 금지 → DPAPI 동일 적용 | 동일 |
| **C-3** | 주문 JSON 동시 쓰기 잠금 — `FileShare.None` + tmp + atomic replace + 재시도 | `Helpers/FileLockHelper.cs` + `Services/OrderStorageService.cs` |
| **C-4** | config.json 동시 쓰기 잠금 — 동일 메커니즘 | `Helpers/FileLockHelper.cs` + `Models/LoginConfig.cs:Save` |
| 코드 품질 | 관리자 비밀번호 하드코딩 `"0000"` 제거. 최초 진입 시 사용자가 직접 설정 + DPAPI 저장 | `Services/AdminAuthService.cs`, `Views/AdminPasswordDialog` |
| D-7 | 시작 시 `Pending` → `Failed` 변환 | `Services/OrderStorageService.cs:SweepStaleOnStartup` |
| 401 자동 재로그인 | UploadService에서 401 시 1회 자동 재로그인 + 재시도 | `Services/UploadService.cs:UploadAsync` |

## 디렉토리 구조

```
DeliveryOrderReceiver-v3/
├── DeliveryOrderReceiver-v3.csproj   # net8.0-windows / WPF / SelfContained / win-x64
├── App.xaml(.cs)                      # 진입점, AppData 경로, crash logger
├── MainWindow.xaml(.cs)               # 호스트 + Login/Main/Settings 뷰 전환
├── .gitignore                         # bin/obj 제외
├── Models/
│   ├── LoginConfig.cs                 # config.json + DPAPI (B-4/B-5/C-4)
│   └── OrderRecord.cs                 # 주문 레코드 (W-TIME UTC)
├── Helpers/
│   ├── DpapiHelper.cs                 # ProtectedData.Protect/Unprotect
│   ├── FileLockHelper.cs              # FileShare.None + atomic replace + 재시도
│   └── EscPosParser.cs                # ESC/POS → 텍스트 (CP949), SHA256
├── Services/
│   ├── AuthService.cs                 # 로그인 (BUG-004, BUG-001)
│   ├── UploadService.cs               # receipt-raw + 401 재시도
│   ├── OrderStorageService.cs         # 날짜별 JSON + C-3 + 중복 감지 + D-7
│   ├── PortService.cs                 # com0com setupc wrapper
│   ├── SerialReceiverService.cs       # COM 수신 + 버퍼링 + flush
│   ├── AutoStartService.cs            # HKCU Run 등록
│   └── AdminAuthService.cs            # 관리자 비밀번호 (DPAPI, 하드코딩 제거)
└── Views/
    ├── LoginView.xaml(.cs)            # 로그인 패널 (BUG-001 fix 포함)
    ├── MainView.xaml(.cs)             # 메인 패널 (수신/업로드/재전송)
    ├── SettingsView.xaml(.cs)         # 관리자 설정 (포트 관리)
    └── AdminPasswordDialog.xaml(.cs)  # 관리자 비밀번호 입력/설정
```

## 빌드 방법 (Windows 호스트 필요)

WPF는 `net8.0-windows` TFM 필수 — **macOS에서는 빌드 불가**.

### Parallels Windows 11 VM에서

```powershell
# 사전: .NET 8 SDK 설치
# https://dotnet.microsoft.com/download/dotnet/8.0

cd ssqq\DeliveryOrderReceiver-v3
dotnet restore
dotnet build -c Release

# 단일 파일 self-contained 배포
dotnet publish -c Release -r win-x64 --self-contained true
# 결과: bin\Release\net8.0-windows\win-x64\publish\DeliveryOrderReceiver-v3.exe
```

### Mac mini에서 Parallels로 실행 (DOR과 동일 방식)

```bash
prlctl exec "Windows 11" powershell -Command \
  "cd C:\Users\min\Documents\claude-1\ssqq\DeliveryOrderReceiver-v3; dotnet publish -c Release -r win-x64 --self-contained true"
```

## 사전 의존성 (실행 환경)

- Windows 10/11 x64
- com0com v2.2.2.0 signed (가상 COM 포트 드라이버)
- .NET 8 Runtime (self-contained 배포 시 불필요)
- 매장천사 프린터 등록: LKT-20

## git 정책

- **이 폴더는 git에 추가하지 않음** (untracked 유지)
- 사용자가 로컬 테스트 (Parallels에서 빌드/실행) 후 직접 push
- 운영 직접 패치 / force push 절대 금지 (메모리 규칙)
- 기존 `DeliveryOrderReceiver/` (v2.0.4) 소스는 한 줄도 안 건드림

## 단일 진실 출처 (참조 전용)

- 서버 계약: `server/apps/api/src/routes/agent-routes.js` (login 145-240, receipt-raw 790-1060)
- 가상프린터 운영 경계: `server/docs/architecture/windows-agent-unified-plan-v1.md` (이 프로젝트는 가상프린터를 다루지 않음)

## 미적용/대기 항목

- 빌드/배포 실행 (PM 별도 지시 대기 — 메모리 규칙)
- 실기기 테스트 (재부팅 자동 시작, com0com 실제 동작, 매장천사 연동)
- release-manifest 엔트리 (PM 지시 대기)
