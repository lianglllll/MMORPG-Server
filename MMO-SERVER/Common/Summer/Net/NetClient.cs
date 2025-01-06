using System.Net;
using System;
using System.Net.Sockets;
using Google.Protobuf;
using Common.Summer.Core;
using Serilog;


namespace Common.Summer.Net
{
    //主要功能是从当一个client去连接其他服务
    public class NetClient
    {
        private Socket m_clientSocket;
        private SocketAsyncEventArgs m_connectArgs;
        private Connection m_connection;

        public delegate void TcpClientConnectedCallback(NetClient tcpClient);
        public delegate void TcpClientConnectedFailedCallback(NetClient tcpClient, bool isEnd);
        public delegate void TcpClientDisconnectedCallback(NetClient tcpClient);

        private event TcpClientConnectedCallback m_connected;            
        private event TcpClientConnectedFailedCallback m_connectFailed;  
        private event TcpClientDisconnectedCallback m_disconnected;

        private int m_curReConnectionCount;
        private int m_maxReConnectionCount = 10;
        private float m_reConnectionInterval = 2f;

        public void Init(string ip, int port, int maxReconnectionCount,
            TcpClientConnectedCallback connected, TcpClientConnectedFailedCallback connectFailed, 
            TcpClientDisconnectedCallback disconnected)
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

            _ConnectToServer();
        }
        public void UnInit()
        {
            m_clientSocket = null;
            m_connectArgs = null;
            m_connected = null;
            m_connectFailed = null;
            m_disconnected = null;
        }
        public Connection CloseConnection()
        {
            var conn = m_connection;
            m_connection = null;
            return conn;
        }
        private void _ConnectToServer()
        {
            if (m_connection != null && m_connection.Socket.Connected) return;
            // 异步连接
            m_clientSocket.ConnectAsync(m_connectArgs);
        }
        public bool ReConnectToServer()
        {
            if(m_clientSocket == null)
            {
                Log.Error("[NetClient]未初始化，紧张重连。");
                return false;
            }
            _ConnectToServer();
            return true;
        }
        private  void _ConnectCallback(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket clientSocket = e.UserToken as Socket;
                m_connection = new Connection();
                m_connection.Init(clientSocket, _OnDisconnected);
                m_connected?.Invoke(this);
            }
            else
            {
                if(m_maxReConnectionCount == 0 || m_curReConnectionCount < m_maxReConnectionCount)
                {
                    m_curReConnectionCount++;
                    Scheduler.Instance.AddTask(_ConnectToServer, m_reConnectionInterval, m_reConnectionInterval, 1);
                    m_connectFailed?.Invoke(this, false);
                }
                else
                {
                    m_connectFailed?.Invoke(this, true);
                }
            }

        }
        private  void _OnDisconnected(Connection conn)
        {
            m_connection = null;
            m_disconnected?.Invoke(this);
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
    }
}
