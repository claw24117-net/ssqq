using System;
using System.IO;
using System.Text.Json;

namespace DeliveryOrderReceiver.Models
{
    public class LoginConfig
    {
        public string Email { get; set; } = "";
        public string ServerUrl { get; set; } = "https://agent.zigso.kr";
        public string Token { get; set; } = "";
        public string LastPort { get; set; } = "";
        public string SiteId { get; set; } = "";
        public bool AutoStart { get; set; } = true;
        public bool AutoLogin { get; set; } = true;
        public bool SaveLoginInfo { get; set; } = true;
        public string CreatedPortA { get; set; } = "";
        public string CreatedPortB { get; set; } = "";
        public int LastBaudRate { get; set; } = 9600;
        public string Password { get; set; } = "";

        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeliveryOrderReceiver"
        );

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static LoginConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<LoginConfig>(json) ?? new LoginConfig();
                }
            }
            catch
            {
                // 파일 읽기 실패 시 기본값 반환
            }
            return new LoginConfig();
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // 저장 실패 무시
            }
        }
    }
}
