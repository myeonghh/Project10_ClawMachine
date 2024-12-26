using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ProjectClawMachine.Helper;
using ProjectClawMachine.View;
using ProjectClawMachine.ViewModel;

namespace ProjectClawMachine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeTcpClient();
            LoadLoginView();
        }

        private async void InitializeTcpClient()
        {
            string serverIp = "10.10.20.105";
            int serverPort = 12345;

            TcpClientHelper clientManager = TcpClientHelper.Instance;
            bool isConnected = await clientManager.Connect(serverIp, serverPort);

            if (isConnected)
            {
                Console.WriteLine("TCP 연결 성공");
            }
            else
            {
                Console.WriteLine("TCP 연결 실패");
            }
        }

        // 로그인 페이지 로드
        private void LoadLoginView()
        {
            var loginView = new LoginView
            {
                DataContext = new LoginViewModel() // LoginView에 LoginViewModel 바인딩
            };
            MainContent.Content = loginView;
        }

        // 회원가입 페이지 로드
        private void LoadSignUpView()
        {
            var signUpView = new SignUpVIew
            {
                DataContext = new SignUpViewModel() // SignUpView에 SignUpViewModel 바인딩
            };
            MainContent.Content = signUpView;
        }
    }
}