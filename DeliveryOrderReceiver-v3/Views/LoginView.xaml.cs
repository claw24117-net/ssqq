using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeliveryOrderReceiver.Models;
using DeliveryOrderReceiver.Services;

namespace DeliveryOrderReceiver.Views;

public partial class LoginView : UserControl
{
    private readonly LoginConfig _config;
    private readonly AuthService _auth;

    public event EventHandler? LoginSucceeded;

    public LoginView(LoginConfig config, AuthService auth)
    {
        InitializeComponent();
        _config = config;
        _auth = auth;

        // 저장된 값 복원
        EmailBox.Text = _config.Email;
        ServerUrlBox.Text = string.IsNullOrEmpty(_config.ServerUrl) ? "https://agent.zigso.kr" : _config.ServerUrl;
        BranchNameBox.Text = _config.BranchName;  // v3.1.0: 지점명 복원
        SaveLoginInfoCheck.IsChecked = _config.SaveLoginInfo;
        AutoLoginCheck.IsChecked = _config.AutoLogin;
        if (_config.SaveLoginInfo && !string.IsNullOrEmpty(_config.Password))
            PasswordBox.Password = _config.Password;

        Loaded += (_, _) => EmailBox.Focus();
        KeyDown += (_, e) => { if (e.Key == Key.Enter) LoginButton_Click(this, new RoutedEventArgs()); };
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailBox.Text.Trim();
        var pwd = PasswordBox.Password;
        var serverUrl = ServerUrlBox.Text.Trim();
        var branchName = BranchNameBox.Text.Trim();  // v3.1.0: 지점명 (한글/영문, 빈 값 허용)

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pwd))
        {
            StatusText.Text = "이메일과 패스워드를 입력하세요.";
            return;
        }

        StatusText.Text = "로그인 중...";
        LoginButton.IsEnabled = false;

        try
        {
            var result = await _auth.LoginAsync(email, pwd, serverUrl);
            if (!result.Success)
            {
                StatusText.Text = $"실패: {result.Error}";
                return;
            }

            // BUG-001 fix: 로그인 성공 후 토큰을 절대 삭제하지 않는다.
            // SaveLoginInfo / AutoLogin 플래그만 설정하고, 미체크여도 현재 세션 토큰은 유지.
            _config.SaveLoginInfo = SaveLoginInfoCheck.IsChecked == true;
            _config.AutoLogin = AutoLoginCheck.IsChecked == true;

            // v3.0.7: ServerUrl 은 항상 사용자 입력값 유지 (하드코딩 default 제거)
            // 다른 서버 (예: api.dvvb.io) 로 전환할 때 SaveLoginInfo 미체크로 zigso.kr 로
            // 강제 리셋되던 버그 해소
            _config.ServerUrl = serverUrl;
            _config.BranchName = branchName;  // v3.1.0: 지점명 항상 저장 (서버 전송용)
            if (_config.SaveLoginInfo)
            {
                _config.Password = pwd;
            }
            else
            {
                // 미체크 시 이메일/패스워드만 제거 (서버 주소/지점명은 유지)
                _config.Email = string.Empty;
                _config.Password = string.Empty;
            }

            _config.Save();

            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
