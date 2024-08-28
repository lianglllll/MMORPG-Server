using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Linq;
using System.IO;
using Google.Protobuf;
using GameServer.Network;
using System.Threading;
using Serilog;
using GameServer;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //初始化日志环境
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs\\client-log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            Thread.Sleep(1000);

            var ip = "127.0.0.1";
            int port = 6666;
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(iPEndPoint);

            Log.Information("已连接远程服务器");

            //用户登录消息吧
            Connection conn = new Connection(socket);


            var msg = new Proto.UserLoginRequest();
            msg.Username = "abc";
            msg.Password = "1323";
            conn.Send(msg);


            var msg2 = new Proto.GameEnterRequest();
            msg2.CharacterId = 1;
                
            conn.Send(msg2);




            Console.ReadLine();
            socket.Close();
        }

    }
}
