using System.Net;
using System;
using System.Net.Sockets;
using Google.Protobuf;
using Common.Summer.Core;
using Serilog;
using System.ComponentModel;


namespace Common.Summer.Net
{
    public enum ConnState
    {
        Disconnected,
        Connecting,
        Reconnecting,   // 以前连接成功过，后面断线了再连接
        Connected
    }

    public class NetClient_New
    {
        // 连接相关
        private ConnState m_curConnState;
        private ConnState m_preConnState;
        private bool m_Inited;
        private SocketAsyncEventArgs m_connectArgs;
        private Socket m_clientSocket;
        private Connection m_connection;

        // 事件回调
        public delegate void TcpClientConnectedCallback2(NetClient_New tcpClient);
        public delegate void TcpClientConnectedFailedCallback2(NetClient_New tcpClient, bool isEnd);
        public delegate void TcpClientDisconnectedCallback2(NetClient_New tcpClient);
        private event TcpClientConnectedCallback2 m_connected;
        private event TcpClientConnectedFailedCallback2 m_connectFailed;
        private event TcpClientDisconnectedCallback2 m_disconnected;
        
        // 重连服务器
        private int m_curReConnectionCount;
        private int m_maxReConnectionCount = 10;
        private float m_reConnectionInterval = 2f;

        // 初始化相关
        public void Init(string ip, int port, 
            TcpClientConnectedCallback2 connected, TcpClientConnectedFailedCallback2 connectFailed,
            TcpClientDisconnectedCallback2 disconnected, int maxReconnectionCount = 10)
        {
            m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_connectArgs = new SocketAsyncEventArgs();
            m_connectArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            m_connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_ConnectCallback);
            m_connectArgs.UserToken = m_clientSocket;

            m_curReConnectionCount = 0;
            m_maxReConnectionCount = maxReconnectionCount;
            m_connected = connected;
            m_connectFailed = connectFailed;
            m_disconnected = disconnected;

            m_curConnState = ConnState.Disconnected;
            m_preConnState = ConnState.Disconnected;
            m_Inited = true;
        }
        public void UnInit()
        {
            m_clientSocket = null;
            m_connectArgs = null;
            m_connected = null;
            m_connectFailed = null;
            m_disconnected = null;

            m_Inited = false;
            m_curConnState = ConnState.Disconnected;
        }

        public void ConnectToServer()
        {
            if(m_Inited == false)
            {
                Log.Warning("NetClient 尚未初始化， 拒绝连接");
                goto End;
            }
            if (m_curConnState == ConnState.Connecting || m_curConnState == ConnState.Connected)
            {
                Log.Warning("NetClient 已连接， 拒绝重复连接");
                goto End;
            }

            // 异步连接
            m_clientSocket.ConnectAsync(m_connectArgs);
            ChangeConnState(ConnState.Connecting);

        End:
            return;
        }
        private void _ConnectCallback(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket clientSocket = e.UserToken as Socket;
                m_connection = new Connection();
                m_connection.Init(clientSocket, _OnDisconnected);

                ChangeConnState(ConnState.Connected);

                // 通知上层
                m_connected?.Invoke(this);
            }
            else
            {
                // 继续尝试连接
                if (m_maxReConnectionCount == 0 || m_curReConnectionCount < m_maxReConnectionCount)
                {
                    ChangeConnState(ConnState.Reconnecting);
                    m_curReConnectionCount++;
                    Scheduler.Instance.AddTask(ConnectToServer, m_reConnectionInterval, m_reConnectionInterval, 1);
                    m_connectFailed?.Invoke(this, false);
                }
                else
                {
                    m_connectFailed?.Invoke(this, true);
                }
            }
        }
        private void _OnDisconnected(Connection conn)
        {
            m_connection = null;
            m_curConnState = ConnState.Disconnected;
            m_disconnected?.Invoke(this);
        }
        
        // 工具
        private void ChangeConnState(ConnState state)
        {
            if(m_curConnState == state) return;
            m_preConnState = m_curConnState; 
            m_curConnState = state;
        }
        public bool Send(IMessage message)
        {
            if (m_connection != null)
            {
                m_connection.Send(message);
                return true;
            }
            return false;
        }
        public void CloseConnection()
        {
            m_connection?.CloseConnection();
            m_connection = null;
        }
    }
}
