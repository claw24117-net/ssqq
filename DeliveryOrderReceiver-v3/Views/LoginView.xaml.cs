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

            if (_config.SaveLoginInfo)
            {
                _config.Password = pwd;
            }
            else
            {
                // 미체크 시 디스크에서만 자격 증명 제거 (현재 메모리/세션 토큰은 유지)
                _config.Email = string.Empty;
                _config.Password = string.Empty;
                _config.ServerUrl = "https://agent.zigso.kr";
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
