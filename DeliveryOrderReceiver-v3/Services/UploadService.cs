using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DeliveryOrderReceiver.Helpers;
using DeliveryOrderReceiver.Models;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// 영수증 업로드 서비스.
///
/// 서버 계약 (server/apps/api/src/routes/agent-routes.js:790-1060):
///   POST /v2/agent/uploads/receipt-raw
///   - Authorization: Bearer {token}
///   - Idempotency-Key: {hash} (중복 방지)
///   - body: eventId, siteId, platformId, platformStoreId, capturedAt,
///           rawChecksum, decodedText, port
///   - dual auth: device api key 우선 → user session fallback
///
/// 401 시 자동 재로그인 + 1회 재시도.
/// </summary>
public class UploadService
{
    private readonly LoginConfig _config;
    private readonly AuthService _auth;
    private readonly HttpClient _http;

    public UploadService(LoginConfig config, AuthService auth)
    {
        _config = config;
        _auth = auth;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("DeliveryOrderReceiver-v3/3.0.1");
    }

    public class UploadResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public bool Duplicate { get; init; }
    }

    public async Task<UploadResult> UploadAsync(OrderRecord order)
    {
        var first = await SendOnceAsync(order);
        if (first.Success || first.Duplicate) return first;

        // 401 → 재로그인 + 1회 재시도
        if (first.Error?.Contains("401") == true)
        {
            var refreshed = await _auth.ValidateOrRefreshAsync();
            if (refreshed)
            {
                var second = await SendOnceAsync(order);
                return second;
            }
        }

        return first;
    }

    private async Task<UploadResult> SendOnceAsync(OrderRecord order)
    {
        if (string.IsNullOrEmpty(_config.Token))
            return new UploadResult { Success = false, Error = "토큰 없음 (로그인 필요)" };

        try
        {
            var url = $"{_config.ServerUrl.TrimEnd('/')}/v2/agent/uploads/receipt-raw";

            // 서버 receipt-raw 핸들러는 platformId/platformStoreId도 비어있지 않은 string을 요구함
            // (agent-routes.js: requiredFields filter — typeof !== "string" || trim() === "")
            // v2.0.4와 동일하게 "unknown" 하드코딩으로 우회. 향후 ESC/POS 텍스트에서 추출 가능.
            var body = new
            {
                eventId = Guid.NewGuid().ToString(),
                siteId = _config.SiteId,
                platformId = "unknown",
                platformStoreId = "unknown",
                capturedAt = order.ReceivedAt, // 이미 UTC ISO-8601 (W-TIME)
                rawChecksum = order.Hash,
                decodedText = order.Content,
                port = order.Port
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.Token);
            req.Headers.TryAddWithoutValidation("Idempotency-Key", order.IdempotencyKey);

            using var resp = await _http.SendAsync(req);

            if (resp.IsSuccessStatusCode)
                return new UploadResult { Success = true };

            if ((int)resp.StatusCode == 409)
                return new UploadResult { Success = true, Duplicate = true };

            var raw = await resp.Content.ReadAsStringAsync();
            return new UploadResult
            {
                Success = false,
                Error = $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {Truncate(raw, 200)}"
            };
        }
        catch (HttpRequestException ex)
        {
            return new UploadResult { Success = false, Error = $"네트워크: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new UploadResult { Success = false, Error = "서버 응답 시간 초과" };
        }
        catch (Exception ex)
        {
            return new UploadResult { Success = false, Error = $"오류: {ex.Message}" };
        }
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "...";
}
