using System;
using System.Collections.Generic;
using GameServer.Net;
using Serilog;
using GameServer.Model;
using GameServer.Manager;
using GameServer.Database;
using Common.Summer.Tools;
using Common.Summer.Net;
using Common.Summer.Core;
using HS.Protobuf.Login;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using HS.Protobuf.Game;

namespace GameServer.Service
{
    /// <summary>
    /// 玩家服务
    /// 注册，登录，创建角色，进入游戏
    /// </summary>
    public class UserService:Singleton<UserService>
    {
        //一位用户拥有的角色上限
        int maxRoleCount = 4;

        /// <summary>
        /// 启动当前服务
        /// </summary>
        public void Start()
        {
            MessageRouter.Instance.Subscribe<EnterGameRequest>(_GameEnterRequest);
            MessageRouter.Instance.Subscribe<ReconnectRequest>(_ReconnectRequest);
            MessageRouter.Instance.Subscribe<ServerInfoRequest>(_ServerInfoRequest);
        }

        /// <summary>
        /// 进入游戏请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void _GameEnterRequest(Connection conn, EnterGameRequest msg)
        {
            //Log.Information($"有玩家进入游戏,角色id={msg.CharacterId}");

            //获取当前用户，通过userid寻找有无这个角色
            DbUser dbUser = conn.Get<Session>().dbUser;
            if (dbUser == null)
            {
                return;
            }
            //查询数据库中角色的信息
            DbCharacter dbCharacter = DbManager.fsql.Select<DbCharacter>()
                .Where(t => t.Id == msg.CharacterId)
                .Where(t => t.PlayerId == dbUser.Id)
                .First();

            //将数据库角色类转为游戏角色类，添加进管理器管理
            Character character = CharacterManager.Instance.CreateCharacter(dbCharacter);

            //将character 与 session 进行关联
            character.session = conn.Get<Session>();
            //将角色引用放入conn连接当中
            character.session.character = character;

            //告知场景新加入了一个entity,进行广播
            Space space = SpaceService.Instance.GetSpaceById(dbCharacter.SpaceId);
            space?.EntityJoin(character);

        }

        /// <summary>
        /// 断线重连请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ReconnectRequest(Connection conn, ReconnectRequest message)
        {
            var sessionId = message.SessionId;
            var session = SessionManager.Instance.GetSession(sessionId);

            //情况1.说明session过期，或者压根没登录所以没有session
            if(session == null)
            {
                conn.Send(new ReconnectResponse { 
                    Success = false
                });
                return;
            }

            //情况2：有session，可以重连
            //关联session和conn
            session.Conn = conn;
            conn.Set<Session>(session);
            ReconnectResponse res = new ReconnectResponse();
            res.Success = true;
            if(session.character != null)
            {
                res.EntityId = session.character.EntityId;
            }
            else
            {
                res.EntityId = 0;
            }
            session.Send(res);
        }

        /// <summary>
        /// 获取服务器信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ServerInfoRequest(Connection sender, ServerInfoRequest message)
        {
            ServerInfoResponse response = new ServerInfoResponse();
            response.OnlinePlayerCount = SessionManager.Instance.OnlineUserCount;
            response.UserCount = (int)DbManager.fsql.Select<DbUser>().Count();
            var list = DbManager.fsql.Select<DbCharacter>().OrderByDescending(a => a.KillCount).Limit(8).ToList(a => new { a.Name,a.KillCount});
            foreach(var character in list)
            {
                var item = new KillRankingListItem();
                item.ChrName = character.Name;
                item.KillCount = character.KillCount;
                response.KillRankingList.Add(item);
            }
            sender.Send(response);

        }
    }
}
