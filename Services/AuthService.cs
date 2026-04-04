using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DeliveryOrderReceiver.Models;

namespace DeliveryOrderReceiver.Services
{
    public class AuthService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// 로그인
        /// POST {serverUrl}/v2/agent/auth/login
        /// </summary>
        public async Task<JsonElement> LoginAsync(string email, string password, string serverUrl)
        {
            var url = $"{serverUrl}/v2/agent/auth/login";

            var payload = new { email, password };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string errorMsg;
                try
                {
                    var errorData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    errorMsg = errorData.TryGetProperty("message", out var msg) ? msg.GetString() ?? $"로그인 실패 ({(int)response.StatusCode})" : $"로그인 실패 ({(int)response.StatusCode})";
                }
                catch
                {
                    errorMsg = $"로그인 실패 ({(int)response.StatusCode})";
                }
                throw new Exception(errorMsg);
            }

            var data = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // 설정 저장
            var config = LoginConfig.Load();
            config.Email = email;
            config.Password = password;
            config.ServerUrl = serverUrl;

            // 서버 응답: {"data": {"userSessionToken": "...", "sites": [...]}, "meta": {...}}
            // data 객체 안에서 토큰/sites 추출, fallback으로 최상위도 확인 (호환성)
            var dataObj = data;
            if (data.TryGetProperty("data", out var innerData))
                dataObj = innerData;

            if (dataObj.TryGetProperty("userSessionToken", out var tokenEl))
                config.Token = tokenEl.GetString() ?? "";
            else if (dataObj.TryGetProperty("token", out var tokenElFallback))
                config.Token = tokenElFallback.GetString() ?? "";

            // sites 배열에서 첫 번째 siteId 저장
            if (dataObj.TryGetProperty("sites", out var sitesEl) && sitesEl.GetArrayLength() > 0)
            {
                var firstSite = sitesEl[0];
                if (firstSite.TryGetProperty("id", out var siteIdEl))
                    config.SiteId = siteIdEl.GetString() ?? "";
            }

            config.Save();

            return data;
        }

        /// <summary>
        /// 자동 로그인 (저장된 토큰 확인)
        /// </summary>
        public LoginConfig AutoLogin()
        {
            var config = LoginConfig.Load();
            if (!config.AutoLogin || string.IsNullOrEmpty(config.Token))
            {
                throw new Exception("저장된 토큰 없음");
            }
            return config;
        }
    }
}
