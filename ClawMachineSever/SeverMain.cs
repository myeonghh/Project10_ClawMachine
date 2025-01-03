﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Asn1.X509;
using static Mysqlx.Notice.Warning.Types;
using System.Collections.Concurrent;
using ZstdSharp.Unsafe;
using Mysqlx.Crud;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Data;

namespace ClawMachineSever
{
    internal class SeverMain
    {

        private class User
        {
            public TcpClient UserSocket { get; set; }
            public TcpClient MachineSocket { get; set; }
            public string UserId { get; set; }
            public string MachineId { get; set; }

            public User(TcpClient userSocket, TcpClient machineSocket, string userId)
            {
                this.UserSocket = userSocket;
                this.MachineSocket = machineSocket;
                this.UserId = userId;
                this.MachineId = "";
            }
        }

        private class Machine
        {
            public TcpClient MachineSocket { get; set; }
            public string MachineId { get; set; }
            public string Category { get; set; }
            public bool IsUse {  get; set; }

            public Machine(TcpClient machineSocket, string machineId, string category)
            {
                this.MachineSocket = machineSocket;
                this.MachineId = machineId;
                this.Category = category;
                this.IsUse = false;
            }
        }

        private List<TcpClient> clients = new List<TcpClient>();
        private List<User> userList = new List<User>();
        private List<Machine> machineList = new List<Machine>();

        private TcpListener server;
        private readonly object clientLock = new object();
        private DatabaseManager dbManager = new DatabaseManager();
        private int cnum = 1;

        private enum ACT { Login, SignUp, MachineConnect, MachineList, MachineChoice, Streaming, ReceiveCheck, MachineControl, GameOut, StreamingRequest, Logout };

        // 서버 시작
        public async Task StartServer(string ip, int port)
        {
            string dbConnectString = "Server=localhost;Port=3306;Database=clawMachine;Uid=root;Pwd=1234;";
            dbManager.Connect(dbConnectString);

            server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start();
            Console.WriteLine($"서버가 {ip}:{port}에서 시작되었습니다.");

            try
            {
                while (true)
                {
                    TcpClient clientSocket = await server.AcceptTcpClientAsync(); // 비동기로 클라이언트 연결 수락

                    lock (clientLock)
                    {
                        clients.Add(clientSocket);
                    }
                    Console.WriteLine($"클라이언트가 연결되었습니다. 현재 클라이언트 수: {clients.Count}");

                    _ = HandleClient(clientSocket); // 클라이언트 데이터 처리
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 실행 중 오류 발생: {ex.Message}");
            }
        }

        // 클라이언트와 통신
        private async Task HandleClient(TcpClient clientSocket)
        {
            NetworkStream stream = clientSocket.GetStream();
            byte[] headerBuffer = new byte[128]; // 고정 크기 헤더 버퍼

            try
            {
                while (true)
                {
                    // 1. 고정된 128바이트 헤더 읽기
                    int headerBytesRead = 0;
                    while (headerBytesRead < headerBuffer.Length)
                    {
                        int bytesRead = await stream.ReadAsync(headerBuffer, headerBytesRead, headerBuffer.Length - headerBytesRead);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("클라이언트 연결 종료");
                            return;
                        }
                        headerBytesRead += bytesRead;
                    }

                    // 2. 헤더 파싱 (actType, senderId, 데이터 크기 읽기)
                    string header = Encoding.UTF8.GetString(headerBuffer).TrimEnd('\0');
                    string[] parts = header.Split('/');
                    ACT actType = (ACT)int.Parse(parts[0]); // actType
                    string senderId = parts[1];             // senderId
                    int dataLength = int.Parse(parts[2]);   // 데이터 크기

                    Console.WriteLine($"헤더 수신: 타입={actType}, ID={senderId}, 데이터 크기={dataLength}");

                    // 3. 데이터 수신 (dataLength 크기만큼 읽기)
                    byte[] dataBuffer = new byte[dataLength];
                    int totalDataBytesRead = 0;

                    while (totalDataBytesRead < dataLength)
                    {
                        int bytesRead = await stream.ReadAsync(dataBuffer, totalDataBytesRead, dataLength - totalDataBytesRead);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("데이터 수신 중 연결 종료");
                            return;
                        }
                        totalDataBytesRead += bytesRead;
                    }

                    Console.WriteLine($"데이터 수신 완료: 크기={dataBuffer.Length}바이트");

                    // 4. 데이터 처리
                    if (actType == ACT.Streaming) // CCTV 이미지 데이터
                    {
                        Console.WriteLine($"이미지 수신 완료");
                        //// 5. 응답 전송 (클라이언트가 다음 이미지를 전송하도록 동기화)
                        //await SendMessage(clientSocket, (int)ACT.ReceiveCheck, "OK");
                        foreach (User client in userList)
                        {
                            if (client.MachineId == senderId)
                            {
                                await SendMessage(client.UserSocket, (int)ACT.Streaming, "", senderId, dataBuffer);
                                break;
                            }
                        }
                    }
                    else // 메시지 데이터
                    {
                        string message = Encoding.UTF8.GetString(dataBuffer);
                        Console.WriteLine($"메시지 수신: {message}");
                        await TextServerOperate(clientSocket, actType, senderId, message);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"클라이언트 통신 오류: {ex.Message}");
            }
            finally
            {
                lock (clientLock)
                {
                    clients.Remove(clientSocket);

                    foreach (User client in userList)
                    {
                        if (client.UserSocket == clientSocket)
                        {
                            foreach (Machine machine in machineList)
                            {
                                if (machine.MachineId == client.MachineId)
                                {
                                    machine.IsUse = false;
                                    break;
                                }
                            }
                            break;
                        }
                    }

                    userList.RemoveAll(c => c.UserSocket == clientSocket);
                    machineList.RemoveAll(c => c.MachineSocket == clientSocket);
                }               

                Console.WriteLine("클라이언트가 연결을 종료했습니다.");
                clientSocket.Close();
            }
        }

        // 서버 작업 처리
        private async Task TextServerOperate(TcpClient clientSocket, ACT actType, string senderId, string msg)
        {
            string machineId;

            switch (actType)
            {
                case ACT.Login:
                    string pw = msg;
                    await LoginControl(clientSocket, senderId, pw);
                    break;
                case ACT.SignUp:
                    await SignUpControl(clientSocket, senderId, msg);
                    break;
                case ACT.MachineConnect:
                    machineId = senderId;
                    string category = msg;
                    machineList.Add(new Machine(clientSocket, machineId, category));
                    Console.WriteLine("뽑기 기계 리스트에 기계 추가");
                    break;
                case ACT.MachineList:
                    await SendMachineList(clientSocket);
                    break;
                case ACT.MachineChoice:
                    machineId = msg;
                    await MachineChoiceControl(clientSocket, senderId, machineId);
                    break;
                case ACT.MachineControl:
                    string control = msg;
                    foreach (var client in userList)
                    {
                        if (client.UserId == senderId)
                        {
                            await SendMessage(client.MachineSocket, (int)ACT.MachineControl, control);
                            break;
                        }
                    }
                    break;
                case ACT.GameOut:
                    foreach (var client in userList)
                    {
                        if (client.UserId == senderId)
                        {
                            await SendMessage(client.MachineSocket, (int)ACT.GameOut);
                            foreach (var machine in machineList)
                            {
                                if (machine.MachineId == client.MachineId)
                                {
                                    machine.IsUse = false;
                                    break;
                                }
                            }                           
                            break;
                        }
                    }
                    break;
                case ACT.Logout:
                    userList.RemoveAll(c => c.UserId == senderId);
                    Console.WriteLine($"{senderId} 유저가 로그아웃 하였습니다.");
                    break;
                default:
                    break;
            }
        }

        private async Task SendMachineList(TcpClient clientSocket)
        {
            try
            {

                string totalMachineInfo = "";

                foreach (var machine in machineList)
                {
                    // 각 기계의 정보를 "MachineId,Category,상태" 형식으로 작성
                    string machineInfo = $"{machine.MachineId},{machine.Category},{(machine.IsUse ? "사용중" : "대기중")}";
                    totalMachineInfo += machineInfo + "/"; // 정보를 "/"로 구분하여 추가
                }

                // 마지막 "/" 제거
                if (totalMachineInfo.EndsWith("/"))
                {
                    totalMachineInfo = totalMachineInfo.Substring(0, totalMachineInfo.Length - 1);
                }

                // 클라이언트로 전송
                await SendMessage(clientSocket, (int)ACT.MachineList, totalMachineInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"기계 목록 전송 중 오류 발생: {ex.Message}");
            }
        }


        private async Task MachineChoiceControl(TcpClient clientSocket, string id, string machineId)
        {
            bool check = false;

            foreach (var machine in machineList)
            {
                if (machine.MachineId == machineId)
                {
                    if (machine.IsUse == false)
                    {
                        foreach (var client in userList)
                        {
                            if (client.UserId == id)
                            {
                                client.MachineSocket = machine.MachineSocket;
                                client.MachineId = machineId;
                            }
                        }

                        await SendMessage(clientSocket, (int)ACT.MachineChoice, "Complete");
                        await SendMessage(machine.MachineSocket, (int)ACT.StreamingRequest, "Request");
                        machine.IsUse = true;
                    }
                    else
                    {
                        await SendMessage(clientSocket, (int)ACT.MachineChoice, "MachineInUse");
                    }

                    check = true;
                    break;
                }
            }

            if (!check)
            {
                await SendMessage(clientSocket, (int)ACT.MachineChoice, "MachineNone");
            }
        }

        private async Task SignUpControl(TcpClient clientSocket, string id, string msg)
        {
            string[] msgParts = msg.Split("/");
            string password = msgParts[0];
            string phoneNum = msgParts[1];
            string address = msgParts[2];

            string query;
            int result;
            MySqlCommand command;

            try
            {
                query = "SELECT COUNT(*) FROM user WHERE id = @id";
                // 명령 객체 생성 및 파라미터 바인딩
                command = dbManager.CreateCommand(query);
                command.Parameters.AddWithValue("@id", id);

                // ExecuteScalar 사용 (조회 결과가 1이면 성공, 0이면 실패)
                result = Convert.ToInt32(command.ExecuteScalar());

                if (result > 0)
                {
                    // 아이디 중복
                    await SendMessage(clientSocket, (int)ACT.SignUp, "IdDup");
                    Console.WriteLine($"아이디 중복: {id}");
                    return;
                }

                query = "SELECT COUNT(*) FROM user WHERE phonenum = @phonenum";
                // 명령 객체 생성 및 파라미터 바인딩
                command = dbManager.CreateCommand(query);
                command.Parameters.AddWithValue("@phonenum", phoneNum);

                // ExecuteScalar 사용 (조회 결과가 1이면 성공, 0이면 실패)
                result = Convert.ToInt32(command.ExecuteScalar());

                if (result > 0)
                {
                    // 핸드폰번호 중복
                    await SendMessage(clientSocket, (int)ACT.SignUp, "PhoneNumDup");
                    Console.WriteLine($"핸드폰번호 중복: {phoneNum}");
                    return;
                }

                query = "INSERT INTO user (id, password, phonenum, address) VALUES (@id, @pw, @phonenum, @address);";
                command = dbManager.CreateCommand(query);

                // 파라미터 추가
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@pw", password);
                command.Parameters.AddWithValue("@phonenum", phoneNum);
                command.Parameters.AddWithValue("@address", address);

                // 실행
                int rowsAffected = command.ExecuteNonQuery();
                await SendMessage(clientSocket, (int)ACT.SignUp, "SignUpSuccess");
                Console.WriteLine($"{rowsAffected}명의 유저가 회원가입 명단에 추가되었습니다.");

            }
            catch (Exception ex)
            {
                await SendMessage(clientSocket, (int)ACT.SignUp, "SignUpFailed");
                Console.WriteLine($"회원가입 처리 중 오류 발생: {ex.Message}");              
            }

        }
        // 로그인 관리 함수
        private async Task LoginControl(TcpClient clientSocket, string id, string pw)
        {
            string query = "SELECT * FROM user WHERE id = @id AND password = @pw;";

            try
            {
                using (MySqlCommand command = dbManager.CreateCommand(query))
                {

                    // 파라미터 바인딩
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@pw", pw);
                    // 데이터 조회
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows) // 데이터가 있으면 로그인 성공
                        {
                            userList.Add(new User(clientSocket, null, id));
                            Console.WriteLine("유저 리스트에 유저 추가");
                    
                            // 로그인 성공 메시지 전송
                            await SendMessage(clientSocket, (int)ACT.Login, "LoginSuccess");
                            Console.WriteLine($"로그인 성공: {id}");
                        }
                        else // 데이터가 없으면 로그인 실패
                        {
                            await SendMessage(clientSocket, (int)ACT.Login, "LoginFailed");
                            Console.WriteLine($"로그인 실패: {id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그인 처리 중 오류 발생: {ex.Message}");
            }
        }

        // 메시지 전송
        private async Task SendMessage(TcpClient clientSocket, int actType, string msg = "", string senderId = "", byte[] imgData = null)
        {
            try
            {
                NetworkStream stream = clientSocket.GetStream();

                // 1. 메시지 데이터 준비
                byte[] bodyBytes;
                if ((ACT)actType == ACT.Streaming) // 이미지 전송
                {
                    bodyBytes = imgData; // 이미지 데이터
                }
                else // 메시지 전송 (텍스트)
                {
                    bodyBytes = Encoding.UTF8.GetBytes(msg); // UTF-8로 인코딩
                }

                // 2. 헤더 준비 (128바이트: actType, senderId, 데이터 길이 포함)
                string header = $"{actType}/{senderId}/{bodyBytes.Length}";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(128, '\0')); // 128바이트 고정 길이로 패딩

                // 3. 헤더와 데이터 결합
                byte[] fullData = new byte[headerBytes.Length + bodyBytes.Length];
                Array.Copy(headerBytes, 0, fullData, 0, headerBytes.Length); // 헤더 복사
                Array.Copy(bodyBytes, 0, fullData, headerBytes.Length, bodyBytes.Length); // 본문 데이터 복사

                // 4. 데이터 전송
                await stream.WriteAsync(fullData, 0, fullData.Length);

                if ((ACT)actType != ACT.Streaming)
                    Console.WriteLine($"데이터 전송 완료: {Encoding.UTF8.GetString(fullData)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"메시지 전송 오류: {ex.Message}");
            }
        }

        ~SeverMain()
        {
            dbManager.Disconnect();
        }
    }
}
