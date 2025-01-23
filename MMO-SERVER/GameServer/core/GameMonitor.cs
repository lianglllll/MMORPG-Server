using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Net;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Game;
using HS.Protobuf.GameGate;
using Serilog;
using System.Collections.Generic;

namespace GameServer.Core
{
    class GameGateEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfo { get; set; }
    }
    class SceneEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfo { get; set; }
    }

    public class GameMonitor : Singleton<GameMonitor>
    {
        private Dictionary<int, GameGateEntry> m_gameGateInstances = new(); // <serverId, GameGate>
        private Dictionary<int, SceneEntry> m_sceneInstances = new();       // <serverId, scene>
        private Dictionary<int, Connection> m_sceneConn = new();       // <sceneId, conn>
        private Queue<int> waitDispatchSceneId = new();

        public bool Init()
        {
            // todo
            waitDispatchSceneId.Enqueue(2);

            ProtoHelper.Instance.Register<RegisterSceneToGGRequest>((int)GameGateProtocl.RegisterScenesToGgReq);
            ProtoHelper.Instance.Register<RegisterSceneToGGResponse>((int)GameGateProtocl.RegisterScenesToGgResp);

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public bool RegisterInstance(Connection conn, ServerInfoNode serverInfoNode)
        {
            conn.Set<int>(serverInfoNode.ServerId);

            RegisterToGResponse resp = new();
            if (serverInfoNode.ServerType == SERVER_TYPE.Gamegate)
            {
                GameGateEntry gEntry = new GameGateEntry()
                {
                    ServerInfo = serverInfoNode,
                    Connection = conn
                };
                m_gameGateInstances.Add(serverInfoNode.ServerId, gEntry);

                // 分配一下连接token
                GameToken token = GameTokenManager.Instance.NewToken(conn,serverInfoNode.ServerId);
                // conn.Set<GameToken>(token);

                // 抓取全部scene给gate
                SendAllSceneToGameGate(conn,token);
            }
            else if(serverInfoNode.ServerType == SERVER_TYPE.Scene)
            {
                SceneEntry sEntry = new SceneEntry()
                {
                    ServerInfo = serverInfoNode,
                    Connection = conn
                };
                m_sceneInstances.Add(serverInfoNode.ServerId, sEntry);

                // 分配一下连接token
                GameToken token = GameTokenManager.Instance.NewToken(conn, serverInfoNode.ServerId);
                conn.Set<GameToken>(token);
                resp.GameToken = token.Id;
                // 分配sceneId
                // 我们假设是足够分配的
                int sceneId = waitDispatchSceneId.Peek();
                waitDispatchSceneId.Dequeue();
                serverInfoNode.SceneServerInfo.SceneId = sceneId;
                m_sceneConn.Add(sceneId, conn);
                // 告诉scene
                resp.AllocateSceneId = sceneId;
                conn.Send(resp);
                // 告诉全部gg
                TellNewSceneToGameGate(serverInfoNode);
            }
            else
            {
                Log.Error("GameMonitor.RegisterInstance 未知server类型。");
                return false;
            }
            return true;
        }
        private bool SendAllSceneToGameGate(Connection conn, GameToken token)
        {
            RegisterToGResponse resp = new();
            resp.GameToken = token.Id;
            foreach (var sEntry in m_sceneInstances.Values)
            {
                resp.SceneInfoNodes.Add(sEntry.ServerInfo);
            }
            conn.Send(resp);
            return true;
        }
        private bool TellNewSceneToGameGate(ServerInfoNode serverInfoNode)
        {
            RegisterSceneToGGRequest req = new();
            req.SceneInfos.Add(serverInfoNode);
            foreach (var ggEntry in m_gameGateInstances.Values)
            {
                ggEntry.Connection.Send(req);
            }
            return true;
        }
        public Connection GetSceneConnBySceneId(int sceneId) { 
            if(m_sceneConn.TryGetValue(sceneId, out var conn))
            {
                return conn;
            }
            return null;
        }


    }
}
