using ProjectClawMachine.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProjectClawMachine.ViewModel
{
    public class GamePlayViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, Streaming, ReceiveCheck };

        private readonly MainWindow _mainWindow; // MainWindow 참조

        public ICommand GoToMainMenuCommand { get; }

        public GamePlayViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow; // MainWindow 참조 저장
            GoToMainMenuCommand = new RelayCommand(_ => _mainWindow.LoadMainMenuView()); // 로그인 페이지 이동
        }

        protected override async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);

            //if (actType == ACT.SignUp)
            //{
            //    string message = Encoding.UTF8.GetString(bodyBuffer);

            //}

            await Task.CompletedTask;
        }
    }
}
