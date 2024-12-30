using ProjectClawMachine.ViewModel.Command;
using ProjectClawMachine.ViewModel;
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
using ProjectClawMachine;

public class IntroViewModel : ViewModelBase
{
    private enum ACT { Login, SignUp, MachineConnect, MachineList, MachineChoice, Streaming, ReceiveCheck, MachineControl, GameOut, StreamingRequest, Logout };

    private readonly MainWindow _mainWindow;

    public IntroViewModel(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        Application.Current.MainWindow.KeyDown += OnKeyDown;
    }

    ~IntroViewModel()
    {
        Application.Current.MainWindow.KeyDown -= OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (PageSession.CurrentPage != "Intro")
            return;

        switch (e.Key)
        {
            case Key.Enter:
                Application.Current.MainWindow.KeyDown -= OnKeyDown;
                _mainWindow.LoadLoginView(); // 로그인 페이지로 이동
                break;
        }
    }
}
