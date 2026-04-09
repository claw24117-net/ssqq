using System;
using System.IO;
using System.Threading;

namespace DeliveryOrderReceiver.Helpers;

/// <summary>
/// 파일 동시 쓰기 잠금 헬퍼 — C-3/C-4 fix.
/// FileShare.None + tmp 파일 쓰기 + 원자적 교체.
/// </summary>
public static class FileLockHelper
{
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 50;

    /// <summary>
    /// 텍스트를 atomic하게 쓴다 (FileShare.None 잠금 + tmp+replace).
    /// 다른 프로세스/스레드가 동시 쓰기 시도 시 잠시 대기 후 재시도.
    /// </summary>
    public static void WriteAllTextAtomic(string path, string contents)
    {
        var dir = Path.GetDirectoryName(path) ?? ".";
        Directory.CreateDirectory(dir);

        var tmpPath = path + ".tmp";

        Exception? lastEx = null;
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                using (var fs = new FileStream(
                    tmpPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                using (var sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    sw.Write(contents);
                    sw.Flush();
                    fs.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(tmpPath, path, null);
                else
                    File.Move(tmpPath, path);

                return;
            }
            catch (IOException ex)
            {
                lastEx = ex;
                Thread.Sleep(RetryDelayMs * (attempt + 1));
            }
        }

        if (lastEx != null) throw lastEx;
    }

    /// <summary>
    /// 텍스트를 잠금 + 재시도와 함께 읽는다.
    /// </summary>
    public static string ReadAllTextLocked(string path)
    {
        Exception? lastEx = null;
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                using var sr = new StreamReader(fs, System.Text.Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch (IOException ex)
            {
                lastEx = ex;
                Thread.Sleep(RetryDelayMs * (attempt + 1));
            }
        }
        if (lastEx != null) throw lastEx;
        return string.Empty;
    }
}
