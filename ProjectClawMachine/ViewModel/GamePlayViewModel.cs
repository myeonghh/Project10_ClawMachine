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
using System.Windows.Controls;

namespace ProjectClawMachine.ViewModel
{
    public class GamePlayViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, MachineConnect, MachineList, MachineChoice, Streaming, ReceiveCheck, MachineControl, GameOut, StreamingRequest, Logout };

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
        public ICommand MoveFrontCommand { get; }
        public ICommand MoveBehindCommand { get; }
        public ICommand GrabCommand { get; }
        public ICommand ExitGameCommand { get; }

        public GamePlayViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            MoveUpCommand = new AsyncRelayCommand(() => SendControlCommand("Up"));
            MoveDownCommand = new AsyncRelayCommand(() => SendControlCommand("Down"));
            MoveLeftCommand = new AsyncRelayCommand(() => SendControlCommand("Left"));
            MoveRightCommand = new AsyncRelayCommand(() => SendControlCommand("Right"));
            MoveFrontCommand = new AsyncRelayCommand(() => SendControlCommand("Front"));
            MoveBehindCommand = new AsyncRelayCommand(() => SendControlCommand("Behind"));
            GrabCommand = new AsyncRelayCommand(() => SendControlCommand("Grab"));
            ExitGameCommand = new AsyncRelayCommand(ExitGame);

            AttachKeyDownEvent(); // KeyDown 이벤트 핸들러 등록
                                 
        }

        // KeyDown 이벤트 핸들러 등록
        private void AttachKeyDownEvent()
        {
            if (!EventSession.IsKeyDownEventAttached)
            {
                Application.Current.MainWindow.KeyDown += OnKeyDown;
                EventSession.IsKeyDownEventAttached = true;
                MessageBox.Show($"키다운 이벤트 등록", "등록", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // KeyDown 이벤트 핸들러 제거
        private void DetachKeyDownEvent()
        {
            if (EventSession.IsKeyDownEventAttached)
            {
                Application.Current.MainWindow.KeyDown -= OnKeyDown;
                EventSession.IsKeyDownEventAttached = false;
                MessageBox.Show($"키다운 이벤트 해제", "해제", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (PageSession.CurrentPage != "GamePlay")
                return;

            switch (e.Key)
            {
                case Key.Up:
                    await SendControlCommand("Behind");
                    break;
                case Key.Down:
                    await SendControlCommand("Front");
                    break;
                case Key.Left:
                    await SendControlCommand("Right");
                    break;
                case Key.Right:
                    await SendControlCommand("Left");
                    break;
                case Key.W:
                    await SendControlCommand("Up");
                    break;
                case Key.S:
                    await SendControlCommand("Down");
                    break;
                case Key.Enter:
                    await SendControlCommand("Grab");
                    break;
            }
        }

        private async Task SendControlCommand(string control)
        {
            try
            {
                string userId = UserSession.CurrentUserId;
                await TcpClientHelper.Instance.SendData((int)ACT.MachineControl, userId, control);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"명령 전송 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExitGame()
        {
            try
            {
                DetachKeyDownEvent(); // KeyDown 이벤트 핸들러 제거
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
                using (MemoryStream ms = new MemoryStream(bodyBuffer))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    CameraView = bitmap;
                }
            }

            await Task.CompletedTask;
        }

        // 소멸자에서 KeyDown 이벤트 핸들러 해제
        ~GamePlayViewModel()
        {
            DetachKeyDownEvent();
        }
    }
}