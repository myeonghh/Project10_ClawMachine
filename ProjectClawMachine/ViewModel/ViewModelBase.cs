using ProjectClawMachine.Helper;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClawMachine.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase()
        {
            // TcpClientHelper 이벤트 구독
            TcpClientHelper.Instance.OnDataReceived += HandleServerData;
        }

        // 서버에서 받은 데이터를 처리하는 공통 메서드
        protected virtual async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            // 하위 ViewModel에서 구현 필요
            await Task.CompletedTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
