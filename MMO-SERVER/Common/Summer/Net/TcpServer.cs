using System;
using System.Net;
using System.Net.Sockets;
using Serilog;
using Common.Summer.Core;

namespace Common.Summer.Net
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
        private IPEndPoint m_endPoint;    //网络终结点
        private Socket m_listenerSocket;  //服务端监听对象
        private int m_backlog = 100;      //可以排队接收的传入连接数

        //委托
        public delegate void ConnectedCallback(Connection conn);                
        public delegate void DisconnectedCallback(Connection conn);             
        private event ConnectedCallback m_connected;       //接收到连接的事件
        private event DisconnectedCallback m_disconnected; //接收到连接断开的事件

        public bool IsRunning
        {
            get
            {
                return m_listenerSocket != null;
            }
        }

        public void Init(string host, int port, int backlog , 
            ConnectedCallback connected, DisconnectedCallback disconnected)
        {
            if (!IsRunning)
            {
                //事件注册
                m_connected += connected;
                m_disconnected += disconnected;

                //构造网络终结点
                m_endPoint = new IPEndPoint(IPAddress.Parse(host), port);
                this.m_backlog = backlog;

                m_listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_listenerSocket.Bind(m_endPoint);                                        //绑定一个IPEndPoint
                m_listenerSocket.Listen(backlog);                                       //开始监听，并设置等待队列长度 

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();             //可以复用,当前监听连接socket复用
                args.Completed += _OnAccepted;                                         //当有用户的连接时触发回调函数

                m_listenerSocket.AcceptAsync(args);                                   //异步接收
            }
            else
            {
                Log.Information("TcpServer already running");
            }
        }
        public void UnInit()
        {
            if (m_listenerSocket != null)
            {
                m_listenerSocket.Close();
                m_listenerSocket = null;
            }
            m_connected = null;
            m_disconnected = null;
        }
        public void Stop()
        {

        }
        public void Resume()
        {

        }

        private void _OnAccepted(object sender, SocketAsyncEventArgs e)
        {
            //连入的客户端
            Socket clientSocket = e.AcceptSocket;
            SocketError flag = e.SocketError;

            //继续接收下一位(异步操作)
            e.AcceptSocket = null;
            m_listenerSocket.AcceptAsync(e);

            //有人连接进来
            try
            {
                if (flag == SocketError.Success && clientSocket != null && clientSocket.Connected)
                {
                    // 为连接成功的 client 构造一个 connection 对象
                    Connection conn = new Connection();
                    conn.Init(clientSocket, _OnDisconnected);

                    // 通过委托将连接成功向上传递给 NetService
                    m_connected?.Invoke(conn);
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
        private void _OnDisconnected(Connection conn)
        {
            m_disconnected?.Invoke(conn);
        }
    }
}