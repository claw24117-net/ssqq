using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeliveryOrderReceiver.Helpers;
using DeliveryOrderReceiver.Models;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// 날짜별 주문 로컬 저장.
///
/// C-3 fix:
///   - 모든 파일 쓰기는 FileLockHelper.WriteAllTextAtomic
///     (FileShare.None + tmp + 원자적 교체 + 재시도)
///   - 동시 쓰기 → 잠시 대기 후 재시도
///
/// 중복 감지: 7일 윈도우, SHA256 해시 비교.
/// 시작 시 'Pending' → 'Failed' (D-7 fix).
/// </summary>
public class OrderStorageService
{
    private static string OrdersDir => Path.Combine(App.AppDataDir, "orders");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    private readonly object _lock = new();

    public OrderStorageService()
    {
        Directory.CreateDirectory(OrdersDir);
    }

    // v3.0.6: KST 날짜 기준으로 파일 저장 (UTC 기준이면 KST 하루가 UTC 파일 2개에 걸림)
    private static readonly TimeZoneInfo KstZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

    private static string FileForDate(DateTime dateUtc)
    {
        var kst = TimeZoneInfo.ConvertTimeFromUtc(dateUtc.ToUniversalTime(), KstZone);
        return Path.Combine(OrdersDir, $"orders_{kst:yyyy-MM-dd}.json");
    }

    public List<OrderRecord> LoadDate(DateTime dateUtc)
    {
        var path = FileForDate(dateUtc);
        if (!File.Exists(path)) return new();
        try
        {
            var json = FileLockHelper.ReadAllTextLocked(path);
            return JsonSerializer.Deserialize<List<OrderRecord>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public List<OrderRecord> LoadToday() => LoadDate(DateTime.UtcNow);

    /// <summary>
    /// 시작 시 'Pending' 상태 → 'Failed'로 변환 (D-7).
    /// </summary>
    public void SweepStaleOnStartup()
    {
        lock (_lock)
        {
            var today = LoadToday();
            var changed = false;
            foreach (var o in today)
            {
                if (o.UploadStatus == UploadStatus.Pending)
                {
                    o.UploadStatus = UploadStatus.Failed;
                    o.LastError ??= "프로그램 시작 시 미완료 상태 감지";
                    changed = true;
                }
            }
            if (changed) WriteAll(DateTime.UtcNow, today);
        }
    }

    /// <summary>
    /// 새 주문 추가. 7일 내 중복이면 Duplicate 상태로 마킹.
    /// </summary>
    public OrderRecord Save(OrderRecord order)
    {
        lock (_lock)
        {
            var dateUtc = order.ReceivedAtUtc;
            var list = LoadDate(dateUtc);

            if (IsDuplicate(order.Hash, dateUtc))
            {
                order.UploadStatus = UploadStatus.Duplicate;
            }

            order.Seq = (list.Count == 0 ? 0 : list.Max(o => o.Seq)) + 1;
            order.IdempotencyKey = order.Hash; // 내용 해시 = 멱등 키
            list.Add(order);

            WriteAll(dateUtc, list);
            return order;
        }
    }

    /// <summary>
    /// 특정 hash의 주문 상태 갱신 (업로드 결과 반영).
    /// </summary>
    public void UpdateStatus(string hash, UploadStatus newStatus, string? error = null)
    {
        lock (_lock)
        {
            var today = LoadToday();
            var target = today.FirstOrDefault(o => o.Hash == hash);
            if (target == null) return;
            target.UploadStatus = newStatus;
            target.LastError = error;
            WriteAll(DateTime.UtcNow, today);
        }
    }

    /// <summary>
    /// 오늘 실패 건 모두 반환 (재전송 버튼용).
    /// </summary>
    public List<OrderRecord> GetTodayFailed()
    {
        lock (_lock)
        {
            return LoadToday().Where(o => o.UploadStatus == UploadStatus.Failed).ToList();
        }
    }

    private bool IsDuplicate(string hash, DateTime referenceUtc)
    {
        for (int i = 0; i < 7; i++)
        {
            var date = referenceUtc.AddDays(-i);
            var list = LoadDate(date);
            if (list.Any(o => o.Hash == hash)) return true;
        }
        return false;
    }

    private void WriteAll(DateTime dateUtc, List<OrderRecord> list)
    {
        var json = JsonSerializer.Serialize(list, JsonOpts);
        FileLockHelper.WriteAllTextAtomic(FileForDate(dateUtc), json);
    }
}
