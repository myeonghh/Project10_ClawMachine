using ProjectClawMachine.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClawMachine.ViewModel
{
    public class SignUpViewModel
    {
        public SignUpViewModel()
        {
            // TCP 데이터 수신 이벤트 구독
            TcpClientHelper.Instance.OnDataReceived += HandleServerData;
        }

        private async Task HandleServerData(string header, byte[] bodyBuffer)
        {
            //// 헤더 분석
            //string[] headerParts = header.Split('/');
            //int actType = int.Parse(headerParts[0]);

            //// 회원가입 관련 데이터 처리
            //if (actType == 2) // 예: actType 2는 회원가입 응답
            //{
            //    string message = Encoding.UTF8.GetString(bodyBuffer);
            //    Console.WriteLine($"회원가입 처리 결과: {message}");
            //}

            //await Task.CompletedTask;
        }
    }
}
