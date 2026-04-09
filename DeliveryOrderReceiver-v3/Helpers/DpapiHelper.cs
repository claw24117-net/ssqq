using System;
using System.Security.Cryptography;
using System.Text;

namespace DeliveryOrderReceiver.Helpers;

/// <summary>
/// Windows DPAPI 헬퍼 — token/password 평문 저장 방지 (B-4/B-5 fix).
/// scope=CurrentUser: 동일 Windows 사용자 계정에서만 복호화 가능.
/// 매장 단일 운영자 PC 시나리오 적합.
/// </summary>
public static class DpapiHelper
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DOR-v3.0.1-config-entropy-2026");

    public static string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            // DPAPI 사용 불가 (예: 다른 OS) — 이 빌드는 net8.0-windows이라 실제로 못 옴
            return string.Empty;
        }
    }

    public static string Unprotect(string? base64)
    {
        if (string.IsNullOrEmpty(base64)) return string.Empty;
        try
        {
            var encrypted = Convert.FromBase64String(base64);
            var bytes = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // 다른 사용자 계정으로 옮긴 config 파일 등 — 복호화 실패는 빈 문자열
            return string.Empty;
        }
    }
}
