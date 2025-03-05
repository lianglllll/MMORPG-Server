using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Security;
using Common.Summer.Tools;
using GameServer.Core;
using GameServer.Core.Model;
using GameServer.Model;
using GameServer.Net;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.DBProxy.DBUser;
using HS.Protobuf.Game;
using HS.Protobuf.Scene;
using Serilog;
using System;
using System.Collections.Generic;

namespace GameServer.Hanle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private int m_curWorldId;
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();

        public bool Init(int gameWorldId)
        {
            m_curWorldId = gameWorldId;

            // 协议注册
            ProtoHelper.Instance.Register<GetCharacterListRequest>((int)GameProtocl.GetCharacterListReq);
            ProtoHelper.Instance.Register<GetCharacterListResponse>((int)GameProtocl.GetCharacterListResp);
            ProtoHelper.Instance.Register<CreateCharacterRequest>((int)GameProtocl.CreateCharacterReq);
            ProtoHelper.Instance.Register<CreateCharacterResponse>((int)GameProtocl.CreateCharacterResp);
            ProtoHelper.Instance.Register<DeleteCharacterRequest>((int)GameProtocl.DeleteCharacterReq);
            ProtoHelper.Instance.Register<DeleteCharacterResponse>((int)GameProtocl.DeleteCharacterResp);
            ProtoHelper.Instance.Register<GetDBCharactersByUidRequest>((int)DBCharacterProtocl.GetDbcharactersByUidReq);
            ProtoHelper.Instance.Register<GetDBCharactersByUidResponse>((int)DBCharacterProtocl.GetDbcharactersByUidResp);
            ProtoHelper.Instance.Register<AddDBCharacterRequset>((int)DBCharacterProtocl.AddDbcharacterReq);
            ProtoHelper.Instance.Register<AddDBCharacterResponse>((int)DBCharacterProtocl.AddDbcharacterResp);
            ProtoHelper.Instance.Register<GetDBUserRequest>((int)DBUserProtocl.GetDbuserReq);
            ProtoHelper.Instance.Register<GetDBUserResponse>((int)DBUserProtocl.GetDbuserResp);
            ProtoHelper.Instance.Register<DeleteDBCharacterByCidRequest>((int)DBCharacterProtocl.DeleteDbcharacterByCidReq);
            ProtoHelper.Instance.Register<DeleteDBCharacterByCidResponse>((int)DBCharacterProtocl.DeleteDbcharacterByCidResp);
            ProtoHelper.Instance.Register<EnterGameRequest>((int)GameProtocl.EnterGameReq);
            ProtoHelper.Instance.Register<EnterGameResponse>((int)GameProtocl.EnterGameResp);
            ProtoHelper.Instance.Register<GetDBCharacterByCidRequest>((int)DBCharacterProtocl.GetDbcharacterByCidReq);
            ProtoHelper.Instance.Register<GetDBCharacterByCidReponse>((int)DBCharacterProtocl.GetDbcharacterByCidResp);
            ProtoHelper.Instance.Register<CharacterEnterSceneRequest>((int)SceneProtocl.CharacterEnterSceneReq);
            ProtoHelper.Instance.Register<SelfCharacterEnterSceneResponse>((int)SceneProtocl.SelfCharacterEnterSceneResp);
            ProtoHelper.Instance.Register<ExitGameRequest>((int)GameProtocl.ExitGameReq);
            ProtoHelper.Instance.Register<CharacterLeaveSceneRequest>((int)SceneProtocl.CharacterLeaveSceneReq);


            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetCharacterListRequest>(_HandleGetCharacterListRequest);
            MessageRouter.Instance.Subscribe<GetDBCharactersByUidResponse>(_HandleGetDBCharactersByUidResponse);

            MessageRouter.Instance.Subscribe<CreateCharacterRequest>(_HandleCreateCharacterRequest);
            MessageRouter.Instance.Subscribe<AddDBCharacterResponse>(_HandleAddDBCharacterResponse);

            MessageRouter.Instance.Subscribe<DeleteCharacterRequest>(_HandleDeleteCharacterRequest);
            MessageRouter.Instance.Subscribe<GetDBUserResponse>(_HandleGetDBUserResponse);
            MessageRouter.Instance.Subscribe<DeleteDBCharacterByCidResponse>(_HandleDeleteDBCharacterByCidResponse);

            MessageRouter.Instance.Subscribe<EnterGameRequest>(_HandleEnterGameRequest);
            MessageRouter.Instance.Subscribe<GetDBCharacterByCidReponse>(_HandleGetDBCharacterByCidReponse);
            MessageRouter.Instance.Subscribe<SelfCharacterEnterSceneResponse>(_HandleSelfCharacterEnterSceneResponse);

            MessageRouter.Instance.Subscribe<ExitGameRequest>(_HandleExitGameRequest);

            return true;
        }

        private void _HandleGetCharacterListRequest(Connection conn, GetCharacterListRequest message)
        {
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            // 查询数据库
            GetDBCharactersByUidRequest req = new();
            req.TaskId = taskId;
            req.UId = message.UId;
            req.WorldId = m_curWorldId;
            ServersMgr.Instance.SendMsgToDBProxy(req);
        }
        private void _HandleGetDBCharactersByUidResponse(Connection conn, GetDBCharactersByUidResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }
            GetCharacterListRequest req = (GetCharacterListRequest)m_tasks[message.TaskId];
            GetCharacterListResponse resp = new();
            resp.SessionId = req.SessionId;
            var gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;

            resp.ResultCode = message.ResultCode;
            if (message.ResultCode != 0)
            {
                resp.ResultMsg = message.ResultMsg;
                goto End1;
            }

            foreach (var node in message.CNodes)
            {
                SimpleCharacterInfoNode infoNode = new();
                infoNode.CId = node.CId;
                infoNode.ChrName = node.ChrName;
                infoNode.ProfessionId = node.ProfessionId;
                infoNode.Level = node.Level;
                resp.CharacterNodes.Add(infoNode);
            }

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleCreateCharacterRequest(Connection conn, CreateCharacterRequest message)
        {
            CreateCharacterResponse resp = new();
            resp.SessionId = message.SessionId;

            //角色名是否为空
            if (string.IsNullOrWhiteSpace(message.CName))
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "角色名不能为空";
                goto End1;
            }
            //角色名长度限制
            if (message.CName.Length > 12)
            {
                resp.ResultCode = 2;
                resp.ResultMsg = "角色名最大长度为12";
                goto End1;
            }
            //角色类型有误
            if (message.ProfessionId >= 5 || message.ProfessionId < 0)
            {
                resp.ResultCode = 3;
                resp.ResultMsg = "请选择角色";
                goto End1;
            }

            // 查询数据库
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            AddDBCharacterRequset req = new();
            DBCharacterNode cNode = new();
            req.TaskId = taskId;
            req.ChrNode = cNode;

            cNode.UId = message.UId;
            cNode.ProfessionId = message.ProfessionId;
            cNode.ChrName = message.CName;
            cNode.WorldId = m_curWorldId;
            // level  默认
            // creationTimestamp  默认

            DBCharacterStatusNode chrStatus = new();
            cNode.ChrStatus = chrStatus;
            chrStatus.Hp = 0;
            chrStatus.Mp = 0;
            chrStatus.Exp = 0;
            chrStatus.CurSceneId = 1; // 起始之地
            chrStatus.X = 0;
            chrStatus.Y = 0;
            chrStatus.Z = 0;

            DBCharacterStatisticsNode chrStatistics = new();
            cNode.ChrStatistics = chrStatistics;
            chrStatistics.KillCount = 0;
            chrStatistics.DeathCount = 0;
            chrStatistics.TaskCompleted = 0;

            ServersMgr.Instance.SendMsgToDBProxy(req);
            goto End2;

        End1:
            conn.Send(resp);
        End2:
            return;
        }
        private void _HandleAddDBCharacterResponse(Connection conn, AddDBCharacterResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }
            CreateCharacterRequest req = (CreateCharacterRequest)m_tasks[message.TaskId];
            CreateCharacterResponse resp = new();
            resp.SessionId = req.SessionId;
            var gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;

            if (message.ResultCode == 1)
            {
                resp.ResultCode = 4;
                resp.ResultMsg = "最大角色数量为4.";
                goto End1;
            }
            else if (message.ResultCode == 2)
            {
                resp.ResultCode = 5;
                resp.ResultMsg = "角色名已存在.";
                goto End1;
            }
            else if (message.ResultCode == 3)
            {
                resp.ResultCode = 6;
                resp.ResultMsg = "未知错误!";
                goto End1;
            }

            resp.ResultCode = 0;
            resp.ResultMsg = "创建成功";
            SimpleCharacterInfoNode sNode = new();
            sNode.CId = message.CId;
            sNode.ChrName = req.CName;
            sNode.ProfessionId = req.ProfessionId;
            sNode.Level = 1;
            resp.CharacterNode = sNode;

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleDeleteCharacterRequest(Connection conn, DeleteCharacterRequest message)
        {
            DeleteCharacterResponse resp = new();
            resp.SessionId = message.SessionId;

            //角色名是否为空
            if (string.IsNullOrWhiteSpace(message.Password))
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "删除失败，密码不能为空";
                goto End1;
            }

            //查询数据库
            GetDBUserRequest getDBUserRequest = new GetDBUserRequest();
            int taskId = m_idGenerator.GetId();
            getDBUserRequest.TaskId = taskId;
            getDBUserRequest.UId = message.UId;
            m_tasks.Add(taskId, message);
            ServersMgr.Instance.SendMsgToDBProxy(getDBUserRequest);

        End1:
            conn.Send(resp);
        }
        private void _HandleGetDBUserResponse(Connection conn, GetDBUserResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End1;
            }

            DeleteCharacterRequest req = (DeleteCharacterRequest)m_tasks[message.TaskId];
            DeleteCharacterResponse resp = new();
            resp.SessionId = req.SessionId;
            Connection gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;

            // 验证用户名密码
            DBUserNode dBUserNode = message.User;
            if (PasswordHasher.Instance.VerifyPassword(req.Password, dBUserNode.Password) == false)
            {
                resp.ResultCode = 2;
                resp.ResultMsg = "删除失败，密码错误";
                goto End1;
            }

            // 告诉数据库进行删除操作
            DeleteDBCharacterByCidRequest deleteDBCharacterByCidRequest = new();
            deleteDBCharacterByCidRequest.TaskId = message.TaskId;
            deleteDBCharacterByCidRequest.CId = req.CId;
            ServersMgr.Instance.SendMsgToDBProxy(deleteDBCharacterByCidRequest);
            gateConn.Send(resp);
            goto End2;

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
        End2:
            return;
        }
        private void _HandleDeleteDBCharacterByCidResponse(Connection conn, DeleteDBCharacterByCidResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            DeleteCharacterRequest req = (DeleteCharacterRequest)m_tasks[message.TaskId];
            DeleteCharacterResponse resp = new();
            resp.SessionId = req.SessionId;
            Connection gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;

            if (message.ResultCode == 1)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "删除失败，未知错误，请联系管理员";
                goto End1;
            }

            resp.ResultCode = 0;
            resp.ResultMsg = "删除成功";

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleEnterGameRequest(Connection conn, EnterGameRequest message)
        {
            // 获取dbCharacter
            GetDBCharacterByCidRequest req = new();
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            req.TaskId = taskId;
            req.CId = message.CharacterId;
            req.ReadMask = new FieldMask { Paths = { "chrStatistics", "chrStatus", "chrAssets", "chrSocial", "chrCombat" } };
            ServersMgr.Instance.SendMsgToDBProxy(req);
        }
        private void _HandleGetDBCharacterByCidReponse(Connection conn, GetDBCharacterByCidReponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            EnterGameRequest req = (EnterGameRequest)m_tasks[message.TaskId];
            EnterGameResponse resp = new();
            resp.SessionId = req.SessionId;
            Connection gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;
            // 验证该dbCharacter是否存在
            if (message.ChrNode == null)
            {
                resp.ResultCode = 2;
                resp.ResultMsg = "没有你选择的角色信息。";
                goto End1;
            }
            // 保存一下与场景无关的character信息
            DBCharacterNode dbChrNode = message.ChrNode;
            if(GameCharacterManager.Instance.GetGameCharacterByCid(dbChrNode.CId) != null)
            {
                resp.ResultCode = 3;
                resp.ResultMsg = "角色已经在线...";
                goto End1;
            }
            var gChr = GameCharacterManager.Instance.CreateGameCharacter(dbChrNode);
            Log.Information("chr enter game,cId = [{0}]", gChr.Cid);

            // 将与场景相关的character移交scene进行初始化
            int curSceneId = dbChrNode.ChrStatus.CurSceneId;
            var sceneConn = GameMonitor.Instance.GetSceneConnBySceneId(curSceneId);
            // sceneConn为空，说明场景还没启动


            CharacterEnterSceneRequest characterEnterSceneRequest = new();
            characterEnterSceneRequest.TaskId = message.TaskId;
            characterEnterSceneRequest.SessionId = req.SessionId;
            GameToken gameToken = GameTokenManager.Instance.GetToken(req.GameToken);
            characterEnterSceneRequest.GameGateServerId = gameToken.ServerId;
            characterEnterSceneRequest.DbChrNode = dbChrNode;

            sceneConn.Send(characterEnterSceneRequest);
            goto End2;
        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
            gateConn.Send(resp);
        End2:
            return;
        }
        private void _HandleSelfCharacterEnterSceneResponse(Connection conn, SelfCharacterEnterSceneResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            EnterGameRequest req = (EnterGameRequest)m_tasks[message.TaskId];
            EnterGameResponse resp = new();
            resp.SessionId = req.SessionId;
            Connection gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;

            if (message.ResultCode != 0)
            {
                resp.ResultCode = 4;
                resp.ResultMsg = message.ResultMsg;
                goto End1;
            }

            // 保存一下场景信息
            var gChr = GameCharacterManager.Instance.GetGameCharacterByCid(req.CharacterId);
            gChr.CurSceneId = message.SelfNetActorNode.SceneId;
            gChr.EntityId = message.SelfNetActorNode.EntityId;

            // 拆message中的数据放到resp中
            resp.ResultCode = 0;
            resp.CharacterId = gChr.Cid;
            resp.SelfNetActorNode = message.SelfNetActorNode;
            if(message.OtherNetActorNodeList != null && message.OtherNetActorNodeList.Count > 0)
            {
                resp.OtherNetActorNodeList.AddRange(message.OtherNetActorNodeList);
            }
            if (message.OtherNetItemNodeList != null && message.OtherNetItemNodeList.Count > 0)
            {
                resp.OtherNetItemNodeList.AddRange(message.OtherNetItemNodeList);
            }

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleExitGameRequest(Connection conn, ExitGameRequest message)
        {
            // 解决当前的chr信息
            var gChar = GameCharacterManager.Instance.GetGameCharacterByCid(message.CharacterId);
            GameCharacterManager.Instance.RemoveGameCharacterByCid(message.CharacterId);

            // 告诉对应scene解决chr
            var req = new CharacterLeaveSceneRequest();
            req.EntityId = gChar.EntityId;
            var sceneConn = GameMonitor.Instance.GetSceneConnBySceneId(gChar.CurSceneId);
            sceneConn.Send(req);

            Log.Information("chr exit game,cId = [{0}]", gChar.Cid);
        }
    }
}
