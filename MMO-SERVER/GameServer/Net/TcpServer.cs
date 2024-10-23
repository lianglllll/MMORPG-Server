using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Serilog;
using Google.Protobuf;

namespace GameServer.Network
{

    /// <summary>
    /// 负责监听TCP网络端口，基于Async编程模型，其中SocketAsyncEventArg实现了iocp模型
    /// 三个委托
    /// ----Connected       有新的连接
    /// ----DataReceived    有新的消息
    /// ----Disconnected    有连接断开
    /// IsRunning           是否正在运行
    /// Stop()              关闭服务
    /// Start()             启动服务
    /// </summary>
    public class TcpServer
    {
        //网络连接的属性
        private IPEndPoint endPoint;    //网络终结点
        private Socket listenerSocket;  //服务端监听对象
        private int backlog = 100;      //可以排队接收的传入连接数

        //委托
        public delegate void ConnectedCallback(Connection conn);                
        public delegate void DataReceivedCallback(Connection conn,IMessage data);
        public delegate void DisconnectedCallback(Connection conn);             
        public event ConnectedCallback Connected;       //接收到连接的事件
        public event DataReceivedCallback DataReceived; //接收到消息的事件          
        public event DisconnectedCallback Disconnected; //接收到连接断开的事件

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public TcpServer(string host,int port,int backlog = 100)
        {
            //构造网络终结点
            endPoint = new IPEndPoint(IPAddress.Parse(host), port);
            this.backlog = backlog;
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return listenerSocket != null;
            }
        }
        /// <summary>
        /// 启动服务，开始监听连接
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenerSocket.Bind(endPoint);                                        //绑定一个IPEndPoint
                listenerSocket.Listen(backlog);                                       //开始监听，并设置等待队列长度 

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();             //可以复用,当前监听连接socket复用
                args.Completed += OnAccept;                                         //当有用户的连接时触发回调函数

                listenerSocket.AcceptAsync(args);                                   //异步接收
            }
            else
            {
                Log.Information("TcpServer already running");
            }
        }
        /// <summary>
        /// 主动关闭服务，停止监听连接
        /// </summary>
        public void Stop()
        {
            if (listenerSocket == null)
            {
                return;
            }
            listenerSocket.Close();
            listenerSocket = null;
        }

        /// <summary>
        /// 接收到连接的回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            //连入的客户端
            Socket clientSocket = e.AcceptSocket;
            SocketError flag = e.SocketError;

            //继续接收下一位(异步操作)
            e.AcceptSocket = null;
            listenerSocket.AcceptAsync(e);

            //有人连接进来
            try
            {
                if (flag == SocketError.Success && clientSocket != null && clientSocket.Connected)
                {
                    // 为连接成功的 client 构造一个 connection 对象
                    Connection conn = new Connection(clientSocket);
                    conn.OnDataReceived += OnDataReceived;
                    conn.OnDisconnected += OnDisconnected;

                    // 通过委托将连接成功向上传递给 NetService
                    Connected?.Invoke(conn);
                }
                else
                {
                    Log.Warning("[TcpServer]Socket 状态异常或连接失败。");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.Error("[TcpServer]Socket 已被释放: " + ex.Message);
            }

        }

        /// <summary>
        /// 接收到数据的回调
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="data"></param>
        private void OnDataReceived(Connection conn, IMessage data)
        {
            DataReceived?.Invoke(conn, data);
        }
        /// <summary>
        /// 接收到连接断开的回调
        /// </summary>
        /// <param name="conn"></param>
        private void OnDisconnected(Connection conn)
        {
            Disconnected?.Invoke(conn);
        }

    }
}
