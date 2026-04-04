using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeliveryOrderReceiver.Services
{
    public class UploadException : Exception
    {
        public int StatusCode { get; }
        public UploadException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class UploadService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// 영수증 데이터 서버 업로드
        /// POST {serverUrl}/v2/agent/uploads/receipt-raw
        /// Header: Authorization: Bearer {token}, Idempotency-Key (내용 해시)
        /// </summary>
        public async Task UploadReceiptAsync(string token, string serverUrl, string siteId, string timestamp, string port, string content, string contentHash)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("로그인이 필요합니다");
            }

            var url = $"{serverUrl}/v2/agent/uploads/receipt-raw";

            var payload = new {
                eventId = Guid.NewGuid().ToString(),
                siteId = siteId,
                platformId = "unknown",
                platformStoreId = "unknown",
                capturedAt = timestamp,
                rawChecksum = contentHash,
                decodedText = content,
                port = port
            };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Idempotency-Key", contentHash);
            request.Content = jsonContent;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                string errorMsg;
                try
                {
                    var errorData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    errorMsg = errorData.TryGetProperty("message", out var msg) ? msg.GetString() ?? $"업로드 실패 ({(int)response.StatusCode})" : $"업로드 실패 ({(int)response.StatusCode})";
                }
                catch
                {
                    errorMsg = $"업로드 실패 ({(int)response.StatusCode})";
                }
                throw new UploadException(errorMsg, (int)response.StatusCode);
            }
        }
    }
}
