using ProjectClawMachine.Helper;
using ProjectClawMachine.View;
using ProjectClawMachine.ViewModel;
using System.Windows;

namespace ProjectClawMachine
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeTcpClient();
            LoadLoginView(); // 초기 화면을 로그인 페이지로 설정
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
        public void LoadLoginView()
        {
            var loginView = new LoginView
            {
                DataContext = new LoginViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = loginView;
        }

        // 회원가입 페이지 로드
        public void LoadSignUpView()
        {
            var signUpView = new SignUpView
            {
                DataContext = new SignUpViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = signUpView;
        }

        // 메인 메뉴 페이지 로드
        public void LoadMainMenuView()
        {
            var mainMenuView = new MainMenuView
            {
                DataContext = new MainMenuViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = mainMenuView;
        }

        // 게임 시작 페이지 로드
        public void LoadGamePlayView()
        {
            var gamePlayView = new GamePlayView
            {
                DataContext = new GamePlayViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = gamePlayView;
        }
    }
}
