using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using DeliveryOrderReceiver.Helpers;
using DeliveryOrderReceiver.Models;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// COM 포트 시리얼 수신 서비스.
///
/// 핵심:
///   - 수신 데이터 버퍼링 → idle 타임아웃 후 1건의 주문으로 확정
///   - 종료 시 버퍼 flush (잔여 데이터 손실 방지)
///   - W-TIME fix: 모든 timestamp는 DateTime.UtcNow.ToString("o") (UTC ISO)
///   - v3.0.2: heartbeat 30초 — 포트 끊김 감지 시 자동 재연결
/// </summary>
public class SerialReceiverService
{
    private SerialPort? _port;
    private readonly List<byte> _buffer = new();
    private CancellationTokenSource? _cts;
    private Timer? _idleTimer;
    private Timer? _heartbeatTimer;
    private const int IdleFlushMs = 800;
    private const int HeartbeatIntervalMs = 30_000; // 30초
    private readonly object _bufferLock = new();
    private readonly object _reconnectLock = new();
    private bool _isReconnecting;

    // 재연결에 사용할 포트/속도 (Stop 시 초기화되지 않도록 별도 보관)
    private string _lastPort = string.Empty;
    private int _lastBaudRate;

    public bool IsRunning => _port?.IsOpen == true;
    public string CurrentPort { get; private set; } = string.Empty;
    public int CurrentBaudRate { get; private set; }

    /// <summary>새 주문이 확정될 때마다 발생.</summary>
    public event EventHandler<OrderRecord>? OrderReceived;

    /// <summary>오류/상태 메시지.</summary>
    public event EventHandler<string>? ErrorOccurred;

    public void Start(string portName, int baudRate)
    {
        Stop();

        _cts = new CancellationTokenSource();
        try
        {
            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadBufferSize = 8192,
                ReadTimeout = 1000
            };
            _port.DataReceived += OnDataReceived;
            _port.Open();

            CurrentPort = portName;
            CurrentBaudRate = baudRate;
            _lastPort = portName;
            _lastBaudRate = baudRate;

            // v3.0.2: heartbeat 시작 — 30초마다 포트 상태 체크
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new Timer(_ => CheckAndReconnect(), null, HeartbeatIntervalMs, HeartbeatIntervalMs);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, TranslateError(ex.Message));
            Stop();
        }
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _idleTimer?.Dispose();
            _idleTimer = null;
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;

            if (_port != null)
            {
                _port.DataReceived -= OnDataReceived;
                if (_port.IsOpen)
                {
                    // 종료 전 잔여 버퍼 flush
                    FlushBufferAsOrder();
                    _port.Close();
                }
                _port.Dispose();
                _port = null;
            }
        }
        catch { /* swallow */ }
        finally
        {
            CurrentPort = string.Empty;
            CurrentBaudRate = 0;
        }
    }

    /// <summary>
    /// v3.0.2: heartbeat — 30초마다 포트 상태 체크. 끊겼으면 자동 재연결.
    /// .NET SerialPort.DataReceived 가 OS 레벨 포트 끊김 시 silent하게 멈추는
    /// 알려진 문제 대응.
    /// </summary>
    private void CheckAndReconnect()
    {
        // 이미 재연결 중이면 스킵 (중복 방지)
        lock (_reconnectLock)
        {
            if (_isReconnecting) return;
            _isReconnecting = true;
        }

        try
        {
            // 포트가 정상이면 패스
            if (_port != null && _port.IsOpen) return;

            var port = _lastPort;
            var baud = _lastBaudRate;
            if (string.IsNullOrEmpty(port) || baud <= 0) return;

            ErrorOccurred?.Invoke(this, $"포트 끊김 감지 — {port} 재연결 시도 중...");

            // 기존 포트 정리 (Stop 은 heartbeat timer 도 죽이므로 직접 정리)
            try
            {
                _idleTimer?.Dispose();
                _idleTimer = null;
                if (_port != null)
                {
                    _port.DataReceived -= OnDataReceived;
                    try { _port.Close(); } catch { }
                    _port.Dispose();
                    _port = null;
                }
            }
            catch { }

            // 재연결 시도
            try
            {
                _port = new SerialPort(port, baud, Parity.None, 8, StopBits.One)
                {
                    ReadBufferSize = 8192,
                    ReadTimeout = 1000
                };
                _port.DataReceived += OnDataReceived;
                _port.Open();

                CurrentPort = port;
                CurrentBaudRate = baud;

                ErrorOccurred?.Invoke(this, $"재연결 성공 — {port} @ {baud}");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"재연결 실패 — {port}. 30초 후 재시도. ({ex.Message})");
            }
        }
        finally
        {
            lock (_reconnectLock) { _isReconnecting = false; }
        }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port == null || !_port.IsOpen) return;

        try
        {
            int n = _port.BytesToRead;
            if (n <= 0) return;
            var buf = new byte[n];
            _port.Read(buf, 0, n);

            lock (_bufferLock)
            {
                _buffer.AddRange(buf);
            }

            // idle 타이머 재시작 — 800ms 동안 신규 데이터 없으면 1건으로 확정
            _idleTimer?.Dispose();
            _idleTimer = new Timer(_ => FlushBufferAsOrder(), null, IdleFlushMs, Timeout.Infinite);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, TranslateError(ex.Message));
        }
    }

    private void FlushBufferAsOrder()
    {
        byte[] snapshot;
        lock (_bufferLock)
        {
            if (_buffer.Count == 0) return;
            snapshot = _buffer.ToArray();
            _buffer.Clear();
        }

        try
        {
            var text = EscPosParser.ExtractText(snapshot);
            if (string.IsNullOrWhiteSpace(text)) return;

            var record = new OrderRecord
            {
                ReceivedAt = DateTime.UtcNow.ToString("o"), // W-TIME
                Port = CurrentPort,
                Content = text,
                Hash = EscPosParser.Sha256Hex(text)
            };

            OrderReceived?.Invoke(this, record);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, TranslateError(ex.Message));
        }
    }

    private static string TranslateError(string raw)
    {
        if (raw.Contains("Access") || raw.Contains("denied"))
            return "포트 접근 거부 — 다른 프로그램이 사용 중일 수 있습니다";
        if (raw.Contains("does not exist") || raw.Contains("찾을 수 없"))
            return "포트가 존재하지 않습니다";
        if (raw.Contains("port is closed"))
            return "포트가 닫혔습니다";
        return $"수신 오류: {raw}";
    }
}
