using System.Net;
using System;
using System.Net.Sockets;
using Google.Protobuf;
using Common.Summer.Core;


namespace Common.Summer.Net
{
    //主要功能是从当一个client去连接其他服务
    public class TcpClient
    {
        private Socket m_clientSocket;
        private SocketAsyncEventArgs m_connectArgs;
        private Connection m_connection;
        public delegate void ConnectedCallback();
        public delegate void ConnectedFailedCallback(bool isEnd);
        public delegate void DisconnectedCallback();
        private event ConnectedCallback m_connected;            
        private event ConnectedFailedCallback m_connectFailed;  
        private event DisconnectedCallback m_disconnected;
        private int m_curReConnectionCount;
        private int m_maxReConnectionCount = 10;
        private float m_reConnectionInterval = 2f;

        public void Init(string ip, int port, 
            ConnectedCallback connected, ConnectedFailedCallback connectFailed, DisconnectedCallback disconnected)
        {
            m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_connectArgs = new SocketAsyncEventArgs();
            m_connectArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            m_connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_ConnectCallback);
            m_connectArgs.UserToken = m_clientSocket;

            m_curReConnectionCount = 0;
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

        private void _ConnectToServer()
        {
            if (m_connection != null && m_connection.Socket.Connected) return;
            // 异步连接
            m_clientSocket.ConnectAsync(m_connectArgs);
        }
        private  void _ConnectCallback(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket clientSocket = e.UserToken as Socket;
                m_connection = new Connection();
                m_connection.Init(clientSocket, _OnDisconnected);
                m_connected?.Invoke();
            }
            else
            {
                if(m_curReConnectionCount < m_maxReConnectionCount)
                {
                    m_curReConnectionCount++;
                    Scheduler.Instance.AddTask(_ConnectToServer, m_reConnectionInterval, m_reConnectionInterval, 1);
                    m_connectFailed?.Invoke(false);
                }
                else
                {
                    m_connectFailed?.Invoke(true);
                }
            }

        }
        private  void _OnDisconnected(Connection sender)
        {
            m_connection = null;
            m_disconnected?.Invoke();
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
