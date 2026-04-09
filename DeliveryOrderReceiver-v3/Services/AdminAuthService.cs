using System.Windows;
using DeliveryOrderReceiver.Helpers;
using DeliveryOrderReceiver.Models;
using DeliveryOrderReceiver.Views;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// 설정 모드 진입 관리자 비밀번호.
///
/// 기존 DOR의 하드코딩 "0000" 제거 (코드 품질 미해결 항목 fix).
///   - 최초 설정 시 사용자가 비밀번호 입력 → DPAPI로 저장
///   - 이후 진입 시 검증
///   - config.AdminPasswordEncrypted (DPAPI 암호화)
/// </summary>
public class AdminAuthService
{
    public bool PromptForAccess()
    {
        var config = LoginConfig.Load();

        if (string.IsNullOrEmpty(config.AdminPassword))
        {
            // 최초 설정 모드 진입 — 비밀번호 새로 만들기
            var setup = new AdminPasswordDialog(setupMode: true);
            if (setup.ShowDialog() != true) return false;

            config.AdminPassword = setup.Password;
            config.Save();
            MessageBox.Show("관리자 비밀번호가 설정되었습니다.", "안내",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        // 검증 모드
        var dlg = new AdminPasswordDialog(setupMode: false);
        if (dlg.ShowDialog() != true) return false;

        if (dlg.Password != config.AdminPassword)
        {
            MessageBox.Show("비밀번호가 일치하지 않습니다.", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        return true;
    }

    public bool ChangePassword(string oldPwd, string newPwd)
    {
        var config = LoginConfig.Load();
        if (config.AdminPassword != oldPwd) return false;
        config.AdminPassword = newPwd;
        config.Save();
        return true;
    }
}
