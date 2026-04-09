#requires -version 5.1
<#
.SYNOPSIS
    DeliveryOrderReceiver v3.0.1 다운로드 (수동 / 매장 PC 테스트용)

.DESCRIPTION
    zigso.kr 로그인 → v3.0.1 exe 다운로드 → SHA256 검증.
    이 스크립트는 latest.json을 건드리지 않으므로 매장 PC 자동 업데이트(v2.0.4)에 영향 없음.

    주의:
      - v3.0.1은 실기기 테스트 0건 상태. 운영 매장에 바로 설치 금지.
      - 단일 매장 1대에서만 시험 설치 후 PM 검증 받기.
      - 기존 v2.0.4 와 같은 폴더에 덮어쓰지 말 것 (DPAPI 마이그레이션 미구현).

.PARAMETER Email
    zigso.kr 로그인 이메일. 미지정 시 대화형 입력.

.PARAMETER OutFile
    저장 경로. 미지정 시 현재 폴더의 DeliveryOrderReceiver-v3.0.1.exe.

.PARAMETER Server
    기본 https://agent.zigso.kr.

.EXAMPLE
    .\Download-DOR-v3.0.1.ps1
    .\Download-DOR-v3.0.1.ps1 -Email epposon0@gmail.com -OutFile D:\dor-test\v3.0.1.exe
#>

[CmdletBinding()]
param(
    [string]$Email,
    [string]$OutFile = ".\DeliveryOrderReceiver-v3.0.1.exe",
    [string]$Server = "https://agent.zigso.kr"
)

$ErrorActionPreference = 'Stop'
$ExpectedSha256 = "e95d58351e0e31ebba6390f0ff47caf7094e274d1949c245daeedc6518843895"
$ExpectedSize = 71808727
$Version = "3.0.1"

function Write-Section($title) {
    Write-Host ""
    Write-Host "=== $title ===" -ForegroundColor Cyan
}

function Fail($message) {
    Write-Host "[실패] $message" -ForegroundColor Red
    exit 1
}

# TLS 1.2 강제
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "DeliveryOrderReceiver v$Version 다운로드 도구" -ForegroundColor Yellow
Write-Host "서버: $Server" -ForegroundColor Gray
Write-Host "저장: $OutFile" -ForegroundColor Gray

# ---- 1. 로그인 ----
Write-Section "1단계 — zigso.kr 로그인"
if ([string]::IsNullOrWhiteSpace($Email)) {
    $Email = Read-Host "이메일"
}
$securePwd = Read-Host "패스워드" -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
$plainPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null

$loginBody = @{ email = $Email; password = $plainPwd } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Method Post `
        -Uri "$Server/v2/agent/auth/login" `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
} catch {
    Fail "로그인 요청 실패: $($_.Exception.Message)"
}

# 평문 패스워드 즉시 폐기
$plainPwd = $null
[System.GC]::Collect()

$token = $loginResp.data.userSessionToken
if ([string]::IsNullOrEmpty($token)) {
    Fail "응답에 userSessionToken 없음 (data 객체 안 확인)"
}
Write-Host "[OK] 로그인 성공" -ForegroundColor Green

# ---- 2. manifest 조회 ----
Write-Section "2단계 — v$Version manifest 확인"
$manifestUrl = "$Server/api/windows-agent/download?version=$Version&kind=manifest.json"
$headers = @{ Authorization = "Bearer $token" }
try {
    $manifestResp = Invoke-RestMethod -Method Get -Uri $manifestUrl -Headers $headers
} catch {
    Fail "manifest 조회 실패: $($_.Exception.Message)"
}
$manifest = if ($manifestResp.data) { $manifestResp.data } else { $manifestResp }
Write-Host "버전     : $($manifest.version)" -ForegroundColor Gray
Write-Host "채널     : $($manifest.channel)" -ForegroundColor Gray
Write-Host "active   : $($manifest.active)" -ForegroundColor Gray
Write-Host "게시 시각: $($manifest.publishedAt)" -ForegroundColor Gray
if ($manifest.notes) {
    Write-Host "노트     : $($manifest.notes)" -ForegroundColor Yellow
}
if ($manifest.active -eq $true) {
    Write-Host "[경고] active=true. latest.json이 v$Version으로 설정돼 있다는 뜻." -ForegroundColor Yellow
}

# ---- 3. exe 다운로드 ----
Write-Section "3단계 — exe 다운로드 (~68 MB)"
$downloadUrl = "$Server/api/windows-agent/download?version=$Version&kind=exe"
$outDir = Split-Path -Parent $OutFile
if ($outDir -and -not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}
try {
    Invoke-WebRequest -Uri $downloadUrl -Headers $headers -OutFile $OutFile -UseBasicParsing
} catch {
    Fail "다운로드 실패: $($_.Exception.Message)"
}
$size = (Get-Item $OutFile).Length
Write-Host "[OK] 저장 완료 — $OutFile ($size bytes)" -ForegroundColor Green
if ($size -ne $ExpectedSize) {
    Write-Host "[경고] 크기 불일치 — 기대 $ExpectedSize, 실제 $size" -ForegroundColor Yellow
}

# ---- 4. SHA256 검증 ----
Write-Section "4단계 — SHA256 검증"
$hash = (Get-FileHash -Path $OutFile -Algorithm SHA256).Hash.ToLower()
Write-Host "기대: $ExpectedSha256" -ForegroundColor Gray
Write-Host "실제: $hash" -ForegroundColor Gray
if ($hash -eq $ExpectedSha256) {
    Write-Host "[OK] SHA256 일치 — 파일 무결성 검증 통과" -ForegroundColor Green
} else {
    Fail "SHA256 불일치. 파일 손상 또는 중간자 공격 가능성. 파일 폐기 권장."
}

Write-Section "완료"
Write-Host "다운로드 완료: $OutFile" -ForegroundColor Green
Write-Host ""
Write-Host "주의 사항:" -ForegroundColor Yellow
Write-Host "  1. 이건 v3.0.1 (git 트래킹 빌드, 실기기 미테스트)" -ForegroundColor Gray
Write-Host "  2. 매장 PC에 설치하기 전 단일 PC에서 동작 검증 필요" -ForegroundColor Gray
Write-Host "  3. v2.0.4 config (config.json) 와 호환되지 않음 (DPAPI 마이그레이션 미구현)" -ForegroundColor Gray
Write-Host "  4. 별도 폴더에 설치하고 새로 로그인할 것" -ForegroundColor Gray
Write-Host ""
