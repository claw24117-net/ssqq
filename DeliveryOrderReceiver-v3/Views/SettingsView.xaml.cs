using System;
using System.Windows;
using System.Windows.Controls;
using DeliveryOrderReceiver.Models;
using DeliveryOrderReceiver.Services;

namespace DeliveryOrderReceiver.Views;

public partial class SettingsView : UserControl
{
    private readonly LoginConfig _config;
    private readonly PortService _ports;
    private readonly AutoStartService _autoStart;
    private readonly AdminAuthService _adminAuth;

    public event EventHandler? BackRequested;
    public event EventHandler? LogoutRequested;

    public SettingsView(LoginConfig config, PortService ports,
                        AutoStartService autoStart, AdminAuthService adminAuth)
    {
        InitializeComponent();
        _config = config;
        _ports = ports;
        _autoStart = autoStart;
        _adminAuth = adminAuth;

        RefreshState();
    }

    private void RefreshState()
    {
        try
        {
            CurrentPairText.Text = string.IsNullOrEmpty(_config.CreatedPortA)
                ? "현재 등록된 포트 쌍 없음"
                : $"현재: {_config.CreatedPortA} ↔ {_config.CreatedPortB} (매장천사용/수신용)";

            var occupied = _ports.ScanOccupiedPorts();
            OccupiedPortsText.Text = occupied.Count == 0
                ? "(없음)"
                : string.Join(", ", occupied);

            PortABox.Text = _config.CreatedPortA;
            PortBBox.Text = _config.CreatedPortB;
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, false);
        }
    }

    private void CreatePortButton_Click(object sender, RoutedEventArgs e)
    {
        var a = PortABox.Text.Trim();
        var b = PortBBox.Text.Trim();
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            SetStatus("포트 A/B 모두 입력하세요", false);
            return;
        }

        try
        {
            var pair = _ports.CreatePair(a, b);
            _config.CreatedPortA = pair.PortA;
            _config.CreatedPortB = pair.PortB;
            _config.Save();
            SetStatus($"포트 생성 성공: {pair.PortA} ↔ {pair.PortB}", true);
            RefreshState();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, false);
        }
    }

    private void DeletePortButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_config.CreatedPortA))
        {
            SetStatus("등록된 포트가 없습니다", false);
            return;
        }

        var confirm = MessageBox.Show(
            "삭제하면 매장천사 프린터 설정도 다시 해야 합니다.\n계속하시겠습니까?",
            "포트 삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _ports.RemovePair(_config.CreatedPortA, _config.CreatedPortB);
            _config.CreatedPortA = string.Empty;
            _config.CreatedPortB = string.Empty;
            _config.Save();
            SetStatus("포트 삭제 완료", true);
            RefreshState();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, false);
        }
    }

    private void RestorePortButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_config.CreatedPortA))
        {
            SetStatus("복원할 포트 정보가 없습니다", false);
            return;
        }

        try
        {
            var pair = _ports.CreatePair(_config.CreatedPortA, _config.CreatedPortB);
            SetStatus($"포트 복원 완료: {pair.PortA} ↔ {pair.PortB}", true);
            RefreshState();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, false);
        }
    }

    private void ChangeAdminPwd_Click(object sender, RoutedEventArgs e)
    {
        var verify = new AdminPasswordDialog(setupMode: false) { Owner = Window.GetWindow(this) };
        if (verify.ShowDialog() != true) return;

        if (verify.Password != _config.AdminPassword)
        {
            SetStatus("기존 비밀번호가 일치하지 않습니다", false);
            return;
        }

        var setup = new AdminPasswordDialog(setupMode: true) { Owner = Window.GetWindow(this) };
        if (setup.ShowDialog() != true) return;

        _config.AdminPassword = setup.Password;
        _config.Save();
        SetStatus("관리자 비밀번호가 변경되었습니다", true);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) =>
        BackRequested?.Invoke(this, EventArgs.Empty);

    private void LogoutButton_Click(object sender, RoutedEventArgs e) =>
        LogoutRequested?.Invoke(this, EventArgs.Empty);

    private void SetStatus(string msg, bool success)
    {
        StatusText.Text = msg;
        StatusText.Foreground = success
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("DangerBrush");
    }
}
