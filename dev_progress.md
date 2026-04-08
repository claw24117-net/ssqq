# 배달 주문 수신기 - 개발 진행서 (v2.0)

## 프로젝트 개요
- **프로젝트명**: 배달 주문 수신기
- **플랫폼**: Windows 전용 (데스크톱 앱)
- **현재 버전**: v3.0.1 (git 트래킹 라벨; csproj `Version`은 2.0.4 유지, 운영 배포 버전과 별도)
- **상태**: v3.0.1 git 코드 완성 (BUG-001/004, W-TIME, W-RETRY 수정 반영). 운영 배포 상태는 git과 분리 트래킹. 잔여 미테스트(P2) + 보안(B-4/B-5) + 동시쓰기(C-3/C-4) PM 지시 대기

---

## 컨셉
매장천사에 가상 COM포트를 프린터(LKT-20)로 추가 등록하여 주문 데이터를 수신. 수신한 ESC/POS 데이터에서 텍스트 추출 후 JSON 형식으로 서버에 업로드.

> **운영 구조**:
> ```
> 배달앱(배민/쿠팡/요기요)
>   → COM12 → COM11 → 매장천사
>     → 물리프린터 1 (COM10, 코벤)
>     → 물리프린터 2
>     → 우리 가상포트 (COM{A}, LKT-20으로 등록)
>       → COM{B} (com0com 쌍)
>       → 우리 프로그램이 COM{B}에서 수신
>       → 서버 업로드
> ```

> **핵심 원칙**:
> - 매장천사 기존 동작 절대 안 건드림
> - 배달앱 설정 변경 불필요
> - 우리 포트가 먼저 거치지 않음
> - 매장천사한테 받는 방향

---

## 인증 구조 (분석 + 기획)

### 현재 서버 인증 구조 (분석 결과)
```
1. 유저 인증 (Bearer token)
   - POST /v2/agent/auth/login → user_session_token 발급
   - 용도: 디바이스 등록에만 사용

2. 디바이스 인증 (x-api-key)
   - POST /v2/agent/devices/register → device_api_key 발급
   - 용도: 영수증 업로드, 하트비트, 부트스트랩
```

### 문제점 (사실 기반)
1. 윈도우 프로그램이 API 키를 관리해야 함 → 재설치/초기화 시 키 유실
2. 디바이스 등록할 때마다 새 키 발급 → DB에 키 쌓임 (현재 7개)
3. API 키가 프로그램 로컬에 저장 → 분실 시 복구 불가
4. 사용자가 "디바이스 등록" 개념을 이해해야 함 → 불필요한 복잡성

### 변경 기획
```
변경 후:
- 윈도우 프로그램: 이메일/패스워드 로그인 → Bearer token만 사용
- 서버: Bearer token으로 영수증 업로드 허용
- 디바이스 등록/API 키 → 윈도우 프로그램에서 제거
- 기존 FreshOps Agent (API 키 방식) → 기존대로 유지 (호환성)
```

### 서버 수정 필요 사항
- `/v2/agent/uploads/receipt-raw`: requireDeviceApiKeyFromRequest → Bearer token도 허용
- Bearer token 시 device_id 없음 → user_id + siteId로 대체
- siteId: 로그인 응답의 sites[0].id 사용
- 기존 API 키 방식도 유지 (FreshOps Agent 호환)

### 윈도우 프로그램 수정 필요 사항
- UploadService: x-api-key → Authorization: Bearer {token}
- 디바이스 등록 버튼/로직 제거
- LoginConfig에서 ApiKey 필드 제거
- 로그인만 하면 바로 업로드 가능

### 영향 범위
- 서버: agent-routes.js receipt-raw 엔드포인트 1곳 수정
- 윈도우: UploadService.cs, MainForm.cs, AuthService.cs, LoginConfig.cs 수정
- FreshOps Agent: 영향 없음 (API 키 방식 유지)

### 현재 상태 (v2.0.4)
- 윈도우 프로그램: Bearer token 방식으로 변경 완료 (v2.0.0)
- 윈도우 프로그램: BUG-001 코드 수정 완료 (v2.0.4, MainForm.cs:749-759)
- 기존 서버(bbb-prod-api): FK 문제 해결 완료
- 빌드/배포: PM 지시 대기 (2026-04-03 PM 빌드 금지 지시)
- 방향: 기존 서버 수정/업그레이드 확정

---

## 핵심 기능

### 1. 로그인
- 이메일/패스워드/서버주소 입력
- 로그인 버튼 → 서버 인증 → Bearer token 발급
- ☑ 로그인 정보 저장 (이메일/서버주소)
- ☑ 자동 로그인 (저장된 토큰 유효 시)
- 토큰 만료 시 로그인 화면 표시

### 2. 가상 COM포트 생성/관리 (설정 모드, 최초 1회)
- 관리자 비밀번호 입력 후 진입 (초기값: 0000)
- 일반 모드에서는 setupc.exe 절대 호출 안 함
- 설정 창 닫으면 setupc 관련 리소스 완전 해제
- 최초 설치 시 1회만 설정, 이후 포트 문제 생길 때만 진입
- ⚠ 경고: "포트 설정 변경은 매장천사에 영향을 줄 수 있습니다. 영업 중 변경 금지"
- com0com으로 가상 COM포트 쌍 생성
- 포트 번호 사용자 직접 입력
- 시스템 점유 포트 자동 스캔 (레지스트리 SERIALCOMM)
- 점유된 포트 "사용중" 표시 + 생성 차단
- 전체 포트 목록 (물리 + com0com 구분)
- 포트 쌍 안내: "매장천사용: COM{A} / 수신용: COM{B}"
- 포트 복원 (설정에 저장된 번호로 재생성)

### 3. 포트 삭제 (안전 삭제)
- 수신 중지 필수 (점유 해제 후 삭제)
- setupc list에서 우리가 생성한 포트(CreatedPortA/B) 인덱스만 찾아서 삭제
- 매장천사 포트 인덱스는 절대 안 건드림
- 삭제 전 확인: "매장천사 프린터 설정도 다시 해야 합니다"
- 매장천사가 포트 사용 중이면 안내
- 관리자 권한 필요 (없으면 에러 안내)
- 삭제 후 CreatedPortA/B 초기화
- 문제점: 인덱스 밀림 위험 → 포트 이름으로 인덱스 매칭

### 4. 데이터 수신
- 통신 속도 드롭다운 (1200~115200, 기본 9600)
- 수신 시작/중지 버튼
- 자동 수신 (마지막 포트 + 속도로 자동 시작)
- 수신 전 매장천사 프린터 등록 필요 안내

### 5. 데이터 처리 (B안: 텍스트 추출)
- ESC/POS 바이너리에서 제어문자 필터링
- 텍스트만 추출 (주문번호, 메뉴, 금액 등)
- GUI에서 수신된 주문 텍스트 목록 표시

### 6. 서버 업로드
- Bearer token으로 인증 (API 키 아님)
- Idempotency-Key 헤더
- POST /v2/agent/uploads/receipt-raw
- 전송 상태 표시

### 7. 자동 실행
- ☑ Windows 시작 시 자동 실행 ON/OFF
- 레지스트리 등록/해제

### 8. 트레이 아이콘
- X 버튼 → 트레이로 최소화 (프로그램 계속 실행)
- 트레이 우클릭: 열기 / 설정 / 종료
- 상태 아이콘 (연결됨/끊김)

### 9. 에러 메시지 한글화
- setupc.exe 에러 → 한글 변환
- 서버 에러 → 한글 변환
- 포트 에러 → 한글 변환

---

## GUI 화면 설계 (단일 창 + Panel 전환)

### 구조
```
단일 창 (Form)
├── 로그인 패널    ← 최초/토큰 만료 시
├── 메인 패널      ← 로그인 후 기본 화면
└── 설정 패널      ← 설정 버튼 클릭 시

X 버튼 → 프로그램 종료
(트레이 기능 삭제됨 — v2.0.1에서 트레이 제거)
```

### 로그인 패널
```
┌─────────────────────────────────┐
│      배달 주문 수신기 v2.0.4     │
│                                 │
│  이메일:    [________________]  │
│  패스워드:  [________________]  │
│  서버주소:  [________________]  │
│                                 │
│  ☑ 로그인 정보 저장             │
│  ☑ 자동 로그인                  │
│                                 │
│        [ 로그인 ]               │
│                                 │
│  상태: _______________          │
└─────────────────────────────────┘
```

### 메인 패널
```
┌──────────────────────────────────────┐
│ ● 연결됨  COM14→COM15 수신 중  [설정]│
├──────────────────────────────────────┤
│                                      │
│ [수신된 주문 목록]                    │
│ ┌──────────────────────────────────┐ │
│ │ [10:31] 배달의민족 - 떡볶이 1..  │ │
│ │ [10:28] 쿠팡이츠 - 치킨 1..     │ │
│ │ [10:25] 요기요 - 피자 1..       │ │
│ └──────────────────────────────────┘ │
│                                      │
│ 서버 전송 상태: 성공                  │
│ ☑ Windows 시작 시 자동 실행          │
│                                      │
│ [ 수신 시작 ]  [ 수신 중지 ]  [재전송]│
└──────────────────────────────────────┘
```

### 설정 패널 (관리자 비밀번호 필요)
```
┌──────────────────────────────────────┐
│ 설정 (관리자 모드)        [← 돌아가기]│
├──────────────────────────────────────┤
│                                      │
│ [COM포트 관리]                        │
│  현재: COM14↔COM15 (매장천사용/수신용)│
│  시스템 포트: COM1(사용중) COM10(사용중)│
│  포트A: [__] 포트B: [__] [생성]      │
│  [ 포트 삭제 ]  [ 포트 복원 ]        │
│                                      │
│ [통신 설정]                           │
│  통신 속도: [9600 ▼]                 │
│                                      │
│ [매장천사 등록 안내]                  │
│  1. 매장천사 → 주방프린터 → 추가      │
│  2. 프린터 종류: LKT-20              │
│  3. 포트: 위에서 생성한 포트A 입력    │
│  4. 통신 속도: 위 설정과 동일         │
│                                      │
│ [ 로그아웃 ]                          │
└──────────────────────────────────────┘
```

### 사용자 흐름
```
최초 설치:
1. com0com 설치
2. 프로그램 실행 → 로그인 패널
3. 이메일/패스워드 입력 + "로그인 정보 저장" + "자동 로그인" 체크
4. 로그인 성공 → 메인 패널
5. [설정] → 포트 생성 (번호 입력)
6. 매장천사에서 LKT-20, COM{A} 프린터 등록 (안내 참고)
7. [← 돌아가기] → 수신 시작
8. "자동 실행" 체크
9. 완료 → 프로그램 실행 유지

이후 재부팅:
프로그램 자동 시작 → 자동 로그인 → 자동 수신 → 프로그램 실행 유지
→ 사용자 조작 불필요
```

---

## 재부팅 대응

### 재부팅 후 자동 흐름
```
PC 재부팅
→ Windows 시작
→ com0com 가상포트 자동 복원 (드라이버 레벨)
→ 프로그램 자동 시작 (autoStart ON)
→ 저장된 토큰으로 자동 로그인
  → 만료: 로그인 패널 표시
  → 유효: 메인 패널
→ 저장된 포트 확인
  → 있음: 자동 수신 시작 → 트레이 상주
  → 없음: "포트가 사라졌습니다. 복원하시겠습니까?"
→ 정상 동작 (사용자 조작 불필요)
```

### 저장 설정 (AppData JSON)
```json
{
  "email": "사용자 이메일",
  "serverUrl": "https://agent.zigso.kr",
  "token": "Bearer 세션 토큰",
  "siteId": "사이트 ID",
  "lastPort": "COM15",
  "lastBaudRate": 9600,
  "autoStart": true,
  "autoLogin": true,
  "saveLoginInfo": true,
  "password": "암호화된 패스워드 (자동 재로그인용)",
  "createdPortA": "COM14",
  "createdPortB": "COM15"
}
```
- ApiKey 필드 제거 (Bearer token으로 대체)
- autoLogin, saveLoginInfo 필드 추가

### 주문 데이터 구조 (로컬 저장)
- 저장 경로: AppData/DeliveryOrderReceiver/orders/
- 파일명: orders_YYYY-MM-DD.json
- 당일 파일만 목록 표시, 이전 날짜 무제한 보관 (수동 삭제)

| 필드 | 설명 |
|------|------|
| seq | 순번 (당일 기준) |
| receivedAt | 수신 시각 |
| port | 수신 포트 |
| content | 원본 텍스트 |
| hash | SHA256 해시 |
| uploadStatus | 성공/실패/중복 |
| idempotencyKey | 내용 해시 (=hash) |

- 중복 감지: 최근 7일 해시 비교, 일치 시 "중복" 처리
- 날짜 경계: 수신 완료 시점 기준
- 파일 손상 방지: 임시 파일 쓰기 후 교체

---

## 설치/설정 순서 (최초 1회)
1. com0com v2.2.2.0 signed 설치 (.NET Framework 3.5 필요)
2. 프로그램 설치 (exe, 로컬에서 실행)
3. 로그인 (이메일/패스워드)
4. [설정] → 포트 생성
5. 매장천사에서 프린터 추가 (LKT-20, COM{A})
6. [← 돌아가기] → 수신 시작
7. 자동 실행 ON
8. 이후 재부팅 시 자동 동작

---

## 기술 스택

| 구분 | 기술 |
|------|------|
| 언어/프레임워크 | C# .NET 8.0 WinForms |
| 가상 COM포트 | com0com v2.2.2.0 signed |
| COM포트 통신 | System.IO.Ports.SerialPort |
| HTTP 통신 | System.Net.Http.HttpClient |
| 빌드 | Self-contained, win-x64 |
| 최종 배포 | Electron + 웹 하이브리드 (향후) |
| 서버 기획 | server_progress.md 참고 |

---

## 서버/API

| 구분 | 내용 |
|------|------|
| 서버 | https://agent.zigso.kr |
| 로그인 | POST /v2/agent/auth/login (email, password) → token |
| 영수증 업로드 | POST /v2/agent/uploads/receipt-raw (Bearer token + Idempotency-Key) |

- 디바이스 등록 API는 윈도우 프로그램에서 사용하지 않음
- 서버에서 Bearer token + x-api-key 둘 다 허용 (호환성)

---

## 테스트 결과

| 항목 | 결과 | 비고 |
|------|------|------|
| 로그인 | ✅ 성공 | |
| 포트 생성 | ✅ 성공 | COM14/COM15 |
| 포트 스캔 | ✅ 성공 | com0com 포트 감지 |
| 매장천사 프린터 등록 | ✅ 성공 | LKT-20, COM14 |
| 매장천사 시험 인쇄 수신 | ✅ 성공 | |
| 배달앱 주문 수신 | ✅ 성공 | v2.0.0에서 확인 (6번 이미지) |
| 서버 업로드 | 🔄 빌드 대기 | v2.0.4에서 BUG-001 코드 수정 완료. 빌드/배포 후 실업로드 검증 필요 |
| 자동 로그인 | ✅ 성공 | v2.0.0에서 확인 |
| 재부팅 자동 시작 | 🔄 테스트 필요 | 코드는 Program.cs:16-26 검증 완료, 실재부팅 테스트 미완 |

---

## 알려진 문제점

### 해결됨
- [x] setupc.exe 경로 못 찾음 → WorkingDirectory + inf 자동복사
- [x] com0com 포트가 GetPortNames에 안 잡힘 → setupc list로 변경
- [x] 포트 하드코딩 충돌 방지 → 레지스트리 동적 스캔
- [x] 창 2개 동시 표시 → SetVisibleCore 오버라이드
- [x] 포트 간섭 문제 → 일반 모드/설정 모드 분리로 해결 (setupc 호출 분리)

### 해결됨 (v2.0.1 → v2.0.2)
- [x] X 버튼 → 프로그램 종료 (v2.0.1에서 구현)
- [x] 트레이 아이콘 제거 (v2.0.1에서 구현)
- [x] 창 사이즈에 맞게 컨트롤 표시 (v2.0.2에서 Dock 레이아웃으로 수정)
- [x] 주문 목록 가로 스크롤 (v2.0.1에서 HorizontalExtent 설정)
- [x] 주문 내역 날짜별 로컬 저장 (v2.0.1에서 OrderStorageService 구현)
- [x] 로컬 중복 감지 (v2.0.1에서 SHA256 해시 7일 비교 구현)
- [x] 업로드 실패 시 로컬 "실패" 저장 + 수동 재전송 버튼 (v2.0.1에서 구현)
- [x] Idempotency-Key를 내용 해시로 변경 (v2.0.1에서 구현)
- [x] 토큰 만료 시 저장된 이메일/패스워드로 자동 재로그인 (v2.0.1에서 구현)
- [x] 종료 시 버퍼 데이터 처리 완료 후 종료 (v2.0.1에서 구현)
- [x] 로컬 JSON 파일 손상 방지 (v2.0.1에서 임시 파일 교체 구현)

### 해결됨 (v3.0.1 git 코드 기준)

> 단일 진실 출처: 각 항목 상세는 `issue_matrix.md` C-1/C-2/B-* 행. 본 섹션은 한 항목 한 줄.

- [x] BUG-001: "자동 로그인" 미체크 시 토큰 삭제 버그 — `_config.Token = ""` if 블록 제거. Forms/MainForm.cs LoginButton_Click(L726). 현재 `_config.Token = ""`은 LogoutButton_Click(L1456)에만 존재
- [x] BUG-002: 로컬 누적 주문 서버 전송 가능 — BUG-004 수정으로 토큰 추출 정상화 (재전송 실동작은 PM 미테스트 항목으로 이동)
- [x] BUG-003: 재전송 버튼 코드 존재 — Forms/MainForm.cs:1034 RetryUploadBtn_Click 구현됨 (실동작 검증은 PM 미테스트로 이동)
- [x] BUG-004: AuthService 토큰 추출 필드명 수정 — `data.userSessionToken` 경로 (Services/AuthService.cs:55-69)
- [x] W-TIME: 시간대 — Forms/MainForm.cs:884 `DateTime.UtcNow.ToString("o")` (정상 UTC, 옛 `DateTime.Now` 버그 제거됨)
- [x] W-RETRY: 재전송 실패 시 lastError 표시

### 미해결 — 보안 (PM 빌드 지시 대기)
- [ ] **B-4**: config.json `token` 평문 저장 — Models/LoginConfig.cs:11
       방향: Windows DPAPI(`ProtectedData.Protect`, scope=CurrentUser)
- [ ] **B-5**: config.json `password` 평문 저장 — Models/LoginConfig.cs:20, Save() L46-
       방향: 동일. 단일 사용자 PC 한정 복호화 (매장 공용 PC 시나리오 적합)

### 미해결 — 데이터 무결성 (PM 빌드 지시 대기)
- [ ] **C-3**: 주문 JSON 동시 쓰기 잠금 없음
       위치: Services/OrderStorageService.cs Save() L52-76, UpdateStatus() L81-101
       현재: File.WriteAllText + File.Replace, FileShare 미설정
- [ ] **C-4**: config.json 동시 쓰기 잠금 없음 — Models/LoginConfig.cs Save() L46

### 미해결 — PM 미테스트 (실기기 필요)
- [ ] 재부팅 자동 시작 실재부팅 (W-701~702, W-1101~1105 / Program.cs:16-26 코드 검증만 완료)
- [ ] 자동 재로그인 + 만료 토큰 흐름 (W-104, W-1205)
- [ ] 포트 관리/삭제 실기기 (W-203, 208, 301~305)
- [ ] v2.0.1 신규 기능 실사용 (W-1201~1207)
- [ ] BUG-003 재전송 버튼 실동작 (Forms/MainForm.cs:1034 RetryUploadBtn_Click — 401 자동 재로그인 분기까지)
- [ ] 화면 레이아웃 최종 확인
- [ ] 날짜별 저장/중복 감지 실동작 확인

### 미해결 — 코드 품질 (PM 우선순위 대기)
- [ ] 관리자 비밀번호 하드코딩 — 설정 모드 진입 비밀번호 `"0000"` 하드코딩, 변경 UI 없음
- [ ] Forms/MainForm.cs 1671줄 분리 — GUI + 비즈니스 로직 통합 → 단일 책임 분리
- [ ] 기획서에 없는 코드가 다른 곳에도 있는지 전수 확인

### 코드 작성 규칙 위반 기록

| 위반 | 내용 | 영향 |
|------|------|------|
| 기획서에 없는 코드 추가 | "자동 로그인" 미체크 시 토큰 삭제 | 서버 업로드 0건, 로컬에만 주문 쌓임 |
| 근거: CLAUDE.md "기획서에 없는 내용 임의 작성 금지" 위반 | | |

### 윈도우 프로그램 정리 체크리스트 (근거 기반)

#### API Key 관련 정리
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| 1 | LoginConfig.cs ApiKey 필드 | ✅ 제거 완료 | v2.0.0에서 삭제 (grep 결과 0건) |
| 2 | AuthService.cs RegisterDeviceAsync | ✅ 제거 완료 | v2.0.0에서 삭제 (grep 결과 0건) |
| 3 | UploadService.cs x-api-key 헤더 | ✅ 제거 완료 | Bearer token으로 변경 (grep 결과 0건) |
| 4 | 디바이스 등록 UI | ✅ 제거 완료 | MainForm에 버튼 없음 (grep 결과 0건) |
| 5 | 서버 API (디바이스등록/API Key) | 보류 | 서버에 그대로 유지, 우리 프로그램에서 안 씀 (PM 결정) |

#### BUG-001 수정 진행 (v2.0.4 기준)
| # | 작업 | 파일 | 상태 | 근거 |
|---|------|------|------|------|
| 1 | 토큰 삭제 코드 제거 | MainForm.cs LoginButton_Click | ✅ 완료 (v2.0.4) | `_config.Token = ""` 코드 제거됨 (라인 749-759) |
| 2 | autoLogin 플래그만 저장하도록 수정 | MainForm.cs LoginButton_Click | ✅ 완료 (v2.0.4) | `_config.AutoLogin = _autoLoginCheckBox.Checked;` 만 저장 |
| 3 | saveLoginInfo 미체크 시 이메일/서버주소/패스워드 초기화 | MainForm.cs LoginButton_Click | ✅ 완료 (v2.0.4) | 라인 752-757: Email/Password/ServerUrl 초기화 (기획서엔 이메일/서버주소만이었으나 Password도 함께 초기화하도록 확장) |
| 4 | 빌드 (dotnet publish) | DeliveryOrderReceiver.csproj | [ ] | csproj `Version 2.0.4`로 변경됨, macOS dotnet 미설치로 빌드 미완. PM 빌드 지시 대기 |
| 5 | 서버 배포 | zigso.kr releases/2.0.4/ | [ ] | manifest.json + latest.json 갱신 (빌드 후) |
| 6 | PM 다운로드 + 재로그인 | 윈도우 프로그램 | [ ] | epposon0@gmail.com / Zigso2026! |
| 7 | 업로드 동작 확인 | 서버 raw_receipts | [ ] | 주문 수신 시 서버에 저장 확인 |
| 8 | 로컬 실패 건 재전송 | 윈도우 [재전송] 버튼 | [ ] | BUG-002 해결 |

#### BUG-001 수정 상세 (v2.0.4 적용된 코드)

```csharp
// MainForm.cs LoginButton_Click (라인 747-759)
// 로그인 정보 저장 처리
_config = LoginConfig.Load();
_config.SaveLoginInfo = _saveLoginInfoCheckBox.Checked;
_config.AutoLogin = _autoLoginCheckBox.Checked;

if (!_saveLoginInfoCheckBox.Checked)
{
    _config.Email = "";
    _config.Password = "";
    _config.ServerUrl = "https://agent.zigso.kr";
}

_config.Save();
```

근거:
- dev_progress.md 기획서 "☑ 자동 로그인": "저장된 토큰 유효 시" 자동 로그인
- autoLogin=false면 다음 시작 시 TryAutoLogin()에서 토큰 체크 안 함 (이미 AuthService.AutoLogin()에서 config.AutoLogin 확인)
- 현재 세션에서 토큰을 삭제할 이유 없음
- v2.0.4 추가: saveLoginInfo 미체크 시 Password도 함께 초기화 (자동 재로그인 차단)

#### 솔직히 추가로 확인 필요한 것
| # | 항목 | 상태 |
|---|------|------|
| 1 | saveLoginInfo 미체크 시 Password도 초기화하는지 | ✅ 확인 완료 (v2.0.4 MainForm.cs:755에서 Password 초기화) |
| 2 | 로그아웃 시 토큰/패스워드 초기화 정상 동작하는지 | ✅ 확인 완료 (MainForm.cs:1456-1457) |
| 3 | 기획서에 없는 코드가 다른 곳에도 있는지 | 전수 확인 필요 (미완) |

---

## 고려사항
- **포트 충돌 방지**: 시스템 점유 포트 자동 감지
- **실행 환경**: exe는 반드시 Windows 로컬에서 실행 (UNC 경로 금지)
- **빌드 타겟**: win-x64 (일반 PC 대상)
- **매장천사 프린터 등록**: 사용자 수동 (LKT-20)
- **매장천사 재시작**: 불필요 (실시간 인식)
- **com0com 설치**: 사전 필수 (.NET Framework 3.5)
- **포트 간섭 방지**: 일반 모드에서 setupc 호출 금지. 설정 모드에서만 포트 조작
- **관리자 비밀번호**: 설정 모드 진입 시 비밀번호 필요 (초기값: 0000)
- **최초 1회 설정**: 포트 생성은 설치 시 1회만. 이후 포트 문제 없는 한 설정 안 들어감
- **윈도우 프로그램 방향**: DeliveryOrderReceiver(claude-1/)가 메인. 코덱스 windows-agent(server/agents/)는 참고용 보관
- **윈도우↔웹 세션 분리**: 윈도우 프로그램 로그인(config.json 토큰)과 웹 로그인(localStorage 토큰)은 별개 세션. 같은 계정이라도 각각 로그인 필요
- **두 클라이언트 경계 (단일 진실 출처)**:
  - DeliveryOrderReceiver (`ssqq/DeliveryOrderReceiver/`):
      C# .NET 8 WinForms (`net8.0-windows`, `PublishSingleFile`+`SelfContained` win-x64,
      DeliveryOrderReceiver.csproj 기준), com0com 가상 COM 직결, 매장천사 운영 매장 전용,
      단일 창 GUI, 매장 운영자 PC 1대 1 설치
  - Windows Agent (`server/agents/windows-agent/`):
      C# .NET 백그라운드 서비스(`WindowsAgentBackgroundService`), MSI 배포,
      file_watch/serial/receipt_printer 3축 (CaptureAdapters.cs),
      zigso.kr 다운로드 + site_owner/org_operator 권한
      (`/management/devices/download`, unified-plan-v1.md §8-12)
  - 공유 서버 API: `POST /v2/agent/auth/login`, `POST /v2/agent/uploads/receipt-raw`
    (자세한 파일/라인은 아래 "서버 측 계약" 항목 참조)
  - 합치지 않는다. windows-agent 전체 기획은
    `server/docs/architecture/windows-agent-unified-plan-v1.md` 참조 (이 기획서 범위 밖).
- **가상프린터 운영 경계 (절대 건드리지 않음, 인용만)**:
  - 구현 상태: `ReceiptPrinterCaptureAdapter` (server/agents/windows-agent/CaptureAdapters.cs)
    가 MSI에 실려 있고 `AgentSettings.CaptureSettings.SourceType` 기본값이
    `"receipt_printer"` (AgentSettings.cs)이며 `AgentRuntime` fallback이 여기로 라우팅된다.
    그러나 **실제 Windows 가상 프린터 드라이버 설치/스풀 캡처/실제 프린터 재전달은
    Phase 4 미구현**이다 (unified-plan-v1.md §8-3 "현재 상태 메모").
  - 운영 금지 (unified-plan-v1.md §8-3 / §8-7):
      · 실제 프린터 제거 금지
      · 기존 프린터 포트 임의 변경 금지
      · 종이 출력이 끊기는 단일 가상 프린터 강제 전환 금지
      · 운영 중 POS의 COM19 사전 검증 없이 변경 금지
      · 실매장에서 즉시 엔진 서비스 자동등록 금지
  - 이 기획서는 가상프린터 관련 코드/설정/UX/배포를 다루지 않는다.
    단일 진실 출처는 `windows-agent-unified-plan-v1.md` §8-1~§8-11이며,
    모든 업데이트는 거기서만 이루어진다.
- **서버 측 계약 (authoritative 참조, 수정 X)**:
  v3.0.1 git 코드의 DOR ↔ 서버는 호환. 서버 코드 변경 필요 없음 (정합성 점검 2026-04-08).
  아래 파일들은 참조 전용 (단일 진실 출처):
  - `server/apps/api/src/routes/agent-routes.js`
      · POST /v2/agent/auth/login — 응답: `data.userSessionToken`, `data.user`,
        `data.organizations`, `data.sites[]` (buildAuthSessionPayload)
      · POST /v2/agent/devices/register — Bearer 필수
      · POST /v2/agent/uploads/receipt-raw — dual auth:
        1) `requireDeviceApiKeyFromRequest` (우선)
        2) 실패 시 `requireUserSessionFromRequest` + 가상 디바이스 자동생성
        필수: `Authorization: Bearer {token}`, `Idempotency-Key: {hash}`,
        body: eventId/siteId/platformId/platformStoreId/capturedAt/rawChecksum/decodedText/port
      · POST /v2/agent/heartbeat, GET /v2/agent/bootstrap
      · 로그인 rate limit (5분간 5회/30초 잠금)
  - `server/apps/api/src/server.js`
      · requireUserSessionFromRequest, requireDeviceApiKeyFromRequest
      · validateLoginBody, bodyLimit 1MB
  - `server/packages/contracts/src/contracts.js`
      · wrapDataEnvelope — `{data, meta:{requestId}}` 래핑
  - `server/packages/db/src/postgres-store.js`
      · recordAuditLog / listManagementAuditLogs, scrypt 해싱, updateUploadJobStatus
  - DOR 측 매칭 코드 (호환 검증 완료):
      · Services/AuthService.cs:55-69 — `data.userSessionToken` 경로 (BUG-004 수정 반영)
      · Services/UploadService.cs — Bearer + Idempotency-Key 송신
  - 이 목록을 박는 이유: 향후 DPAPI(B-4/B-5) 또는 파일 잠금(C-3/C-4) 구현 시
    이 요청/응답 shape을 깨지 않도록 reviewer가 한눈에 확인 가능해야 함.
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
  - **git ↔ 운영 sync 주의**: git 트래킹 코드(v3.0.1)와 운영 배포 버전이
    별도로 관리되고 있음. 운영 직접 패치/force push 금지. 운영 상태 확인은
    SSH 또는 PM 확인 필요.
  - Windows Agent 빌드 (참고, 본 작업 대상 아님):
      · `server/agents/windows-agent/scripts/publish-win-x64.ps1`
      · `server/agents/windows-agent/scripts/build-msi.ps1` (WiX v4+)
      · `server/agents/windows-agent/installer/Product.wxs`
  - 서버 빌드/배포 (참고, 본 작업 대상 아님):
      · `server/scripts/docker-build-strict.sh` (semver 강제, latest 금지)
      · `server/scripts/docker-deploy-strict.sh` (version_gt, 건강성 180초)
      · `server/scripts/upload-gate-strict.sh` (폐쇄형 검증 게이트)
      · `server/scripts/safe-deploy.sh` (docker cp + `node --check` + auto rollback)
      · `server/docs/release-manifest/` (per-release `{TAG}.md` + `{TAG}.json`)
  - PM 검증 루프: 배포 후 PM이 실기기에서 수동 테스트 → 결과 기록
  - 현재 금지/대기: 신규 빌드·배포·가상프린터 구현 모두 PM 별도 지시 대기.
    본 작업은 문서만 갱신.

---

## 향후 계획
- Electron + 웹 하이브리드 전환 (UI 서버 로드)
- 자동 업데이트 서버 구축
- 서버 다운로드 페이지
- 매장천사 배달 대행 API 연동 검토

---

## 코드 작성 규칙
- 기존 파일 수정 원칙
- 기획서 작성 → PM 상의 → 기획서 확정 → 코드 작업
- 하드코딩 금지
- 작업 완료 시 work_log.md + git push
- **윈도우 프로그램 빌드/배포 금지**: 2026-04-03 PM 지시 — git 트래킹 v3.0.1 코드 상태에서 신규 빌드/배포 모두 PM 별도 지시 대기. 운영(prod) 배포 버전은 git과 별도 트래킹이며 직접 패치/force push 금지.

---

## 변경 이력
| 날짜 | 내용 | 담당 |
|------|------|------|
| 2026-03-30 | 프로젝트 시작 | 리더 |
| 2026-03-31 | com0com/B안/WinForms 확정 | 기획 |
| 2026-04-01 | 운영 구조 확정 (매장천사 프린터 추가) | 리더 |
| 2026-04-01 | v1.0.0~v1.7.0 개발 (포트관리/로그인/수신/빌드) | 리더 |
| 2026-04-01 | 인증 구조 분석 → Bearer token 전환 기획 | 리더 |
| 2026-04-01 | GUI 단일창+Panel 전환+트레이 기획 | 리더 |
| 2026-04-01 | 기획서 전면 재정비 (분석 기반) | 리더 |
| 2026-04-01 | 포트 설정 분리 (관리자 비밀번호 0000), 최초 1회 설정, 포트 간섭 방지 | 리더 |
| 2026-04-01 | v2.0.1 기획: 날짜별 저장, 중복감지, 재전송, 창 수정, 트레이 제거 | 리더 |
| 2026-04-01 | v2.0.0~v2.0.2 개발 (인증전환, 단일창UI, 트레이, 날짜별저장, 중복감지, 재전송, 레이아웃) | 리더 |
| 2026-04-02 | 서버 전수 분석, 기획서 갱신, 깃 구조 변경 (claude-1/server/ 분리) | 리더 |
| 2026-04-03 | [긴급] BUG-001~003 기록. 기획서 없이 추가한 코드로 서버 업로드 0건 문제 | 리더 |
| 2026-04-03 | 윈도우 프로그램 정리 체크리스트 + BUG-001 수정 계획 상세 작성 | 리더 |
| 2026-04-04 | v2.0.4 BUG-001 코드 수정 (`_config.Token = ""` 제거) + saveLoginInfo 미체크 시 Password 초기화 추가 | 리더 |
| 2026-04-04 | v2.0.4 코드 검증 완료 (BUG-001 수정 / 4대 기능 코드 존재 확인) — `v2.0.4-final-verification.md` | 리더 |
| 2026-04-04 | v2.0.4 빌드 시도 — macOS에 dotnet 미설치로 빌드 실패. Windows 호스트 또는 dotnet 설치 필요 | 리더 |
| 2026-04-08 | 기획서를 v2.0.4 실제 코드 상태에 맞게 갱신 (BUG-001 ✅, 미해결 항목 잔류) | 리더 |
| 2026-04-08 | v3.0.1 정합화 — 라벨 단일화, 4경계(클라이언트/가상프린터/서버계약/개발방법) 명시, git/prod 분리 트래킹 명시 | 에이전트 |
