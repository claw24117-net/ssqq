using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeliveryOrderReceiver.Models;
using DeliveryOrderReceiver.Services;
using Microsoft.Win32;
using System.Security.Principal;

namespace DeliveryOrderReceiver.Forms
{
    public class MainForm : Form
    {
        private LoginConfig _config;
        private readonly ComPortService _comPortService = new ComPortService();
        private readonly SerialListenerService _serialListener = new SerialListenerService();
        private readonly UploadService _uploadService = new UploadService();
        private readonly AuthService _authService = new AuthService();
        private readonly OrderStorageService _orderStorage = new OrderStorageService();

        // Panels
        private Panel _loginPanel = null!;
        private Panel _mainPanel = null!;
        private Panel _settingsPanel = null!;

        // LoginPanel controls
        private TextBox _emailTextBox = null!;
        private TextBox _passwordTextBox = null!;
        private TextBox _serverUrlTextBox = null!;
        private CheckBox _saveLoginInfoCheckBox = null!;
        private CheckBox _autoLoginCheckBox = null!;
        private Button _loginButton = null!;
        private Label _loginStatusLabel = null!;

        // MainPanel controls
        private Label _connectionStatusLabel = null!;
        private ListBox _orderListBox = null!;
        private Label _uploadStatusLabel = null!;
        private CheckBox _autoStartCheckBox = null!;
        private Button _startListenBtn = null!;
        private Button _stopListenBtn = null!;
        private Button _retryUploadBtn = null!;

        // SettingsPanel controls
        private Label _currentPortLabel = null!;
        private Label _systemPortLabel = null!;
        private TextBox _portATextBox = null!;
        private TextBox _portBTextBox = null!;
        private Button _createPortBtn = null!;
        private Button _deletePortBtn = null!;
        private Button _restorePortBtn = null!;
        private ComboBox _baudRateSelect = null!;
        private Label _settingsStatusLabel = null!;

        private bool _autoLoginSuccess = false;
        private bool _isListening = false;

        private static readonly int[] BaudRates = { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        private const string AdminPassword = "0000";

        public MainForm()
        {
            _config = LoginConfig.Load();
            InitializeForm();
            InitializeLoginPanel();
            InitializeMainPanel();
            InitializeSettingsPanel();
            TryAutoLogin();
        }

        protected override void SetVisibleCore(bool value)
        {
            // 자동 로그인 성공 시 깜빡임 없이 바로 MainPanel 표시
            if (_autoLoginSuccess && !IsHandleCreated)
            {
                value = false;
                CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        private void InitializeForm()
        {
            Text = "배달 주문 수신기 v2.0.4";
            Size = new Size(500, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(480, 550);
            Font = new Font("맑은 고딕", 9);
        }

        #region LoginPanel

        private void InitializeLoginPanel()
        {
            _loginPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var titleLabel = new Label
            {
                Text = "배달 주문 수신기 v2.0.4",
                Font = new Font("맑은 고딕", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 40)
            };

            var emailLabel = new Label
            {
                Text = "이메일:",
                Location = new Point(40, 100),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9)
            };

            _emailTextBox = new TextBox
            {
                Location = new Point(40, 120),
                Size = new Size(400, 25),
                Font = new Font("맑은 고딕", 10)
            };

            var passwordLabel = new Label
            {
                Text = "패스워드:",
                Location = new Point(40, 155),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9)
            };

            _passwordTextBox = new TextBox
            {
                Location = new Point(40, 175),
                Size = new Size(400, 25),
                UseSystemPasswordChar = true,
                Font = new Font("맑은 고딕", 10)
            };

            var serverLabel = new Label
            {
                Text = "서버주소:",
                Location = new Point(40, 210),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9)
            };

            _serverUrlTextBox = new TextBox
            {
                Location = new Point(40, 230),
                Size = new Size(400, 25),
                Text = "https://agent.zigso.kr",
                Font = new Font("맑은 고딕", 10)
            };

            _saveLoginInfoCheckBox = new CheckBox
            {
                Text = "로그인 정보 저장",
                Location = new Point(40, 270),
                AutoSize = true,
                Checked = _config.SaveLoginInfo,
                Font = new Font("맑은 고딕", 9)
            };

            _autoLoginCheckBox = new CheckBox
            {
                Text = "자동 로그인",
                Location = new Point(40, 295),
                AutoSize = true,
                Checked = _config.AutoLogin,
                Font = new Font("맑은 고딕", 9)
            };

            _loginButton = new Button
            {
                Text = "로그인",
                Location = new Point(40, 335),
                Size = new Size(400, 40),
                Font = new Font("맑은 고딕", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _loginButton.FlatAppearance.BorderSize = 0;
            _loginButton.Click += LoginButton_Click;

            _loginStatusLabel = new Label
            {
                Text = "상태: 대기 중",
                Location = new Point(40, 390),
                Size = new Size(400, 20),
                Font = new Font("맑은 고딕", 9),
                ForeColor = Color.Gray
            };

            // 엔터키로 로그인
            _passwordTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    LoginButton_Click(s, e);
                }
            };

            // 저장된 이메일/서버주소 로드
            if (!string.IsNullOrEmpty(_config.Email))
                _emailTextBox.Text = _config.Email;
            if (!string.IsNullOrEmpty(_config.ServerUrl))
                _serverUrlTextBox.Text = _config.ServerUrl;

            _loginPanel.Controls.AddRange(new Control[] {
                titleLabel, emailLabel, _emailTextBox,
                passwordLabel, _passwordTextBox,
                serverLabel, _serverUrlTextBox,
                _saveLoginInfoCheckBox, _autoLoginCheckBox,
                _loginButton, _loginStatusLabel
            });

            Controls.Add(_loginPanel);
        }

        #endregion

        #region MainPanel

        private void InitializeMainPanel()
        {
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            // 상단바: 연결 상태 + [설정] 버튼 (Dock=Top)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            _connectionStatusLabel = new Label
            {
                Text = "\u25CF 대기 중",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                ForeColor = Color.Gray
            };

            var settingsButton = new Button
            {
                Text = "설정",
                Size = new Size(60, 30),
                Location = new Point(topPanel.Width - 70, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 9),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            settingsButton.Click += (s, e) => ShowSettingsPanel();

            topPanel.Controls.AddRange(new Control[] { _connectionStatusLabel, settingsButton });

            // 하단 고정 패널 (Dock=Bottom) — 전송상태 + 자동실행 + 버튼3개
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110
            };

            // 서버 전송 상태
            _uploadStatusLabel = new Label
            {
                Text = "서버 전송 상태: 대기 중",
                Location = new Point(10, 5),
                Size = new Size(440, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 9),
                ForeColor = Color.Gray
            };

            // 자동 실행 체크박스
            _autoStartCheckBox = new CheckBox
            {
                Text = "Windows 시작 시 자동 실행",
                Location = new Point(10, 30),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Checked = _config.AutoStart,
                Font = new Font("맑은 고딕", 9)
            };
            _autoStartCheckBox.CheckedChanged += AutoStartCheckBox_Changed;

            // 수신 시작/중지 버튼 + 재전송 버튼 — FlowLayoutPanel로 균등 배치
            var buttonFlowPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 60),
                Size = new Size(440, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _startListenBtn = new Button
            {
                Text = "수신 시작",
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 5, 0)
            };
            _startListenBtn.FlatAppearance.BorderSize = 0;
            _startListenBtn.Click += StartListenBtn_Click;

            _stopListenBtn = new Button
            {
                Text = "수신 중지",
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 5, 0)
            };
            _stopListenBtn.FlatAppearance.BorderSize = 0;
            _stopListenBtn.Click += StopListenBtn_Click;

            _retryUploadBtn = new Button
            {
                Text = "재전송",
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(255, 153, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            _retryUploadBtn.FlatAppearance.BorderSize = 0;
            _retryUploadBtn.Click += RetryUploadBtn_Click;

            buttonFlowPanel.Controls.AddRange(new Control[] { _startListenBtn, _stopListenBtn, _retryUploadBtn });

            bottomPanel.Controls.AddRange(new Control[] {
                _uploadStatusLabel, _autoStartCheckBox,
                buttonFlowPanel
            });

            // 중간 영역: 주문 목록 라벨 + ListBox (나머지 공간 채움)
            var middlePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 5, 10, 5)
            };

            var orderLabel = new Label
            {
                Text = "수신된 주문 목록",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };

            _orderListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9),
                HorizontalScrollbar = true
            };

            middlePanel.Controls.Add(_orderListBox);
            middlePanel.Controls.Add(orderLabel);

            // 순서 중요: Dock=Fill인 middlePanel을 먼저 추가하고, Top/Bottom은 나중에
            // WinForms에서는 Controls에 나중에 추가된 Dock 컨트롤이 먼저 배치됨
            _mainPanel.Controls.Add(middlePanel);
            _mainPanel.Controls.Add(bottomPanel);
            _mainPanel.Controls.Add(topPanel);

            Controls.Add(_mainPanel);
        }

        #endregion

        #region SettingsPanel

        private void InitializeSettingsPanel()
        {
            _settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                AutoScroll = true
            };

            // 상단: 설정 타이틀 + 돌아가기
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var settingsTitleLabel = new Label
            {
                Text = "설정 (관리자 모드)",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("맑은 고딕", 11, FontStyle.Bold)
            };

            var backButton = new Button
            {
                Text = "\u2190 돌아가기",
                Size = new Size(100, 30),
                Location = new Point(370, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 9),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            backButton.Click += (s, e) => ShowMainPanel();

            headerPanel.Controls.AddRange(new Control[] { settingsTitleLabel, backButton });

            // 경고 메시지
            var warningLabel = new Label
            {
                Text = "\u26A0 포트 설정 변경은 매장천사에 영향을 줄 수 있습니다. 영업 중 변경 금지",
                Location = new Point(10, 50),
                Size = new Size(460, 20),
                Font = new Font("맑은 고딕", 8),
                ForeColor = Color.Red
            };

            // COM포트 관리 그룹
            var portGroupBox = new GroupBox
            {
                Text = "COM포트 관리",
                Location = new Point(10, 75),
                Size = new Size(455, 180),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };

            _currentPortLabel = new Label
            {
                Text = "",
                Location = new Point(15, 25),
                Size = new Size(420, 20),
                Font = new Font("맑은 고딕", 9, FontStyle.Regular),
                ForeColor = Color.DarkBlue
            };

            _systemPortLabel = new Label
            {
                Text = "",
                Location = new Point(15, 48),
                Size = new Size(420, 20),
                Font = new Font("맑은 고딕", 8, FontStyle.Regular),
                ForeColor = Color.Gray
            };

            var portALabel = new Label
            {
                Text = "포트A:",
                Location = new Point(15, 78),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };

            _portATextBox = new TextBox
            {
                Location = new Point(60, 75),
                Size = new Size(60, 25),
                Font = new Font("맑은 고딕", 9)
            };

            var portBLabel = new Label
            {
                Text = "포트B:",
                Location = new Point(135, 78),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };

            _portBTextBox = new TextBox
            {
                Location = new Point(180, 75),
                Size = new Size(60, 25),
                Font = new Font("맑은 고딕", 9)
            };

            _createPortBtn = new Button
            {
                Text = "생성",
                Location = new Point(255, 73),
                Size = new Size(60, 28),
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };
            _createPortBtn.Click += CreatePortBtn_Click;

            _deletePortBtn = new Button
            {
                Text = "포트 삭제",
                Location = new Point(15, 115),
                Size = new Size(100, 30),
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };
            _deletePortBtn.Click += DeletePortBtn_Click;

            _restorePortBtn = new Button
            {
                Text = "포트 복원",
                Location = new Point(125, 115),
                Size = new Size(100, 30),
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };
            _restorePortBtn.Click += RestorePortBtn_Click;

            _settingsStatusLabel = new Label
            {
                Text = "",
                Location = new Point(15, 152),
                Size = new Size(420, 20),
                Font = new Font("맑은 고딕", 8, FontStyle.Regular),
                ForeColor = Color.Gray
            };

            portGroupBox.Controls.AddRange(new Control[] {
                _currentPortLabel, _systemPortLabel,
                portALabel, _portATextBox, portBLabel, _portBTextBox,
                _createPortBtn, _deletePortBtn, _restorePortBtn, _settingsStatusLabel
            });

            // 통신 설정 그룹
            var commGroupBox = new GroupBox
            {
                Text = "통신 설정",
                Location = new Point(10, 265),
                Size = new Size(455, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };

            var baudRateLabel = new Label
            {
                Text = "통신 속도:",
                Location = new Point(15, 28),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };

            _baudRateSelect = new ComboBox
            {
                Location = new Point(85, 25),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };
            foreach (var rate in BaudRates)
                _baudRateSelect.Items.Add(rate.ToString());
            var savedBaudRate = _config.LastBaudRate > 0 ? _config.LastBaudRate : 9600;
            var baudIdx = Array.IndexOf(BaudRates, savedBaudRate);
            _baudRateSelect.SelectedIndex = baudIdx >= 0 ? baudIdx : 3;
            _baudRateSelect.SelectedIndexChanged += (s, e) =>
            {
                if (_baudRateSelect.SelectedItem != null && int.TryParse(_baudRateSelect.SelectedItem.ToString(), out int rate))
                {
                    _config.LastBaudRate = rate;
                    _config.Save();
                }
            };

            commGroupBox.Controls.AddRange(new Control[] { baudRateLabel, _baudRateSelect });

            // 매장천사 등록 안내 그룹
            var guideGroupBox = new GroupBox
            {
                Text = "매장천사 등록 안내",
                Location = new Point(10, 335),
                Size = new Size(455, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };

            var guideLabel = new Label
            {
                Text = "1. 매장천사 \u2192 주방프린터 \u2192 추가\n" +
                       "2. 프린터 종류: LKT-20\n" +
                       "3. 포트: 위에서 생성한 포트A 입력\n" +
                       "4. 통신 속도: 위 설정과 동일",
                Location = new Point(15, 22),
                Size = new Size(420, 80),
                Font = new Font("맑은 고딕", 9, FontStyle.Regular)
            };

            guideGroupBox.Controls.Add(guideLabel);

            // 로그아웃 버튼
            var logoutButton = new Button
            {
                Text = "로그아웃",
                Location = new Point(10, 455),
                Size = new Size(455, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += LogoutButton_Click;

            _settingsPanel.Controls.AddRange(new Control[] {
                headerPanel, warningLabel, portGroupBox,
                commGroupBox, guideGroupBox, logoutButton
            });

            Controls.Add(_settingsPanel);
        }

        #endregion

        #region Form Closing

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 종료 시 버퍼 데이터 처리
            if (_serialListener.IsListening)
            {
                // 버퍼에 데이터가 있을 수 있으므로 잠시 대기
                _serialListener.StopListening();
                _isListening = false;
                // StopListening이 버퍼를 비우고 콜백을 호출한 후 정리
                // 약간의 대기로 진행 중인 업로드가 완료되도록 함
                Thread.Sleep(600);
            }

            _serialListener.Dispose();
            base.OnFormClosing(e);
        }

        #endregion

        #region Panel Switching

        private void ShowLoginPanel()
        {
            _loginPanel.Visible = true;
            _mainPanel.Visible = false;
            _settingsPanel.Visible = false;
            Text = "배달 주문 수신기 v2.0.4 - 로그인";
        }

        private void ShowMainPanel()
        {
            _loginPanel.Visible = false;
            _mainPanel.Visible = true;
            _settingsPanel.Visible = false;
            Text = "배달 주문 수신기 v2.0.4";
            UpdateConnectionStatus();

            // 설정 패널에서 돌아올 때 setupc 관련 리소스 해제
            // ComPortService는 매번 새 프로세스를 실행하므로 별도 해제 불필요
        }

        private void ShowSettingsPanel()
        {
            // 관리자 비밀번호 확인
            if (!PromptAdminPassword())
                return;

            _loginPanel.Visible = false;
            _mainPanel.Visible = false;
            _settingsPanel.Visible = true;
            Text = "배달 주문 수신기 v2.0.4 - 설정";
            UpdateSettingsPortInfo();
        }

        #endregion

        #region Auto Login

        private async void TryAutoLogin()
        {
            try
            {
                var config = _authService.AutoLogin();
                _config = config;
                _autoLoginSuccess = true;

                // 바로 MainPanel 표시
                ShowMainPanel();
                Show();

                // 당일 주문 목록 로드
                LoadTodayOrders();

                AutoStartListening();
            }
            catch
            {
                _autoLoginSuccess = false;
                ShowLoginPanel();
            }
        }

        #endregion

        #region Login

        private async void LoginButton_Click(object? sender, EventArgs e)
        {
            var email = _emailTextBox.Text.Trim();
            var password = _passwordTextBox.Text;
            var serverUrl = _serverUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _loginStatusLabel.Text = "상태: 이메일과 패스워드를 입력해주세요.";
                _loginStatusLabel.ForeColor = Color.Red;
                return;
            }

            _loginButton.Enabled = false;
            _loginStatusLabel.Text = "상태: 로그인 중...";
            _loginStatusLabel.ForeColor = Color.Gray;

            try
            {
                await _authService.LoginAsync(email, password, serverUrl);

                // 로그인 정보 저장 처리
                _config = LoginConfig.Load();
                _config.SaveLoginInfo = _saveLoginInfoCheckBox.Checked;
                _config.AutoLogin = _autoLoginCheckBox.Checked;

                if (!_saveLoginInfoCheckBox.Checked)
                {
                    _config.Email = "";
                    _config.Password = "";
                    _config.ServerUrl = "https://agent.zigso.kr";
                }

                _config.Save();

                _loginStatusLabel.Text = "상태: 로그인 성공";
                _loginStatusLabel.ForeColor = Color.Green;

                ShowMainPanel();

                // 당일 주문 목록 로드
                LoadTodayOrders();

                AutoStartListening();
            }
            catch (Exception ex)
            {
                _loginStatusLabel.Text = $"상태: {ex.Message}";
                _loginStatusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _loginButton.Enabled = true;
            }
        }

        #endregion

        #region Listening

        private void StartListenBtn_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.CreatedPortB))
            {
                MessageBox.Show("수신할 포트가 설정되지 않았습니다.\n설정에서 포트를 먼저 생성하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var comPort = _config.CreatedPortB;
            int baudRate = _config.LastBaudRate > 0 ? _config.LastBaudRate : 9600;

            try
            {
                _serialListener.StartListening(comPort, OnDataReceived, baudRate);
                _isListening = true;

                _startListenBtn.Enabled = false;
                _stopListenBtn.Enabled = true;

                _config.LastPort = comPort;
                _config.Save();

                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"수신 시작 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopListenBtn_Click(object? sender, EventArgs e)
        {
            _serialListener.StopListening();
            _isListening = false;

            _startListenBtn.Enabled = true;
            _stopListenBtn.Enabled = false;

            UpdateConnectionStatus();
        }

        private void AutoStartListening()
        {
            if (!string.IsNullOrEmpty(_config.LastPort) && !string.IsNullOrEmpty(_config.CreatedPortB))
            {
                var timer = new System.Windows.Forms.Timer { Interval = 1000 };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();

                    try
                    {
                        int baudRate = _config.LastBaudRate > 0 ? _config.LastBaudRate : 9600;
                        _serialListener.StartListening(_config.CreatedPortB, OnDataReceived, baudRate);
                        _isListening = true;
                        _startListenBtn.Enabled = false;
                        _stopListenBtn.Enabled = true;
                        UpdateConnectionStatus();
                    }
                    catch
                    {
                        UpdateConnectionStatus();
                    }
                };
                timer.Start();
            }
        }

        private void UpdateConnectionStatus()
        {
            if (_isListening && !string.IsNullOrEmpty(_config.CreatedPortA) && !string.IsNullOrEmpty(_config.CreatedPortB))
            {
                _connectionStatusLabel.Text = $"\u25CF 연결됨  {_config.CreatedPortA}\u2192{_config.CreatedPortB} 수신 중";
                _connectionStatusLabel.ForeColor = Color.Green;
            }
            else if (!string.IsNullOrEmpty(_config.CreatedPortA) && !string.IsNullOrEmpty(_config.CreatedPortB))
            {
                _connectionStatusLabel.Text = $"\u25CB 대기 중  {_config.CreatedPortA}\u2192{_config.CreatedPortB}";
                _connectionStatusLabel.ForeColor = Color.Gray;
            }
            else
            {
                _connectionStatusLabel.Text = "\u25CB 포트 미설정";
                _connectionStatusLabel.ForeColor = Color.Orange;
            }
        }

        #endregion

        #region Data Received / Upload

        private void OnDataReceived(byte[] buffer)
        {
            var text = EscPosParser.Parse(buffer);
            if (string.IsNullOrWhiteSpace(text))
                return;

            var timestamp = DateTime.UtcNow.ToString("o");
            var portName = _config.LastPort;
            var hash = OrderStorageService.ComputeHash(text);

            // 중복 체크
            bool isDuplicate = _orderStorage.IsDuplicate(hash);

            if (isDuplicate)
            {
                // 중복: 로컬 저장(상태:중복) + 목록에 "중복" 표시 + 업로드 안 함
                var order = new OrderRecord
                {
                    ReceivedAt = timestamp,
                    Port = portName,
                    Content = text,
                    Hash = hash,
                    UploadStatus = "중복",
                    IdempotencyKey = hash
                };
                _orderStorage.Save(order);

                if (InvokeRequired)
                    Invoke(new Action(() => AddOrderToList(timestamp, portName, text, "중복")));
                else
                    AddOrderToList(timestamp, portName, text, "중복");
            }
            else
            {
                // 비중복: 로컬 저장(상태:대기) + 업로드 시도
                var order = new OrderRecord
                {
                    ReceivedAt = timestamp,
                    Port = portName,
                    Content = text,
                    Hash = hash,
                    UploadStatus = "대기",
                    IdempotencyKey = hash
                };
                _orderStorage.Save(order);

                if (InvokeRequired)
                    Invoke(new Action(() => AddOrderToList(timestamp, portName, text, "대기")));
                else
                    AddOrderToList(timestamp, portName, text, "대기");

                _ = UploadReceiptAsync(timestamp, portName, text, hash);
            }
        }

        private void AddOrderToList(string timestamp, string port, string content, string status)
        {
            var statusTag = status == "중복" ? "[중복] " : status == "실패" ? "[실패] " : "";
            var displayText = $"{statusTag}[{DateTime.Parse(timestamp):HH:mm}] {content.Replace("\n", " ").Replace("\r", "")}";
            _orderListBox.Items.Insert(0, displayText);

            while (_orderListBox.Items.Count > 100)
            {
                _orderListBox.Items.RemoveAt(_orderListBox.Items.Count - 1);
            }

            // 가로 스크롤 범위 업데이트
            using (var g = _orderListBox.CreateGraphics())
            {
                int maxWidth = 0;
                foreach (var item in _orderListBox.Items)
                {
                    var width = (int)g.MeasureString(item.ToString() ?? "", _orderListBox.Font).Width;
                    if (width > maxWidth) maxWidth = width;
                }
                _orderListBox.HorizontalExtent = maxWidth + 10;
            }
        }

        private async Task UploadReceiptAsync(string timestamp, string port, string content, string hash)
        {
            try
            {
                var serverUrl = _config.ServerUrl;
                if (string.IsNullOrEmpty(serverUrl))
                    serverUrl = "https://agent.zigso.kr";

                try
                {
                    await _uploadService.UploadReceiptAsync(_config.Token, serverUrl, _config.SiteId, timestamp, port, content, hash);
                }
                catch (UploadException ue) when (ue.StatusCode == 401)
                {
                    // 토큰 만료 시 자동 재로그인 시도
                    if (!string.IsNullOrEmpty(_config.Email) && !string.IsNullOrEmpty(_config.Password))
                    {
                        try
                        {
                            var loginResult = await _authService.LoginAsync(_config.Email, _config.Password, serverUrl);
                            _config = LoginConfig.Load();

                            // 재로그인 후 업로드 재시도
                            await _uploadService.UploadReceiptAsync(_config.Token, serverUrl, _config.SiteId, timestamp, port, content, hash);
                        }
                        catch
                        {
                            // 재로그인 실패 시 로그인 화면으로 전환
                            if (InvokeRequired)
                                Invoke(() => ShowLoginPanel());
                            else
                                ShowLoginPanel();
                            throw;
                        }
                    }
                    else
                    {
                        // 저장된 로그인 정보 없으면 로그인 화면으로 전환
                        if (InvokeRequired)
                            Invoke(() => ShowLoginPanel());
                        else
                            ShowLoginPanel();
                        throw;
                    }
                }

                // 업로드 성공: 상태 "성공"으로 업데이트
                _orderStorage.UpdateStatus(hash, "성공");

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _uploadStatusLabel.Text = $"서버 전송 상태: 성공 ({DateTime.Parse(timestamp):HH:mm:ss})";
                        _uploadStatusLabel.ForeColor = Color.Green;
                    }));
                }
            }
            catch (Exception ex)
            {
                // 업로드 실패: 상태 "실패"로 업데이트
                _orderStorage.UpdateStatus(hash, "실패");

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _uploadStatusLabel.Text = $"서버 전송 상태: 실패 - {TranslateServerError(ex.Message)}";
                        _uploadStatusLabel.ForeColor = Color.Red;
                    }));
                }
            }
        }

        /// <summary>
        /// 재전송 버튼 클릭 - "실패" 상태인 주문을 다시 업로드
        /// </summary>
        private async void RetryUploadBtn_Click(object? sender, EventArgs e)
        {
            var failedOrders = _orderStorage.GetFailedOrders();
            if (failedOrders.Count == 0)
            {
                MessageBox.Show("재전송할 주문이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _retryUploadBtn.Enabled = false;
            _uploadStatusLabel.Text = $"재전송 중... ({failedOrders.Count}건)";
            _uploadStatusLabel.ForeColor = Color.Gray;

            int successCount = 0;
            int failCount = 0;
            string lastError = "";

            foreach (var order in failedOrders)
            {
                try
                {
                    var serverUrl = _config.ServerUrl;
                    if (string.IsNullOrEmpty(serverUrl))
                        serverUrl = "https://agent.zigso.kr";

                    try
                    {
                        await _uploadService.UploadReceiptAsync(_config.Token, serverUrl, _config.SiteId, order.ReceivedAt, order.Port, order.Content, order.Hash);
                    }
                    catch (UploadException ue) when (ue.StatusCode == 401)
                    {
                        // 토큰 만료 시 자동 재로그인 시도
                        if (!string.IsNullOrEmpty(_config.Email) && !string.IsNullOrEmpty(_config.Password))
                        {
                            await _authService.LoginAsync(_config.Email, _config.Password, serverUrl);
                            _config = LoginConfig.Load();
                            await _uploadService.UploadReceiptAsync(_config.Token, serverUrl, _config.SiteId, order.ReceivedAt, order.Port, order.Content, order.Hash);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    _orderStorage.UpdateStatus(order.Hash, "성공");
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    lastError = ex.Message;
                }
            }

            _uploadStatusLabel.Text = failCount > 0
                ? $"재전송 완료: 성공 {successCount}건, 실패 {failCount}건 (마지막 에러: {lastError})"
                : $"재전송 완료: 성공 {successCount}건, 실패 {failCount}건";
            _uploadStatusLabel.ForeColor = failCount > 0 ? Color.Orange : Color.Green;
            _retryUploadBtn.Enabled = true;
        }

        /// <summary>
        /// 프로그램 시작 시 당일 파일 로드하여 목록 표시
        /// </summary>
        private void LoadTodayOrders()
        {
            try
            {
                var todayOrders = _orderStorage.LoadToday();
                _orderListBox.Items.Clear();

                // 최신순으로 표시
                foreach (var order in todayOrders.OrderByDescending(o => o.Seq))
                {
                    var statusTag = order.UploadStatus == "중복" ? "[중복] " :
                                    order.UploadStatus == "실패" ? "[실패] " : "";
                    var displayText = $"{statusTag}[{DateTime.Parse(order.ReceivedAt):HH:mm}] {order.Content.Replace("\n", " ").Replace("\r", "")}";
                    _orderListBox.Items.Add(displayText);
                }

                // 가로 스크롤 범위 업데이트
                if (_orderListBox.Items.Count > 0)
                {
                    using (var g = _orderListBox.CreateGraphics())
                    {
                        int maxWidth = 0;
                        foreach (var item in _orderListBox.Items)
                        {
                            var width = (int)g.MeasureString(item.ToString() ?? "", _orderListBox.Font).Width;
                            if (width > maxWidth) maxWidth = width;
                        }
                        _orderListBox.HorizontalExtent = maxWidth + 10;
                    }
                }
            }
            catch
            {
                // 로드 실패 시 무시
            }
        }

        #endregion

        #region Settings - Port Management

        private async void UpdateSettingsPortInfo()
        {
            // 현재 포트 정보
            if (!string.IsNullOrEmpty(_config.CreatedPortA) && !string.IsNullOrEmpty(_config.CreatedPortB))
            {
                _currentPortLabel.Text = $"현재: {_config.CreatedPortA}\u2194{_config.CreatedPortB} (매장천사용/수신용)";
                _currentPortLabel.ForeColor = Color.DarkBlue;
            }
            else
            {
                _currentPortLabel.Text = "현재: 포트 미설정";
                _currentPortLabel.ForeColor = Color.Gray;
            }

            // 시스템 포트 스캔
            try
            {
                var physicalPorts = GetPhysicalPorts();
                var com0comPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // com0com 포트 판별: 설정에 저장된 CreatedPortA/B와 일치하는 포트
                if (!string.IsNullOrEmpty(_config.CreatedPortA))
                    com0comPorts.Add(_config.CreatedPortA);
                if (!string.IsNullOrEmpty(_config.CreatedPortB))
                    com0comPorts.Add(_config.CreatedPortB);

                // 레지스트리에서 com0com 드라이버 포트 확인
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            if (valueName.IndexOf("com0com", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var portName = key.GetValue(valueName)?.ToString();
                                if (!string.IsNullOrEmpty(portName))
                                    com0comPorts.Add(portName);
                            }
                        }
                    }
                }
                catch { }

                var portList = string.Join(" ", physicalPorts.Select(p =>
                    com0comPorts.Contains(p) ? $"{p}(com0com)" : $"{p}(사용중)"));
                _systemPortLabel.Text = $"시스템 포트: {(string.IsNullOrEmpty(portList) ? "없음" : portList)}";
            }
            catch
            {
                _systemPortLabel.Text = "시스템 포트: 스캔 실패";
            }
        }

        private async void CreatePortBtn_Click(object? sender, EventArgs e)
        {
            // 관리자 권한 사전 체크
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    MessageBox.Show("포트 생성에는 관리자 권한이 필요합니다.\n프로그램을 관리자 권한으로 실행하세요.", "관리자 권한 필요", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var portAInput = _portATextBox.Text.Trim();
            var portBInput = _portBTextBox.Text.Trim();

            if (string.IsNullOrEmpty(portAInput) || string.IsNullOrEmpty(portBInput))
            {
                _settingsStatusLabel.Text = "포트A와 포트B 번호를 모두 입력하세요.";
                _settingsStatusLabel.ForeColor = Color.Red;
                return;
            }

            if (!int.TryParse(portAInput, out int portANum) || portANum < 1 || portANum > 256)
            {
                _settingsStatusLabel.Text = "포트A: 유효한 포트 번호를 입력하세요 (1~256)";
                _settingsStatusLabel.ForeColor = Color.Red;
                return;
            }

            if (!int.TryParse(portBInput, out int portBNum) || portBNum < 1 || portBNum > 256)
            {
                _settingsStatusLabel.Text = "포트B: 유효한 포트 번호를 입력하세요 (1~256)";
                _settingsStatusLabel.ForeColor = Color.Red;
                return;
            }

            if (portANum == portBNum)
            {
                _settingsStatusLabel.Text = "포트A와 포트B는 다른 번호여야 합니다.";
                _settingsStatusLabel.ForeColor = Color.Red;
                return;
            }

            if (IsPortOccupied(portANum))
            {
                _settingsStatusLabel.Text = $"COM{portANum}은 사용 중입니다. 다른 번호를 입력하세요.";
                _settingsStatusLabel.ForeColor = Color.Red;
                return;
            }

            if (IsPortOccupied(portBNum))
            {
                _settingsStatusLabel.Text = $"COM{portBNum}은 사용 중입니다. 다른 번호를 입력하세요.";
                _settingsStatusLabel.ForeColor = Color.Red;
                return;
            }

            _createPortBtn.Enabled = false;
            _settingsStatusLabel.Text = "포트 생성 중...";
            _settingsStatusLabel.ForeColor = Color.Gray;

            try
            {
                await _comPortService.CreatePortWithNumbersAsync(portANum, portBNum);
                _settingsStatusLabel.Text = $"포트 생성 완료: COM{portANum} \u2194 COM{portBNum}";
                _settingsStatusLabel.ForeColor = Color.Green;

                _config.CreatedPortA = $"COM{portANum}";
                _config.CreatedPortB = $"COM{portBNum}";
                _config.Save();

                UpdateSettingsPortInfo();
            }
            catch (Exception ex)
            {
                _settingsStatusLabel.Text = $"포트 생성 실패: {TranslateSetupcError(ex.Message)}";
                _settingsStatusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _createPortBtn.Enabled = true;
            }
        }

        private async void DeletePortBtn_Click(object? sender, EventArgs e)
        {
            // 관리자 권한 사전 체크
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    MessageBox.Show("포트 삭제에는 관리자 권한이 필요합니다.\n프로그램을 관리자 권한으로 실행하세요.", "관리자 권한 필요", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (_serialListener.IsListening)
            {
                MessageBox.Show("먼저 수신을 중지하세요.", "삭제 불가", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_config.CreatedPortA) || string.IsNullOrEmpty(_config.CreatedPortB))
            {
                MessageBox.Show("삭제할 포트가 없습니다.\n포트를 먼저 생성하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 매장천사 포트(CreatedPortA) 사용 중 감지
            try
            {
                using var testPort = new SerialPort(_config.CreatedPortA);
                testPort.Open();
                testPort.Close();
            }
            catch
            {
                // 포트 열기 실패 = 다른 프로세스가 사용 중
                var useResult = MessageBox.Show(
                    $"{_config.CreatedPortA}이(가) 다른 프로그램(매장천사 등)에서 사용 중입니다.\n매장천사를 먼저 종료한 후 삭제하세요.\n\n그래도 삭제를 진행하시겠습니까?",
                    "포트 사용 중",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (useResult != DialogResult.Yes)
                    return;
            }

            var confirmResult = MessageBox.Show(
                $"{_config.CreatedPortA}\u2194{_config.CreatedPortB}를 삭제합니다.\n매장천사 프린터 설정도 다시 해야 합니다.\n삭제하시겠습니까?",
                "포트 삭제 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmResult != DialogResult.Yes)
                return;

            _deletePortBtn.Enabled = false;
            _settingsStatusLabel.Text = "포트 삭제 중...";
            _settingsStatusLabel.ForeColor = Color.Gray;

            try
            {
                var listOutput = await _comPortService.ListPortsAsync();
                var lines = listOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                string? foundIndex = null;

                foreach (var line in lines)
                {
                    if (line.IndexOf(_config.CreatedPortA, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("CNCA", StringComparison.OrdinalIgnoreCase))
                        {
                            var endIdx = 4;
                            while (endIdx < trimmed.Length && char.IsDigit(trimmed[endIdx]))
                                endIdx++;
                            foundIndex = trimmed.Substring(0, endIdx).Replace("CNCA", "");
                            break;
                        }
                    }
                }

                if (foundIndex == null)
                {
                    _settingsStatusLabel.Text = "포트 인덱스를 찾을 수 없습니다. 이미 삭제되었을 수 있습니다.";
                    _settingsStatusLabel.ForeColor = Color.Orange;
                    _config.CreatedPortA = "";
                    _config.CreatedPortB = "";
                    _config.Save();
                    UpdateSettingsPortInfo();
                    return;
                }

                await _comPortService.DeletePortAsync(foundIndex);

                _settingsStatusLabel.Text = $"포트 삭제 완료: {_config.CreatedPortA}\u2194{_config.CreatedPortB}";
                _settingsStatusLabel.ForeColor = Color.Green;
                _config.CreatedPortA = "";
                _config.CreatedPortB = "";
                _config.Save();
                UpdateSettingsPortInfo();
            }
            catch (Exception ex)
            {
                _settingsStatusLabel.Text = $"포트 삭제 실패: {TranslateSetupcError(ex.Message)}";
                _settingsStatusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _deletePortBtn.Enabled = true;
            }
        }

        private async void RestorePortBtn_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_config.CreatedPortA) || string.IsNullOrEmpty(_config.CreatedPortB))
            {
                MessageBox.Show("저장된 이전 포트 정보가 없습니다.", "복원 불가", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var portANum = int.Parse(_config.CreatedPortA.Replace("COM", ""));
            var portBNum = int.Parse(_config.CreatedPortB.Replace("COM", ""));

            var confirmResult = MessageBox.Show(
                $"이전 포트를 복원합니다.\n매장천사용: {_config.CreatedPortA} / 수신용: {_config.CreatedPortB}\n\n진행하시겠습니까?",
                "포트 복원",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
                return;

            _restorePortBtn.Enabled = false;
            _settingsStatusLabel.Text = "포트 복원 중...";
            _settingsStatusLabel.ForeColor = Color.Gray;

            try
            {
                await _comPortService.CreatePortWithNumbersAsync(portANum, portBNum);
                _settingsStatusLabel.Text = $"포트 복원 완료: COM{portANum} \u2194 COM{portBNum}";
                _settingsStatusLabel.ForeColor = Color.Green;
                UpdateSettingsPortInfo();
            }
            catch (Exception ex)
            {
                _settingsStatusLabel.Text = $"포트 복원 실패: {TranslateSetupcError(ex.Message)}";
                _settingsStatusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _restorePortBtn.Enabled = true;
            }
        }

        #endregion

        #region Settings - Misc

        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show(
                "로그아웃하시겠습니까?\n수신이 중지됩니다.",
                "로그아웃 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
                return;

            // 수신 중지
            if (_serialListener.IsListening)
            {
                _serialListener.StopListening();
                _isListening = false;
                _startListenBtn.Enabled = true;
                _stopListenBtn.Enabled = false;
            }

            // 토큰/설정 초기화
            _config.Token = "";
            _config.Password = "";
            _config.Save();

            // LoginPanel으로 전환
            _loginStatusLabel.Text = "상태: 로그아웃됨";
            _loginStatusLabel.ForeColor = Color.Gray;
            ShowLoginPanel();
        }

        private bool PromptAdminPassword()
        {
            var form = new Form
            {
                Text = "관리자 인증",
                Size = new Size(320, 160),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "관리자 비밀번호를 입력하세요:",
                Location = new Point(15, 15),
                Size = new Size(270, 20),
                Font = new Font("맑은 고딕", 9)
            };

            var textBox = new TextBox
            {
                Location = new Point(15, 40),
                Size = new Size(270, 25),
                UseSystemPasswordChar = true,
                Font = new Font("맑은 고딕", 10)
            };

            var okButton = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(125, 80),
                Size = new Size(75, 30),
                Font = new Font("맑은 고딕", 9)
            };

            var cancelButton = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(210, 80),
                Size = new Size(75, 30),
                Font = new Font("맑은 고딕", 9)
            };

            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;
            form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                if (textBox.Text == AdminPassword)
                    return true;

                MessageBox.Show("비밀번호가 올바르지 않습니다.", "인증 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return false;
        }

        #endregion

        #region AutoStart

        private void AutoStartCheckBox_Changed(object? sender, EventArgs e)
        {
            var isChecked = _autoStartCheckBox.Checked;
            _config.AutoStart = isChecked;
            _config.Save();

            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (isChecked)
                    {
                        key.SetValue("DeliveryOrderReceiver", $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue("DeliveryOrderReceiver", false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"자동 실행 설정 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region Utility Methods

        private HashSet<string> GetPhysicalPorts()
        {
            var ports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var portName = key.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(portName) && portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                            ports.Add(portName);
                    }
                }
            }
            catch { }
            return ports;
        }

        private bool IsPortOccupied(int portNum)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var portName = key.GetValue(valueName)?.ToString();
                        if (portName == $"COM{portNum}")
                            return true;
                    }
                }

                var activePorts = SerialPort.GetPortNames();
                if (activePorts.Any(p => p.Equals($"COM{portNum}", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            catch { }

            return false;
        }

        private string TranslateSetupcError(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return "알 수 없는 오류";

            if (errorMsg.Contains("Access is denied", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("access denied", StringComparison.OrdinalIgnoreCase))
                return "관리자 권한이 필요합니다. 프로그램을 관리자 권한으로 실행하세요.";

            if (errorMsg.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return "com0com이 설치되지 않았거나 setupc.exe를 찾을 수 없습니다.";

            if (errorMsg.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("in use", StringComparison.OrdinalIgnoreCase))
                return "해당 포트가 이미 존재하거나 사용 중입니다.";

            if (errorMsg.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                return "잘못된 포트 설정입니다. 포트 번호를 확인하세요.";

            if (errorMsg.Contains("driver", StringComparison.OrdinalIgnoreCase))
                return "com0com 드라이버 오류입니다. com0com을 재설치하세요.";

            if (errorMsg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "작업 시간이 초과되었습니다. 다시 시도하세요.";

            if (errorMsg.Contains("setupc.exe", StringComparison.OrdinalIgnoreCase))
                return errorMsg;

            return errorMsg;
        }

        private string TranslateServerError(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return "알 수 없는 오류";

            if (errorMsg.Contains("UNAUTHORIZED", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("401"))
                return "로그인이 필요합니다";

            if (errorMsg.Contains("FORBIDDEN", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("403"))
                return "접근 권한이 없습니다";

            if (errorMsg.Contains("INVALID_REQUEST", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("400"))
                return "잘못된 요청입니다";

            if (errorMsg.Contains("INTERNAL_SERVER_ERROR", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("500"))
                return "서버 오류가 발생했습니다";

            if (errorMsg.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase)
                || errorMsg.Contains("dedupe", StringComparison.OrdinalIgnoreCase))
                return "이미 전송된 주문입니다";

            if (errorMsg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "서버 응답 시간 초과";

            if (errorMsg.Contains("connection", StringComparison.OrdinalIgnoreCase))
                return "서버 연결 실패";

            return errorMsg;
        }

        #endregion
    }
}
