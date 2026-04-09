using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeliveryOrderReceiver.Helpers;

namespace DeliveryOrderReceiver.Models;

/// <summary>
/// 사용자 설정 + 자격 증명 (config.json).
///
/// 보안 (B-4/B-5 fix):
///   - Token / Password는 절대 평문 저장 안 함
///   - DPAPI(CurrentUser scope)로 암호화 후 저장
///   - 디스크에는 *Encrypted 필드만 직렬화됨
///   - 메모리에서만 평문 접근 (Token / Password 프로퍼티)
///
/// 동시 쓰기 (C-4 fix):
///   - Save() / Load()는 FileLockHelper로 FileShare.None 잠금
///   - tmp + atomic replace
/// </summary>
public class LoginConfig
{
    public string Email { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = "https://agent.zigso.kr";
    public string SiteId { get; set; } = string.Empty;
    public string LastPort { get; set; } = string.Empty;
    public int LastBaudRate { get; set; } = 9600;
    public bool AutoStart { get; set; }
    public bool AutoLogin { get; set; }
    public bool SaveLoginInfo { get; set; }
    public string CreatedPortA { get; set; } = string.Empty;
    public string CreatedPortB { get; set; } = string.Empty;

    // 디스크 직렬화용 (DPAPI 암호화 base64)
    [JsonPropertyName("tokenEncrypted")]
    public string TokenEncrypted { get; set; } = string.Empty;

    [JsonPropertyName("passwordEncrypted")]
    public string PasswordEncrypted { get; set; } = string.Empty;

    [JsonPropertyName("adminPasswordEncrypted")]
    public string AdminPasswordEncrypted { get; set; } = string.Empty;

    // 메모리 평문 접근 (직렬화 제외)
    [JsonIgnore]
    public string Token
    {
        get => DpapiHelper.Unprotect(TokenEncrypted);
        set => TokenEncrypted = DpapiHelper.Protect(value);
    }

    [JsonIgnore]
    public string Password
    {
        get => DpapiHelper.Unprotect(PasswordEncrypted);
        set => PasswordEncrypted = DpapiHelper.Protect(value);
    }

    [JsonIgnore]
    public string AdminPassword
    {
        get => DpapiHelper.Unprotect(AdminPasswordEncrypted);
        set => AdminPasswordEncrypted = DpapiHelper.Protect(value);
    }

    private static string ConfigPath => Path.Combine(App.AppDataDir, "config.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static LoginConfig Load()
    {
        if (!File.Exists(ConfigPath)) return new LoginConfig();
        try
        {
            var json = FileLockHelper.ReadAllTextLocked(ConfigPath);
            return JsonSerializer.Deserialize<LoginConfig>(json) ?? new LoginConfig();
        }
        catch
        {
            return new LoginConfig();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, SerializerOptions);
        FileLockHelper.WriteAllTextAtomic(ConfigPath, json);
    }
}
