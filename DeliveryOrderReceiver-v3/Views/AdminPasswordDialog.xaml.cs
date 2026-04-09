using System.Windows;

namespace DeliveryOrderReceiver.Views;

public partial class AdminPasswordDialog : Window
{
    public string Password { get; private set; } = string.Empty;
    private readonly bool _setupMode;

    public AdminPasswordDialog(bool setupMode)
    {
        InitializeComponent();
        _setupMode = setupMode;
        if (setupMode)
        {
            Title = "관리자 비밀번호 설정";
            TitleText.Text = "최초 설정 — 새 관리자 비밀번호를 입력하세요.";
            ConfirmPanel.Visibility = Visibility.Visible;
        }
        else
        {
            Title = "관리자 비밀번호";
            TitleText.Text = "관리자 비밀번호를 입력하세요.";
        }
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var pwd = PasswordBox.Password;
        if (string.IsNullOrEmpty(pwd))
        {
            ErrorText.Text = "비밀번호를 입력하세요.";
            return;
        }
        if (_setupMode)
        {
            if (pwd.Length < 4)
            {
                ErrorText.Text = "최소 4자 이상이어야 합니다.";
                return;
            }
            if (pwd != ConfirmBox.Password)
            {
                ErrorText.Text = "확인 비밀번호가 일치하지 않습니다.";
                return;
            }
        }
        Password = pwd;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
