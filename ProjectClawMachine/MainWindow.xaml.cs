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
            LoadIntroView(); // 초기 화면을 로그인 페이지로 설정
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

        // 인트로 페이지 로드
        public void LoadIntroView()
        {
            var introView = new IntroView
            {
                DataContext = new IntroViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = introView;
            PageSession.CurrentPage = "Intro";
        }

        // 로그인 페이지 로드
        public void LoadLoginView()
        {
            var loginView = new LoginView
            {
                DataContext = new LoginViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = loginView;
            PageSession.CurrentPage = "Login";
        }

        // 회원가입 페이지 로드
        public void LoadSignUpView()
        {
            var signUpView = new SignUpView
            {
                DataContext = new SignUpViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = signUpView;
            PageSession.CurrentPage = "SignUp";
        }

        // 메인 메뉴 페이지 로드
        public void LoadMainMenuView()
        {
            var mainMenuView = new MainMenuView
            {
                DataContext = new MainMenuViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = mainMenuView;
            PageSession.CurrentPage = "MainMenu";
        }

        // 게임 시작 페이지 로드
        public void LoadGamePlayView()
        {
            var gamePlayView = new GamePlayView
            {
                DataContext = new GamePlayViewModel(this) // MainWindow 참조 전달
            };
            MainContent.Content = gamePlayView;
            PageSession.CurrentPage = "GamePlay";
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (this.WindowState == System.Windows.WindowState.Maximized && this.WindowStyle == System.Windows.WindowStyle.None)
                {
                    // 전체화면 → 일반화면
                    this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.Topmost = false;
                }
                else
                {
                    // 일반화면 → 전체화면
                    this.WindowStyle = System.Windows.WindowStyle.None;
                    this.WindowState = System.Windows.WindowState.Maximized;
                    this.ResizeMode = System.Windows.ResizeMode.NoResize;
                    this.Topmost = true;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
