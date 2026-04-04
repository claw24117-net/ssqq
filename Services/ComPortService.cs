using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DeliveryOrderReceiver.Services
{
    public class ComPortService
    {
        /// <summary>
        /// com0com setupc.exe 경로 탐색
        /// </summary>
        private string FindSetupc()
        {
            string[] possiblePaths = new[]
            {
                @"C:\Program Files (x86)\com0com\setupc.exe",
                @"C:\Program Files\com0com\setupc.exe"
            };

            foreach (var p in possiblePaths)
            {
                if (File.Exists(p))
                    return p;
            }

            throw new Exception("com0com setupc.exe를 찾을 수 없습니다. com0com v2.2.2.0 signed를 설치해주세요.");
        }

        /// <summary>
        /// setupc.exe 명령 실행
        /// </summary>
        private async Task<string> RunSetupc(string args)
        {
            var setupcPath = FindSetupc();

            var psi = new ProcessStartInfo
            {
                FileName = setupcPath,
                WorkingDirectory = Path.GetDirectoryName(setupcPath),
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new Exception("setupc.exe 실행 실패");

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"setupc.exe 실행 실패: {stderr}");
            }

            return stdout;
        }

        /// <summary>
        /// 가상 COM포트 쌍 목록 조회
        /// </summary>
        public async Task<string> ListPortsAsync()
        {
            return await RunSetupc("list");
        }

        /// <summary>
        /// setupc.exe install 전 필요한 inf/sys/cat/dll 파일을 C:\ 루트에 복사
        /// </summary>
        private void CopyDriverFilesToRoot()
        {
            var setupcPath = FindSetupc();
            var setupcDir = Path.GetDirectoryName(setupcPath);
            if (string.IsNullOrEmpty(setupcDir)) return;

            string[] requiredFiles = new[]
            {
                "com0com.inf",
                "com0com.sys",
                "com0com.cat",
                "setup.dll",
                "cncport.inf",
                "comport.inf"
            };

            foreach (var fileName in requiredFiles)
            {
                var src = Path.Combine(setupcDir, fileName);
                var dst = Path.Combine(@"C:\", fileName);
                if (File.Exists(src) && !File.Exists(dst))
                {
                    File.Copy(src, dst);
                }
            }
        }


        /// <summary>
        /// 가상 COM포트 쌍 생성 (시스템 점유 포트 동적 감지)
        /// </summary>
        public async Task<(string PortA, string PortB, string Output)> CreatePortAsync()
        {
            CopyDriverFilesToRoot();

            int portA = FindAvailablePort(20);
            int portB = FindAvailablePort(portA + 1);

            var output = await RunSetupc($"install PortName=COM{portA} PortName=COM{portB}");

            return ($"COM{portA}", $"COM{portB}", output);
        }

        /// <summary>
        /// 사용자 지정 포트 번호로 가상 COM포트 쌍 생성
        /// </summary>
        public async Task<(string PortA, string PortB, string Output)> CreatePortWithNumbersAsync(int portANum, int portBNum)
        {
            CopyDriverFilesToRoot();

            var output = await RunSetupc($"install PortName=COM{portANum} PortName=COM{portBNum}");

            return ($"COM{portANum}", $"COM{portBNum}", output);
        }

        /// <summary>
        /// 시스템에서 현재 사용 중인 COM포트 목록 조회 (레지스트리 SERIALCOMM)
        /// </summary>
        private HashSet<int> GetOccupiedPorts()
        {
            var occupied = new HashSet<int>();
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var portName = key.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(portName) && portName.StartsWith("COM"))
                        {
                            if (int.TryParse(portName.Substring(3), out int portNum))
                            {
                                occupied.Add(portNum);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 레지스트리 접근 실패 시 빈 목록 반환
            }
            return occupied;
        }

        /// <summary>
        /// 사용 가능한 포트 번호 찾기 (시스템 점유 포트 동적 감지)
        /// </summary>
        private int FindAvailablePort(int startFrom)
        {
            var occupied = GetOccupiedPorts();
            int port = startFrom;
            while (occupied.Contains(port))
            {
                port++;
            }
            return port;
        }

        /// <summary>
        /// 가상 COM포트 쌍 삭제
        /// </summary>
        public async Task<string> DeletePortAsync(string portIndex)
        {
            return await RunSetupc($"remove {portIndex}");
        }
    }
}
