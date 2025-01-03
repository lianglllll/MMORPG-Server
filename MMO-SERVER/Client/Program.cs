using System;
using System.Threading;
using Serilog;
using System.IO;
using System.Collections.Generic;
using lLua.Binchunk;
using Common.Summer.Security;
using Serilog.Sinks.SystemConsole.Themes;
using System.Net;
using Common.Summer.Net;
using HS.Protobuf.Common;
using Common.Summer.Proto;
using HS.Protobuf.Login;
using Google.Protobuf;
using Common.Summer.Core;

namespace ClientTest
{
    class Program
    {
        static void TestAES()
        {
            string key = "thisIsASecretKey";
            string iv = "thisIsAnIV123456";

            AesEncryption aes = new AesEncryption(key, iv);

            string original = "Hello, World!";
            Console.WriteLine("Original:   " + original);

            string encrypted = aes.Encrypt(original);
            Console.WriteLine("Encrypted:  " + encrypted);

            string decrypted = aes.Decrypt(encrypted);
            Console.WriteLine("Decrypted:  " + decrypted);
        }

        static void TestRSA()
        {
            RsaEncryption rsaEncryption = new RsaEncryption();

            // 获取公钥和私钥
            string publicKey = rsaEncryption.GetPublicKey();
            string privateKey = rsaEncryption.GetPrivateKey();

            Console.WriteLine("Public Key: " + publicKey);
            Console.WriteLine("Private Key: " + privateKey);

            // 加密数据
            string originalText = "Hello, RSA!";
            Console.WriteLine("\nOriginal: " + originalText);

            rsaEncryption.ImportPublicKey(publicKey);
            string encryptedText = rsaEncryption.Encrypt(originalText);
            Console.WriteLine("Encrypted: " + encryptedText);

            // 解密数据
            rsaEncryption.ImportPrivateKey(privateKey);
            string decryptedText = rsaEncryption.Decrypt(encryptedText);
            Console.WriteLine("Decrypted: " + decryptedText);
        }

        static void Lua()
        {
            string fileName = "luac.out";

            // 获取当前执行的可执行文件所在的目录
            string directoryPath = AppDomain.CurrentDomain.BaseDirectory;

            // 构建完整的文件路径
            string filePath = Path.Combine(directoryPath, fileName);

            try
            {
                // 读取文件内容
                byte[] fileBytes = File.ReadAllBytes(filePath);
                var p = Binary_Chunk.Undump(fileBytes);
                Binary_Chunk.Print(p);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            //初始化日志环境
            // 定义自定义控制台主题
            var customTheme = new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text] = "\x1b[37m", // 白色
                [ConsoleThemeStyle.SecondaryText] = "\x1b[37m", // 灰色
                [ConsoleThemeStyle.TertiaryText] = "\x1b[90m", // 深灰色
                [ConsoleThemeStyle.Invalid] = "\x1b[33m", // 黄色
                [ConsoleThemeStyle.Null] = "\x1b[34m", // 蓝色
                [ConsoleThemeStyle.Name] = "\x1b[32m", // 绿色
                [ConsoleThemeStyle.String] = "\x1b[36m", // 青色
                [ConsoleThemeStyle.Number] = "\x1b[35m", // 洋红色
                [ConsoleThemeStyle.Boolean] = "\x1b[34m", // 蓝色
                [ConsoleThemeStyle.Scalar] = "\x1b[32m", // 绿色
                [ConsoleThemeStyle.LevelVerbose] = "\x1b[90m", // 深灰色
                [ConsoleThemeStyle.LevelDebug] = "\x1b[37m", // 白色
                [ConsoleThemeStyle.LevelInformation] = "\x1b[32m", // 绿色
                [ConsoleThemeStyle.LevelWarning] = "\x1b[33m", // 黄色
                [ConsoleThemeStyle.LevelError] = "\x1b[31m", // 红色
                [ConsoleThemeStyle.LevelFatal] = "\x1b[41m\x1b[37m" // 红色背景，白色文本
            });
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    theme: customTheme,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Thread.Sleep(2000);

            ProtoHelper.Register<IPEnvelope>((int)CommonProtocl.IpEnvelope);
            ProtoHelper.Register<UserLoginRequest>((int)LoginProtocl.UserLoginRequest);
            ProtoHelper.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResponse);

            MessageRouter.Instance.Start(1);
            MessageRouter.Instance.Subscribe<IPEnvelope>(_HandleIPEnvelope);

            
            NetClient netClient = new NetClient();
            netClient.Init("127.0.0.1", 10700,
                (tcpClient) => { 
                    Log.Debug("Connected to LoginGate Server.");

                    IPEnvelope iPEnvelope = new IPEnvelope();
                    TCPEnvelope tCPEnvelope = new TCPEnvelope();
                    iPEnvelope.ProtocolCode = 2;
                    iPEnvelope.EncryptionLevel = 0;
                    iPEnvelope.TcpEnvelope = tCPEnvelope;
                    tCPEnvelope.ClientId = 1;
                    tCPEnvelope.SeqId = 1;
                    IMessage userLoginRequest = new UserLoginRequest { Username = "admin", Password = "admin" };
                    byte[] data = ProtoHelper.IMessageParse2BytesNoLen(userLoginRequest);
                    tCPEnvelope.Data = ByteString.CopyFrom(data);

                    for(int i = 0; i < 5; i++)
                    {
                        tCPEnvelope.SeqId += 1;
                        netClient.Send(iPEnvelope);
                    }

                },
                (tcpClient, isEnd) => { },
                (tcpClient) => { });



            Console.ReadLine();
        }

        private static void _HandleIPEnvelope(Connection sender, IPEnvelope message)
        {
            Log.Information(message.ToString());  

        }
    }
}
