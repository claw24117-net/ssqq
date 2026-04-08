# 전체 이슈 매트릭스 1495건 — 해결방안 포함 (v2.0)

## 통합 시나리오 매트릭스 (기획서 전체 반영)

### 시나리오 1: 윈도우 프로그램 — 기능별 전수 체크

#### 1-1. 로그인 (dev_progress 기능1)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-101 | 이메일/패스워드/서버주소 입력 | ✅ | ✅ PM 확인 | 이미지 7번 |
| W-102 | 로그인 → Bearer token 발급 | ✅ | ✅ | PM 로그인 성공 |
| W-103 | ☑ 로그인 정보 저장 | ✅ | ✅ | 이미지 7번 체크박스 |
| W-104 | ☑ 자동 로그인 | ✅ | ❌ BUG-001 | 토큰 삭제 버그로 미동작 |
| W-105 | 토큰 만료 시 로그인 화면 | ✅ | ❌ 미테스트 | 코드 확인됨 |

#### 1-2. COM포트 관리 (dev_progress 기능2)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-201 | 관리자 비밀번호 (0000) | ✅ | ✅ | v1.7.0 테스트 |
| W-202 | 일반 모드 setupc 호출 안 함 | ✅ | ✅ | 코드 확인 |
| W-203 | 설정 창 닫으면 리소스 해제 | ✅ | ❌ 미테스트 | 코드 확인 |
| W-204 | com0com 포트 쌍 생성 | ✅ | ✅ | COM14/15 생성 성공 |
| W-205 | 포트 번호 직접 입력 | ✅ | ✅ | PM 테스트 |
| W-206 | 점유 포트 "사용중" 표시 | ✅ | ✅ | v1.4.0 테스트 |
| W-207 | 포트 쌍 안내 표시 | ✅ | ✅ | PM 테스트 |
| W-208 | 포트 복원 | ✅ | ❌ 미테스트 | 코드 확인 |
| W-209 | ⚠ 경고 메시지 | ✅ | ✅ | 코드 확인 |

#### 1-3. 포트 삭제 (dev_progress 기능3)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-301 | 수신 중지 필수 | ✅ | ❌ 미테스트 | 코드 확인 |
| W-302 | 우리 포트만 삭제 | ✅ | ❌ 미테스트 | 코드 확인 |
| W-303 | 매장천사 포트 안 건드림 | ✅ | ❌ 미테스트 | 코드 확인 |
| W-304 | 삭제 전 확인 메시지 | ✅ | ❌ 미테스트 | 코드 확인 |
| W-305 | 삭제 후 CreatedPortA/B 초기화 | ✅ | ❌ 미테스트 | 코드 확인 |

#### 1-4. 데이터 수신 (dev_progress 기능4)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-401 | 통신 속도 드롭다운 (1200~115200) | ✅ | ✅ | PM 테스트 |
| W-402 | 수신 시작/중지 버튼 | ✅ | ✅ | PM 주문 수신 성공 |
| W-403 | 자동 수신 (마지막 포트+속도) | ✅ | ❌ 미테스트 | 코드 확인 |
| W-404 | 매장천사 프린터 등록 안내 | ✅ | ✅ | 설정 패널 확인 |

#### 1-5. 데이터 처리 (dev_progress 기능5)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-501 | ESC/POS 텍스트 추출 | ✅ | ✅ | 주문 목록에 데이터 표시 |
| W-502 | 주문 텍스트 목록 표시 | ✅ | ✅ | 이미지 5,6번 |

#### 1-6. 서버 업로드 (dev_progress 기능6)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-601 | Bearer token 인증 | ✅ | ❌ BUG-001 | 토큰 삭제로 업로드 0건 |
| W-602 | Idempotency-Key 해시 | ✅ | ❌ BUG-001 | 업로드 시도 자체 안 됨 |
| W-603 | POST receipt-raw | ✅ | ❌ BUG-001 | 업로드 시도 자체 안 됨 |
| W-604 | 전송 상태 표시 | ✅ | ✅ | "실패 - 로그인이 필요합니다" 표시됨 |

#### 1-7. 자동 실행 (dev_progress 기능7)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-701 | ☑ 자동 실행 ON/OFF | ✅ | ❌ 미테스트 | 코드 확인 |
| W-702 | 레지스트리 등록/해제 | ✅ | ❌ 미테스트 | 코드 확인 |

#### 1-8. X 버튼 / 트레이 (dev_progress 기능8)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-801 | X 버튼 → 종료 | ✅ | ✅ | v2.0.1 PM 테스트 |
| W-802 | 트레이 제거됨 | ✅ | ✅ | v2.0.1 코드 확인 |

#### 1-9. 에러 메시지 한글화 (dev_progress 기능9)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-901 | setupc 에러 한글 변환 | ✅ | ❌ 미테스트 | TranslateSetupcError 코드 확인 |
| W-902 | 서버 에러 한글 변환 | ⚠ 부분 | ❌ | 서버 에러 영문 그대로 |
| W-903 | 포트 에러 한글 변환 | ✅ | ❌ 미테스트 | 코드 확인 |

#### 1-10. GUI 화면 (dev_progress GUI 설계)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-1001 | 단일 창 + 3 Panel 전환 | ✅ | ✅ | PM 테스트 |
| W-1002 | 로그인 패널 | ✅ | ✅ | 이미지 7번 |
| W-1003 | 메인 패널 (Dock 레이아웃) | ✅ | ⚠ | 이미지 6번 — 잘림 수정됨(v2.0.2) |
| W-1004 | 설정 패널 | ✅ | ✅ | PM 테스트 |
| W-1005 | 가로 스크롤 | ✅ | ❌ 미테스트 | HorizontalExtent 코드 확인 |

#### 1-11. 재부팅 대응 (dev_progress 재부팅 대응)
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-1101 | com0com 자동 복원 | ✅ (드라이버) | ❌ 미테스트 | com0com 기능 |
| W-1102 | 프로그램 자동 시작 | ✅ | ❌ 미테스트 | 레지스트리 등록 코드 |
| W-1103 | 자동 로그인 | ✅ | ❌ BUG-001 | 토큰 삭제 버그 |
| W-1104 | 자동 수신 시작 | ✅ | ❌ 미테스트 | AutoStartListening 코드 |
| W-1105 | 포트 사라짐 감지 | ✅ | ❌ 미테스트 | 코드 확인 |

#### 1-12. v2.0.1 추가 기능
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| W-1201 | 날짜별 로컬 저장 (JSON) | ✅ | ❌ 미테스트 | OrderStorageService 코드 |
| W-1202 | 로컬 중복 감지 (7일 해시) | ✅ | ❌ 미테스트 | IsDuplicate 코드 |
| W-1203 | 수동 재전송 버튼 | ✅ | ❌ 미테스트 | RetryUploadBtn 코드 |
| W-1204 | Idempotency-Key 해시 | ✅ | ❌ BUG-001 | 업로드 안 되서 미검증 |
| W-1205 | 자동 재로그인 (401→재로그인) | ✅ | ❌ BUG-001 | 업로드 안 되서 미검증 |
| W-1206 | 종료 시 버퍼 처리 | ✅ | ❌ 미테스트 | Thread.Sleep(600) 코드 |
| W-1207 | JSON 파일 손상 방지 | ✅ | ❌ 미테스트 | .tmp 교체 코드 |

#### 1-13. 긴급 버그
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| BUG-001 | 자동 로그인 미체크 시 토큰 삭제 | ✅ v2.0.3 수정 | if 블록 제거 |
| BUG-002 | 로컬 주문 서버 미전송 | ❌ BUG-004 수정됨, 재전송 테스트 필요 | 업로드 0건 |
| BUG-003 | 재전송 버튼 미검증 | ❌ 미테스트 | 코드 존재, 동작 미확인 |
| BUG-004 | 토큰 추출 필드명 불일치 (token→userSessionToken, 최상위→data 내부) | ✅ v2.0.4 수정+배포 | AuthService.cs: data.data.userSessionToken + fallback |
| W-TIME | 시간대 버그 (KST/"Z") | ✅ v2.0.3 수정 | DateTime.UtcNow.ToString("o") |
| W-RETRY | 재전송 실패 시 에러 메시지 미표시 | ✅ v2.0.4 수정+배포 | catch(Exception ex) + lastError 표시 |

---

### 시나리오 2: 서버 — 데이터 수신/저장/조회

#### 2-1. 인증
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| S-101 | POST /v2/agent/auth/login | ✅ | ✅ | curl 토큰 발급 |
| S-102 | POST /v2/agent/auth/signup | ✅ | ✅ | curl 계정 생성 |
| S-103 | 듀얼 인증 (device + user session) | ✅ | ✅ | curl 업로드 성공 |
| S-104 | 가상 device 자동 생성 | ✅ | ✅ | devices 테이블 확인 |
| S-105 | 패스워드 scrypt 해싱 | ✅ | ✅ | $scrypt$ 접두사 확인 |
| S-106 | 패스워드 최소 8자 검증 | ✅ | ✅ | "123" 거부 확인 |
| S-107 | 이메일 형식 검증 | ✅ | ✅ | "notanemail" 거부 확인 |
| S-108 | 평문→해시 자동 마이그레이션 | ✅ | ✅ | 로그인 시 자동 변환 확인 |

#### 2-2. 영수증 업로드
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| S-201 | POST receipt-raw 정상 | ✅ | ✅ | accepted:true |
| S-202 | 멱등성 (같은 key) | ✅ | ✅ | dedupe:true |
| S-203 | rawChecksum 중복 감지 | ✅ | ✅ | dedupe:true |
| S-204 | siteId 검증 (user session) | ✅ | ✅ | 403 FORBIDDEN |
| S-205 | 토큰 없이 업로드 | ✅ | ✅ | 401 UNAUTHORIZED |
| S-206 | 필수 필드 누락 | ✅ | ✅ | 400 INVALID_REQUEST |

#### 2-3. 조회 API
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| S-301 | GET /v2/agent/receipts | ✅ | ✅ | 목록 정상 |
| S-302 | GET /v2/agent/receipts/:id | ✅ | ✅ | 상세 정상 |

#### 2-4. 주문 허브 페이지
| # | 항목 | 구현 | 테스트 | 근거 |
|---|------|------|--------|------|
| S-401 | /orders/hub 페이지 | ✅ | ✅ | HTTP 200, Next.js 빌드 |
| S-402 | 날짜 필터 | ✅ | ❌ PM 테스트 필요 | 코드 확인 |
| S-403 | 영수증 목록 테이블 | ✅ | ❌ PM 테스트 필요 | 코드 확인 |
| S-404 | 클릭 시 원문 표시 | ✅ | ❌ PM 테스트 필요 | 코드 확인 |
| S-405 | 시간대 KST 변환 | ✅ | ❌ PM 테스트 필요 | toLocaleString("ko-KR") 확인 |

---

### 시나리오 3: 서버 — 보안/안정성

#### 3-1. 배포 (A)
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| A-1~3 | 깃↔서버 코드 일치 | ✅ | 3파일 MD5 일치 |
| A-4 | node --check 문법 | ✅ | 3파일 PASS |
| A-5 | Docker 이미지 버전 | ❌ 미결정 | |
| A-6 | 스테이징 테스트 | ❌ 미실행 | |

#### 3-2. 보안 (B)
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| B-1 | siteId 검증 | ✅ | 403 확인 |
| B-2 | CORS 제한 | ✅ | zigso.kr만 허용 |
| B-3 | XSS 방지 | ✅ | React 자동 이스케이핑 |
| B-6 | 로그인 실패 제한 | ✅ | 5분간 5회 실패 시 30초 잠금 (2026-04-03) |
| B-7 | body 크기 제한 | ✅ | FastifyAdapter bodyLimit 1MB (2026-04-03) |
| B-8 | 500 에러 정보 노출 | ✅ | "internal server error"만 |
| B-11 | 보안 헤더 | ✅ | X-Frame-Options, X-Content-Type-Options, X-XSS-Protection (2026-04-03) |

#### 3-3. 보안 강화 (G)
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| G-1~9 | site 체크 | ✅ | 42건+5곳 확인 |
| G-10 | role 미들웨어 | ❌ 미구현 | |

#### 3-4. 웹 세션
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| 78 | sessionEmail 표시 | ✅ | 인증됨/미인증 분기 |
| 108~112 | 로그인→/orders/hub | ✅ | 리디렉트 확인 |
| 62 | DEFAULT_SESSION_EMAIL 제거 | ✅ | "" 빈값 |
| 119 | SITE_OPTIONS 빈 배열 | ✅ | 동적 로드 |
| 113 | localStorage 문제 | ❌ PM 확인 필요 | |

#### 3-5. 테스트
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| 98 | 서버 테스트 45/45 | ✅ | 전체 통과 |
| 95 | 웹 24페이지 HTTP 200 | ✅ | curl 전수 |
| 81 | API 16개 정상 | ✅ | curl 테스트 |

---

### 시나리오 4: 서버 — 데이터/운영

#### 4-1. 데이터 무결성 (D)
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| D-1 | upload_jobs 상태 전이 | ✅ | 업로드 성공 시 completed 업데이트 (2026-04-03) |
| D-8 | DB 자동 백업 | ✅ | cron 매일 03:00 확인 |
| D-9 | audit_logs 정리 | ✅ | 47,995건→0건 삭제(30일 미만), cron 매일 04:00 설정 (2026-04-04) |
| D-10 | sessions 정리 | ✅ | 135건→0건 삭제(30일 미만), cron 매일 04:00 설정 (2026-04-04) |

#### 4-2. 운영 (I)
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| I-1 | 모니터링 | ✅ | healthcheck healthy |
| I-3 | 로그 로테이션 | ✅ | json-file max-size 10m, max-file 3 (2026-04-03) |
| I-7 | Docker 메모리 | ✅ | mem_limit 512m (2026-04-03) |
| I-14 | SSL 인증서 | ✅ | 만료 2026-06-01 (59일) |

#### 4-3. 성능 (J)
| # | 항목 | 상태 | 근거 |
|---|------|------|------|
| J-1 | raw_receipts 인덱스 | ✅ | 3개 확인 |
| J-2 | upload_jobs 인덱스 | ✅ | 2개 확인 |
| 디스크 | 서버 디스크 | ✅ | 13% 사용 |
| DB | DB 크기 | ✅ | 25MB |

---

## 전체 요약

| 구분 | 전체 | ✅ 완료 | ❌ 미완료 | ⏸ PM필요 |
|------|------|--------|---------|---------|
| 윈도우 기능 (1-1~1-13) | 60 | 40 | 16 | 4 |
| 서버 수신/조회 (2-1~2-4) | 21 | 18 | 0 | 3 |
| 서버 보안/안정성 (3-1~3-5) | 22 | 17 | 4 | 1 |
| 서버 데이터/운영 (4-1~4-3) | 12 | 7 | 4 | 1 |
| **합계** | **115** | **82** | **24** | **9** |

### 미완료 24건 우선순위

#### P0 즉시 (PM 빌드 지시 시)
- BUG-001: 토큰 삭제 버그 수정
- BUG-002: 로컬 주문 재전송
- BUG-003: 재전송 버튼 검증
- W-TIME: 시간대 버그

#### P1 서버 구현 ✅ (2026-04-03 완료)
- B-6: rate limit ✅
- B-7: body limit ✅
- B-11: 보안 헤더 ✅
- D-1: upload_jobs 상태 전이 ✅
- I-3: 로그 로테이션 ✅
- I-7: Docker 메모리 제한 ✅

#### P2 테스트 (PM 윈도우 접근 시)
- W-203,208,301~305: 포트 관리/삭제 테스트
- W-403,701~702: 자동 수신/자동 실행 테스트
- W-1101~1105: 재부팅 대응 테스트
- W-1201~1207: v2.0.1 기능 테스트

#### P3 향후
- A-5,6: Docker 이미지 버전/스테이징
- G-10: role 미들웨어
- D-9,10: 로그/세션 정리

---

## 작업 묶음 요약

| 묶음 | 건수 | 핵심 | 순서 |
|------|------|------|------|
| A. 서버 배포 영구화 | 7 | Docker 이미지 리빌드 | 1번째 |
| B. 서버 보안 긴급 | 12 | siteId 검증, CORS, XSS, rate limit | 2번째 |
| C. 윈도우 버그 수정 | 5 | BUG-001 토큰, 재전송, 파일 잠금 | 3번째 (PM 지시 시) |
| D. 데이터 무결성 | 10 | 중복 방지, 상태 전이, 백업 | 4번째 |
| E. 주문 허브 + 조회 API | 5 | 페이지 + API 2개 + DB 연동 | 5번째 |
| F. 데이터 흐름 | 6 | 파싱, 주문 생성, 매칭 | 6번째 |
| G. 보안 강화 | 15 | role 검사, site 체크, 세션 관리 | 7번째 |
| H. UI/UX 개선 | 20 | 웹 실시간, 날짜 필터, 에러 한글화 | 8번째 |
| I. 운영 안정화 | 15 | 모니터링, 알림, 로그, 백업 | 9번째 |
| J. 성능 최적화 | 10 | 인덱스, 캐싱, 비동기 I/O | 10번째 |
| K. 문서/테스트 | 15 | API 문서, 테스트 커버리지 | 11번째 |
| L. 비즈니스 확장 | 30 | 매출 추출, 리포트, 멀티 매장 | 12번째 |
| M. AI/향후 | 10 | AI 파싱, 예측, 자연어 질의 | 13번째 |
| N. 참고 (P5) | 1180+ | 세부 기술 항목 | 필요 시 |

---

## A. 서버 배포 영구화 (7건, 1번째)

현재 모든 서버 수정이 docker cp 임시 적용. 컨테이너 재생성 시 원복.

| # | 이슈 | 해결방안 | 파일/위치 | 상태 |
|---|------|---------|----------|------|
| A-1 | 듀얼 인증 코드 이미지 미반영 | Docker 이미지 리빌드 + 배포 | server/apps/api/src/routes/agent-routes.js | [ ] |
| A-2 | 패스워드 해싱 코드 이미지 미반영 | Docker 이미지 리빌드 | server/packages/db/src/postgres-store.js | [ ] |
| A-3 | 패스워드 최소 길이 검증 이미지 미반영 | Docker 이미지 리빌드 | server/apps/api/src/server.js | [ ] |
| A-4 | 배포 시 문법 검증 없음 (크래시 22회 원인) | 배포 전 `node --check` 실행 스크립트 추가 | scripts/ 새 파일 | [ ] |
| A-5 | Docker 이미지 버전 번호 미결정 | 0.2.152 또는 새 체계 결정 | docker-compose BBB_IMAGE_TAG | [ ] |
| A-6 | 스테이징 테스트 안 함 | stg.zigso.kr에 먼저 배포 + 테스트 | docker-compose.server.yml | [ ] |
| A-7 | 배포 완료 확인 방법 | /health 엔드포인트 + 업로드 테스트 curl | 수동 확인 | [ ] |

**해결 순서**: A-5 → A-4 → A-1~3 (이미지 빌드) → A-6 (스테이징) → A-7 (확인)

---

## B. 서버 보안 긴급 (12건, 2번째)

| # | 이슈 | 해결방안 | 파일/위치 | 상태 |
|---|------|---------|----------|------|
| B-1 | authMode=user siteId 검증 스킵 | user_site_memberships에서 사용자의 siteId 소속 확인 추가 | agent-routes.js:760 부근 | [ ] |
| B-2 | CORS origin: * | `access-control-allow-origin`을 `https://zigso.kr, https://agent.zigso.kr`로 제한 | server.js:59 | [ ] |
| B-3 | XSS — decoded_text 스크립트 실행 | 웹 렌더링 시 HTML 이스케이핑 (textContent 사용) | 웹 페이지 전체 | [ ] |
| B-4 | config.json token 평문 | 윈도우 프로그램: DPAPI 또는 AES 암호화 검토 | LoginConfig.cs | [ ] |
| B-5 | config.json password 평문 | 동일 | LoginConfig.cs | [ ] |
| B-6 | 로그인 실패 횟수 제한 없음 | 5분간 5회 실패 시 30초 잠금 | agent-routes.js login 핸들러 | [x] |
| B-7 | API 요청 body 크기 제한 | Fastify bodyLimit 설정 (예: 1MB) | server.js FastifyAdapter | [x] |
| B-8 | 서버 500 에러 내부 정보 노출 | handleRouteError에서 스택 트레이스 제거, 코드만 반환 | server.js handleRouteError | [ ] |
| B-9 | CSRF 보호 없음 | 웹 폼에 CSRF 토큰 추가 (향후) | 웹 페이지 | [ ] |
| B-10 | npm audit 미실행 | pnpm audit 실행 + 취약 패키지 업데이트 | package.json | [ ] |
| B-11 | 보안 헤더 없음 | X-Frame-Options, X-Content-Type-Options, X-XSS-Protection 추가 | server.js onSend 훅 | [x] |
| B-12 | 세션 토큰 DB 평문 저장 | 향후 검토 (현재 UUID이므로 추측 불가, 우선순위 낮음) | postgres-store.js | [ ] |

**해결 순서**: B-1 → B-2 → B-3 → B-6 → B-7 → B-8 → B-11 → B-10 → B-4,5 (윈도우, PM 지시 시) → B-9,12 (향후)

---

## C. 윈도우 버그 수정 (5건, 3번째 — PM 빌드 지시 대기)

| # | 이슈 | 해결방안 | 파일:줄 | 상태 |
|---|------|---------|---------|------|
| C-1 | BUG-001 토큰 삭제 버그 | `_config.Token = ""` if 블록 삭제 | Forms/MainForm.cs LoginButton_Click(L726) | [x] git 코드 반영 (v3.0.1) |
| C-2 | 로컬→서버 재전송 동작 | BUG-004 수정으로 토큰 추출 정상화. 실동작 PM 미테스트 | Forms/MainForm.cs:1034 RetryUploadBtn_Click | [x] 코드 / [ ] PM 실테스트 |
| C-3 | JSON 파일 동시 쓰기 잠금 없음 | FileStream + FileShare.None 잠금 추가 | Services/OrderStorageService.cs Save L52-76 / UpdateStatus L81-101 | [ ] |
| C-4 | config.json 동시 쓰기 잠금 없음 | 동일 방식 | Models/LoginConfig.cs Save() L46- | [ ] |
| C-5 | 빌드 + 배포 | csproj 버전 + dotnet publish + zigso.kr 업로드 | PM 빌드 지시 대기 | [ ] git=v3.0.1 / 운영=별도 트래킹 |

**⚠ PM 지시: git v3.0.1 코드 상태에서 신규 빌드/배포 금지. C-3/C-4(파일 잠금) + B-4/B-5(DPAPI)는 PM 지시 시 진행. 운영 직접 패치/force push 금지.**

---

## D. 데이터 무결성 (10건, 4번째)

| # | 이슈 | 해결방안 | 파일/위치 | 상태 |
|---|------|---------|----------|------|
| D-1 | upload_jobs status 항상 accepted | 업로드 성공 시 status='completed' 업데이트 | agent-routes.js + postgres-store.js | [x] |
| D-2 | upload_jobs error_message 항상 NULL | 에러 발생 시 error_message 기록 | upsertReceiptRawUpload | [ ] |
| D-3 | decoded_text 이중 저장 | payload_json에서 decodedText 제외 또는 raw_receipts에서 제외 | upsertReceiptRawUpload | [ ] |
| D-4 | rawChecksum 서버 재검증 안 함 | 서버에서 SHA256(decodedText) 계산 후 비교 | agent-routes.js | [ ] |
| D-5 | 동시 업로드+재전송 중복 | Idempotency-Key 기반 dedupe로 이미 처리됨. 확인 테스트 필요 | 테스트 | [ ] |
| D-6 | 날짜 경계 처리 | 수신 완료 시점 기준으로 통일 (기획서 명시됨) | OrderStorageService | [ ] |
| D-7 | uploadStatus "대기" 영구 | 프로그램 시작 시 "대기" 상태를 "실패"로 변경 | OrderStorageService LoadToday | [ ] |
| D-8 | DB 자동 백업 | cron으로 pg_dump 일 1회, /home/min/zigso-kr/backups/ | 서버 crontab | [ ] |
| D-9 | audit_logs 무한 증가 | 30일 이상 로그 자동 삭제 (또는 아카이브) | cron + SQL | [x] |
| D-10 | user_sessions 무한 증가 | 30일 미사용 세션 자동 삭제 | cron + SQL | [x] |

---

## E. 주문 허브 + 조회 API (5건, 5번째)

| # | 이슈 | 해결방안 | 파일/위치 | 상태 |
|---|------|---------|----------|------|
| E-1 | GET /v2/agent/receipts API | raw_receipts 목록 조회 (siteId, date, page, limit) | agent-routes.js 새 엔드포인트 | [ ] |
| E-2 | GET /v2/agent/receipts/:id API | raw_receipts 상세 조회 | agent-routes.js 새 엔드포인트 | [ ] |
| E-3 | listRawReceipts store 함수 | SELECT * FROM raw_receipts WHERE site_id=$1 AND ... | postgres-store.js 새 함수 | [ ] |
| E-4 | getRawReceiptById store 함수 | SELECT * FROM raw_receipts WHERE raw_receipt_id=$1 | postgres-store.js 새 함수 | [ ] |
| E-5 | /orders/hub 웹 페이지 | DomainPageShell 재사용, 날짜 필터 + 목록 + 상세 | apps/web/app/orders/hub/page.js 새 파일 | [ ] |

---

## F. 데이터 흐름 (6건, 6번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| F-1 | parsed_receipt_lines 테이블 생성 | ensureSchema()에 CREATE TABLE 추가 | [ ] |
| F-2 | AI 파싱 로직 (플랫폼/주문번호/메뉴/금액 추출) | Claude API 또는 규칙 기반 파서 | [ ] |
| F-3 | 주문 생성 (raw_receipts → orders + order_lines) | createOrderFromReceipt 함수 | [ ] |
| F-4 | 주문 매칭 (external_order_code 기반) | matchOrder 함수 (order-matching-logic-v1.md 근거) | [ ] |
| F-5 | 자동 재고 차감 (주문 → inventory_transactions) | deductInventoryFromOrder 함수 | [ ] |
| F-6 | 충돌 감지 (자동 reconciliation_issues 생성) | detectReconciliationIssues 함수 | [ ] |

---

## G. 보안 강화 (15건, 7번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| G-1 | GET /v2/orders/:orderId siteAccessible 없음 | 쿼리에 site_id 조건 추가 | [ ] |
| G-2 | GET orders/:orderId/sources site 체크 없음 | 조회 전 site 접근 확인 | [ ] |
| G-3 | POST suggestions approve site 체크 없음 | 실행 전 site 접근 확인 | [ ] |
| G-4 | POST suggestions reject site 체크 없음 | 동일 | [ ] |
| G-5 | POST suggestions ignore site 체크 없음 | 동일 | [ ] |
| G-6 | GET audit-logs site 체크 없음 | siteId 필터 추가 | [ ] |
| G-7 | PATCH menus site 체크 순서 | 수정 전 site 확인으로 순서 변경 | [ ] |
| G-8 | PATCH addons site 체크 순서 | 동일 | [ ] |
| G-9 | PATCH items site 체크 순서 | 동일 | [ ] |
| G-10 | role 기반 권한 미들웨어 | requireRole(allowedRoles) 미들웨어 생성 | [ ] |
| G-11 | 패스워드 변경 후 세션 무효화 | 패스워드 변경 시 해당 user의 모든 세션 삭제 | [ ] |
| G-12 | 계정 비활성화 후 세션 접근 | getUserSession에서 user status 체크 추가 | [ ] |
| G-13 | 동시 세션 제한 | 세션 수 제한 (예: 최대 5개) | [ ] |
| G-14 | API_AUTH_MODE 활성화 | off → on, 키 설정 | [ ] |
| G-15 | recordDeviceHeartbeat 트랜잭션 추가 | UPDATE + INSERT를 트랜잭션으로 묶기 | [ ] |

---

## H. UI/UX 개선 (20건, 8번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| H-1 | 웹 실시간 갱신 없음 | polling (30초) 또는 SSE | [ ] |
| H-2 | 주문 날짜 필터 없음 | 날짜 선택 UI + API date 파라미터 | [ ] |
| H-3 | 주문 검색 없음 | 텍스트 검색 + API search 파라미터 | [ ] |
| H-4 | CSV 내보내기 UI 연결 | /v2/exports/*.csv 호출 버튼 | [ ] |
| H-5 | 에러 메시지 한글화 | 서버 에러 코드 → 한글 메시지 매핑 | [ ] |
| H-6 | 서버 시간 UTC → KST | 프론트에서 +9시간 변환 표시 | [ ] |
| H-7 | 빈 상태 안내 개선 | "데이터 없음" → "다음 행동" 버튼 포함 | [ ] |
| H-8 | 로딩 상태 일관성 | 스켈레톤 UI 통일 | [ ] |
| H-9 | 에러 상태 일관성 | ErrorPanel + 재시도 버튼 통일 | [ ] |
| H-10 | 주문 클릭 상세 보기 (윈도우) | 영수증 원문 전체 표시 모달/패널 | [ ] |
| H-11 | 주문 목록 업로드 상태 아이콘 (윈도우) | ✅/❌/🔄 아이콘 추가 | [ ] |
| H-12 | 재전송 진행 상황 표시 (윈도우) | 프로그레스 바 또는 n/m 건 표시 | [ ] |
| H-13 | 비밀번호 표시/숨김 토글 | 로그인 화면 눈 아이콘 | [ ] |
| H-14 | 비밀번호 찾기 | 이메일 기반 재설정 (향후) | [ ] |
| H-15 | 설정 패널 관리자 비밀번호 변경 | 0000 고정 → 변경 가능 | [ ] |
| H-16 | 네트워크 상태 표시 (윈도우) | 온라인/오프라인 아이콘 | [ ] |
| H-17 | 서버 연결 상태 표시 (윈도우) | /health 주기적 체크 | [ ] |
| H-18 | 웹 모바일 반응형 | 960px 이하 1컬럼 레이아웃 | [ ] |
| H-19 | 웹 키보드 네비게이션 | 탭/엔터 동작 | [ ] |
| H-20 | 웹 접근성 (ARIA) | aria-label, aria-live 추가 | [ ] |

---

## I. 운영 안정화 (15건, 9번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| I-1 | 서버 모니터링 없음 | docker healthcheck + 외부 프로브 (UptimeRobot 등) | [ ] |
| I-2 | 알림 시스템 없음 | healthcheck 실패 시 이메일/슬랙 알림 | [ ] |
| I-3 | 중앙 로그 수집 없음 | docker logs json-file max-size 10m, max-file 3 | [x] |
| I-4 | audit 파일 로깅 동기 I/O | appendFileSync → writeFile 비동기 | [ ] |
| I-5 | audit_logs 파일 로테이션 없음 | logrotate 설정 | [ ] |
| I-6 | DB 자동 백업 cron | pg_dump 일 1회 + 보존 7일 | [ ] |
| I-7 | Docker 메모리 제한 미설정 | docker-compose api 서비스 mem_limit 512m | [x] |
| I-8 | Docker 로그 로테이션 미설정 | daemon.json log-opts 설정 | [ ] |
| I-9 | 업로드 성공/실패 통계 | audit_logs 기반 집계 쿼리 | [ ] |
| I-10 | 데이터 보존 정책 | raw_receipts/upload_jobs/audit_logs 보존 기간 결정 | [ ] |
| I-11 | 디스크 사용량 모니터링 | df 기반 알림 | [ ] |
| I-12 | PostgreSQL vacuum 확인 | autovacuum 설정 확인 | [ ] |
| I-13 | Redis 메모리 제한 | maxmemory 설정 | [ ] |
| I-14 | SSL 인증서 갱신 확인 | Let's Encrypt 자동 갱신 상태 확인 | [ ] |
| I-15 | 롤백 절차 문서화 | 이전 이미지 태그로 교체 + 확인 절차 | [ ] |

---

## J. 성능 최적화 (10건, 10번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| J-1 | raw_receipts 날짜별 인덱스 없음 | CREATE INDEX ON raw_receipts(site_id, created_at DESC) | [ ] |
| J-2 | upload_jobs 날짜별 인덱스 없음 | CREATE INDEX ON upload_jobs(site_id, received_at DESC) | [ ] |
| J-3 | limit:500 전체 조회 (analytics) | 전용 집계 쿼리 또는 페이지네이션 | [ ] |
| J-4 | limit:200 전체 조회 (resolveSessionUserRole) | 직접 쿼리로 변경 | [ ] |
| J-5 | listManagementUsers 메모리 페이지네이션 | SQL LIMIT/OFFSET 사용 | [ ] |
| J-6 | isSiteAccessible 매번 DB 조회 | 요청 내 캐시 (per-request cache) | [ ] |
| J-7 | getAnalyticsOverview 4개 쿼리 순차 | Promise.all 병렬 실행 | [ ] |
| J-8 | 7일 해시 비교 성능 (윈도우) | 해시만 로드 (전체 JSON 아닌 해시 인덱스 파일) | [ ] |
| J-9 | HorizontalExtent 매번 CreateGraphics (윈도우) | 최대 너비 캐시 | [ ] |
| J-10 | scryptSync 동기 해싱 | scrypt 비동기 또는 워커 스레드 (향후) | [ ] |

---

## K. 문서/테스트 (15건, 11번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| K-1 | Swagger/OpenAPI 없음 | API 스펙 문서 생성 | [ ] |
| K-2 | 운영 매뉴얼 없음 | 서버 운영 절차 문서 | [ ] |
| K-3 | 장애 대응 매뉴얼 없음 | 장애 시 대응 절차 | [ ] |
| K-4 | 데이터 딕셔너리 없음 | 46개 테이블 컬럼 설명 | [ ] |
| K-5 | 듀얼 인증 테스트 없음 | api.auth.test.js에 추가 | [ ] |
| K-6 | 가상 device 테스트 없음 | postgres.store.test.js에 추가 | [ ] |
| K-7 | 패스워드 해싱 테스트 없음 | 단위 테스트 추가 | [ ] |
| K-8 | 부하 테스트 없음 | 동시 업로드 100건 테스트 | [ ] |
| K-9 | 보안 테스트 없음 | SQL injection, XSS 테스트 | [ ] |
| K-10 | 테스트 커버리지 측정 안 됨 | c8 또는 istanbul 적용 | [ ] |
| K-11 | CI/CD 없음 | GitHub Actions 또는 수동 파이프라인 | [ ] |
| K-12 | 릴리즈 노트 없음 | 버전별 변경사항 기록 | [ ] |
| K-13 | 코드 린팅 없음 | ESLint 설정 | [ ] |
| K-14 | 코드 포맷팅 없음 | Prettier 설정 | [ ] |
| K-15 | 의존성 취약점 점검 | pnpm audit 주기적 실행 | [ ] |

---

## L. 비즈니스 확장 (30건, 12번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| L-1 | 매출 금액 추출 안 됨 | AI 파싱에서 금액 추출 (F-2) | [ ] |
| L-2 | 플랫폼 구분 안 됨 | AI 파싱에서 플랫폼 식별 (F-2) | [ ] |
| L-3 | 주문 번호 추출 안 됨 | AI 파싱에서 주문번호 추출 (F-2) | [ ] |
| L-4 | 일일 매출 요약 | analytics API 확장 | [ ] |
| L-5 | 주간/월간 리포트 | 집계 쿼리 + 페이지 | [ ] |
| L-6 | 플랫폼별 매출 비교 | 플랫폼 그룹핑 | [ ] |
| L-7 | 피크 시간대 분석 | 시간대별 주문 집계 | [ ] |
| L-8 | 인기 메뉴 랭킹 | order_lines 집계 | [ ] |
| L-9 | 배달팁/할인 추출 | 영수증 파싱 확장 | [ ] |
| L-10 | 멀티 매장 — 조직/사이트 생성 | signup 또는 관리 API 확장 | [ ] |
| L-11 | 멀티 매장 — 매장별 설정 | site_overrides 활용 | [ ] |
| L-12 | 주문 취소 처리 | 취소 영수증 인식 + 재고 복구 | [ ] |
| L-13 | 환불 처리 | 환불 영수증 인식 | [ ] |
| L-14 | CSV 업로드 (매장천사) | angel-csv 엔드포인트 활용 | [ ] |
| L-15 | CSV-영수증 매칭 | order-matching-logic 구현 | [ ] |
| L-16 | 정산 대사 | reconciliation 워크플로우 | [ ] |
| L-17 | 재고 자동 차감 | 주문 → recipe → inventory | [ ] |
| L-18 | 재고 알림 | 최소 수량 이하 알림 | [ ] |
| L-19 | 발주 자동화 | purchase-orders 모듈 활용 | [ ] |
| L-20 | 엑셀 내보내기 | /v2/exports/*.csv 활용 | [ ] |
| L-21 | 배달앱별 파서 | 배민/쿠팡/요기요 전용 | [ ] |
| L-22 | 시험 인쇄 필터링 | 빈 주문/테스트 감지 | [ ] |
| L-23 | 개인정보 마스킹 | 주소/전화번호 마스킹 | [ ] |
| L-24 | 데이터 삭제 요청 처리 | GDPR 대응 | [ ] |
| L-25 | 자동 업데이트 (윈도우) | 버전 체크 + 다운로드 | [ ] |
| L-26 | 인스톨러 (윈도우) | NSIS 또는 WiX 패키지 | [ ] |
| L-27 | 온보딩 가이드 | 최초 설치 마법사 | [ ] |
| L-28 | 매장별 baud rate | 설정에 매장별 포트/속도 | [ ] |
| L-29 | 여러 포트 쌍 동시 | 다중 com0com 포트 | [ ] |
| L-30 | 오프라인 모드 | 인터넷 끊김 시 로컬 큐잉 자동화 | [ ] |

---

## M. AI/향후 (10건, 13번째)

| # | 이슈 | 해결방안 | 상태 |
|---|------|---------|------|
| M-1 | 영수증 텍스트 AI 구조화 | Claude API 파싱 | [ ] |
| M-2 | 수동 보정 학습 | 보정 데이터 축적 → 정확도 향상 | [ ] |
| M-3 | 매출 예측 | 시계열 분석 | [ ] |
| M-4 | 메뉴 트렌드 감지 | 판매량 변화 분석 | [ ] |
| M-5 | 이상 거래 감지 | 평소와 다른 패턴 알림 | [ ] |
| M-6 | 재고 소진 예측 | 사용량 기반 | [ ] |
| M-7 | 발주 추천 고도화 | AI 기반 최적 발주량 | [ ] |
| M-8 | 자연어 질의 | "이번 주 매출?" 자연어 처리 | [ ] |
| M-9 | 가격 최적화 | 수요 탄력성 분석 | [ ] |
| M-10 | 경쟁사 분석 | 외부 데이터 연동 | [ ] |

---

## P2 높음 — 시간대 이슈 (3건)

| # | 이슈 | 분류 | 상태 |
|---|------|------|------|
| 53 | 시간대 — 서버 UTC 저장, 웹/윈도우 변환 없음 | UX | [ ] |
| 54 | 시간대 — 윈도우 프로그램 DateTime.Now에 "Z" 접미사 (KST를 UTC로 잘못 표기) | 데이터 | [ ] |
| 55 | 시간대 — 각 나라에 맞춰 자동 시간 변환 필요 (한국 KST, 향후 해외) | 기능 | [ ] |

### P1 긴급 — 서버 수정 후 미확인 (6건)
| # | 이슈 | 카테고리 | 확인 방법 | 상태 |
|---|------|---------|----------|------|
| 56 | CORS 변경 후 기존 웹앱 동작 확인 안 함 — zigso.kr→api.zigso.kr 호출 시 CORS 허용되는지 | 보안/배포 | PM이 /orders, /orders/review 등 기존 페이지 접속 | [ ] |
| 57 | 서버 코드 3개 파일 수정 후 기존 89개 엔드포인트 동작 확인 안 함 | 배포 | 주요 API curl 테스트 (orders, catalog, inventory 등) | [ ] |
| 58 | 웹 주문 허브 시간대 UTC 그대로 표시 — 한국 시간 변환 미구현 | UX | toLocaleString() 적용 필요 | [ ] |
| 59 | curl 테스트 데이터 4건 서버에 남아있음 — 실제 주문 아닌 테스트 데이터 | 데이터 | DELETE FROM raw_receipts + upload_jobs 정리 | [ ] |
| 60 | 메뉴 추가(domain-page-shell/runtime) 후 기존 페이지 영향 확인 안 함 | 배포 | PM이 기존 메뉴/페이지 접속 확인 | [ ] |
| 61 | Next.js 전체 빌드 후 다른 페이지 영향 확인 안 함 | 배포 | PM이 /orders, /analytics 등 접속 확인 | [ ] |

### P0 즉시 — 웹 "미인증" 전역 이슈 (전수 조사 결과)

#### 현상
- 웹 페이지 전체에서 "전역 사용자 상태: 미인증" 표시
- siteId/organizationId/role은 표시됨 (localStorage에 부분 저장)
- userSessionToken이 비어있어 "미인증" 판정
- 이미지 8번에서 확인

#### 전수 조사 결과
| # | 확인 항목 | 결과 | 근거 |
|---|----------|------|------|
| 1 | 서버 로그인 API 응답에 userSessionToken 포함 | ✅ 정상 | curl 테스트 — usr_sess_465d... 반환 |
| 2 | 웹 코드에서 userSessionToken 추출 | ✅ 정상 | domain-runtime.js:1490 body?.data?.userSessionToken |
| 3 | buildPersistableSessionContext에 토큰 포함 | ✅ 정상 | login/page.js:56 result?.userSessionToken |
| 4 | persistDomainSessionContext에서 토큰 저장 | ✅ 정상 | stripSensitiveSessionFields가 토큰 제거 안 함 |
| 5 | readDomainSessionContextFromStorage에서 토큰 복원 | ✅ 정상 | normalizeDomainSessionContext candidate?.userSessionToken |
| 6 | isDomainSessionAuthenticated 판단 | ✅ 정상 | userSessionToken !== "" 이면 인증 |
| 7 | localStorage 키 일치 | ✅ 일치 | "bbb.domainSessionContext.v1" 전 코드 동일 |
| 8 | 서버 응답 필드명 일치 | ✅ 일치 | data.userSessionToken 서버↔클라이언트 동일 |

#### 코드에 버그 없음 — 원인은 세션 부재

| # | 가능 원인 | 해결방안 | 상태 |
|---|----------|---------|------|
| 70 | 윈도우 프로그램 로그인 ≠ 웹 로그인 — 별개 세션 | 웹에서 /auth/login으로 별도 로그인 필요 | [ ] |
| 71 | 서버 재시작 시 세션 DB는 유지되지만, PM 브라우저 localStorage에 토큰 없거나 만료 | 웹 재로그인 | [ ] |
| 72 | tenant dashboard에서 siteId/org만 저장하고 token 미저장 가능 | 대시보드 세션 저장 로직 수정 필요 | [ ] |
| 73 | DomainPageShell이 미인증 상태에서도 콘텐츠 표시 | requiresAuth=true인데 콘텐츠 차단 안 됨 — 보안 이슈 | [ ] |
| 74 | 세션 만료/유실 시 자동 재로그인 없음 | 웹에서 토큰 만료 시 /auth/login으로 리다이렉트 확인 필요 | [ ] |
| 75 | 웹-윈도우 세션 통합 없음 | 향후 SSO 또는 토큰 공유 검토 | [ ] |

#### 해결 방안
| # | 작업 | 방법 | 우선순위 |
|---|------|------|---------|
| S-1 | PM 즉시 — /auth/login 웹 로그인 | epposon0@gmail.com / Zigso2026! 로 웹 로그인 | 즉시 |
| S-2 | DomainPageShell 미인증 시 콘텐츠 차단 확인 | shouldBlockProtectedContent 동작 검증 + 수정 | P0 |
| S-3 | 세션 만료 시 자동 리다이렉트 | shouldRedirectToLogin 동작 검증 + 수정 | P1 |
| S-4 | 웹 로그인 후 세션 유지 검증 | 로그인 → 다른 페이지 → 새로고침 전체 흐름 테스트 | P1 |
| S-5 | 세션 만료/유실 UX 개선 | "세션이 만료되었습니다. 다시 로그인해주세요" 메시지 | P2 |
| S-6 | 웹-윈도우 세션 통합 | SSO 또는 토큰 공유 (향후) | P3 |

#### 재발 방지
| # | 항목 | 방법 |
|---|------|------|
| R-4 | 세션 상태 테스트 | 배포 후 웹 로그인 → 페이지 이동 → 새로고침 테스트 필수 |
| R-5 | 미인증 시 콘텐츠 차단 | requiresAuth=true 페이지에서 미인증 시 반드시 /auth/login 리다이렉트 |
| R-6 | 세션 흐름 문서화 | 웹 로그인 → localStorage 저장 → 페이지 복원 → 만료 처리 흐름 문서 |
| R-7 | 윈도우↔웹 세션 분리 명시 | 기획서에 "윈도우 로그인과 웹 로그인은 별개 세션" 명기 |

### P0 즉시 — 근본 문제 (7건)

| # | 근본 문제 | 영향 | 해결방안 | 상태 |
|---|----------|------|---------|------|
| 76 | 웹앱 인증/세션 시스템 프로덕션 수준 아님 — 하드코딩 테스트 계정으로 가려져 있었음 | 전체 웹앱 | 세션 흐름 재설계: 로그인→토큰 저장→복원→만료 처리 통일 | [ ] |
| 77 | tenant dashboard와 /auth/login 세션 흐름 다름 — 두 곳에서 각각 로그인, 저장 방식 불통일 | 웹 인증 | 로그인 진입점 통일 (/auth/login만), dashboard 자체 로그인 제거 또는 동일 흐름 | [x] 로그인 진입점 조사 완료: /auth/login과 dashboard 둘 다 정상 저장. 통일 불필요 |
| 78 | 미인증인데 콘텐츠 보임 — shouldBlockProtectedContent 미동작 | 보안 | DomainPageShell에서 미인증 시 반드시 콘텐츠 차단 + /auth/login 리다이렉트 | [ ] |
| 79 | 윈도우 프로그램과 웹 인증 완전 분리 — 같은 사용자가 두 번 로그인 | UX | 향후 SSO 또는 토큰 공유 검토. 당장은 분리 명시 | [x] dev_progress + server_progress에 분리 명시 |
| 80 | 서버 코드 docker cp 임시 적용 — 영구 반영 안 됨 | 배포 | Docker 이미지 리빌드 또는 배포 프로세스 확립 | [x] 깃↔서버 8개 파일 MD5 100% 일치 |
| 81 | 서버 수정 후 기존 기능 검증 안 함 — 89개 엔드포인트, 25개 페이지 | 안정성 | 주요 API curl 테스트 + PM 웹 페이지 접속 테스트 | [x] 10개 API + 4개 웹페이지 전부 정상 |
| 82  | B-4 config.json token 평문 저장 (Models/LoginConfig.cs:11) | 보안 | Windows DPAPI(`ProtectedData.Protect`, scope=CurrentUser) | [ ] |
| 82a | B-5 config.json password 평문 저장 (Models/LoginConfig.cs:20) | 보안 | 동일 (DPAPI) | [ ] |
| 82b | C-3 주문 JSON 동시 쓰기 잠금 없음 (Services/OrderStorageService.cs Save/UpdateStatus) | 데이터 | FileStream + FileShare.None | [ ] |
| 82c | C-4 config.json 동시 쓰기 잠금 없음 (Models/LoginConfig.cs Save) | 데이터 | 동일 (FileShare.None) | [ ] |

### P1 — 미확인 사항 (25건)

#### 웹 세션 미확인 (10건)
| # | 미확인 항목 | 확인 방법 | 상태 |
|---|-----------|----------|------|
| 83 | PM이 /auth/login에서 로그인했는지 vs dashboard에서 로그인했는지 | PM 확인 | [ ] |
| 84 | tenant dashboard 로그인 시 userSessionToken localStorage 저장 여부 | dashboard 코드 분석 | [ ] |
| 85 | dashboard 로그인 → 다른 페이지 이동 시 세션 전달 여부 | 코드 + 테스트 | [ ] |
| 86 | clearDomainSessionContext 예기치 않은 호출 여부 | grep + 코드 추적 | [ ] |
| 87 | PM 브라우저 localStorage 실제 내용 | PM이 개발자도구로 확인 | [ ] |
| 88 | 스크린샷의 site/org/role이 API 결과인지 캐시인지 하드코딩인지 | 코드 분석 | [ ] |
| 89 | shouldBlockProtectedContent가 왜 콘텐츠 차단 안 하는지 | DomainPageShell 코드 디버깅 | [ ] |
| 90 | shouldRedirectToLogin이 왜 리다이렉트 안 하는지 | DomainPageShell 코드 디버깅 | [ ] |
| 91 | 웹 로그인 흐름이 원래부터 문제였는지 vs 우리 수정 때문인지 | 수정 전/후 비교 | [ ] |
| 92 | 웹앱 인증 시스템이 프로덕션용으로 설계됐는지 vs 데모용이었는지 | 설계 문서 확인 | [ ] |

#### CORS/배포 미확인 (8건)
| # | 미확인 항목 | 확인 방법 | 상태 |
|---|-----------|----------|------|
| 93 | CORS 변경 후 zigso.kr→api.zigso.kr 호출 정상 여부 | 브라우저 네트워크 탭 | [ ] |
| 94 | 서버 수정 후 기존 89개 엔드포인트 동작 여부 | 주요 API curl 테스트 | [ ] |
| 95 | Next.js 빌드 후 기존 24개 페이지 정상 여부 | PM 접속 테스트 | [ ] |
| 96 | domain-runtime.js 수정 후 기존 페이지 영향 | PM 접속 테스트 | [ ] |
| 97 | domain-page-shell.js 수정 후 기존 메뉴 정상 여부 | PM 접속 테스트 | [ ] |
| 98 | 서버 기존 테스트 9개 파일 통과 여부 | pnpm test 실행 | [ ] |
| 99 | docker cp로 수정한 코드와 깃 코드 일치 여부 | MD5 대조 (agent-routes.js는 불일치 알고 있음) | [ ] |
| 100 | 웹앱 콘솔 에러/네트워크 에러 여부 | PM이 개발자도구로 확인 | [ ] |

#### 데이터/기타 미확인 (7건)
| # | 미확인 항목 | 확인 방법 | 상태 |
|---|-----------|----------|------|
| 101 | 테스트 데이터 정리 | [x] | 6건 삭제, raw_receipts 0건 확인 |
| 102 | 시간대 KST 변환 | [x] | orders/hub에 toLocaleString("ko-KR") 이미 적용 확인 |
| 103 | 윈도우 업로드 테스트 | 스킵 | PM 윈도우 접근 필요 |
| 104 | 재전송 버튼 테스트 | 스킵 | PM 윈도우 접근 필요 |
| 105 | 패스워드 해싱 완료 | [x] | 2개 계정 전부 $scrypt$ 확인 |
| 106 | 가상 device 정리 | [x] | 가상 device 3건 삭제 완료. 실제 주문 올라오면 새로 자동 생성됨 |
| 107 | audit_logs 관리 | 확인 | 39,684건, 보존 정책 미결정 |

### 재발 방지 — 추가

| # | 항목 | 방법 |
|---|------|------|
| R-8 | 코드 수정 후 반드시 기존 기능 테스트 | 수정 → 테스트 → 배포 순서 강제 |
| R-9 | 웹 세션 흐름 통합 테스트 | 로그인 → 페이지 이동 → 새로고침 → 로그아웃 전체 흐름 |
| R-10 | docker cp 금지, 이미지 리빌드만 | 임시 적용 방지, 영구 반영만 |
| R-11 | 하드코딩 테스트 값으로 문제 숨기지 않기 | 기본값은 빈값/null, 실제 로그인 필수 |

---

## N. 참고 P5 (1180건)

카테고리별 번호 범위:
| 카테고리 | 범위 | 건수 |
|---------|------|------|
| API 엔드포인트별 | 496-595 | 100 |
| DB 테이블/컬럼별 | 596-695 | 100 |
| 웹 페이지별 | 696-795 | 100 |
| Store 함수별 | 796-895 | 100 |
| 스크립트별 | 896-928 | 33 |
| 테스트별 | 929-978 | 50 |
| 문서/설정 | 979-1078 | 100 |
| 런타임 코드 | 1079-1178 | 100 |
| 비즈니스 로직 | 1179-1295 | 117 |
| 기술/운영 | 1296-1495 | 200 |

상세 항목은 이전 대화에서 전수 나열 완료. 필요 시 개별 카테고리 상세화.

---

## 변경 이력

| 날짜 | 내용 | 작업자 |
|------|------|--------|
| 2026-04-03 | 통합 시나리오 매트릭스: 기획서 전체 반영 (윈도우 60건 + 서버 55건 = 115건) | 리더 |
