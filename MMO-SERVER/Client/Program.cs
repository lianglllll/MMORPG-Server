using System;
using System.Threading;
using Serilog;
using System.IO;
using System.Collections.Generic;
using lLua.Binchunk;
using Common.Summer.Security;
using Serilog.Sinks.SystemConsole.Themes;

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

            // 测试日志输出
            Log.Debug("This is a debug message.");  // 白色
            Log.Information("This is an information message.");  // 绿色
            Log.Warning("This is a warning message.");  // 黄色
            Log.Error("This is an error message.");  // 红色
            Log.Fatal("This is a fatal message.");  // 红色背景，白色文本

            Log.CloseAndFlush();

            Thread.Sleep(1000);

            //var ip = "127.0.0.1";
            //int port = 6666;
            //IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //socket.Connect(iPEndPoint);

            //Log.Information("已连接远程服务器");

            ////用户登录消息吧
            //Connection conn = new Connection(socket);


            //var msg = new Proto.UserLoginRequest();
            //msg.Username = "abc";
            //msg.Password = "1323";
            //conn.Send(msg);


            //var msg2 = new Proto.GameEnterRequest();
            //msg2.CharacterId = 1;

            //conn.Send(msg2);

            //socket.Close();

            //TestRSA();

            // Lua();
            Console.ReadLine();
        }
    }
}
