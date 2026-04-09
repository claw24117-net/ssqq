using System.Windows;
using DeliveryOrderReceiver.Models;
using DeliveryOrderReceiver.Services;
using DeliveryOrderReceiver.Views;

namespace DeliveryOrderReceiver;

public partial class MainWindow : Window
{
    private readonly LoginConfig _config;
    private readonly AuthService _auth;
    private readonly UploadService _upload;
    private readonly OrderStorageService _storage;
    private readonly SerialReceiverService _serial;
    private readonly PortService _ports;
    private readonly AutoStartService _autoStart;
    private readonly AdminAuthService _adminAuth;

    public MainWindow()
    {
        InitializeComponent();

        _config = LoginConfig.Load();
        _auth = new AuthService(_config);
        _upload = new UploadService(_config, _auth);
        _storage = new OrderStorageService();
        _serial = new SerialReceiverService();
        _ports = new PortService();
        _autoStart = new AutoStartService();
        _adminAuth = new AdminAuthService();

        Loaded += async (_, _) =>
        {
            // 자동 로그인 시도 (BUG-001 fix: 토큰 삭제 코드 없음)
            if (_config.AutoLogin && !string.IsNullOrEmpty(_config.Token))
            {
                var ok = await _auth.ValidateOrRefreshAsync();
                if (ok) { ShowMain(); return; }
            }
            ShowLogin();
        };
    }

    public void ShowLogin()
    {
        var view = new LoginView(_config, _auth);
        view.LoginSucceeded += (_, _) => ShowMain();
        ViewHost.Content = view;
    }

    public void ShowMain()
    {
        var view = new MainView(_config, _upload, _storage, _serial);
        view.SettingsRequested += (_, _) => ShowSettings();
        view.LogoutRequested += (_, _) => { Logout(); };
        ViewHost.Content = view;
    }

    public void ShowSettings()
    {
        if (!_adminAuth.PromptForAccess()) return;
        var view = new SettingsView(_config, _ports, _autoStart, _adminAuth);
        view.BackRequested += (_, _) => ShowMain();
        view.LogoutRequested += (_, _) => Logout();
        ViewHost.Content = view;
    }

    private void Logout()
    {
        _serial.Stop();
        _config.Token = string.Empty;
        _config.Save();
        ShowLogin();
    }
}
