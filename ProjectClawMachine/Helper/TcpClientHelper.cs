using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClawMachine.Helper
{
    internal class TcpClientHelper
    {
        private static TcpClientHelper _instance;
        private static readonly object _lock = new object();

        private TcpClient client;
        private NetworkStream stream;

        public event Func<string, byte[], Task> OnDataReceived;

        private TcpClientHelper() { }

        public static TcpClientHelper Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new TcpClientHelper();
                }
            }
        }

        public async Task<bool> Connect(string serverIp, int serverPort)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort);
                stream = client.GetStream();
                Console.WriteLine($"서버에 연결되었습니다: {serverIp}:{serverPort}");
                _ = ReceiveData();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
                return false;
            }
        }

        private async Task ReceiveData()
        {
            try
            {
                while (true)
                {
                    byte[] headerBuffer = new byte[128];
                    int headerBytesRead = 0;

                    while (headerBytesRead < headerBuffer.Length)
                    {
                        int bytesRead = await stream.ReadAsync(headerBuffer, headerBytesRead, headerBuffer.Length - headerBytesRead);
                        if (bytesRead == 0) throw new Exception("서버 연결이 종료되었습니다.");
                        headerBytesRead += bytesRead;
                    }

                    string header = Encoding.UTF8.GetString(headerBuffer).TrimEnd('\0');
                    string[] parts = header.Split('/');
                    int dataLength = int.Parse(parts[2]);

                    byte[] bodyBuffer = new byte[dataLength];
                    int totalBytesRead = 0;

                    while (totalBytesRead < dataLength)
                    {
                        int bytesRead = await stream.ReadAsync(bodyBuffer, totalBytesRead, dataLength - totalBytesRead);
                        if (bytesRead == 0) throw new Exception("서버 연결이 종료되었습니다.");
                        totalBytesRead += bytesRead;
                    }

                    if (OnDataReceived != null)
                    {
                        await OnDataReceived(header, bodyBuffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 종료: {ex.Message}");
            }
        }

        public async Task SendData(int actType, string senderId = "", string msg = "")
        {
            try
            {
                string header = $"{actType}/{senderId}/{msg.Length}";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(128, '\0'));
                byte[] bodyBytes = Encoding.UTF8.GetBytes(msg);

                byte[] dataToSend = new byte[headerBytes.Length + bodyBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                Buffer.BlockCopy(bodyBytes, 0, dataToSend, headerBytes.Length, bodyBytes.Length);

                await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
                Console.WriteLine("데이터 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터 전송 실패: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            stream?.Close();
            client?.Close();
            Console.WriteLine("서버 연결이 종료되었습니다.");
        }
    }
}
