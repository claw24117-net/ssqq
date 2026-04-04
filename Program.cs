using System;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DeliveryOrderReceiver
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // EUC-KR / CP949 인코딩 지원
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Windows 자동 시작 등록
            try
            {
                var config = Models.LoginConfig.Load();
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (config.AutoStart)
                    key?.SetValue("DeliveryOrderReceiver", Application.ExecutablePath);
                else
                    key?.DeleteValue("DeliveryOrderReceiver", false);
            }
            catch { }

            ApplicationConfiguration.Initialize();
            Application.Run(new Forms.MainForm());
        }
    }
}
