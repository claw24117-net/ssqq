# 배달 주문 수신기 (Delivery Order Receiver)

## 프로젝트 개요

배달 주문 수신기는 배달 플랫폼에서 주문 정보를 실시간으로 수신하고 처리하는 Windows Forms 기반 애플리케이션입니다.

## 버전

- **현재 버전**: v2.0.4

## 주요 기능

- 배달 주문 정보 실시간 수신
- 주문 데이터 파싱 및 처리
- Windows Forms 기반 UI
- 주문 상태 관리 및 추적

## 프로젝트 구조

```
ssqq/
├── Program.cs                      # 애플리케이션 진입점
├── DeliveryOrderReceiver.csproj    # 프로젝트 파일
├── Forms/                          # UI Forms
├── Models/                         # 데이터 모델
├── Services/                       # 비즈니스 로직 서비스
├── v2.0.4-analysis.md             # v2.0.4 분석 문서
└── README.md                       # 이 파일
```

## 개발 환경

- **.NET Framework**: .NET Framework 기반
- **UI Framework**: Windows Forms
- **언어**: C#

## 빌드 및 실행

```bash
# 빌드
dotnet build DeliveryOrderReceiver.csproj

# 실행
dotnet run --project DeliveryOrderReceiver.csproj
```

## 라이선스

내부 프로젝트

## 문제 보고

이슈나 버그는 프로젝트 관리자에게 보고하세요.
