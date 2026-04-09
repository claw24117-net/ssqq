using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DeliveryOrderReceiver.Models;
using DeliveryOrderReceiver.Services;

namespace DeliveryOrderReceiver.Views;

public partial class MainView : UserControl
{
    private readonly LoginConfig _config;
    private readonly UploadService _upload;
    private readonly OrderStorageService _storage;
    private readonly SerialReceiverService _serial;
    private readonly AutoStartService _autoStart = new();

    public ObservableCollection<OrderRecord> Orders { get; } = new();

    public event EventHandler? SettingsRequested;
    public event EventHandler? LogoutRequested;

    public MainView(LoginConfig config, UploadService upload,
                    OrderStorageService storage, SerialReceiverService serial)
    {
        InitializeComponent();
        _config = config;
        _upload = upload;
        _storage = storage;
        _serial = serial;

        OrdersGrid.ItemsSource = Orders;

        // 시작 시 'Pending' → 'Failed' (D-7)
        _storage.SweepStaleOnStartup();
        ReloadOrders();

        // 포트 후보 채우기
        try
        {
            foreach (var p in System.IO.Ports.SerialPort.GetPortNames().OrderBy(x => x))
                PortCombo.Items.Add(p);
            if (!string.IsNullOrEmpty(_config.LastPort))
                PortCombo.Text = _config.LastPort;
            else if (PortCombo.Items.Count > 0)
                PortCombo.SelectedIndex = 0;
        }
        catch { }

        if (_config.LastBaudRate > 0)
        {
            foreach (ComboBoxItem item in BaudCombo.Items)
            {
                if (item.Content?.ToString() == _config.LastBaudRate.ToString())
                {
                    BaudCombo.SelectedItem = item;
                    break;
                }
            }
        }

        AutoStartCheck.IsChecked = _autoStart.IsEnabled();

        _serial.OrderReceived += OnOrderReceived;
        _serial.ErrorOccurred += OnError;

        Unloaded += (_, _) =>
        {
            _serial.OrderReceived -= OnOrderReceived;
            _serial.ErrorOccurred -= OnError;
        };
    }

    private void ReloadOrders()
    {
        Orders.Clear();
        foreach (var o in _storage.LoadToday().OrderByDescending(o => o.Seq))
            Orders.Add(o);
    }

    private async void OnOrderReceived(object? sender, OrderRecord order)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            var saved = _storage.Save(order);
            Orders.Insert(0, saved);

            if (saved.UploadStatus == UploadStatus.Duplicate)
            {
                SetStatus("중복 주문 감지 (전송 안 함)", false);
                return;
            }

            // 즉시 업로드 시도
            var result = await _upload.UploadAsync(saved);
            if (result.Success)
            {
                _storage.UpdateStatus(saved.Hash, result.Duplicate ? UploadStatus.Duplicate : UploadStatus.Success);
                SetStatus("업로드 성공", true);
            }
            else
            {
                _storage.UpdateStatus(saved.Hash, UploadStatus.Failed, result.Error);
                SetStatus($"업로드 실패: {result.Error}", false);
            }

            ReloadOrders();
        });
    }

    private void OnError(object? sender, string msg)
    {
        Dispatcher.Invoke(() => SetStatus(msg, false));
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var port = PortCombo.Text?.Trim() ?? "";
        var baudText = (BaudCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "9600";
        if (string.IsNullOrEmpty(port))
        {
            SetStatus("포트를 선택하세요", false);
            return;
        }
        if (!int.TryParse(baudText, out var baud)) baud = 9600;

        _config.LastPort = port;
        _config.LastBaudRate = baud;
        _config.Save();

        _serial.Start(port, baud);
        if (_serial.IsRunning)
        {
            StatusDot.Fill = (Brush)FindResource("SuccessBrush");
            SetStatus($"수신 중 ({port} @ {baud})", true);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _serial.Stop();
        StatusDot.Fill = (Brush)FindResource("MutedBrush");
        SetStatus("수신 중지", false);
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        var failed = _storage.GetTodayFailed();
        if (failed.Count == 0)
        {
            SetStatus("재전송할 실패 건이 없습니다", true);
            return;
        }

        SetStatus($"재전송 중 ({failed.Count}건)...", false);
        int ok = 0, fail = 0;
        foreach (var o in failed)
        {
            var r = await _upload.UploadAsync(o);
            if (r.Success)
            {
                _storage.UpdateStatus(o.Hash, r.Duplicate ? UploadStatus.Duplicate : UploadStatus.Success);
                ok++;
            }
            else
            {
                _storage.UpdateStatus(o.Hash, UploadStatus.Failed, r.Error);
                fail++;
            }
        }
        ReloadOrders();
        SetStatus($"재전송 완료: 성공 {ok} / 실패 {fail}", fail == 0);
    }

    private void AutoStartCheck_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (AutoStartCheck.IsChecked == true) _autoStart.Enable();
            else _autoStart.Disable();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, false);
            AutoStartCheck.IsChecked = _autoStart.IsEnabled();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void SetStatus(string msg, bool success)
    {
        StatusLabel.Text = msg;
        StatusLabel.Foreground = success
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("DangerBrush");
    }
}
