using System;
using System.Collections.Generic;
using System.Text;

namespace DeliveryOrderReceiver.Services
{
    public static class EscPosParser
    {
        /// <summary>
        /// ESC/POS 바이너리 데이터에서 텍스트 추출 (B안)
        /// - ESC/GS 제어 시퀀스 필터링
        /// - EUC-KR/CP949 한글 지원
        /// - 줄바꿈 유지
        /// </summary>
        public static string Parse(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return "";

            var textBytes = new List<byte>();
            int i = 0;

            while (i < buffer.Length)
            {
                byte b = buffer[i];

                // ESC (0x1B) 시퀀스 스킵
                if (b == 0x1B)
                {
                    i = SkipEscSequence(buffer, i);
                    continue;
                }

                // GS (0x1D) 시퀀스 스킵
                if (b == 0x1D)
                {
                    i = SkipGsSequence(buffer, i);
                    continue;
                }

                // DLE (0x10) 스킵
                if (b == 0x10)
                {
                    i += 2;
                    continue;
                }

                // FS (0x1C) 스킵
                if (b == 0x1C)
                {
                    i += 2;
                    continue;
                }

                // 줄바꿈 (0x0A) 유지
                if (b == 0x0A)
                {
                    textBytes.Add(b);
                    i++;
                    continue;
                }

                // 캐리지 리턴 (0x0D) 유지
                if (b == 0x0D)
                {
                    textBytes.Add(b);
                    i++;
                    continue;
                }

                // 인쇄 가능한 ASCII 문자 (0x20 ~ 0x7E)
                if (b >= 0x20 && b <= 0x7E)
                {
                    textBytes.Add(b);
                    i++;
                    continue;
                }

                // EUC-KR/CP949 한글 바이트 (0x80 이상)
                if (b >= 0x80 && i + 1 < buffer.Length)
                {
                    textBytes.Add(b);
                    textBytes.Add(buffer[i + 1]);
                    i += 2;
                    continue;
                }

                // 기타 제어문자 스킵
                i++;
            }

            // EUC-KR(CP949) 디코딩 시도, 실패 시 UTF-8
            try
            {
                var euckr = Encoding.GetEncoding(949); // CP949/EUC-KR
                return euckr.GetString(textBytes.ToArray());
            }
            catch
            {
                try
                {
                    return Encoding.UTF8.GetString(textBytes.ToArray());
                }
                catch
                {
                    return Encoding.ASCII.GetString(textBytes.ToArray());
                }
            }
        }

        /// <summary>
        /// ESC 시퀀스 스킵 (0x1B 이후)
        /// </summary>
        private static int SkipEscSequence(byte[] buffer, int pos)
        {
            if (pos + 1 >= buffer.Length) return pos + 1;

            byte next = buffer[pos + 1];

            // ESC @ (초기화) - 2바이트
            if (next == 0x40) return pos + 2;
            // ESC ! (인쇄 모드) - 3바이트
            if (next == 0x21) return pos + 3;
            // ESC a (정렬) - 3바이트
            if (next == 0x61) return pos + 3;
            // ESC d (줄 넘기기) - 3바이트
            if (next == 0x64) return pos + 3;
            // ESC E (볼드) - 3바이트
            if (next == 0x45) return pos + 3;
            // ESC J (줄간격) - 3바이트
            if (next == 0x4A) return pos + 3;
            // ESC p (커터) - 5바이트
            if (next == 0x70) return pos + 5;

            // 기본: 2바이트 스킵
            return pos + 2;
        }

        /// <summary>
        /// GS 시퀀스 스킵 (0x1D 이후)
        /// </summary>
        private static int SkipGsSequence(byte[] buffer, int pos)
        {
            if (pos + 1 >= buffer.Length) return pos + 1;

            byte next = buffer[pos + 1];

            // GS V (커터) - 4바이트
            if (next == 0x56) return pos + 4;
            // GS ! (문자 크기) - 3바이트
            if (next == 0x21) return pos + 3;
            // GS B (반전) - 3바이트
            if (next == 0x42) return pos + 3;

            // GS k (바코드) - 가변 길이
            if (next == 0x6B)
            {
                if (pos + 2 < buffer.Length)
                {
                    byte m = buffer[pos + 2];
                    if (m <= 6)
                    {
                        // NUL 종결 방식
                        int end = pos + 3;
                        while (end < buffer.Length && buffer[end] != 0x00)
                        {
                            end++;
                        }
                        return end + 1;
                    }
                    else
                    {
                        // 길이 지정 방식
                        if (pos + 3 < buffer.Length)
                        {
                            int n = buffer[pos + 3];
                            return pos + 4 + n;
                        }
                    }
                }
                return pos + 2;
            }

            // 기본: 2바이트 스킵
            return pos + 2;
        }
    }
}
