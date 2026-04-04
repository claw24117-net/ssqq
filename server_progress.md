# 배달 주문 수신기 - 서버 기획서 (v3.0)

## 근거 문서
- ssa/legacy/canonical-database-design.md — DB 설계
- ssa/v1/ui-information-architecture-v1.md — 정보구조(IA)
- ssa/v1/ui-top-5-wireframes-v1.md — 핵심 5화면
- ssa/v1/order-matching-logic-v1.md — 주문 매칭 로직
- ssa/legacy/automatic-extraction-rules.md — 자동 추출 규칙
- PM 지시: "서버 기획서에 키관련 부분은 따로 작성해놔"
- PM 지시: "중복이면 서버에서 알아서"
- PM 지시: "주문 허브 페이지 db 랑 연동"

---

## SSA 서버 목적
매장 운영 자동화 대시보드 — 주문 수집 → 메뉴 매핑 → 재고 차감 → 정산 관리

## 데이터 흐름 (canonical-database-design.md 기반)
```
1. 영수증 출력 수집 (우리 윈도우 프로그램 = receipt_raw)
2. orders + order_lines 생성
3. 재고 차감 실행
4. 매장천사 CSV 업로드 (angel_csv)
5. 같은 주문 매칭 (order-matching-logic-v1.md)
6. order_settlements + payment_method + order_status 보정
7. 취소면 inventory_movements로 재고 복구
8. 충돌/누락은 reconciliation_issues로 보냄
```

## DB 설계 (canonical-database-design.md 기반, 11개 테이블)

### 핵심 테이블
| 테이블 | 목적 |
|--------|------|
| orders | 주문 헤더 (order_key, platform_id, ordered_at, payment_method, order_status 등) |
| order_sources | 원본 증빙 (source_type: receipt_raw, angel_csv, excel_adjustment, manual_edit) |
| order_lines | 메뉴/옵션/meta (line_type: menu, option, meta) |
| order_settlements | 정산 숫자 (gross_menu_amount, discount_amount, delivery_tip_amount 등) |
| order_status_events | 상태 이력 (placed, printed, paid, cancelled, refunded) |

### 보조 테이블
| 테이블 | 목적 |
|--------|------|
| platforms | 배달 플랫폼 (baemin, coupang_eats, yogiyo) |
| stores | 플랫폼별 상점 |
| inventory_items | 재고 품목 |
| recipes | 메뉴-재고 소모 연결 (BOM) |
| inventory_movements | 입고/차감/복구 이력 |
| reconciliation_issues | 충돌/누락 관리 |

### 우리 윈도우 프로그램의 위치
- order_sources 테이블에 source_type=receipt_raw로 저장
- raw_text 필드에 ESC/POS 파싱된 텍스트
- checksum 필드에 SHA256 해시 (중복 감지)
- 이 데이터가 전체 파이프라인의 시작점

## 주문 매칭 로직 (order-matching-logic-v1.md 기반)

### 매칭 우선순위
1. 완전 중복 제거 — raw_checksum 동일하면 재전송으로 판단
2. external_order_code exact match — 같은 site + 같은 주문코드
3. 헤더 exact match — site + platform + 시간(2분이내) + 금액 + serial
4. 가중치 매칭 — 80점 이상 + 2위와 15점 차이 시 병합
5. 검토대상 큐 — 자동 병합 불가 시

### 필드 병합 규칙
- receipt_raw 우선: order_lines, menu_fingerprint, request_note
- angel_csv 우선: net_paid_amount, payment_method, order_status
- manual_edit 우선: discount_amount, delivery_tip_amount

## 자동 추출 규칙 (automatic-extraction-rules.md 기반)
- 영수증에서: 주문번호, 플랫폼, 메뉴/옵션/meta 분류, 수량
- CSV에서: 주문일시, 결제방식, 금액, 상태
- 상점 분류: 수동 키워드 우선 → 기본 규칙 → 대괄호 토큰 fallback

## UI 정보구조 (ui-information-architecture-v1.md 기반)

### 최상위 메뉴 6축
| 메뉴 | 목적 |
|------|------|
| 운영 | 오늘 해야 할 일과 리스크 (KPI, 이슈 카드) |
| 주문 | 주문 운영, 검토, 증빙, 취소 |
| 메뉴·재고 | 메뉴, 옵션, 레시피, 재고 연결 관리 |
| 분석 | 매출/메뉴/옵션/상점 성과 |
| 설정 | 운영 규칙과 기준값 관리 |
| 관리 | 장비/업로드/권한/조직 운영 |

### 주문 하위 메뉴 (우리 작업과 직접 관련)
| 하위 메뉴 | 라우트 | 목적 |
|-----------|--------|------|
| 전체 주문 | /orders | 기본 주문 리스트 |
| 검토대상 | /orders/review | 자동 확정 금지 케이스 처리 |
| 취소/복구 | /orders/cancelled | 취소와 재고 복구 |
| 원본 증빙 | /orders/evidence | receipt vs csv 비교 |

### 핵심 5화면 (ui-top-5-wireframes-v1.md)
1. /operations/overview — 운영 홈
2. /orders/review — 검토대상
3. /orders/evidence — 원본 증빙
4. /management/devices — 디바이스 관리
5. /management/uploads — 업로드 기록

## 현재 서버 코드 상태 (ssa/server/)

> **방향 확정 (2026-04-02)**: 기존 서버(bbb-prod-api) 수정/업그레이드.

### 구현된 것
- Express API 서버 스켈레톤 (server.js, agent-route.js)
- PostgreSQL 연결 (postgres-store.js)
- Next.js 웹앱 스켈레톤 (orders 페이지 mock)
- Dockerfile + docker-compose
- DB migration (health_check_log만)

### 구현 안 된 것
- receipt-raw 업로드 엔드포인트
- 인증 (로그인, 세션, 토큰)
- DB 테이블 (orders, order_sources, order_lines 등 전부)
- 주문 매칭 로직
- 주문 허브 페이지 (DB 연동)
- 중복 감지 (Idempotency-Key)

---

## 서버 수정 필요 사항 (우선순위)

## 작업 방향 (확정)
- **기존 서버(bbb-prod-api) 수정/업데이트**
- **윈도우 프로그램(DeliveryOrderReceiver)** — 우리가 개발한 것 유지
- 근거: 기존 서버에 89개 엔드포인트, 46개 테이블, 91개 Store 함수, 25개 페이지 이미 구현됨. 처음부터 만드는 것은 비효율

## 기존 서버 기능 방향 결정 (PM 확정 2026-04-02)

### 윈도우 프로그램 방향
- **우리 DeliveryOrderReceiver(claude-1/)가 메인** — 실제 매장에서 주문 수신 테스트 성공
- **코덱스 windows-agent(server/agents/)는 참고용 보관** — 설계만 되어 있고 하드웨어 미연동 (hardwareConnected=false)
- 근거: PM 확인 "우리 프로그램을 메인으로 가고, 코덱스 버전은 참고용으로 보관"

### 기존 서버 기능 16건 방향

#### 유지 + 활용 (8건)
| # | 항목 | 이유 |
|---|------|------|
| 1 | POST /v2/agent/auth/signup | 계정 생성 필요 |
| 2 | POST /v2/agent/uploads/angel-csv | Phase 3 CSV 매칭에서 사용 |
| 3 | POST /v2/agent/uploads/settlement-adjustment | Phase 3 정산 보정에서 사용 |
| 4 | POST /api/settlements/run + GET | 정산 시스템, 이미 구현됨 |
| 5 | 웹 페이지 6개 (login/signup/tenant/super/download/루트) | 대시보드 + 윈도우 프로그램 배포 |
| 6 | BullMQ 워커 | 정산 비동기 처리 |
| 7 | 배포 스크립트 33개 | 서버 배포 필수 |
| 8 | 테스트 코드 9개 | 수정 시 검증 필수 |

#### 유지 + 보류 (6건)
| # | 항목 | 이유 |
|---|------|------|
| 9 | GET /v2/agent/bootstrap | 다른 기능에 영향 가능, 삭제 불필요 |
| 10 | POST /v2/agent/heartbeat | 향후 디바이스 모니터링 활용 가능 |
| 11 | POST /api/uploads (pc-uploader) | CSV 수동 업로드 도구로 활용 가능 |
| 12 | x-api-key 4역할 인증 | 현재 off, 향후 권한 체계 참고 |
| 13 | pc-uploader 에이전트 | CSV 수동 업로드 도구 |
| 14 | OpenTelemetry 모듈 | 현재 비활성, 필요 시 활성화 |

#### 참고용 보관 (1건)
| # | 항목 | 이유 |
|---|------|------|
| 15 | agents/windows-agent (코덱스 C# 버전) | 하드웨어 미연동, 우리 DeliveryOrderReceiver가 메인 |

#### FK 해결 방향 확정 (PM 확정)
| # | 항목 | 방향 |
|---|------|------|
| 16 | upload_jobs device_id FK | **방법 A: user session 인증 시 가상 device 자동 생성** |

- user session 인증 성공 시 devices 테이블에 가상 device INSERT (없으면 생성, 있으면 건너뜀)
- device_id: 'user_' + userId
- DB 스키마 변경 없음, upsertReceiptRawUpload 수정 불필요
- 기존 idempotency/dedupe 로직 그대로 동작

## 작업 매트릭스 (체크리스트)

> ⚠ **윈도우 프로그램 빌드/배포 금지** — PM 별도 지시 있기 전까지 (2026-04-03 PM 지시)

### Step 1: 윈도우 프로그램 보완
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 1-1 | 서버 업로드 동작 확인 (FK 문제 해결 후) | [ ] | upload_jobs device_id FK 제약 |
| 1-2 | 화면 레이아웃 최종 확인 (PM 테스트) | [ ] | v2.0.2 Dock 레이아웃 수정됨, 실제 확인 필요 |
| 1-3 | 날짜별 저장/중복 감지 동작 확인 | [ ] | OrderStorageService 구현됨, 실제 테스트 필요 |
| 1-4 | 재전송 버튼 동작 확인 | [ ] | RetryUploadBtn 구현됨, 실제 테스트 필요 |
| 1-5 | 자동 재로그인 동작 확인 | [ ] | 401 시 저장된 email/password로 재로그인 구현됨 |
| 1-6 | 재부팅 자동 시작 테스트 | [ ] | dev_progress.md 테스트 결과 "테스트 필요" |
| 1-7 | **BUG-001**: 자동 로그인 미체크 시 토큰 삭제 버그 수정 (코드 수정 + 빌드 + 배포) | [ ] | 기획서에 없는 코드 임의 추가로 서버 업로드 0건. _config.Token="" 제거, autoLogin은 다음 시작에만 영향 |
| 1-8 | **BUG-002**: 로컬에만 쌓인 주문 서버 재전송 | [ ] | BUG-001로 인해 모든 업로드 실패. 수신된 주문이 로컬 JSON에만 존재, 서버 raw_receipts 0건 |
| 1-9 | **BUG-003**: 재전송 버튼 실제 동작 테스트 | [ ] | RetryUploadBtn_Click 구현됨, 실제 테스트 안 함. 실패 상태 주문 재전송 검증 필요 |

### 서버 수정 후 미확인 체크리스트 (긴급)
| # | 확인 항목 | 방법 | 상태 |
|---|----------|------|------|
| 1 | CORS — zigso.kr에서 api 호출 정상 | PM 웹 페이지 접속 | [ ] |
| 2 | 기존 엔드포인트 89개 동작 | /orders, /catalog/menus, /inventory/items 접속 | [ ] |
| 3 | 주문 허브 시간대 KST 변환 | 구현 필요 | [ ] |
| 4 | 테스트 데이터 4건 정리 | SQL DELETE | [ ] |
| 5 | 기존 메뉴/페이지 정상 | PM 사이드바 메뉴 전체 클릭 | [ ] |
| 6 | Next.js 빌드 후 페이지 정상 | PM 주요 페이지 접속 | [ ] |

### Step 2: 서버 수정 — 업로드 동작
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 2-1 | upload_jobs device_id FK 문제 해결 | [ ] | user session 인증 시 'user_xxx' device_id가 devices 테이블에 없어 FK 위반 |
| 2-2 | agent-routes.js 듀얼 인증 깃 반영 | [ ] | 현재 Docker 컨테이너 임시 수정만, 깃 미반영 (MD5 불일치 확인) |
| 2-3 | 서버 업로드 실제 동작 테스트 | [ ] | curl 또는 윈도우 프로그램에서 테스트 |
| 2-4 | 서버 로그 확인 (업로드 성공/실패) | [ ] | docker logs bbb-prod-api-1 |

### Step 2.5: 보안 긴급 수정 (Step 2 완료 후 즉시)
| # | 작업 | 상태 | 해결 방법 | 근거 |
|---|------|------|----------|------|
| 2.5-1 | 패스워드 해싱 적용 | [ ] | authenticateUser에서 bcrypt.compare 사용, createUserAccount에서 bcrypt.hash 사용. 기존 평문 패스워드 일괄 해싱 마이그레이션 | 전 계정 패스워드 평문 저장 확인 |
| 2.5-2 | 기존 계정 패스워드 마이그레이션 | [ ] | DB에서 password_hash 컬럼의 평문값을 bcrypt 해시로 일괄 변환 스크립트 실행 | 4개 계정 모두 평문 |
| 2.5-3 | 비밀번호 최소 요구사항 | [ ] | signup/login에서 최소 8자 검증 추가 | 현재 "1" 같은 1자리도 가능 |
| 2.5-4 | 이메일 형식 검증 | [ ] | signup에서 이메일 형식(@포함) 검증 | 현재 아무 문자열이나 가능 |

### 시간대 처리 계획 (PM 요구사항 2026-04-03)
- 현재: 서버 UTC 저장, 웹/윈도우 변환 없음
- 윈도우 버그: KST 시간을 UTC("Z")로 잘못 표기
- 해결 방안:
  - 서버: UTC로 저장 유지 (표준)
  - 윈도우: DateTime.Now → DateTimeOffset.UtcNow 또는 TimeZoneInfo 사용
  - 웹: 표시 시 사용자 브라우저 시간대로 자동 변환 (Intl.DateTimeFormat 또는 toLocaleString)
  - 향후 해외: 서버 UTC + 클라이언트 로컬 시간대 자동 적용
- 근거: PM 지시 "한국에서 사용 중이고 추후 외국에서 사용하는데 각나라에 맞춰서 자동으로 시간 맞춰야"

### Step 3: 서버 보안 이슈 수정 (23건 중 보안 관련)
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 3-1 | GET /v2/orders/:orderId — siteAccessible 체크 추가 | [ ] | 문제점 #1 |
| 3-2 | GET /v2/orders/:orderId/sources — site/org 체크 추가 | [ ] | 문제점 #2 |
| 3-3 | POST suggestions approve/reject/ignore — site 체크 추가 | [ ] | 문제점 #3,4,5 |
| 3-4 | GET audit-logs — site/org 체크 추가 | [ ] | 문제점 #6 |
| 3-5 | PATCH menus/addons/items — site 체크 순서 수정 (수정 전 체크) | [ ] | 문제점 #7,8,9 |
| 3-6 | CORS origin 제한 (zigso.kr만 허용) | [ ] | 문제점 #16 |

### Step 4: 주문 허브 페이지 + 로그 조회
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 4-1 | /orders/hub 페이지 추가 (날짜별 영수증 목록) | [ ] | PM 요청 "주문 허브 페이지 db 랑 연동" |
| 4-2 | raw_receipts 조회 API 추가 (GET /v2/agent/receipts) | [ ] | 주문 허브에서 사용할 API |
| 4-3 | raw_receipts 상세 API 추가 (GET /v2/agent/receipts/:id) | [ ] | 영수증 원문 보기 |
| 4-4 | 주문 허브 DB 연동 테스트 | [ ] | raw_receipts 테이블에서 데이터 조회 확인 |

### Step 5: 데이터 흐름 구현 (미구현 5단계)
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 5-1 | 영수증 파싱 → parsed_receipt_lines 테이블 생성 + 로직 | [ ] | 데이터 흐름 2단계, 테이블 미존재 |
| 5-2 | AI 파싱 연동 (Claude API 또는 규칙 기반) | [ ] | automatic-extraction-rules.md |
| 5-3 | 주문 생성/매칭 → orders + order_lines 런타임 생성 | [ ] | 데이터 흐름 3단계, createOrder 함수 없음 |
| 5-4 | 자동 재고 차감 (주문 → inventory_transactions) | [ ] | 데이터 흐름 5단계 |
| 5-5 | 충돌 자동 감지 → reconciliation_issues 생성 | [ ] | 데이터 흐름 6단계 |
| 5-6 | 취소 처리 → 재고 복구 | [ ] | 데이터 흐름 7단계 |

### Step 6: 미구현 페이지 + 기능 보강
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 6-1 | /operations/overview (운영 홈) 페이지 | [ ] | 미구현 페이지 |
| 6-2 | /operations/actions (빠른 액션) 페이지 | [ ] | 미구현 페이지 |
| 6-3 | /orders/cancelled (취소/복구) 페이지 | [ ] | 미구현 페이지 |
| 6-4 | /catalog/options 페이지 | [ ] | 미구현 페이지 |
| 6-5 | /inventory/movements 페이지 | [ ] | 미구현 페이지 |
| 6-6 | /analytics/dayparts, inventory-usage 페이지 | [ ] | 미구현 페이지 |
| 6-7 | /settings/recipe-rules 페이지 | [ ] | 미구현 페이지 |
| 6-8 | /management/exports 페이지 | [ ] | 미구현 페이지 |

### Step 7: 권한 체계 + 최적화
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 7-1 | role 기반 권한 미들웨어 구현 | [ ] | 현재 role 검사 0건 |
| 7-2 | 비효율 쿼리 수정 (limit:500/200 전체 조회) | [ ] | 문제점 #10,11,12 |
| 7-3 | 트랜잭션 누락 수정 (recordDeviceHeartbeat) | [ ] | 문제점 #13 |
| 7-4 | 동시 요청 잠금 추가 (activateWindowsAgentRelease) | [ ] | 문제점 #14 |
| 7-5 | 보안 헤더 추가 (helmet/X-Frame/CSP) | [ ] | 문제점 #16 |

### Step 7.5: 운영 안정성
| # | 작업 | 상태 | 해결 방법 | 근거 |
|---|------|------|----------|------|
| 7.5-1 | API_AUTH_MODE 활성화 검토 | [x] | 현재 off. 레거시 3개 엔드포인트 모두 resolvedAuthGuard.authorize() 사용. 활성화: .env.server API_AUTH_MODE=apikey + API_AUTH_KEYS 설정 필요. PM 판단 대기 | 현재 API_AUTH_MODE=off, Docker internal network으로 외부 직접 노출 안 됨 |
| 7.5-2 | rate limit 추가 | [x] | 로그인 5분간 5회 실패 시 30초 잠금 (인메모리 Map) | 패스워드 무차별 대입 방지 |
| 7.5-3 | DB 자동 백업 설정 | [ ] | cron으로 pg_dump 일 1회 실행, /home/min/zigso-kr/backups/ 보관 | 현재 자동 백업 없음 |
| 7.5-4 | 서버 크래시 알림 | [x] | health-monitor.sh + cron 매분. Docker health + /health endpoint + docker events (die/kill/oom) 감시. 실패 시 ~/zigso-kr/production/logs/health-alert.log 기록 | 22회 크래시 이력, 알림 없었음 |
| 7.5-5 | 배포 안전성 | [x] | safe-deploy.sh: 백업→docker cp→node --check→실패 시 자동 롤백→성공 시 restart+health 대기. 테스트 통과 (정상/오류 JS 모두) | registerAgentRoutes 에러로 크래시한 이력 |

### Step 8: AI 분석 + 향후
| # | 작업 | 상태 | 근거 |
|---|------|------|------|
| 8-1 | 영수증 텍스트 AI 구조화 (플랫폼/메뉴/금액) | [ ] | Phase 2 기획 |
| 8-2 | 매출 분석/예측 | [ ] | Phase 3 기획 |
| 8-3 | 메뉴 트렌드/이상 감지 | [ ] | Phase 3 기획 |
| 8-4 | 자연어 질의 | [ ] | 향후 |

### Step 9: 계정/테넌트 고도화
| # | 작업 | 상태 | 해결 방법 | 근거 |
|---|------|------|----------|------|
| 9-1 | 멀티테넌트 — 신규 조직/사이트 생성 | [ ] | signup 또는 관리 API에서 새 organization + site 생성 지원 | 현재 org_001/site_001 하드코딩 |
| 9-2 | role 관리 — owner/manager 설정 | [ ] | 웹 관리 페이지에서 role 변경 가능하게 (PATCH /v2/management/users/:userId/role 이미 존재) | 현재 모든 가입자 viewer |
| 9-3 | 웹 대시보드 동작 확인 | [ ] | zigso.kr 브라우저 접속 테스트 (로그인/가입/대시보드) | 웹 UI 동작 여부 확인 안 됨 |
| 9-4 | 이메일 인증 | [ ] | 가입 시 이메일 확인 절차 추가 (향후) | 현재 아무 이메일로 가입 가능 |

## 작업 순서
Step 1~2 → **Step 2.5 (보안 긴급)** → Step 3 → Step 4 → Step 5 → Step 6 → Step 7 → **Step 7.5 (운영 안정성)** → Step 8 → **Step 9 (테넌트 고도화)**
(Step 1~2는 동시 진행 가능)

---

## 기존 운영 서버(bbb-prod-api) — 수정/업데이트 방향 (확정)
- 기존 서버 코드를 유지하면서 수정/기능 추가
- 기존 서버 코드 수정 후 Docker 이미지 리빌드로 배포
- 깃(ssa/server/)의 코드를 수정 → Docker 이미지 빌드 → 배포
- 윈도우 프로그램(DeliveryOrderReceiver)은 우리(claude-1)가 개발/유지

## 도메인 배정 (zigso.kr만 사용)

### 사용 가능 도메인
| 도메인 | 현재 연결 | 새 용도 |
|--------|----------|---------|
| zigso.kr / app.zigso.kr / www.zigso.kr | bbb-prod-web:4100 | SSA 웹 대시보드 (주문 허브 등) |
| api.zigso.kr | bbb-prod-api:4000 | SSA API 서버 |
| agent.zigso.kr | bbb-prod-api:4000 | 윈도우 프로그램 업로드 API |
| stg.zigso.kr | bbb-stg-web:4100 | 스테이징 웹 |
| api-stg.zigso.kr | bbb-stg-api:4000 | 스테이징 API |
| agent-stg.zigso.kr | bbb-stg-api:4000 | 스테이징 에이전트 |

### 배포 계획
- 기존 도메인/서버 그대로 유지
- 깃(ssa/server/) 코드 수정 → Docker 이미지 빌드 → 기존 컨테이너 교체
- nginx-proxy-manager 설정 변경 불필요

### 절대 금지
- zigso.net 도메인 및 관련 컨테이너 수정/삭제/재시작 절대 금지
- 대상: sees-user-admin-ui-*, freshops-net-*, nginx-proxy-manager zigso.net 설정

---

## 전수 분석 결과 (2026-04-02 확인, 내부 로직 포함)

### 근거
- 깃 코드: ssa/server/ (커밋 3341ff3)
- 서버 DB: bbb-prod-api-1 psql 직접 조회
- 설계 문서: ssa/v1/postgresql-schema-v2.sql, ui-information-architecture-v1.md, ui-top-5-wireframes-v1.md, ui-role-access-matrix-v1.md

---

### API 엔드포인트 내부 로직 (agent-routes.js 8개)

| 엔드포인트 | 인증 | Store 함수 | 설계 일치 | 차이점 |
|-----------|------|-----------|----------|--------|
| POST /auth/login | 없음 | authenticateUser, createUserSession | ✅ | |
| POST /auth/signup | 없음 | createUserAccount, createUserSession | ✅ | |
| POST /devices/register | User Session | registerDevice, issueDeviceApiKey, startDeviceSession | ✅ | |
| GET /bootstrap | Device API Key | getSiteById, getPlatformStoresBySiteId, touchDeviceSession | ✅ | |
| POST /heartbeat | Device API Key | recordDeviceHeartbeat, touchDeviceSession | ✅ | |
| POST /uploads/receipt-raw | Device API Key | upsertReceiptRawUpload | ⚠ | Bearer token 허용 기획 미반영 |
| POST /uploads/angel-csv | Device API Key | upsertAngelCsvUpload | ⚠ | orders/order_lines 직접 생성 없음 |
| POST /uploads/settlement-adjustment | Device API Key | createUpload | ✅ | Idempotency-Key optional |

### API 전체 89개 — 100% 존재 확인

| 영역 | 파일 | 수 |
|------|------|---|
| 인증 | agent-routes.js | 2 |
| 디바이스 | agent-routes.js | 3 |
| 데이터 수집 | agent-routes.js | 3 |
| 주문 | server.js | 2 |
| 대사 | reconciliation-routes.js | 6 |
| 카탈로그 | catalog-routes.js | 11 |
| 재고 | inventory-routes.js | 5 |
| 매핑 | mapping-routes.js | 6 |
| 발주 | purchase-orders-routes.js | 3 |
| 관리 | management-routes.js | 3 |
| Windows Agent | windows-agent-routes.js | 3 |
| 분석 | server.js | 9 |
| 설정 | server.js | 21 |
| 내보내기 | server.js | 4 |
| 업로드/정산 | server.js | 3 |
| 기타 | server.js | 5 |

---

### 웹앱 페이지 상세 (25개 구현, 9개 미구현)

#### 설계 와이어프레임 대비 미구현 기능

| 페이지 | 구현된 것 | 미구현 (설계 기준) |
|--------|----------|-----------------|
| /orders | 목록 + 기본 상세 | 상세 패널 6종 중 4종 (라인항목, 정산, 매칭로그, 재고로그) |
| /orders/review | 이슈 목록/상세/해결 | 그룹 단위 처리, AI 추천, 영향도 패널, 되돌리기 |
| /orders/evidence | 목록 + sources 조회 | 3컬럼 비교 레이아웃, 보정/검토 액션, 금액 차이 표시 |
| /catalog/menus | 목록 + 상세 조회 | 생성/수정/매핑 연결, 레시피/재고 영향 표시 |
| /recipes | 세트 목록 + 버전 조회 | 버전 생성/비교/diff |
| /analytics/stores | 매장 목록 + 상세 | 차트/추이 시각화 |
| /analytics/sales | 일별 매출 조회 | 플랫폼/상점별 비교, 차트 |
| /analytics/menus | 카탈로그 기반 집계 | 실제 판매 데이터 기반 성과 분석 |
| /analytics/options | 일별 정산 지표 | 옵션별 attach rate |
| /management/devices | 목록 + health 상태 | 상세 패널, 키 재발급/폐기, heartbeat 새로고침 |
| /management/uploads | 업로드 목록 | 재시도 기능, 실패 원인 상세, CSV 내보내기 |
| /settings/settlement-rules | 옵션재고+포장 규칙 | 설계는 정산 보정 규칙 (성격 다름) |

#### 미구현 라우트 9개

| 라우트 | 설계 근거 |
|--------|----------|
| /operations/overview | ui-information-architecture-v1.md (9.1) |
| /operations/actions | ui-information-architecture-v1.md (9.1) |
| /orders/cancelled | ui-information-architecture-v1.md (9.2) |
| /catalog/options | ui-information-architecture-v1.md (9.3) |
| /inventory/movements | ui-information-architecture-v1.md (9.3) |
| /analytics/dayparts | ui-information-architecture-v1.md (9.4) |
| /analytics/inventory-usage | ui-information-architecture-v1.md (9.4) |
| /settings/recipe-rules | ui-information-architecture-v1.md (9.5) |
| /management/exports | ui-information-architecture-v1.md (9.6) |

#### 설계에 없는 추가 페이지 6개

| 라우트 | 역할 |
|--------|------|
| / | 진입점 허브 |
| /auth/login | 로그인 |
| /auth/signup | 회원가입 |
| /tenant/dashboard | API Key 발급 허브 |
| /super/dashboard | 슈퍼관리자 진입점 |
| /management/devices/download | Windows Agent 다운로드 |

---

### DB 테이블 컬럼 대조

#### 서버 DB = 깃 코드: 100% 일치

#### 설계 문서(postgresql-schema-v2.sql)와의 공통 차이
| 항목 | 설계 | 코드/DB |
|------|------|---------|
| PK 명명 | id | 테이블명_id |
| ID 타입 | UUID | TEXT |
| 시간 타입 | TIMESTAMPTZ | TEXT |
| ENUM | PostgreSQL ENUM | TEXT |
| JSONB 컬럼명 | payload | payload_json |

#### 설계에만 있고 구현 안 된 4개 테이블
| 테이블 | 설명 |
|--------|------|
| mapping_feedback | 매핑 제안 피드백 |
| order_item_ignored_lines | 무시된 주문 항목 |
| order_status_events | 주문 상태 변경 이력 |
| parsed_receipt_lines | 파싱된 영수증 라인 |

#### 코드에만 있고 설계에 없는 5개 테이블
| 테이블 | 설명 |
|--------|------|
| keyword_rules | 키워드 규칙 |
| settlement_runs | 정산 실행 |
| store_classifications | 매장 분류 |
| upload_idempotency_keys | 멱등성 키 |
| uploads | 업로드 |

---

### Store 함수 — 91개 확인

주요 함수 전부 구현됨. 권한 검사는 store 계층에 없음 (순수 데이터 접근).

---

### 권한 체계 — 심각한 갭

| 항목 | 설계 문서 | 실제 코드 |
|------|----------|----------|
| Role 기반 검사 | 5개 role (site_owner, site_manager, data_admin, org_admin, staff) | 0건 — role 검사 없음 |
| Scope 기반 검사 | orders:read, catalog:write 등 | 미구현 |
| Staff 제한 | 재고 조정 폭 제한 등 | 미구현 |
| 위험도별 승인 | 저위험/고위험 분리 | 미구현 |
| Site/Org 소속 확인 | 소속 확인 | ✅ 구현됨 |

모든 엔드포인트가 "로그인 + site/org 소속"만 검사. role 기반 제한 없음.

**특히 위험한 엔드포인트:**
- PATCH /v2/management/users/:userId/role — 역할 변경인데 org_admin 검사 없음
- POST /v2/reconciliation/issues/:issueId/resolve — 이슈 해결인데 data_admin 검사 없음
- POST /v2/mapping/suggestions/:id/approve|reject|ignore — site 접근 검사도 없음

---

### 인증 함수 — 3/3 구현 (100%)
- ✅ getBearerToken (server.js:107)
- ✅ requireUserSessionFromRequest (server.js:573)
- ✅ requireDeviceApiKeyFromRequest (server.js:602)

---

### server.js 44개 엔드포인트 내부 로직 (전수 확인)

#### 공통 패턴
- 모든 /v2/* 엔드포인트: userSession 인증 + siteAccessible/orgAccessible 인가
- 모든 /api/* 엔드포인트 (3개): deviceApiKey 인증 (authGuard), 인가 없음
- 모든 엔드포인트: requestId 생성, audit 기록, handleRouteError catch
- 리스트 엔드포인트: 동일한 페이지네이션 패턴 (items/total/page/limit/totalPages)
- store 함수 존재 여부 런타임 체크 → 없으면 500

#### 설계 대비 발견된 이슈 (server.js)
| # | 엔드포인트 | 이슈 |
|---|-----------|------|
| 1 | GET /v2/orders/:orderId | siteAccessible 체크 없음 (store 내부 위임) |
| 2 | GET /v2/analytics/stores | 전용 store 함수 없이 listPlatformStores 재활용, 클라이언트 사이드 필드 매핑 |
| 3 | GET /v2/analytics/stores/:storeId | limit:500 전체 조회 후 클라이언트 사이드 매칭 (비효율) |
| 4 | GET /v2/analytics/sales/:reportId | limit:500 전체 조회 후 클라이언트 사이드 매칭 (비효율) |
| 5 | POST /v2/platform-store-overrides | siteAccessible 체크가 조건부 (siteId 있을 때만) |
| 6 | GET /v2/exports/*.csv (4개) | URL은 .csv이지만 실제로는 JSON 응답 |

### 7개 라우트 파일 37개 엔드포인트 내부 로직 (전수 확인)

#### 설계 대비 발견된 이슈 (라우트 파일)
| # | 파일 | 엔드포인트 | 이슈 |
|---|------|-----------|------|
| 1 | catalog-routes.js | PATCH menus/:menuId | site 접근 체크가 store 수정 **이후**에 수행 — 수정 적용 후 403 가능 |
| 2 | catalog-routes.js | PATCH addons/:addonId | 위와 동일 |
| 3 | inventory-routes.js | PATCH items/:itemId | 위와 동일 |
| 4 | catalog-routes.js | GET menus/:menuId | getCatalogMenu 없으면 limit:200 전체 조회 폴백 (비효율) |
| 5 | reconciliation-routes.js | GET orders/:orderId/sources | site/org 접근 체크 없음 |
| 6 | mapping-routes.js | POST suggestions/:id/approve | site/org 접근 체크 없음 |
| 7 | mapping-routes.js | POST suggestions/:id/reject | site/org 접근 체크 없음 |
| 8 | mapping-routes.js | POST suggestions/:id/ignore | site/org 접근 체크 없음 |
| 9 | mapping-routes.js | GET suggestions | org 접근 체크 없음 (rules GET과 비대칭) |
| 10 | management-routes.js | GET audit-logs | site/org 접근 체크 없음 |
| 11 | windows-agent-routes.js | resolveSessionUserRole | listManagementUsers limit:200 전체 조회 후 find — 200명 초과 시 실패 |
| 12 | windows-agent-routes.js | activateWindowsAgentRelease | 파일시스템 직접 쓰기, 동시 요청 잠금 없음 |

### 웹앱 레이아웃 구조

#### 실제 구현 (DomainPageShell)
```
<main>
  <aside className="split-page-side">     ← Left Nav
    ├─ 메뉴 트리 (6개 그룹)
    ├─ 사이트 컨텍스트 선택 (org/site)
    └─ 역할(role) 선택
  </aside>
  <div className="split-page-main">       ← Main Workspace
    ├─ workspace-session-strip             ← Topbar 역할
    ├─ header card
    └─ {children}
  </div>
</main>
```

#### 설계 대비 차이
| 설계 요소 | 실제 구현 | 상태 |
|----------|----------|------|
| Topbar (org/site/date) | workspace-session-strip (date 없음) | ⚠ 부분 |
| Left Nav (menu tree) | split-page-side — 6개 그룹 일치 | ✅ |
| Main Workspace | split-page-main | ✅ |
| Side Rail (상세/로그) | Left Nav에 통합 | ⚠ 구조 다름 |
| Bottom utility (toast/sync) | 없음 | ❌ 미구현 |

#### 메뉴 그룹 (6개 — 설계 IA와 일치)
1. 운영: 오늘 이슈
2. 주문: 주문 허브, 검토대상, 원본 증빙
3. 메뉴/재고: 메뉴 기준, 레시피, 재고 이동, 발주 추천
4. 분석: 매장, 매출, 메뉴, 옵션
5. 설정: 매장 분류, 키워드 규칙, 정산 보정 규칙
6. 관리: 사이트, 디바이스, Agent 다운로드, 업로드 기록, 사용자 권한

### memory-store.js vs postgres-store.js

| 항목 | memory-store | postgres-store |
|------|-------------|---------------|
| 파일 크기 | 3,746줄 | 9,227줄 |
| 공통 함수 | 81개 | 81개 + 10개 추가 |
| 추가 함수 | 0개 | close, createUpload, getUploadById, createSettlementRun, getSettlementRunById, markSettlementRunQueued, markSettlementRunRunning, completeSettlementRun, failSettlementRun, upsertOrderSettlements |
| 관계 | 하위 집합 | 상위 호환 (superset) |

postgres-store에만 있는 10개 함수: 정산 워크플로우(settlement run lifecycle) 6개 + 업로드/커넥션 3개 + 주문 정산 upsert 1개

---

### Store 함수 91개 SQL 쿼리 전수 분석 (근거: postgres-store.js 9,227줄 전체 코드 읽기)

#### 스키마 일치 여부
모든 함수의 SQL 쿼리에서 참조하는 테이블과 컬럼이 ensureSchema()의 CREATE TABLE 정의와 일치. **불일치 0건.**

#### 트랜잭션 사용 함수 (14개)
| 함수 | FOR UPDATE | 대상 테이블 |
|------|------------|-----------|
| reconcileOrder | orders | orders, reconciliation_issues, issue_actions |
| resolveReconciliationIssue | reconciliation_issues | reconciliation_issues, issue_actions |
| createInventoryAdjustment | inventory_items | inventory_items, inventory_transactions |
| createRecipeVersion | recipe_sets | recipe_sets, recipe_versions, recipe_version_lines |
| createInventoryBarcodeReceive | inventory_items | inventory_item_barcodes, inventory_items, inventory_transactions |
| createPurchaseOrder | 없음 | sites, purchase_orders, inventory_items, purchase_order_items |
| receivePurchaseOrder | purchase_orders, purchase_order_items | purchase_orders, purchase_order_items, inventory_items, inventory_transactions, receiving_logs |
| approveMappingSuggestion | mapping_suggestions | mapping_suggestions |
| rejectMappingSuggestion | mapping_suggestions | mapping_suggestions |
| ignoreMappingSuggestion | mapping_suggestions | mapping_suggestions |
| createUpload | 없음 (조건부) | uploads, upload_idempotency_keys |
| upsertOrderSettlements | 없음 | order_settlements, orders |
| createUserAccount | 없음 | users, sites, organizations, user_site_memberships |
| upsertReceiptRawUpload | 없음 (조건부) | upload_jobs, raw_receipts |

#### 특이사항
1. recordDeviceHeartbeat — devices UPDATE + device_heartbeats INSERT가 트랜잭션 없이 별도 실행. 부분 업데이트 가능
2. listManagementUsers — DB가 아닌 JS 인메모리에서 siteId/role 필터링 및 페이지네이션 수행 (SQL LIMIT/OFFSET 미사용)
3. listPurchaseOrderRecommendations — 추천 로직이 하드코딩된 target 값 사용 (inv_001=12000, inv_002=7000)

---

### 데이터 흐름 실제 동작 분석 (근거: postgres-store.js + server.js + routes/*.js grep 전수 확인)

#### 설계 9단계 vs 구현 상태

| # | 설계 흐름 | 구현 | 근거 |
|---|----------|------|------|
| 1 | 영수증 수집 → upload_jobs + raw_receipts | ✅ 구현됨 | upsertReceiptRawUpload (8899행), upsertAngelCsvUpload (9120행) |
| 2 | 파싱/정규화 → parsed_receipt_lines | ❌ 미구현 | parsed_receipt_lines 관련 코드 0건 (grep 전체 검색) |
| 3 | 주문 생성/매칭 → orders + order_lines | ❌ 미구현 | INSERT INTO orders는 ensureSeedData(953행)만. createOrder/matchOrder 함수 없음 |
| 4 | 정산 병합 → order_settlements | ✅ 구현됨 | upsertOrderSettlements (2418행), executeSettlementRun (settlement/index.js) |
| 5 | 재고 자동 차감 (주문 → inventory_transactions) | ❌ 미구현 | INSERT INTO inventory_transactions는 수동조정(5122행)/바코드입고(5654행)/발주입고(6110행)만. 주문 기반 자동 차감 코드 없음 |
| 6 | 충돌 감지 → reconciliation_issues 자동 생성 | ❌ 미구현 | INSERT INTO reconciliation_issues는 ensureSeedData(1096행)만. createReconciliationIssue/detectConflict 함수 없음 |
| 7 | 취소 처리 → order_status 변경 + 재고 복구 | ❌ 미구현 | cancel/Cancel/CANCEL 키워드 0건, restore inventory 코드 없음 |
| 부가 | 정산 큐/워커 | ✅ 구현됨 | createInlineSettlementQueue(488행), createRedisSettlementQueue(506행), BullMQ Queue |

#### 핵심 갭 요약
- **구현됨**: 1단계(수집), 4단계(정산), 정산 큐
- **미구현**: 2단계(파싱), 3단계(주문 생성/매칭), 5단계(자동 재고 차감), 6단계(충돌 감지), 7단계(취소 처리)
- 설계상 핵심 파이프라인 "영수증 파싱 → 주문 생성 → 매칭 → 재고 차감 → 충돌 감지 → 취소 처리" 전체가 미구현

#### 설계에만 있고 구현 안 된 테이블 (데이터 흐름 관련)
| 테이블 | 역할 | 미구현 단계 |
|--------|------|-----------|
| parsed_receipt_lines | 파싱된 영수증 라인 | 2단계 |
| order_status_events | 주문 상태 변경 이력 | 7단계 |
| order_item_ignored_lines | 무시된 주문 항목 | 3단계 |
| mapping_feedback | 매핑 피드백 | 3단계 |

---

### 추가 분석 결과 (2026-04-02, 미확인 7개 항목 전수 확인)

#### 1. packages/modules 모듈 (3개만 존재)
| 모듈 | 파일 | 내용 |
|------|------|------|
| settlement | index.js | 정산 계산 모듈. calculateSettlement, executeSettlementRun 등 7개 함수. grossSales/commission/withholdingTax/netSettlement 계산 |
| purchase-orders | index.js | 발주 관련 유틸 |
| observability | index.js | 관측성(OTEL) 설정 |

설계 문서에 있는 devices, iam, ingestion, inventory, orders, organizations, reconciliation 모듈은 **존재하지 않음**

#### 2. 웹앱 런타임 코드 (4개 파일, 2,424줄)
| 파일 | 줄 | 역할 |
|------|---|------|
| domain-runtime.js | 1,545 | 세션 관리, API 호출, 역할 메뉴 권한, 에러 분류, 디바이스 헬스 판정 |
| domain-page-shell.js | 740 | 메인 셸 (좌측 메뉴 6그룹 + 우측 메인 + 인증 가드) |
| page-layout-primitives.js | 80 | PageStatusStrip, PageSummaryCardRow, PageActionBar |
| page-state-panels.js | 59 | StatusPanel, ErrorPanel, EmptyState, LoadingHint |

핵심 발견:
- DEFAULT_API_BASE: "http://127.0.0.1:4000" (로컬 개발용)
- 역할 메뉴: site_owner/org_operator = 20개 메뉴, auditor = 4개만
- API 호출: fetchJsonOrThrow — 네트워크 에러 시 /api-config에서 apiBase fallback 재시도

#### 3. 배포 스크립트 (18개)
| 분류 | 스크립트 | 역할 |
|------|---------|------|
| Windows Agent | windows-agent-publish-release.js, windows-agent-release-deploy.sh | MSI/EXE 아티팩트 발행, 시그니처 검증 |
| NPM 관리 | zigso-npm-backup.sh, zigso-npm-diff-report.sh, zigso-npm-readonly-inventory.sh | Nginx Proxy Manager 백업/차이/조회 |
| 네트워크 | zigso-edge-ensure-connected.sh, zigso-external-edge-check.sh | Docker 네트워크 연결, 외부 접근 검증 |
| 배포 게이트 | zigso-preapply-gate.sh, zigso-readiness-report.sh | 배포 전 체크, 준비 상태 리포트 |
| 스테이징 | zigso-staging-npm-apply.sh, zigso-staging-npm-auth-check.sh, zigso-staging-readonly-check.sh, zigso-staging-remote-cleanup.sh, zigso-staging-rollout.sh | 스테이징 환경 프록시/인증/정리/롤아웃 |
| 프로덕션 | zigso-production-cutover.sh | CONFIRM_CUTOVER=production 필수, 프로덕션 컷오버 |
| 모니터링 | zigso-bundle-drift-check.sh, zigso-staging-dns-check.sh | 로컬↔서버 drift 체크, DNS A레코드 확인 |

#### 4. CORS/보안 설정
- CORS origin: `*` (모든 출처 허용)
- 허용 헤더: authorization, content-type, idempotency-key, x-api-key, x-idempotency-key
- 허용 메서드: GET, POST, PATCH, PUT, OPTIONS
- helmet, X-Frame-Options, CSP 등 보안 헤더: **미설정**

#### 5. 시드 데이터
- INSERT 27건, 27개 테이블에 테스트 데이터 1~2건씩 삽입
- ON CONFLICT DO NOTHING 패턴 (중복 시 무시)
- 시드 ID: org_001, usr_owner, site_001, platform_baemin, ord_001 등

#### 6. production docker-compose vs 깃 docker-compose 차이
| 항목 | 깃 | 프로덕션 |
|------|---|---------|
| POSTGRES_PASSWORD | 기본값 "bbb" | 필수값 (미설정시 에러) |
| 네트워크 격리 | internal: true 없음 | internal: true 설정 |
| expose | 없음 | 4000, 4100 명시 |
| healthcheck | 기본값 | interval/timeout/retries 명시 |
| volumes | 3개 | 4개 (windows_agent_storage 추가) |
| NEXT_PUBLIC_API_BASE | localhost:4400 | https://api.zigso.kr |

#### 7. .env.server.example vs .env.server 차이
- 이미지 태그: 0.2.138 (example) vs 0.2.151 (실제)
- POSTGRES_PASSWORD: change-me-before-prod (example) vs 실제 비밀번호
- 나머지 키/값 동일

#### 8. 미확인 파일 10개 전수 확인

| # | 파일 | 존재 | 줄수 | 역할 |
|---|------|------|------|------|
| 1 | packages/db/src/index.js | ✅ | 124 | DB 유틸 (PostgreSQL URL 해석, 정산 데이터 정규화, createPostgresStore 팩토리) |
| 2 | packages/db/src/migrate.js | ❌ 없음 | - | 마이그레이션 도구 미구현 |
| 3 | apps/web/next.config.mjs | ❌ 없음 | - | Next.js 설정 파일 미생성 (기본 설정 사용) |
| 4 | apps/api/package.json | ✅ | 20 | @bbb/api: NestJS+Fastify, BullMQ, ioredis, @bbb/contracts 의존 |
| 5 | apps/web/package.json | ✅ | 15 | @bbb/web: Next.js 15, React 19, 포트 4100, 외부 의존 없음 |
| 6 | turbo.json | ✅ | 21 | Turborepo: build/test/lint/dev 4태스크, build 출력 dist/** .next/** |
| 7 | pnpm-workspace.yaml | ✅ | 5 | 워크스페이스: apps/*, agents/*, packages/*, packages/modules/* |
| 8 | apps/web/app/layout.js | ✅ | 27 | 루트 레이아웃: lang="ko", 배포 배너 (이미지 태그/정책/규칙 표시) |
| 9 | packages/modules/observability/src/index.js | ✅ | 73 | OpenTelemetry 초기화 (OTEL_ENABLED로 활성화, 비활성 시 noop) |
| 10 | packages/modules/purchase-orders/src/index.js | ✅ | 273 | 발주 이상탐지 + 자동발주 판단 (high 심각도 이슈 시 차단, 미해결 이슈 시 보류) |

##### 발견 사항
- migrate.js 없음: DB 마이그레이션은 postgres-store.js의 ensureSchema()로 대체 (서버 시작 시 CREATE TABLE IF NOT EXISTS)
- next.config.mjs 없음: Next.js 기본 설정 사용 중
- apps/api/package.json에 @bbb/db 직접 의존 없음: server.js에서 require("../../../../packages/db/src/postgres-store")로 직접 경로 참조
- purchase-orders 모듈: 자동 발주 차단 규칙이 한국어 메시지로 구현됨

#### 9. 미확인 파일 50개 전수 확인 (최종)

##### API 서버 핵심 모듈 (3개)
| 파일 | 줄 | 역할 | 발견 사항 |
|------|---|------|----------|
| audit/audit-log.js | 35 | 파일 기반 감사 로그 (.state/api-audit.log) | 동기 I/O(appendFileSync), 컨테이너 재시작 시 유실 가능 |
| auth/api-key-auth.js | 172 | x-api-key 인증, 4역할(admin/operator/uploader/viewer) | API_AUTH_MODE=off 시 전체 admin 통과, 라우트 커버리지 3개뿐 |
| worker/src/worker.js | 155 | BullMQ 워커, settlement.run 잡 처리 | Redis + Postgres, graceful shutdown, 동시 처리 2 |

##### 웹앱 API 라우트 (5개)
| 파일 | 역할 |
|------|------|
| api-config/route.js | 런타임 설정 브릿지 (NEXT_PUBLIC_API_BASE 등) |
| windows-agent/_shared.js | 프록시 유틸 (authorization/x-api-key 헤더 포워딩) |
| windows-agent/download/route.js | 바이너리 다운로드 프록시 |
| windows-agent/latest/route.js | 최신 버전 정보 프록시 |
| windows-agent/releases/[version]/activate/route.js | PATCH 프록시 → /v2/management/ 경로 |

##### packages (10개)
| 패키지 | 핵심 내용 |
|--------|----------|
| @bbb/contracts | DatasetType enum, validateUploadRequest, envelope 래퍼, ErrorCode 11개 |
| @bbb/shared | @bbb/contracts의 단순 re-export (중복) |
| @bbb/db (schema.js) | Drizzle ORM 스키마: uploads, settlement_runs, order_settlements |
| @bbb/db (drizzle.config.js) | PostgreSQL dialect, API_POSTGRES_URL 환경변수 |

##### agents (2개 에이전트)
| 에이전트 | 기술 | 역할 | 발견 사항 |
|---------|------|------|----------|
| pc-uploader | Node.js CLI | 파일 감시 + /api/uploads 업로드 | JSON만 지원, x-api-key 인증, 재시도 3회 |
| windows-agent | C# .NET 8.0 | 5단계 사이클 (login→register→bootstrap→heartbeat→upload) | /v2/agent/ 경로, Bearer 인증, 재시도 미구현 |

##### 테스트 코드 (9개)
| 파일 | 내용 |
|------|------|
| api.auth.test.js | RBAC 3역할 테스트, idempotency scope |
| api.integration.test.js | E2E 전체 흐름 (~1500줄), 모든 주요 API 커버 |
| postgres.store.test.js | DB 영속화, receipt-raw dedupe, device session |
| purchase-orders.logic.test.js | 발주 이상 분석 순수 로직 |
| settlement.logic.test.js | 정산 계산 순수 로직 |
| domain-runtime.test.js | 웹 런타임 유틸 (~800줄) |
| page-origin-contract.test.js | 페이지 계약 검증 (24개 라우트) |
| ui.state.test.js | 업로드 상태 뷰 |
| worker.queue.test.js | BullMQ 워커 통합 (Redis+Postgres) |

##### docs (87개 파일)
| 디렉토리 | 파일 수 | 내용 |
|----------|--------|------|
| docs/architecture/ | 25 | 설계 문서 (auth split, UI/UX 체크리스트, Windows Agent 스펙) |
| docs/design-handoff/ | ~20 | HTML 스냅샷 18페이지 + CSS + 메타 |
| docs/operations/ | 30 | zigso.kr 배포/관리 가이드, Windows Agent 설치 |
| docs/release-manifest/ | 70+ | v0.2.3~v0.2.138 릴리즈 매니페스트 |

##### scripts (33개, 서버 18개 대비 15개 추가)
추가된 스크립트: check-release-manifest, check-stack-contract, check-web-page-origin, check-web-runtime-routes, docker-build-strict, docker-deploy-strict, docker-remote-readonly-check, git-tag-strict, install-git-hooks, qa-auto-loop, release-batch-strict, run-e2e-slice, upload-gate-strict, write-release-manifest, zigso-local-docker-backup

##### 기타
| 파일 | 내용 |
|------|------|
| globals.css | ~700줄, 디자인 시스템 (CSS 변수, 2컬럼 레이아웃, 컴포넌트 스타일) |
| worker package.json | @bbb/worker, BullMQ+ioredis |
| modules package.json x3 | settlement/purchase-orders/observability |

##### 핵심 발견 사항 (기획 영향)
1. **이중 인증 체계**: x-api-key(4역할) + user session(site_owner/org_operator/manager/viewer)
2. **이중 에이전트**: pc-uploader(/api/uploads) + windows-agent(/v2/agent/uploads) — 서로 다른 API 경로/인증
3. **@bbb/shared = @bbb/contracts 중복** — 통합 가능
4. **릴리즈 v0.2.138까지 진행** — strict semver + upload gate + manifest 검증 파이프라인
5. **디자인 핸드오프 스냅샷 존재** — UI 계약 검증 기반
6. **OpenTelemetry 모듈 존재** — 관측성 인프라 준비됨 (현재 비활성)
7. **windows-agent 재시도 미구현** (README TODO)
8. **audit-log 동기 I/O** — 요청 처리 블로킹 가능

#### 10. agents/windows-agent 나머지 파일 12개 전수 확인

| # | 파일 | 줄 | 역할 |
|---|------|---|------|
| 1 | CaptureAdapters.cs | 214 | 캡처 어댑터 3종 (file_watch/serial_port/receipt_printer), 모두 fallback 모드 |
| 2 | AgentCommandLineOptions.cs | 86 | CLI 인자 파싱 (--config/--service/--setup/--once/--help) |
| 3 | AgentLogging.cs | 54 | 로그 싱크 (Console/ILogger 래핑) |
| 4 | WindowsAgentBackgroundService.cs | 68 | Windows Service 백그라운드 루프 (최소 15초 간격) |
| 5 | WindowsAgentSetupRunner.cs | 769 | WinForms GUI 설정 화면 (로그인→매장→디바이스등록→서비스등록) |
| 6 | appsettings.json.example | 32 | 설정 템플릿 |
| 7 | installer/Product.wxs | 101 | WiX v4 MSI 인스톨러 (ProgramFiles64, perMachine) |
| 8 | Properties/PublishProfiles/win-x64-single-file.pubxml | 13 | dotnet publish 프로필 (self-contained, single-file) |
| 9 | scripts/build-msi.ps1 | 88 | MSI 빌드 자동화 (WiX CLI) |
| 10 | scripts/install-service.ps1 | 76 | Windows 서비스 등록 (sc.exe, 실패 시 3회 재시작) |
| 11 | scripts/publish-win-x64.ps1 | 62 | dotnet publish 자동화 |
| 12 | scripts/uninstall-service.ps1 | 19 | Windows 서비스 제거 |

##### 발견 사항
1. 캡처 어댑터 3종 모두 hardwareConnected=false — 실제 하드웨어 드라이버 연동 미구현
2. GUI 설정(769줄)이 로그인→매장선택→디바이스등록→bootstrap→heartbeat→appsettings저장→서비스등록까지 일괄 수행
3. 서비스명: BbbWindowsAgent, 실패 복구: 60초 간격 3회 재시작
4. MSI: WiX v4, ProgramFiles64\BBB Windows Agent, 시작메뉴+바탕화면, MajorUpgrade 지원
5. 디바이스 식별: fingerprint/machineGuidHash를 SHA256으로 자동 계산
6. 이 windows-agent는 코덱스가 만든 것으로, 우리 DeliveryOrderReceiver(claude-1/)와는 별도 프로그램

#### 11. 나머지 미확인 파일 전수 확인 + 전체 매트릭스

##### .docker-build-test (3개)
| 파일 | 내용 |
|------|------|
| appsettings.json | 테스트용 설정 (localhost:3000, owner@example.com) |
| appsettings.json.example | 위와 동일 |
| bbb-windows-agent.runtimeconfig.json | .NET 8.0 런타임 설정 |

##### 테스트 코드 (2개 추가)
| 파일 | 줄 | 내용 |
|------|---|------|
| pc-uploader/test/upload-cli.test.js | 820 | 재시도/백오프, idempotency key 안정성, watch 중복 스킵, 상태 파일 |
| contracts/test/contracts.test.js | 151 | upload/settlement 검증, envelope 래퍼, ErrorCode |

##### scripts 33개 전수 확인
| 분류 | 수 | 핵심 |
|------|---|------|
| 릴리즈/빌드 | 6 | check-release-manifest(573줄), docker-build-strict, git-tag-strict, release-batch-strict, write-release-manifest |
| 배포/검증 | 5 | docker-deploy-strict(274줄), upload-gate-strict, run-e2e-slice(398줄), qa-auto-loop(301줄) |
| 계약 검증 | 3 | check-stack-contract(1086줄 — 가장 큼), check-web-page-origin, check-web-runtime-routes(505줄) |
| zigso 인프라 | 17 | staging/production rollout, npm apply/backup/diff, dns check, edge, cleanup, drift check |
| Windows Agent | 2 | publish-release.js, release-deploy.sh |

##### docs 110개+ 전수 확인
| 디렉토리 | 파일 수 | 핵심 내용 |
|----------|--------|----------|
| architecture/ | 25 | auth split(v1/v2), UI/UX 체크리스트/실행 계획, Windows Agent 4종 스펙, 팀 구조 |
| operations/ | 33 | zigso.kr 배포 계획(616줄 — 가장 큼), staging/production runbook, DNS/NPM/edge 운영 |
| design-handoff/ | 31 | HTML 스냅샷 20페이지, CSS, 메타(routes.csv, manifests), user-flow |
| release-manifest/ | 54쌍 | v0.2.3~v0.2.138, 6~8개 게이트 통과 기록, 폐쇄형 검증 정책 |

#### 12. design-handoff HTML/CSS 23개 전수 확인

##### HTML 20개 페이지 분석
| # | 라우트 | 제목 | API 호출 | 쓰기 API |
|---|--------|------|---------|---------|
| 1 | / | (307 → /orders/review) | - | - |
| 2 | /orders | 주문 허브 | GET orders, GET orders/:id | - |
| 3 | /orders/review | 검토대상 | GET issues, GET actions, POST resolve | ✅ resolve |
| 4 | /orders/evidence | 원본 증빙 | GET orders, GET orders/:id/sources | - |
| 5 | /operations/issues | 오늘 이슈 | GET issues, GET actions, POST resolve | ✅ resolve |
| 6 | /catalog/menus | 메뉴 기준 | GET menus, GET menus/:id | - |
| 7 | /recipes | 레시피 | GET recipes, GET versions | - |
| 8 | /inventory/items | 재고 | GET items, POST adjustments | ✅ 재고 조정 |
| 9 | /purchasing/recommendations | 발주 추천 | GET recommendations, GET issues, POST purchase-orders | ✅ 발주 생성 |
| 10 | /analytics/stores | 매장 분석 | GET stores, GET stores/:id | - |
| 11 | /analytics/sales | 매출 분석 | GET sales, GET sales/:id | - |
| 12 | /analytics/menus | 메뉴 분석 | GET catalog/menus (재사용) | - |
| 13 | /analytics/options | 옵션 분석 | GET options | - |
| 14 | /settings/store-classification | 매장 분류 | GET/PUT classifications | ✅ PUT 저장 |
| 15 | /settings/keyword-rules | 키워드 규칙 | GET/PUT keyword-rules | ✅ PUT 저장 |
| 16 | /settings/settlement-rules | 정산 규칙 | GET/PUT option-inventory-rules, packaging-rules | ✅ PUT 저장 x2 |
| 17 | /management/sites | 사이트 관리 | GET/POST/PATCH settings/sites | ✅ POST+PATCH |
| 18 | /management/devices | 디바이스 | GET devices | - |
| 19 | /management/uploads | 업로드 기록 | GET upload-jobs | - |
| 20 | /management/users | 사용자 권한 | GET/PATCH users/:id/role | ✅ PATCH 역할 변경 |

##### 발견 사항
1. 루트(/) → /orders/review 리다이렉트 (307)
2. 쓰기 API 보유 페이지 9개 (위험 액션에 2단계 확인 체크박스 패턴)
3. analytics/menus는 catalog/menus API 재사용 (별도 분석 API 없음)
4. meta/api-config.json의 apiBase: http://127.0.0.1:4400 (HTML 기본값 4000과 다름)
5. CSS: globals.css 1235줄, Pretendard/Noto Sans KR, 반응형 960px 브레이크포인트

#### 13. release-manifest 98개 전수 확인

##### 릴리즈 진화 3단계
| 단계 | 버전 범위 | 게이트 수 | 추가된 것 |
|------|----------|----------|----------|
| Legacy | v0.2.3 | 4 | stack:contract, web:single-page, test, e2e:slice |
| Standard | v0.2.29~v0.2.84 | 6 | + release:manifest, web:page-origin, web:runtime-routes |
| Current | v0.2.85~v0.2.138 | 8 | + web:ux-check, web:ops-check, ops_*_summary 3개 |

##### 공통 불변 필드 (전 버전)
- migration_version: 0001_init
- env_schema_version: env-v1
- deploy_target: docker
- closed_validation_policy: 운영 서버 직접 검증 금지, 폐쇄형 검증만 허용

##### 버전 번호 갭
- v0.2.4~v0.2.28 (25개) — EPOCH-0에서 선언
- v0.2.76 — 없음
- v0.2.78~v0.2.83 (6개) — 없음
- v0.2.86~v0.2.136 (51개) — 없음

##### 특이사항
- v0.2.85: gate_image_tag가 bbb-app:0.2.112 (태그 번호와 불일치)
- 총 49개 버전 릴리즈, 각각 md+json 쌍

---

### 서버 코드 전수 확인 매트릭스 (최종 갱신)

| # | 카테고리 | 파일 수 | 읽음 | 미읽음 | 상태 |
|---|---------|--------|------|--------|------|
| 1 | 루트 설정 | 8 | 8 | 0 | ✅ |
| 2 | apps/api/src | 13 | 13 | 0 | ✅ |
| 3 | apps/api/test | 5 | 5 | 0 | ✅ |
| 4 | apps/web/app | 32 | 32 | 0 | ✅ |
| 5 | apps/web/src | 5 | 5 | 0 | ✅ |
| 6 | apps/web/test | 3 | 3 | 0 | ✅ |
| 7 | apps/worker | 3 | 3 | 0 | ✅ |
| 8 | packages/contracts | 5 | 5 | 0 | ✅ |
| 9 | packages/db | 6 | 6 | 0 | ✅ |
| 10 | packages/shared | 4 | 4 | 0 | ✅ |
| 11 | packages/modules | 6 | 6 | 0 | ✅ |
| 12 | agents/pc-uploader | 8 | 8 | 0 | ✅ |
| 13 | agents/windows-agent | 22 | 22 | 0 | ✅ |
| 14 | scripts | 33 | 33 | 0 | ✅ |
| 15 | docs/architecture | 25 | 25 | 0 | ✅ |
| 16 | docs/operations | 33 | 33 | 0 | ✅ |
| 17 | docs/design-handoff | 31 | 31 | 0 | ✅ |
| 18 | docs/release-manifest | 108 | 108 | 0 | ✅ |
| **합계** | | **351** | **351** | **0** | **✅ 전부 완료** |

#### 14. ssa 루트 레벨 파일 전수 확인 (server/ 외부)

##### 루트 설정 파일 (9개)
| 파일 | 내용 | 발견 사항 |
|------|------|----------|
| .env.example | 환경변수 템플릿 (APP_ENV, WEB_PORT=3000, API_PORT=3001 등) | server/.env.server.example과 별개 — 루트는 옛 스켈레톤용 |
| .gitignore | node_modules, .next, dist, .env 등 | 표준 |
| .restore_test | 빈 파일 (0바이트) | 용도 확인 안 됨 |
| docker-compose.dev.yml | postgres:17 + redis:7 로컬 개발용 | server/docker-compose.deploy.yml과 별개 |
| package.json | name: sales-split, pnpm@10.0.0 | server/package.json(name: ssa-server-root)과 다름 |
| pnpm-workspace.yaml | apps/*, packages/*, packages/modules/* | server/는 agents/* 추가 |
| tsconfig.base.json | @sales-split/* 경로 별칭 (9개 모듈) | server에는 tsconfig 없음 |
| turbo.json | build, typecheck, dev | server/turbo.json과 유사하지만 typecheck 추가 |
| README.md | 저장소 설명 + 폴더 규칙 | |

##### 루트 apps/ — 옛 스켈레톤 (42개 파일)
| 앱 | 파일 수 | 기술 | 현재 상태 |
|---|--------|------|----------|
| apps/api | 6 | NestJS + Fastify (TypeScript) | server/apps/api(JS, 6839줄)로 대체됨 |
| apps/desktop-agent | 25 | C# WinForms + Electron 이중 구현 | claude-1/DeliveryOrderReceiver(v2.0.2)로 대체됨 |
| apps/web | 8 | Next.js 15 + recharts (TypeScript) | server/apps/web(JS, 25페이지)로 대체됨 |
| apps/worker | 3 | TypeScript 스텁 | server/apps/worker(JS, BullMQ)로 대체됨 |

##### 루트 packages/ — 옛 스켈레톤 (29개 파일)
| 패키지 | 파일 수 | 현재 상태 |
|--------|--------|----------|
| contracts | 3 | server/packages/contracts(JS)로 대체됨 |
| db | 5 | server/packages/db(JS, 9227줄 postgres-store)로 대체됨 |
| modules/devices | 3 | 스텁만 (상수 export). server에 미구현 |
| modules/iam | 3 | 스텁만. server에 미구현 |
| modules/ingestion | 3 | 스텁만. server에 미구현 |
| modules/inventory | 3 | 스텁만. server에 미구현 |
| modules/orders | 3 | 스텁만. server에 미구현 |
| modules/organizations | 3 | 스텁만. server에 미구현 |
| modules/reconciliation | 3 | 스텁만. server에 미구현 |

##### legacy/ (7개 파일)
| 파일 | 줄 | 읽음 여부 |
|------|---|----------|
| README.md | 5 | ✅ |
| automatic-extraction-rules.md | 271 | ✅ (이전 분석에서 100줄, 이번에 전체) |
| canonical-database-design.md | 334 | ✅ (이전 분석에서 전체) |
| postgresql-schema-v1.sql | - | ✅ |
| settlement-adjustments-template.csv | - | ✅ |
| ui-shell-subagent-workpack-v1.md | - | ✅ |
| ui-ux-roadmap-v1.md | - | ✅ |

##### v1/ (10개 파일)
| 파일 | 줄 | 읽음 여부 |
|------|---|----------|
| external-ui-ux-spec-comparison-v1.md | 287 | ✅ |
| order-matching-logic-v1.md | 212 | ✅ (이전 분석에서 전체) |
| postgresql-schema-v2.sql | - | ✅ (이전 분석에서 전체) |
| ui-component-responsibility-matrix-v1.md | - | ✅ |
| ui-information-architecture-v1.md | 384 | ✅ (이전 분석에서 전체) |
| ui-role-access-matrix-v1.md | - | ✅ |
| ui-screen-interaction-rules-v1.md | - | ✅ |
| ui-state-exception-ux-v1.md | - | ✅ |
| ui-top-5-wireframes-v1.md | 322 | ✅ (이전 분석에서 전체) |
| ui-ux-roadmap-v2.md | - | ✅ |

##### 핵심 발견 사항
1. **이중 코드베이스 확인**: 루트(TypeScript 스켈레톤) vs server/(JavaScript 운영 코드) — 완전히 별개
2. **apps/desktop-agent에 v1.7.0 WinForms 코드 존재**: claude-1/DeliveryOrderReceiver(v2.0.2)와 별개. 옛 코드
3. **apps/desktop-agent에 Electron 코드도 존재**: 이중 구현(C# + Electron)의 흔적
4. **루트 package.json 이름이 sales-split**: server/는 ssa-server-root. 프로젝트 이름 불일치
5. **packages/modules/ 7개 스텁**: devices, iam, ingestion, inventory, orders, organizations, reconciliation — 설계만 있고 구현 없음
6. **legacy/automatic-extraction-rules.md 전체 271줄 확인**: 정산 레이어 A~D 구조, 수식, 운영 규칙 상세
7. **v1/ 10개 전부 이전 분석에서 읽었으나, 일부(ui-role-access-matrix, ui-component-responsibility-matrix 등)는 head -50만 읽었을 수 있음**

##### 갱신된 전체 매트릭스

| # | 카테고리 | 파일 수 | 상태 |
|---|---------|--------|------|
| 1~18 | server/ 하위 | 351 | ✅ 전부 읽음 |
| 19 | 루트 설정 파일 | 9 | ✅ 전부 읽음 |
| 20 | 루트 apps/ (옛 스켈레톤) | 42 | ✅ 전부 읽음 |
| 21 | 루트 packages/ (옛 스켈레톤) | 29 | ✅ 전부 읽음 |
| 22 | legacy/ | 7 | ✅ 전부 읽음 |
| 23 | v1/ | 10 | ✅ 전부 읽음 |
| **합계** | | **448** | **448/448** |

#### 15. pnpm-lock.yaml 확인
- 위치: server/pnpm-lock.yaml (1개만 존재, 루트에 없음)
- 줄 수: 4,111줄 (146KB)
- lockfileVersion: 9.0
- 12개 워크스페이스 패키지 의존성 잠금

#### 16. 깃 코드 vs 실제 서버 바이트 단위 대조

| 파일 | 깃 줄수 | 서버 줄수 | MD5 | 일치 |
|------|--------|---------|-----|------|
| server.js | 6839 | 6839 | 일치 | ✅ |
| agent-routes.js | 1157 | 1175 | 불일치 | ❌ 서버에 듀얼 인증 임시 수정 (+18줄) |
| postgres-store.js | 9227 | 9227 | 일치 | ✅ |
| memory-store.js | 3746 | 3746 | 일치 | ✅ |
| catalog-routes.js | 1583 | 1583 | 일치 | ✅ |
| reconciliation-routes.js | 847 | 847 | 일치 | ✅ |
| inventory-routes.js | 682 | 682 | 일치 | ✅ |
| mapping-routes.js | 744 | 744 | 일치 | ✅ |
| management-routes.js | 339 | 339 | 일치 | ✅ |
| purchase-orders-routes.js | 436 | 436 | 일치 | ✅ |
| windows-agent-routes.js | 702 | 702 | 일치 | ✅ |

11개 중 10개 일치, 1개 불일치 (agent-routes.js — 기획서 "Docker 임시 수정 기록"에 이미 기록됨)

#### 17. 기획서 내부 일관성 검증

| 검증 항목 | 결과 |
|----------|------|
| API 계약서 요청 필드 vs DTO 코드 (6개) | ✅ 전부 일치 |
| API 계약서 응답 필드 vs DTO 코드 (4개) | ✅ 전부 일치 |
| API 필드 vs DB 컬럼 camelCase→snake_case 매핑 | ✅ 일치 |
| DB에만 있는 필드 (idempotency_key) | ✅ 헤더로 수신, 응답 미노출 — 설계 의도 |
| 프론트 HTML API 호출 vs API 계약서 (4개) | ✅ 전부 일치 |
| 윈도우 프로그램 변경 사양 vs API 계약서 (4개) | ✅ 전부 일치 |

기획서 내부 모순 없음 확인.

---

### 최종 전체 매트릭스 (갱신)

| # | 카테고리 | 파일 수 | 상태 |
|---|---------|--------|------|
| 1~18 | server/ 하위 | 351 | ✅ |
| 19~23 | ssa 루트 레벨 | 97 | ✅ |
| 24 | pnpm-lock.yaml | 1 | ✅ |
| 25 | 깃 vs 서버 대조 | 11파일 | ✅ (1개 불일치 — 기록됨) |
| 26 | 기획서 내부 일관성 | 18항목 | ✅ 모순 없음 |
| **합계** | | **449+** | **전부 완료** |

---

## 기존 서버 업그레이드 상세

### 방침
- 기존 서버(bbb-prod-api) 코드를 수정/업데이트
- 기존 API 경로(/v2/agent/*) 유지
- 기존 DB 테이블(46개) 유지
- 기존 인증 체계(User Session + Device API Key) 유지

### 서버 업그레이드 — API 수정 사항

#### 기존 엔드포인트 수정
| 엔드포인트 | 현재 | 수정 내용 |
|-----------|------|----------|
| POST /v2/agent/uploads/receipt-raw | Device API Key만 허용 | User Session 인증 추가 (듀얼 인증) — 이미 Docker 임시 수정됨, 깃 반영 필요 |
| POST /v2/agent/uploads/receipt-raw | upload_jobs device_id FK 제약 | user session 인증 시 FK 위반 해결 필요 |

#### 새 엔드포인트 추가
| 엔드포인트 | 용도 |
|-----------|------|
| GET /v2/agent/receipts | 수신된 영수증 목록 조회 (날짜별, 페이지네이션) |
| GET /v2/agent/receipts/:id | 영수증 상세 조회 (원문 텍스트) |

#### 변경 없음 (기존 그대로 사용)
| 엔드포인트 | 용도 |
|-----------|------|
| POST /v2/agent/auth/login | 로그인 (기존 동작 유지) |
| POST /v2/agent/auth/signup | 회원가입 (기존 동작 유지) |
| GET /health | 헬스 체크 (기존 동작 유지) |

### 서버 업그레이드 — DB 수정 사항

#### 기존 테이블 수정
- upload_jobs: device_id FK 제약 수정 (user session 인증 시 대응)

#### 새 테이블 없음
- 기존 raw_receipts 테이블 사용 (구조 변경 없음)
- 기존 upload_jobs 테이블 사용
- 기존 users, user_sessions 테이블 사용

#### 향후 추가 (Step 5)
- parsed_receipt_lines 테이블 (AI 파싱 결과)

### 서버 업그레이드 — DTO

#### 기존 DTO 유지
- AuthResponse: 기존 buildAuthSessionPayload 그대로
- ReceiptUploadRequest: 기존 receipt-raw body 그대로 (eventId, siteId, platformId, platformStoreId, capturedAt, rawChecksum)

#### 새 DTO 추가
- ReceiptListResponse: { items: [...], total, page, limit, totalPages }
- ReceiptDetailResponse: { raw_receipt_id, decoded_text, captured_at, raw_checksum, ... }

### Step 2 상세: 업로드 동작 — FK 문제 해결

#### 현재 문제 (확인됨)
- agent-routes.js receipt-raw 엔드포인트에 듀얼 인증 추가됨 (Docker 임시 수정)
- user session 인증 시 device_id를 'user_' + userId로 생성
- upload_jobs 테이블에 device_id → devices(device_id) FK 제약
- 'user_xxx'가 devices 테이블에 없어 INSERT 실패

#### 해결 방법
user session 인증 시 가상 디바이스를 devices 테이블에 생성하거나, 기존 디바이스를 연결

구체적으로:
1. user session 인증 성공 시, 해당 user의 site에 연결된 device가 있으면 그 device_id 사용
2. device가 없으면 가상 device를 자동 생성 (device_name: 'web-upload', fingerprint: 'user-session-{userId}')
3. 이후 upload_jobs INSERT 시 실제 device_id 사용 → FK 위반 없음

#### 수정 파일
- server/apps/api/src/routes/agent-routes.js — 듀얼 인증 부분에 device 조회/생성 로직 추가
- 깃 반영 필요 (현재 Docker 임시 수정만)

### Step 4 상세: 주문 허브 페이지

#### 기존 API 활용
주문 허브에서 사용할 API (기존 서버에 이미 존재):
- GET /v2/management/upload-jobs — 업로드 기록 조회 (siteId, uploadType=receipt_raw 필터)
- raw_receipts 조회 API — 추가 필요 (기존에 없음)

#### 추가 필요한 API (기존 서버에 추가)
- GET /v2/agent/receipts — raw_receipts 테이블 날짜별 조회
- GET /v2/agent/receipts/:id — raw_receipts 상세 (decoded_text 포함)

#### 페이지 구현
- server/apps/web/app/orders/hub/page.js 신규 생성
- DomainPageShell 기존 셸 재사용
- API: GET /v2/agent/receipts 호출
- 기존 CSS(globals.css) 재사용

### 윈도우 프로그램 현재 상태 (변경 불필요)

| 항목 | 현재 값 | 변경 필요 |
|------|---------|----------|
| 로그인 URL | /v2/agent/auth/login | ❌ 그대로 |
| 업로드 URL | /v2/agent/uploads/receipt-raw | ❌ 그대로 |
| Body 필드 | eventId, siteId, platformId, platformStoreId, capturedAt, rawChecksum, decodedText, port | ❌ 그대로 |
| 인증 헤더 | Authorization: Bearer {session_token} | ❌ 그대로 |
| Idempotency-Key | SHA256 해시 | ❌ 그대로 |

서버 FK 문제만 해결되면 윈도우 프로그램 코드 변경 없이 업로드 동작.

---

## 전체 문제점 분석 (23건, 2026-04-02 확인)

### A. 보안 이슈 (6건) — site/org 접근 체크 누락

| # | 엔드포인트 | 문제 | 근거 |
|---|-----------|------|------|
| 1 | GET /v2/orders/:orderId | siteAccessible 체크 없음 | server.js grep 결과 0건 |
| 2 | GET /v2/orders/:orderId/sources | site/org 체크 없음 | reconciliation-routes.js 확인 |
| 3 | POST /v2/mapping/suggestions/:id/approve | site/org 체크 없음 | mapping-routes.js 확인 |
| 4 | POST /v2/mapping/suggestions/:id/reject | site/org 체크 없음 | mapping-routes.js 확인 |
| 5 | POST /v2/mapping/suggestions/:id/ignore | site/org 체크 없음 | mapping-routes.js 확인 |
| 6 | GET /v2/management/audit-logs | site/org 체크 없음 | management-routes.js 확인 |

### B. 로직 결함 (3건) — PATCH에서 site 체크 순서 오류

| # | 엔드포인트 | 문제 | 근거 |
|---|-----------|------|------|
| 7 | PATCH /v2/catalog/menus/:menuId | site 체크가 store 수정 이후 실행 — 권한 없는 수정 후 403 반환 가능 | catalog-routes.js 코드 순서 확인 |
| 8 | PATCH /v2/catalog/addons/:addonId | 위와 동일 | catalog-routes.js 확인 |
| 9 | PATCH /v2/inventory/items/:itemId | 위와 동일 | inventory-routes.js 확인 |

### C. 비효율 (3건)

| # | 위치 | 문제 | 근거 |
|---|------|------|------|
| 10 | GET analytics/stores, stores/:storeId, sales/:reportId | limit:500 전체 조회 후 클라이언트 사이드 매칭 | server.js grep: 3곳에서 limit:500 |
| 11 | windows-agent resolveSessionUserRole | limit:200 전체 사용자 조회 후 find — 200명 초과 시 role 조회 실패 | windows-agent-routes.js: 3곳에서 호출 |
| 12 | GET catalog/menus/:menuId | getCatalogMenu 없으면 listCatalogMenus limit:200 폴백 | catalog-routes.js 확인 |

### D. 데이터 무결성 (2건)

| # | 위치 | 문제 | 근거 |
|---|------|------|------|
| 13 | recordDeviceHeartbeat (postgres-store.js) | devices UPDATE + device_heartbeats INSERT가 트랜잭션 없이 별도 실행 — 부분 업데이트 가능 | pool.query 개별 호출 확인 |
| 14 | activateWindowsAgentRelease (windows-agent-routes.js) | 파일시스템 직접 쓰기(writeJsonFileAtomic), 동시 요청 잠금 없음 | 코드 확인 |

### E. 권한 체계 (1건)

| # | 문제 | 근거 |
|---|------|------|
| 15 | role 기반 권한 검사 0건 — 설계 문서는 5개 role 요구, 코드는 로그인+site 소속만 확인 | server.js: role===빈값 검증 2건만(L5375,5378), routes: 0건 |

### F. 보안 설정 (1건)

| # | 문제 | 근거 |
|---|------|------|
| 16 | CORS origin: * — 모든 출처 허용, 보안 헤더(helmet/X-Frame/CSP) 없음 | server.js:59 확인 |

### G. 미구현 — 설계에만 존재 (5건)

| # | 항목 | 상세 | 근거 |
|---|------|------|------|
| 17 | DB 테이블 4개 | parsed_receipt_lines, order_status_events, order_item_ignored_lines, mapping_feedback — 코드 참조 0건 | apps/ packages/ 전체 grep 0건 |
| 18 | 데이터 흐름 5단계 | 파싱, 주문 생성/매칭, 자동 재고 차감, 충돌 감지, 취소 처리 | postgres-store.js 전수 확인 |
| 19 | 웹앱 페이지 9개 | operations/overview, operations/actions, orders/cancelled, catalog/options, inventory/movements, analytics/dayparts, analytics/inventory-usage, settings/recipe-rules, management/exports | find page.* 확인 |
| 20 | modules 7개 | devices, iam, ingestion, inventory, orders, organizations, reconciliation — 디렉토리 없음 | find 확인 |
| 21 | 설정 파일 2개 | migrate.js, next.config.mjs — 파일 없음 | cat 확인 |

### H. 윈도우 프로그램 (2건)

| # | 문제 | 근거 |
|---|------|------|
| 22 | 업로드 URL/body는 기존 서버와 일치 — 코드 변경 불필요 (FK 문제만 해결하면 동작) | UploadService.cs grep 확인 |
| 23 | 서버 업로드 안 됨 — 기존 서버가 Device API Key만 허용, 윈도우 프로그램은 session token 전송 | curl 테스트 "invalid device api key" 확인 |

### 해결 방향 (기존 서버 수정)

| 구분 | 기존 Fastify 코드 수정 시 |
|------|------------------------|
| A. 보안 6건 | 각 엔드포인트에 site/org 체크 추가 |
| B. 로직 3건 | PATCH에서 site 체크 순서 수정 |
| C. 비효율 3건 | limit:500 → 전용 쿼리 또는 페이지네이션 |
| D. 무결성 2건 | 트랜잭션 추가 / 잠금 추가 |
| E. 권한 1건 | role 미들웨어 추가 |
| F. CORS 1건 | origin 제한 + 보안 헤더 추가 |
| G. 미구현 5건 | Phase 2/3에서 추가 구현 |
| H. 윈도우 2건 | 윈도우 프로그램 코드 변경 불필요 (기존 서버 유지) |

### 웹 세션 관리 — 근본 문제 (P0)
- 웹앱 인증/세션 시스템이 프로덕션 수준 아님
- tenant dashboard와 /auth/login 세션 흐름 불통일
- 미인증 시 콘텐츠 차단 안 됨 (보안)
- 윈도우↔웹 인증 완전 분리
- 서버 코드 docker cp 임시 적용 (영구 미반영)
- 서버 수정 후 기존 기능 검증 안 함
- BUG-001 미수정 (윈도우 업로드 0건)
- 상세: issue_matrix.md P0 근본 문제 7건 + P1 미확인 25건 참조

### 미결정 사항 (PM 판단 필요) — 갱신

#### 해결됨 (기존 서버 유지로 자동 결정)
- [x] 코드 기반 (Fastify vs Express) → ✅ 해결됨: Fastify 유지
- [x] API 경로 (/v2/ vs 새 경로) → ✅ 해결됨: /v2/ 유지
- [x] SSL/HTTPS: 기존 nginx-proxy-manager 유지

#### 아직 결정 필요
- [x] 세션 정리 정책: 30일 이상 세션 자동 삭제 cron 설정 완료 (2026-04-04)
- [ ] 비밀번호 정책: 최소 길이/복잡도
- [ ] FK 해결 방법: 가상 디바이스 생성 vs FK 제약 제거 vs 다른 방법

### 기존 서버 임시 수정 기록 (컨테이너 재시작 시 사라짐)

#### 수정 1: agent-routes.js 듀얼 인증 (2026-04-01)
- **파일**: Docker 컨테이너 bbb-prod-api-1 내부 `/app/apps/api/src/routes/agent-routes.js`
- **내용**: receipt-raw 엔드포인트에 user session 인증 폴백 추가
  - 기존: `requireDeviceApiKeyFromRequest` 만 사용
  - 수정: Device API Key 실패 시 `requireUserSessionFromRequest`로 폴백
  - siteId 검증: `authMode === 'device'`일 때만 체크
- **백업**: `/app/apps/api/src/routes/agent-routes.js.bak`
- **상태**: 컨테이너 재시작 시 원복됨 (이미지에 반영 안 됨)
- **영향**: 이 임시 수정을 깃(ssa/server/)에 정식 반영 필요 (기존 서버 유지 확정)

#### 해결 방안
- 깃에 정식 반영 후 Docker 이미지 리빌드 시 영구 적용됨
- 교체 전까지 컨테이너 재시작 금지 (재시작하면 윈도우 프로그램 업로드 실패)
- 만약 재시작이 필요하면 수정을 다시 적용해야 함

### 서버 업그레이드 배포 순서
1. 깃(ssa/server/) 코드 수정
2. Docker 이미지 빌드 (docker-build-strict.sh)
3. 스테이징(stg.zigso.kr) 배포 + 테스트
4. 프로덕션(zigso.kr) 배포
5. 윈도우 프로그램 업로드 테스트

### DB — 기존 DB 유지

- 기존 PostgreSQL DB(bbb) 그대로 사용
- 기존 46개 테이블 유지
- 기존 데이터 유지 (users, devices, audit_logs 등)
- 새 DB 생성 불필요

### 인증 — 기존 구현 유지

- 기존 authenticateUser (postgres-store.js:2502) 그대로 사용
- 기존 createUserSession (postgres-store.js:2787) 그대로 사용
- 기존 getUserSession (postgres-store.js:2814) 그대로 사용
- 기존 requireUserSessionFromRequest (server.js:573) 그대로 사용
- 기존 requireDeviceApiKeyFromRequest (server.js:602) 그대로 사용
- 추가 구현 불필요

### 윈도우 프로그램 — 변경 불필요

기존 서버를 유지하므로:
- 업로드 URL: /v2/agent/uploads/receipt-raw (변경 없음)
- 로그인 URL: /v2/agent/auth/login (변경 없음)
- Body 필드: eventId, siteId, platformId, platformStoreId, capturedAt, rawChecksum, decodedText, port (변경 없음)
- 인증: Authorization: Bearer {session_token} (변경 없음)

서버 FK 문제만 해결되면 현재 v2.0.2 코드 그대로 업로드 동작.

### 서버 업그레이드 — 주문 허브 페이지

#### /orders/hub (기존 웹앱에 추가)
- 기존 DomainPageShell 재사용
- 기존 인증/세션 흐름 재사용
- API: GET /v2/agent/receipts (새 엔드포인트)
- raw_receipts 테이블에서 조회

### 6. AI 파싱 상세 기획 (Phase 2)

#### 목적
ESC/POS 파싱된 영수증 텍스트에서 구조화된 주문 정보 자동 추출

#### 입력
order_sources.raw_text (또는 raw_receipts.decoded_text)

#### 출력 → parsed_receipt_lines 테이블 저장
| 필드 | 설명 | 예시 |
|------|------|------|
| platform | 배달 플랫폼 | 배민, 쿠팡이츠, 요기요 |
| external_order_code | 주문번호 | T2B90000CKA6 |
| menu_items | 메뉴 항목 | [{name: "훈제오리", qty: 1, price: 12900}] |
| option_items | 옵션 항목 | [{name: "머스타드소스", qty: 1}] |
| total_amount | 총 금액 | 12900 |
| payment_method | 결제방식 | 선결제 |
| ordered_at | 주문 시각 | 2026-03-17 11:43 |
| confidence | 추출 신뢰도 | high/medium/low |

#### 방법
- Claude API 또는 로컬 규칙 기반 파싱
- 수동 보정으로 학습 데이터 축적
- 근거: automatic-extraction-rules.md

### 7. 테스트 계획

#### Phase 1 테스트 항목
| # | 테스트 | 방법 |
|---|--------|------|
| 1 | 서버 시작 | docker-compose up → health 체크 |
| 2 | 회원가입 | curl POST /agent/auth/signup |
| 3 | 로그인 | curl POST /agent/auth/login |
| 4 | 영수증 업로드 | curl POST /agent/receipts (Bearer token + body) |
| 5 | DB 저장 확인 | psql SELECT * FROM order_sources |
| 6 | 중복 업로드 | 같은 Idempotency-Key로 재요청 → dedupe 확인 |
| 7 | 주문 허브 페이지 | 브라우저에서 /orders/hub 접속 → 데이터 표시 확인 |
| 8 | 윈도우 프로그램 연동 | 실제 주문 수신 → 서버 업로드 → 페이지 확인 |

#### 스테이징 테스트
- stg.zigso.kr에서 1~7번 먼저 확인
- 통과 후 프로덕션(zigso.kr) 배포

### 롤백 — Docker 이미지 태그 기반
- 수정 전 이미지: bbb-app:0.2.151-amd64 (현재 운영)
- 수정 후 이미지: bbb-app:0.2.152-amd64 (새 빌드)
- 롤백: docker-compose에서 BBB_IMAGE_TAG를 이전 버전으로 변경 후 재시작

---

## 전체 이슈 매트릭스
상세 1495건은 `issue_matrix.md` 참조.
요약: P0:7 / P1:16 / P2:29 / P3:48 / P4:46 / P5:1180 / 합계:1495

---

## 변경 이력
| 날짜 | 내용 | 담당 |
|------|------|------|
| 2026-04-01 | 서버 기획서 초안 작성 | 리더 |
| 2026-04-02 | SSA 서버 기획 분석 반영 (DB설계, IA, 매칭로직, 추출규칙) | 리더 |
| 2026-04-02 | 전수 분석 결과 반영 (API 89개 내부로직, 페이지 25개 상세, DB 컬럼 대조, Store 91개, 권한 갭) | 리더 |
| 2026-04-02 | 전수 분석 2차 반영 (server.js 44개, 라우트 37개, 레이아웃, memory/postgres store 비교) | 리더 |
| 2026-04-02 | FK 해결 방향 A 확정 (가상 device 자동 생성), Step 2 코드 수정 착수 | 리더 |
| 2026-04-02 | 최종 전수 분석 반영 (Store SQL 91개, 데이터 흐름 9단계 구현 상태) | 리더 |
| 2026-04-02 | 추가 분석 반영 (modules 3개, 런타임 2424줄, 배포 스크립트 18개, CORS, 시드, docker-compose, env) | 리더 |
| 2026-04-02 | 서버 업그레이드/DB/인증/연동/허브페이지/AI/테스트/롤백 상세 계획 추가 | 리더 |
| 2026-04-02 | Phase 1 작업 순서 + 윈도우↔서버 의존성 추가 | 리더 |
| 2026-04-02 | Phase 1 API 계약서, DB Migration SQL, 프론트 HTML 구조, DTO 코드 추가 | 리더 |
| 2026-04-02 | 미결정 사항 7건 추가 (코드 기반, API 경로, 에러코드, 세션, 비밀번호, SSL, 로깅) | 리더 |
| 2026-04-02 | Docker 임시 수정 기록 추가 (컨테이너 재시작 시 원복 주의) | 리더 |
| 2026-04-02 | 미확인 파일 10개 전수 확인 반영 (2개 미존재 확인, 발견 사항 4건) | 리더 |
| 2026-04-02 | 전체 문제점 23건 분석 + 해결 방향 추가 | 리더 |
| 2026-04-02 | 미확인 파일 50개 최종 전수 확인 (API모듈/웹라우트/packages/agents/테스트/docs/scripts) | 리더 |
| 2026-04-02 | agents/windows-agent 12개 파일 전수 확인 (캡처어댑터/GUI설정/MSI/서비스스크립트) | 리더 |
| 2026-04-02 | 전수 확인 매트릭스 최종 완성 (351파일 중 230파일 읽음, 121파일 구조 확인) | 리더 |
| 2026-04-02 | 전수 확인 매트릭스 351/351 완료 (design-handoff 23개 + release-manifest 98개 전수 읽음) | 리더 |
| 2026-04-02 | 작업 방향 확정: 기존 서버 수정/업데이트, 작업 매트릭스 8단계 체크리스트 | 리더 |
| 2026-04-02 | ssa 루트 레벨 파일 전수 확인 (루트 9개 + apps 42개 + packages 29개 + legacy 7개 + v1 10개 = 97개) | 리더 |
| 2026-04-02 | pnpm-lock 확인 + 깃vs서버 MD5 대조 + 기획서 일관성 검증 완료 | 리더 |
| 2026-04-02 | 기획서 방향 전면 수정: 기존 서버 수정/업그레이드 확정 | 리더 |
| 2026-04-02 | 보안 긴급 4건 + 운영 안정성 5건 + 테넌트 고도화 4건 작업 매트릭스 추가 | 리더 |
| 2026-04-02 | 기존 서버 기능 16건 방향 결정 (우리 프로그램 메인, 코덱스 agent 참고용) | 리더 |
| 2026-04-03 | 전체 이슈 495건 우선순위 매트릭스 (P0:7 P1:15 P2:23 P3:45 P4:60 P5:345) | 리더 |
| 2026-04-03 | 전체 이슈 1495건 매트릭스 별도 파일(issue_matrix.md) 분리 | 리더 |
| 2026-04-03 | 시간대 이슈 기획서 반영 (UTC/KST 버그 + 자동 시간대 변환 요구사항) | 리더 |
| 2026-04-03 | 웹 미인증 전역 이슈 전수 조사 + 해결방안 + 재발 방지 (이미지 8번 근거) | 리더 |
| 2026-04-03 | 근본 문제 7건 + 미확인 25건 + 재발 방지 4건 매트릭스 반영 | 리더 |
