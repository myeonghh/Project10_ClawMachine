using ProjectClawMachine.Helper;
using ProjectClawMachine.Model;
using ProjectClawMachine.ViewModel.Command;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ProjectClawMachine.ViewModel
{
    public class SignUpViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, Streaming, ReceiveCheck };

        private readonly MainWindow _mainWindow;

        // 회원가입 데이터 모델
        public SignUpModel SignUpData { get; set; }

        public ICommand GoToLoginCommand { get; }
        public ICommand SignUpCommand { get; }

        public SignUpViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            // 모델 초기화
            SignUpData = new SignUpModel();

            // 명령 초기화
            GoToLoginCommand = new RelayCommand(_ => _mainWindow.LoadLoginView()); // 로그인 페이지 이동
            SignUpCommand = new RelayCommand(async _ => await SignUp()); // 회원가입 요청
        }

        private async Task SignUp()
        {
            // 모델의 유효성 검사
            if (!SignUpData.IsValid())
            {
                MessageBox.Show("모든 정보를 입력해주세요.", "회원가입 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 회원가입 데이터를 서버로 전송
                string signUpData = $"{SignUpData.Password}/{SignUpData.PhoneNumber}/{SignUpData.Address}";
                await TcpClientHelper.Instance.SendData((int)ACT.SignUp, senderId: SignUpData.UserId, msg: signUpData);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"회원가입 중 오류 발생: {ex.Message}", "회원가입 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);

            if (actType == ACT.SignUp)
            {
                string message = Encoding.UTF8.GetString(bodyBuffer);

                switch (message)
                {
                    case "SignUpSuccess":
                        MessageBox.Show("회원가입이 성공적으로 완료되었습니다.", "회원가입 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                        _mainWindow.LoadLoginView(); // 회원가입 성공 후 로그인 페이지로 이동
                        break;
                    case "SignUpFailed":
                        MessageBox.Show("회원가입에 실패했습니다. 다시 시도해주세요.", "회원가입 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    case "IdDup":
                        MessageBox.Show("이미 존재하는 아이디입니다.", "회원가입 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    case "PhoneNumDup":
                        MessageBox.Show("이미 존재하는 번호입니다.", "회원가입 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    default:
                        break;
                }
            }

            await Task.CompletedTask;
        }
    }
}
