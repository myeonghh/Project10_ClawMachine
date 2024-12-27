using ProjectClawMachine.Helper;
using ProjectClawMachine.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ProjectClawMachine.ViewModel
{
    public class MainMenuViewModel : ViewModelBase
    {
        private enum ACT { Login, SignUp, Streaming, ReceiveCheck };

        private readonly MainWindow _mainWindow; // MainWindow 참조

        public ICommand GoToLoginCommand { get; }

        public MainMenuViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow; // MainWindow 참조 저장
            GoToLoginCommand = new RelayCommand(_ => _mainWindow.LoadLoginView()); // 로그인 페이지 이동
        }

        protected override async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            string[] headerParts = header.Split('/');
            ACT actType = (ACT)int.Parse(headerParts[0]);



            await Task.CompletedTask;
        }
    }
}
