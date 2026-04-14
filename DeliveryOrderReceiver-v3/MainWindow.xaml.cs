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

        // v3.0.5: 버전 자동 표시 (csproj Version 에서 가져옴)
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"배달 주문 수신기 v{ver?.ToString(3) ?? "3.0.5"}";

        _config = LoginConfig.Load();
        _auth = new AuthService(_config);
        _upload = new UploadService(_config, _auth);
        _storage = new OrderStorageService();
        _serial = new SerialReceiverService();
        _ports = new PortService();
        _autoStart = new AutoStartService();
        _adminAuth = new AdminAuthService(_config);

        // v3.0.4: 항상 로그인 화면 먼저 (여러 매장이 각자 아이디로 사용)
        // 저장된 이메일/패스워드는 로그인 화면에 미리 채워짐
        Loaded += (_, _) => ShowLogin();
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
