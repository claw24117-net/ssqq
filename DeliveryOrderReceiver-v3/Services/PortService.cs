using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// com0com 가상 COM 포트 관리 (setupc.exe wrapper).
///
/// 핵심 원칙 (기존 DOR에서 검증된 룰):
///   - 일반 모드에서는 setupc.exe 절대 호출 안 함 (포트 간섭 방지)
///   - 설정 모드에서만 포트 조작
///   - 매장천사가 만든 포트 인덱스는 절대 안 건드림
///   - 우리가 만든 CreatedPortA/B 인덱스만 삭제
///   - 포트 이름으로 인덱스 매칭 (인덱스 밀림 방지)
/// </summary>
public class PortService
{
    private const string SetupcDefaultPath = @"C:\Program Files (x86)\com0com\setupc.exe";

    public string SetupcPath { get; set; } = SetupcDefaultPath;

    public class PortPair
    {
        public int IndexA { get; set; }
        public int IndexB { get; set; }
        public string PortA { get; set; } = string.Empty;
        public string PortB { get; set; } = string.Empty;
    }

    /// <summary>
    /// setupc list 파싱 → 모든 com0com 포트 쌍 반환.
    /// </summary>
    public List<PortPair> ListPairs()
    {
        var output = RunSetupc("list");
        var pairs = new List<PortPair>();

        // setupc list 출력 형식:
        //   CNCA0 PortName=COM14
        //   CNCB0 PortName=COM15
        //   CNCA1 PortName=COM16
        //   CNCB1 PortName=COM17
        var aMap = new Dictionary<int, string>();
        var bMap = new Dictionary<int, string>();

        foreach (var line in output.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("CNCA"))
            {
                var (idx, name) = ParseLine(t, "CNCA");
                if (idx >= 0) aMap[idx] = name;
            }
            else if (t.StartsWith("CNCB"))
            {
                var (idx, name) = ParseLine(t, "CNCB");
                if (idx >= 0) bMap[idx] = name;
            }
        }

        foreach (var idx in aMap.Keys)
        {
            if (bMap.TryGetValue(idx, out var bName))
            {
                pairs.Add(new PortPair
                {
                    IndexA = idx,
                    IndexB = idx,
                    PortA = aMap[idx],
                    PortB = bName
                });
            }
        }

        return pairs.OrderBy(p => p.IndexA).ToList();
    }

    private static (int idx, string name) ParseLine(string line, string prefix)
    {
        try
        {
            var rest = line.Substring(prefix.Length);
            int sp = rest.IndexOf(' ');
            if (sp < 0) return (-1, "");
            int idx = int.Parse(rest[..sp]);
            int eq = rest.IndexOf('=');
            string name = eq < 0 ? "" : rest[(eq + 1)..].Trim();
            return (idx, name);
        }
        catch
        {
            return (-1, "");
        }
    }

    /// <summary>
    /// 새 가상 COM 포트 쌍 생성 (PortNameA / PortNameB).
    /// 시스템 점유 포트는 사전 차단.
    /// </summary>
    public PortPair CreatePair(string portA, string portB)
    {
        var occupied = ScanOccupiedPorts();
        if (occupied.Contains(portA, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{portA} 는 이미 사용 중입니다");
        if (occupied.Contains(portB, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{portB} 는 이미 사용 중입니다");

        // setupc install PortName=COM14 PortName=COM15
        var args = $"install PortName={portA} PortName={portB}";
        RunSetupc(args);

        // 생성 후 list에서 우리 포트 인덱스 확인
        var pairs = ListPairs();
        var created = pairs.FirstOrDefault(p =>
            p.PortA.Equals(portA, StringComparison.OrdinalIgnoreCase) &&
            p.PortB.Equals(portB, StringComparison.OrdinalIgnoreCase));

        if (created == null)
            throw new InvalidOperationException("포트 생성 실패 (setupc list에서 확인 안 됨)");

        return created;
    }

    /// <summary>
    /// 우리가 만든 포트만 안전 삭제 (이름으로 인덱스 매칭).
    /// 매장천사 포트 인덱스는 절대 안 건드림.
    /// </summary>
    public void RemovePair(string portA, string portB)
    {
        var pairs = ListPairs();
        var target = pairs.FirstOrDefault(p =>
            p.PortA.Equals(portA, StringComparison.OrdinalIgnoreCase) &&
            p.PortB.Equals(portB, StringComparison.OrdinalIgnoreCase));

        if (target == null) return; // 이미 없음

        RunSetupc($"remove {target.IndexA}");
    }

    /// <summary>
    /// 시스템 점유 COM 포트 스캔 (레지스트리 SERIALCOMM).
    /// </summary>
    public List<string> ScanOccupiedPorts()
    {
        var ports = new List<string>();
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            if (key == null) return ports;
            foreach (var name in key.GetValueNames())
            {
                var value = key.GetValue(name)?.ToString();
                if (!string.IsNullOrEmpty(value)) ports.Add(value);
            }
        }
        catch { /* 권한 부족 시 빈 목록 */ }
        return ports;
    }

    private string RunSetupc(string args)
    {
        if (!File.Exists(SetupcPath))
            throw new FileNotFoundException($"setupc.exe 를 찾을 수 없습니다: {SetupcPath}");

        var workDir = Path.GetDirectoryName(SetupcPath) ?? ".";

        var psi = new ProcessStartInfo
        {
            FileName = SetupcPath,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("setupc.exe 실행 실패");

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(15000);

        if (proc.ExitCode != 0)
        {
            var msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(TranslateSetupcError(msg.Trim()));
        }

        return stdout;
    }

    private static string TranslateSetupcError(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "setupc.exe 알 수 없는 오류";
        if (raw.Contains("Access is denied") || raw.Contains("권한"))
            return "관리자 권한 필요 — 프로그램을 관리자로 실행하세요";
        if (raw.Contains("already") || raw.Contains("exists"))
            return "이미 존재하는 포트입니다";
        if (raw.Contains("not found") || raw.Contains("찾을 수 없"))
            return "포트를 찾을 수 없습니다";
        return raw;
    }
}
