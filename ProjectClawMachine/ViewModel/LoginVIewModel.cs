using ProjectClawMachine.Helper;
using ProjectClawMachine.ViewModel.Command;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ProjectClawMachine.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, MachineConnect, MachineList, MachineChoice, Streaming, ReceiveCheck, MachineControl, GameOut };

        private readonly MainWindow _mainWindow; // MainWindow 참조

        private string _username;
        private string _password;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand GoToSignUpCommand { get; }

        public LoginViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow; // MainWindow 참조 저장
            LoginCommand = new RelayCommand(async _ => await Login());
            GoToSignUpCommand = new RelayCommand(_ => _mainWindow.LoadSignUpView()); // 회원가입 페이지 이동
        }

        private async Task Login()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                MessageBox.Show("아이디와 비밀번호를 모두 입력하세요.", "로그인 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await TcpClientHelper.Instance.SendData((int)ACT.Login, Username, Password);
                Console.WriteLine("로그인 요청 전송 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그인 중 오류 발생: {ex.Message}", "로그인 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);

            if (actType == ACT.Login)
            {
                string message = Encoding.UTF8.GetString(bodyBuffer);
                if (message == "LoginSuccess")
                {
                    // 로그인 성공한 사용자 ID를 저장
                    UserSession.CurrentUserId = Username;

                    // 서버에 뽑기 기계 리스트 요청
                    await TcpClientHelper.Instance.SendData((int)ACT.MachineList, Username);
                    _mainWindow.LoadMainMenuView(); // 메인 메뉴로 이동
                }
                else if (message == "LoginFailed")
                {
                    MessageBox.Show("아이디 또는 비밀번호가 맞지 않습니다.", "로그인 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            await Task.CompletedTask;
        }
    }
}
