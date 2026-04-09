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
///
/// r3 fix (Bug 1):
///   - 기존 r2 까지는 PromptForAccess() 와 ChangePassword() 가 매번 LoginConfig.Load()
///     를 호출해서 새 인스턴스 B를 만들었음. MainWindow 의 stale 인스턴스 A 가 후속
///     [수신 시작] 등에서 _config.Save() 호출 시 빈 AdminPasswordEncrypted 로
///     디스크를 덮어써서 다음 [설정] 진입 시 다시 setup mode 가 떴음.
///   - r3: LoginConfig 를 생성자 주입으로 받아 MainWindow._config 와 동일 인스턴스
///     공유. 모든 setter/save 가 같은 인스턴스에서 일어나므로 race 0.
/// </summary>
public class AdminAuthService
{
    private readonly LoginConfig _config;

    public AdminAuthService(LoginConfig config)
    {
        _config = config;
    }

    public bool PromptForAccess()
    {
        if (string.IsNullOrEmpty(_config.AdminPassword))
        {
            // 최초 설정 모드 진입 — 비밀번호 새로 만들기
            var setup = new AdminPasswordDialog(setupMode: true);
            if (setup.ShowDialog() != true) return false;

            _config.AdminPassword = setup.Password;
            _config.Save();
            MessageBox.Show("관리자 비밀번호가 설정되었습니다.", "안내",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        // 검증 모드
        var dlg = new AdminPasswordDialog(setupMode: false);
        if (dlg.ShowDialog() != true) return false;

        if (dlg.Password != _config.AdminPassword)
        {
            MessageBox.Show("비밀번호가 일치하지 않습니다.", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        return true;
    }

    public bool ChangePassword(string oldPwd, string newPwd)
    {
        if (_config.AdminPassword != oldPwd) return false;
        _config.AdminPassword = newPwd;
        _config.Save();
        return true;
    }
}
