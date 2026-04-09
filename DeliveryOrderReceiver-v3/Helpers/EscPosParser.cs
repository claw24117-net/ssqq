using System.Text;

namespace DeliveryOrderReceiver.Helpers;

/// <summary>
/// ESC/POS 바이너리에서 한글 텍스트 추출 (B안).
/// 제어문자 / 명령어 시퀀스 필터.
/// 인코딩: CP949 (한글 윈도우 표준) → UTF-8.
/// </summary>
public static class EscPosParser
{
    /// <summary>
    /// 바이너리 → 텍스트만 추출.
    /// </summary>
    public static string ExtractText(byte[] data)
    {
        if (data == null || data.Length == 0) return string.Empty;

        var filtered = new List<byte>(data.Length);
        int i = 0;
        while (i < data.Length)
        {
            byte b = data[i];

            // ESC (0x1B) 시퀀스 스킵
            if (b == 0x1B && i + 1 < data.Length)
            {
                i += SkipEscSequence(data, i);
                continue;
            }

            // GS (0x1D) 시퀀스 스킵
            if (b == 0x1D && i + 1 < data.Length)
            {
                i += SkipGsSequence(data, i);
                continue;
            }

            // 출력 가능한 ASCII / 한글 / 줄바꿈만 통과
            if (b == 0x0A || b == 0x0D || b == 0x09 || b >= 0x20)
            {
                filtered.Add(b);
            }

            i++;
        }

        try
        {
            // CP949 (한국어 Windows) 디코딩
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var cp949 = Encoding.GetEncoding(949);
            return cp949.GetString(filtered.ToArray()).Trim();
        }
        catch
        {
            // fallback: UTF-8
            return Encoding.UTF8.GetString(filtered.ToArray()).Trim();
        }
    }

    /// <summary>SHA256 해시 (Idempotency-Key 용)</summary>
    public static string Sha256Hex(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static int SkipEscSequence(byte[] data, int idx)
    {
        // ESC + 1바이트 명령
        if (idx + 1 >= data.Length) return 1;
        byte cmd = data[idx + 1];

        // ESC @ (초기화), ESC ! (모드), ESC E (강조), ESC - (밑줄), ESC a (정렬) 등
        // 대부분 ESC + 명령 + 1~2 파라미터
        switch (cmd)
        {
            case 0x40: // @ 초기화
                return 2;
            case 0x21: // ! 모드 (1 파라미터)
            case 0x2D: // - 밑줄
            case 0x45: // E 강조
            case 0x47: // G 이중인쇄
            case 0x61: // a 정렬
            case 0x4D: // M 폰트
                return 3;
            case 0x24: // $ 절대 위치 (2 파라미터)
                return 4;
            default:
                return 2;
        }
    }

    private static int SkipGsSequence(byte[] data, int idx)
    {
        if (idx + 1 >= data.Length) return 1;
        byte cmd = data[idx + 1];
        // GS + 명령 + 파라미터
        switch (cmd)
        {
            case 0x56: // V 컷
                return 4;
            case 0x21: // ! 문자 크기
                return 3;
            default:
                return 2;
        }
    }
}
