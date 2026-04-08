# 작업 로그 (DeliveryOrderReceiver)

> 본 파일은 git 트래킹 기준 작업 로그. 운영(prod) 직접 패치 이력은 포함하지 않음.
> 운영 배포 상태는 git과 별도 트래킹.

---

### 2026-04-08

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
