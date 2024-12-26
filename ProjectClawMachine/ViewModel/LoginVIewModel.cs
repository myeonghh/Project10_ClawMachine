using ProjectClawMachine.Helper;
using ProjectClawMachine.ViewModel.Command;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ProjectClawMachine.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private enum ACT { Login, SignUp, Streaming, ReceiveCheck };

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

        // 로그인 버튼 Command
        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            // 항상 실행 가능한 Command로 수정
            LoginCommand = new RelayCommand(async _ => await Login());
        }

        // 로그인 처리
        private async Task Login()
        {
            if (!CanLogin())
            {
                MessageBox.Show("아이디와 비밀번호를 모두 입력하세요.", "로그인 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 서버로 로그인 데이터 전송
                await TcpClientHelper.Instance.SendData(actType: (int)ACT.Login, senderId: Username, msg: Password);
                Console.WriteLine("로그인 요청 전송 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그인 중 오류 발생: {ex.Message}", "로그인 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 입력 값 검증
        private bool CanLogin()
        {
            return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        }

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            //서버 응답 처리(필요 시 구현)
            // 예시: actType 확인 및 결과 처리
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);

            switch (actType)
            {
                case ACT.Login:
                    string message = Encoding.UTF8.GetString(bodyBuffer);

                    if (message == "LoginSuccess")
                    {

                    }
                    else if (message == "LoginFailed")
                    {
                        MessageBox.Show("아이디 혹은 비밀번호가 맞지 않습니다.", "로그인 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case ACT.SignUp:
                    break;
                case ACT.Streaming:
                    break;
                case ACT.ReceiveCheck:
                    break;
                default:
                    break;
            }

        }
    }
}
