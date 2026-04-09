# v3.0.1 다운로드 스크립트

## 왜 스크립트인가

`https://zigso.kr/management/devices/download` 페이지는 `latest.json` 기반으로 **단일 안정 버전**(현재 v2.0.4)만 노출하도록 하드코딩되어 있다. v3.0.1은 `active=false` / `channel=beta` 상태로 배포되었기 때문에, 이 페이지에서 노출하려면 페이지 코드 자체를 수정해야 한다.

페이지 수정 = `bbb-prod-web-1` 컨테이너 내부 코드 직접 패치 = git/prod drift +1 = 메모리 규칙("운영 직접 패치 금지") 위반 누적. 그래서 페이지를 안 건드리고, 대신 사용자/매장 운영자가 직접 실행하는 다운로드 스크립트로 v3.0.1 받아가도록 한다.

장점:
- **드리프트 0** — 운영 컨테이너/소스 0 변경
- **격리 유지** — `latest.json`이 여전히 v2.0.4를 가리키므로 매장 PC 자동 업데이트는 v2.0.4 그대로
- **수동 제어** — 사용자가 명시적으로 다운로드 실행해야만 v3.0.1을 받음
- **검증 내장** — SHA256 자동 비교

## 파일

| 파일 | 환경 | 용도 |
|---|---|---|
| `Download-DOR-v3.0.1.ps1` | Windows PowerShell 5.1+ | **매장 PC / 테스트 PC에서 실행**. 메인 배포 도구. |
| `download-v3.0.1.sh` | macOS / Linux bash | 개발자 검증용. 매장 PC와 동일 흐름. |

## 사용법 — Windows (매장 PC)

### 1. 스크립트 매장 PC에 옮기기

가장 간단한 방법:
1. 이 폴더의 `Download-DOR-v3.0.1.ps1` 를 USB 또는 이메일로 매장 PC에 전달
2. 또는 PowerShell에서 직접 실행 (자격 증명 다시 입력)

### 2. PowerShell 실행

```powershell
# 일반 PowerShell 창 열기 (관리자 권한 불필요)
cd $env:USERPROFILE\Downloads
.\Download-DOR-v3.0.1.ps1
```

스크립트 실행 차단되면:
```powershell
# 한 번만 실행 정책 우회
PowerShell -ExecutionPolicy Bypass -File .\Download-DOR-v3.0.1.ps1
```

### 3. 입력
- 이메일 (zigso.kr 계정)
- 패스워드 (입력 시 화면에 안 보임)

### 4. 결과
- `.\DeliveryOrderReceiver-v3.0.1.exe` 가 현재 폴더에 저장됨 (~68 MB)
- SHA256 자동 검증
- 검증 실패 시 스크립트가 중단되고 에러 메시지 출력

### 옵션
```powershell
# 다른 폴더에 저장
.\Download-DOR-v3.0.1.ps1 -OutFile D:\dor-test\v3.0.1.exe

# 이메일 미리 지정
.\Download-DOR-v3.0.1.ps1 -Email epposon0@gmail.com
```

## 사용법 — Mac/Linux (개발자)

```bash
cd /Users/min/Documents/claude-1/ssqq/DeliveryOrderReceiver-v3/scripts
./download-v3.0.1.sh
# 또는
./download-v3.0.1.sh /tmp/v3.0.1.exe
# 또는 환경변수
EMAIL=foo@bar PASSWORD=xxx ./download-v3.0.1.sh
```

## 검증값

| 항목 | 값 |
|---|---|
| 버전 | 3.0.1 |
| 파일명 | DeliveryOrderReceiver-v3.0.1.exe |
| 크기 | 71,808,718 bytes (~68.5 MB) |
| SHA256 | `23f7345650fa84e4dfc508e167b56373babf5734565bb17153538f4518410613` |
| 빌드 시각 (UTC) | 2026-04-08T15:39:18Z |
| 채널 | beta (active=false) |

스크립트는 위 SHA256과 다운로드한 파일을 비교하고 불일치 시 즉시 중단한다.

## 매장 PC 설치 시 주의 사항

**중요 — 기존 v2.0.4와 같은 폴더에 덮어쓰지 말 것.**

이유:
1. v3.0.1은 `config.json` 의 token / password 를 **DPAPI 암호화 형식**으로 저장한다 (B-4/B-5 fix).
2. 기존 v2.0.4는 평문 저장이라 형식이 다르다.
3. v3.0.1은 v2.0.4의 평문 config를 **자동 마이그레이션 안 함** (마이그레이션 코드 미구현).
4. 같은 폴더에서 실행하면 v3.0.1이 v2.0.4의 평문 config를 읽지 못해서 자동 로그인 실패.

권장 설치 흐름:
1. **별도 폴더** 생성 (예: `D:\DeliveryOrderReceiver-v3\`)
2. exe 를 그 폴더에 복사
3. **com0com 포트는 기존 매장의 것 그대로 사용** (포트 새로 만들지 말 것 — 매장천사 영향)
4. v3.0.1 실행 → 새로 로그인 → 기존 포트 선택 → 수신 시작
5. 단일 매장 1대에서만 시험 운영
6. PM 검증 통과 후에만 다른 매장 확장

## 트러블슈팅

### "userSessionToken 없음"
- 이메일/패스워드 오타
- 계정 권한 문제 (`/v2/agent/auth/login` 5분간 5회 실패 시 30초 잠금)

### "SHA256 불일치"
- 다운로드 중 손상 → 재시도
- 또는 서버에 새 빌드가 올라온 경우 — 스크립트의 `EXPECTED_SHA256` 값 확인 필요

### "401 Unauthorized" (manifest/exe 단계)
- 토큰 만료 → 재실행
- 계정에 windows-agent 다운로드 권한 없음 → PM 확인

### "SSL/TLS error"
- 회사 방화벽 / SSL 검사 → 회피 불가, 다른 네트워크에서 시도

## git 정책

이 스크립트들은 `ssqq/DeliveryOrderReceiver-v3/scripts/` 안에 있고 v3.0.1 폴더 전체와 함께 **git untracked** 상태로 유지된다 (사용자 명시 정책). 매장 PC 배포 시 USB/이메일로 직접 전달.
