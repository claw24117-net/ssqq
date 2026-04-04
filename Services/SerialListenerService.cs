using System;
using System.IO.Ports;
using System.Threading;

namespace DeliveryOrderReceiver.Services
{
    public class SerialListenerService : IDisposable
    {
        private SerialPort? _serialPort;
        private byte[] _dataBuffer = Array.Empty<byte>();
        private System.Threading.Timer? _receiveTimer;
        private Action<byte[]>? _callback;
        private readonly object _lock = new object();

        // 데이터 수신 후 일정 시간 동안 추가 데이터가 없으면 하나의 주문으로 처리
        private const int ReceiveTimeoutMs = 500;

        /// <summary>
        /// 시리얼 수신 시작
        /// </summary>
        public void StartListening(string portName, Action<byte[]> callback, int baudRate = 9600)
        {
            // 이미 열려있으면 먼저 닫기
            StopListening();

            _callback = callback;

            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.ErrorReceived += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"시리얼 포트 에러: {e.EventType}");
            };

            _serialPort.Open();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead <= 0) return;

                var chunk = new byte[bytesToRead];
                _serialPort.Read(chunk, 0, bytesToRead);

                lock (_lock)
                {
                    // 버퍼에 데이터 누적
                    var newBuffer = new byte[_dataBuffer.Length + chunk.Length];
                    Buffer.BlockCopy(_dataBuffer, 0, newBuffer, 0, _dataBuffer.Length);
                    Buffer.BlockCopy(chunk, 0, newBuffer, _dataBuffer.Length, chunk.Length);
                    _dataBuffer = newBuffer;

                    // 타이머 리셋
                    _receiveTimer?.Dispose();
                    _receiveTimer = new System.Threading.Timer(OnReceiveTimeout, null, ReceiveTimeoutMs, Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"데이터 수신 에러: {ex.Message}");
            }
        }

        private void OnReceiveTimeout(object? state)
        {
            byte[] data;

            lock (_lock)
            {
                if (_dataBuffer.Length == 0) return;

                data = _dataBuffer;
                _dataBuffer = Array.Empty<byte>();
            }

            _callback?.Invoke(data);
        }

        /// <summary>
        /// 시리얼 수신 중지
        /// </summary>
        public void StopListening()
        {
            _receiveTimer?.Dispose();
            _receiveTimer = null;

            lock (_lock)
            {
                _dataBuffer = Array.Empty<byte>();
            }

            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                }
                catch { }

                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        public bool IsListening => _serialPort?.IsOpen == true;

        public void Dispose()
        {
            StopListening();
        }
    }
}
