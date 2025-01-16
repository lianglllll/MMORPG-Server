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
            MessageRouter.Instance.Subscribe<CreateCharacterRequest>(_CharacterCreateRequest);
            MessageRouter.Instance.Subscribe<DeleteCharacterRequest>(_CharacterDeleteRequest);
            MessageRouter.Instance.Subscribe<EnterGameRequest>(_GameEnterRequest);
            MessageRouter.Instance.Subscribe<ReconnectRequest>(_ReconnectRequest);
            MessageRouter.Instance.Subscribe<ServerInfoRequest>(_ServerInfoRequest);
        }


        /// <summary>
        /// 用户创建角色请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _CharacterCreateRequest(Connection conn, CreateCharacterRequest message)
        {
            CreateCharacterResponse resp = new ();
            
            //这里需要安全校验
            DbUser dbUser = conn.Get<Session>().dbUser;

            if (dbUser == null)
            {//未登录的用户，有人试图跳过创建
                Log.Information("未登录的用户，有人试图跳过创建");
                resp.ResultCode = 1;
                resp.ResultMsg = "未登录,不能创建角色";
                conn.Send(resp);
                return;
            }


            //查询该用户的角色数量是否达到上限
            long roleCount = DbManager.fsql.Select<DbCharacter>().Where(t => t.PlayerId.Equals(dbUser.Id)).Count();
            if (roleCount >= maxRoleCount)
            {
                //角色上限了
                Log.Information("角色上限了");
                resp.ResultCode = 2;
                resp.ResultMsg = "角色数量最多为4";
                conn.Send(resp);
                return;
            }

            //角色名是否为空
            //你非要问为什么要在这里弄而不是前端弄这个检验，前端的校验是可以被越过的。
            if (string.IsNullOrWhiteSpace(message.Name))
            {
                Log.Information("角色名为空");
                resp.ResultCode = 3;
                resp.ResultMsg = "角色名不能为空";
                conn.Send(resp);
                return;
            }

            string name = message.Name.Trim();
            //角色名长度限制
            if (name.Length > 7)
            {
                Log.Information("角色名最大长度为7");
                resp.ResultCode = 4;
                resp.ResultMsg = "角色名最大长度为7";
                conn.Send(resp);
                return;
            }

            //角色名重名
            if (DbManager.fsql.Select<DbCharacter>().Where(t => t.Name.Equals(name)).Count() > 0)
            {
                Log.Information("角色名已存在");
                resp.ResultCode = 5;
                resp.ResultMsg = "角色名已存在";
                conn.Send(resp);
                return;
            }

            //角色类型有误
            if(message.VocationId >= 5 || message.VocationId < 0)
            {
                Log.Information("角色类型有误："+message.VocationId);
                resp.ResultCode = 6;
                resp.ResultMsg = "请选择角色";
                conn.Send(resp);
                return;
            }

            //存放入数据库中
            var pointDef = DataManager.Instance.revivalPointDefindeDict[0];
            var unitDef = DataManager.Instance.unitDefineDict[message.VocationId];
            DbCharacter dbCharacter = new DbCharacter()
            {
                Name = message.Name,
                JobId = message.VocationId,
                Hp = (int)unitDef.HPMax,
                Mp = (int)unitDef.MPMax,
                Level = 1,
                Exp = 0,
                SpaceId = 0,//新手村
                Gold = 0,
                PlayerId = dbUser.Id,
                //出生坐标 
                X = pointDef.X,
                Y = pointDef.Y,
                Z = pointDef.Z,

            };
            int aff = DbManager.fsql.Insert(dbCharacter).ExecuteAffrows();
            
            if (aff > 0)
            {
                //创建成功向客户端返回响应
                resp.ResultCode = 0;
                resp.ResultMsg = "创建成功";
                conn.Send(resp);
            }
            else
            {
                resp.ResultCode = 7;
                resp.ResultMsg = "创建失败";
                conn.Send(resp);
            }

        }

        /// <summary>
        /// 删除角色请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _CharacterDeleteRequest(Connection conn, DeleteCharacterRequest message)
        {
            DeleteCharacterResponse resp = new ();

            //获取当前用户id
            DbUser dbUser = conn.Get<Session>().dbUser;
            if (null == dbUser)
            {
                Log.Information("未登录，删除角色");

                return;
            }

            int affCount = DbManager.fsql.Delete<DbCharacter>().Where(t => t.Id == message.CharacterId).Where(t => t.PlayerId == dbUser.Id).ExecuteAffrows();
            if (affCount > 0)
            {//返回
                resp.ResultCode = 0;
                resp.ResultMsg = "删除成功";
                conn.Send(resp);
            }
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
