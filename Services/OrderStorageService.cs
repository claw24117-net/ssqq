using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DeliveryOrderReceiver.Services
{
    public class OrderRecord
    {
        public int Seq { get; set; }
        public string ReceivedAt { get; set; } = "";
        public string Port { get; set; } = "";
        public string Content { get; set; } = "";
        public string Hash { get; set; } = "";
        public string UploadStatus { get; set; } = "대기";
        public string IdempotencyKey { get; set; } = "";
    }

    public class OrderStorageService
    {
        private static readonly string OrdersDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeliveryOrderReceiver",
            "orders"
        );

        /// <summary>
        /// SHA256 해시 계산
        /// </summary>
        public static string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// 날짜별 파일 경로
        /// </summary>
        private static string GetFilePath(DateTime date)
        {
            return Path.Combine(OrdersDir, $"orders_{date:yyyy-MM-dd}.json");
        }

        /// <summary>
        /// 주문 저장
        /// </summary>
        public void Save(OrderRecord order)
        {
            if (!Directory.Exists(OrdersDir))
                Directory.CreateDirectory(OrdersDir);

            var filePath = GetFilePath(DateTime.Now);
            var orders = LoadFromFile(filePath);

            // 순번 설정 (당일 기준)
            order.Seq = orders.Count > 0 ? orders.Max(o => o.Seq) + 1 : 1;

            orders.Add(order);

            // 임시 파일에 쓰고 교체 (손상 방지)
            var tmpPath = filePath + ".tmp";
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(orders, options);
            File.WriteAllText(tmpPath, json);

            // 원본 파일로 교체
            if (File.Exists(filePath))
                File.Replace(tmpPath, filePath, null);
            else
                File.Move(tmpPath, filePath);
        }

        /// <summary>
        /// 주문 상태 업데이트
        /// </summary>
        public void UpdateStatus(string hash, string newStatus)
        {
            var filePath = GetFilePath(DateTime.Now);
            var orders = LoadFromFile(filePath);

            var target = orders.LastOrDefault(o => o.Hash == hash);
            if (target == null) return;

            target.UploadStatus = newStatus;

            // 임시 파일에 쓰고 교체 (손상 방지)
            var tmpPath = filePath + ".tmp";
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(orders, options);
            File.WriteAllText(tmpPath, json);

            if (File.Exists(filePath))
                File.Replace(tmpPath, filePath, null);
            else
                File.Move(tmpPath, filePath);
        }

        /// <summary>
        /// 당일 파일 로드
        /// </summary>
        public List<OrderRecord> LoadToday()
        {
            var filePath = GetFilePath(DateTime.Now);
            return LoadFromFile(filePath);
        }

        /// <summary>
        /// "실패" 상태인 주문 목록 반환
        /// </summary>
        public List<OrderRecord> GetFailedOrders()
        {
            var orders = LoadToday();
            return orders.Where(o => o.UploadStatus == "실패").ToList();
        }

        /// <summary>
        /// 중복 감지 (최근 7일 해시 비교)
        /// </summary>
        public bool IsDuplicate(string hash)
        {
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Now.AddDays(-i);
                var filePath = GetFilePath(date);
                var orders = LoadFromFile(filePath);

                if (orders.Any(o => o.Hash == hash))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 파일에서 주문 목록 로드
        /// </summary>
        private static List<OrderRecord> LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<List<OrderRecord>>(json) ?? new List<OrderRecord>();
                }
            }
            catch
            {
                // 파일 손상 시 빈 목록 반환
            }
            return new List<OrderRecord>();
        }
    }
}
