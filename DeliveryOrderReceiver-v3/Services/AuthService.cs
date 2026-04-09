using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DeliveryOrderReceiver.Models;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// 서버 인증 서비스.
///
/// BUG-004 fix (처음부터 반영):
///   - 로그인 응답 = { data: { userSessionToken, user, organizations, sites }, meta: {...} }
///   - 토큰 추출 경로: data.userSessionToken (data 객체 내부)
///   - 단일 진실 출처: server/apps/api/src/routes/agent-routes.js:145-240
///
/// BUG-001 fix:
///   - LoginAsync 성공 후 _config.Token = "" 같은 토큰 삭제 코드 절대 없음
///   - autoLogin 플래그만 호출자가 별도로 설정
/// </summary>
public class AuthService
{
    private readonly LoginConfig _config;
    private readonly HttpClient _http;

    public AuthService(LoginConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryOrderReceiver-v3/3.0.1");
    }

    public class LoginResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public int? StatusCode { get; init; }
    }

    /// <summary>
    /// 이메일/패스워드로 로그인 → Bearer token 발급.
    /// 성공 시 _config.Token / SiteId 채움.
    /// </summary>
    public async Task<LoginResult> LoginAsync(string email, string password, string serverUrl)
    {
        try
        {
            var url = $"{serverUrl.TrimEnd('/')}/v2/agent/auth/login";
            var body = new { email, password };
            using var resp = await _http.PostAsJsonAsync(url, body);

            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                return new LoginResult
                {
                    Success = false,
                    Error = ExtractErrorMessage(raw, resp.ReasonPhrase),
                    StatusCode = (int)resp.StatusCode
                };
            }

            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("data", out var dataEl))
                return new LoginResult { Success = false, Error = "응답 형식 오류 (data 없음)" };

            // BUG-004 fix: data.userSessionToken (data 객체 내부)
            if (!dataEl.TryGetProperty("userSessionToken", out var tokenEl))
                return new LoginResult { Success = false, Error = "응답 형식 오류 (userSessionToken 없음)" };

            var token = tokenEl.GetString();
            if (string.IsNullOrEmpty(token))
                return new LoginResult { Success = false, Error = "빈 토큰 응답" };

            _config.Token = token;
            _config.Email = email;
            _config.ServerUrl = serverUrl;

            // sites[0].id를 SiteId로 저장
            if (dataEl.TryGetProperty("sites", out var sitesEl) &&
                sitesEl.ValueKind == JsonValueKind.Array &&
                sitesEl.GetArrayLength() > 0)
            {
                var first = sitesEl[0];
                if (first.TryGetProperty("id", out var idEl))
                    _config.SiteId = idEl.GetString() ?? string.Empty;
            }

            return new LoginResult { Success = true };
        }
        catch (HttpRequestException ex)
        {
            return new LoginResult { Success = false, Error = $"네트워크 오류: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new LoginResult { Success = false, Error = "서버 응답 시간 초과" };
        }
        catch (Exception ex)
        {
            return new LoginResult { Success = false, Error = $"오류: {ex.Message}" };
        }
    }

    /// <summary>
    /// 저장된 토큰 검증 또는 자격 증명으로 재로그인.
    /// 자동 로그인 플로우용.
    /// </summary>
    public async Task<bool> ValidateOrRefreshAsync()
    {
        if (string.IsNullOrEmpty(_config.Token)) return await TryReloginAsync();

        // 토큰으로 가벼운 호출 (bootstrap)
        try
        {
            var url = $"{_config.ServerUrl.TrimEnd('/')}/v2/agent/bootstrap";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.Token);
            using var resp = await _http.SendAsync(req);

            if (resp.IsSuccessStatusCode) return true;
            if ((int)resp.StatusCode == 401) return await TryReloginAsync();

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryReloginAsync()
    {
        if (string.IsNullOrEmpty(_config.Email) || string.IsNullOrEmpty(_config.Password))
            return false;

        var result = await LoginAsync(_config.Email, _config.Password, _config.ServerUrl);
        if (result.Success) _config.Save();
        return result.Success;
    }

    private static string ExtractErrorMessage(string raw, string? fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("error", out var errEl))
            {
                if (errEl.ValueKind == JsonValueKind.String) return errEl.GetString() ?? fallback ?? "알 수 없는 오류";
                if (errEl.TryGetProperty("message", out var msgEl)) return msgEl.GetString() ?? fallback ?? "알 수 없는 오류";
            }
            if (doc.RootElement.TryGetProperty("message", out var rootMsg))
                return rootMsg.GetString() ?? fallback ?? "알 수 없는 오류";
        }
        catch { }
        return fallback ?? "알 수 없는 오류";
    }
}
