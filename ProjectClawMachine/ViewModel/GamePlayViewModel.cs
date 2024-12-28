using ProjectClawMachine.ViewModel.Command;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProjectClawMachine.Helper;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;

namespace ProjectClawMachine.ViewModel
{
    public class GamePlayViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, MachineConnect, MachineList, MachineChoice, Streaming, ReceiveCheck, MachineControl, GameOut };

        private readonly MainWindow _mainWindow;

        private ImageSource _cameraView;

        public ImageSource CameraView
        {
            get => _cameraView;
            set
            {
                _cameraView = value;
                OnPropertyChanged(nameof(CameraView));
            }
        }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand MoveLeftCommand { get; }
        public ICommand MoveRightCommand { get; }
        public ICommand GrabCommand { get; }
        public ICommand ExitGameCommand { get; }

        public GamePlayViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            // 버튼 클릭 명령 초기화 (비동기 명령 사용)
            MoveUpCommand = new AsyncRelayCommand(() => SendControlCommand("Up"));
            MoveDownCommand = new AsyncRelayCommand(() => SendControlCommand("Down"));
            MoveLeftCommand = new AsyncRelayCommand(() => SendControlCommand("Left"));
            MoveRightCommand = new AsyncRelayCommand(() => SendControlCommand("Right"));
            GrabCommand = new AsyncRelayCommand(() => SendControlCommand("Grab"));
            ExitGameCommand = new AsyncRelayCommand(ExitGame);

            // 키보드 입력 이벤트 핸들러 추가
            Application.Current.MainWindow.KeyDown += async (s, e) => await OnKeyDownAsync(e);
        }

        // 키보드 입력 처리 (비동기)
        private async Task OnKeyDownAsync(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    await SendControlCommand("Up");
                    break;
                case Key.Down:
                    await SendControlCommand("Down");
                    break;
                case Key.Left:
                    await SendControlCommand("Left");
                    break;
                case Key.Right:
                    await SendControlCommand("Right");
                    break;
                case Key.Enter:
                    await SendControlCommand("Grab");
                    break;
            }
        }

        // 서버로 명령 전송
        private async Task SendControlCommand(string control)
        {
            try
            {
                // 로그인한 사용자 ID 사용
                string userId = UserSession.CurrentUserId;

                // 서버로 메시지 전송
                await TcpClientHelper.Instance.SendData((int)ACT.MachineControl, userId, control);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"명령 전송 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // "그만하기" 명령 실행
        private async Task ExitGame()
        {
            try
            {
                // 로그인한 사용자 ID 사용
                string userId = UserSession.CurrentUserId;

                // 서버로 종료 요청 전송
                await TcpClientHelper.Instance.SendData((int)ACT.GameOut, userId);

                // 메인 메뉴로 돌아가기
                _mainWindow.LoadMainMenuView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"게임 종료 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);

            if (actType == ACT.Streaming)
            {
                // dataBuffer를 BitmapImage로 변환
                using (var ms = new MemoryStream(bodyBuffer))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze(); // UI 스레드에서 안전하게 접근할 수 있도록 Freeze

                    // CameraView 속성 업데이트
                    CameraView = bitmap;
                }
            }

            await Task.CompletedTask;
        }
    }
}