using ProjectClawMachine.Helper;
using ProjectClawMachine.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectClawMachine.ViewModel
{
    public class MainMenuViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, MachineConnect, MachineList, MachineChoice, Streaming, ReceiveCheck, MachineControl, GameOut, StreamingRequest, Logout };

        private readonly MainWindow _mainWindow; // MainWindow 참조

        public ICommand GoToLoginCommand { get; }
        public ICommand LogoutCommand { get; }

        public ICommand MachineChoiceCommand { get; }

        public MainMenuViewModel(MainWindow mainWindow)
        {
            string userId = UserSession.CurrentUserId;

            _mainWindow = mainWindow; // MainWindow 참조 저장
            GoToLoginCommand = new RelayCommand(_ => _mainWindow.LoadLoginView()); // 로그인 페이지 이동

            LogoutCommand = new RelayCommand(async _ => await Logout(userId)); // 로그아웃

            MachineChoiceCommand = new RelayCommand(async _ => await MachineChoice(userId)); // 로그아웃
        }

        private async Task MachineChoice(string userId)
        {
            await TcpClientHelper.Instance.SendData((int)ACT.MachineChoice, userId, "1");
        }

        private async Task Logout(string userId)
        {
            await TcpClientHelper.Instance.SendData((int)ACT.Logout, userId);
            _mainWindow.LoadLoginView(); // 로그인 페이지로 이동
        }

        protected override async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);

            if (actType == ACT.MachineList)
            {

            }
            else if (actType == ACT.MachineChoice)
            {
                string message = Encoding.UTF8.GetString(bodyBuffer);

                switch (message)
                {
                    case "Complete":                  
                        _mainWindow.LoadGamePlayView(); // 게임 플레이 페이지로 이동
                        break;
                    case "MachineInUse":
                        MessageBox.Show("이미 사용중인 기계입니다.", "게임실행 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    case "MachineNone":
                        MessageBox.Show("연결되지 않은 기계입니다.", "게임실행 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    default:
                        break;
                }
            }

            await Task.CompletedTask;
        }
    }
}
