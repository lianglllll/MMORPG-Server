using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Network;
using Proto;
using Serilog;
using GameServer.Model;
using GameServer.Manager;
using GameServer;
using GameServer.Database;
using GameServer.Net;

namespace GameServer.Service
{


    /// <summary>
    /// 玩家服务
    /// 注册，登录，创建角色，进入游戏
    /// 应该还需要一个userManager，service只负责接收来到的请求并且向mananger传递，根据manager传回来的信息进行响应结果
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
            MessageRouter.Instance.Subscribe<Proto.GameEnterRequest>(_GameEnterRequest);
            MessageRouter.Instance.Subscribe<Proto.UserLoginRequest>(_UserLoginRequest);
            MessageRouter.Instance.Subscribe<Proto.CharacterCreateRequest>(_CharacterCreateRequest);
            MessageRouter.Instance.Subscribe<Proto.CharacterListRequest>(_CharacterListRequest);
            MessageRouter.Instance.Subscribe<Proto.CharacterDeleteRequest>(_CharacterDeleteRequest);
            MessageRouter.Instance.Subscribe<Proto.UserRegisterRequest>(_UserRegisterRequest);
            MessageRouter.Instance.Subscribe<Proto.ReconnectRequest>(_ReconnectRequest);

            MessageRouter.Instance.Subscribe<Proto.ServerInfoRequest>(_ServerInfoRequest);
        }

        /// <summary>
        /// 用户创建请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _UserRegisterRequest(Connection conn, UserRegisterRequest message)
        {

            //查询用户是否存在
            long count = DbManager.fsql.Select<DbUser>().Where(p => p.Username == message.Username).Count();

            UserRegisterResponse resp = new UserRegisterResponse();

            if(count > 0)
            {
                resp.Code = 1;
                resp.Message = "用户名已存在";
            }
            else
            {
                DbUser dbUser = new DbUser()
                {
                    Username = message.Username,
                    Password = message.Password
                };
                int affRows = DbManager.fsql.Insert(dbUser).ExecuteAffrows();
                resp.Code = 0;
                resp.Message = "注册成功";
            }

            conn.Send(resp);
        }

        /// <summary>
        /// 用户登录请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _UserLoginRequest(Connection conn, UserLoginRequest message)
        {
            //获取username password
            string username = message.Username;
            string password = message.Password;

            //查询数据库
            DbUser dbUser =  DbManager.fsql.Select<DbUser>()
                .Where(p => p.Username == username)
                .Where(p => p.Password == password)
                .First();

            //校验
            if (dbUser == null)
            {
                //1.要考虑到今天限制登录，2.这个号被封了  3.被关小黑屋了  4.没实名认证
                UserLoginResponse resp1 = new UserLoginResponse();
                resp1.Success = false;
                resp1.Message = "用户名或密码不正确";
                conn.Send(resp1);
                return;
            }

            //如果当前用户在线，将其踢出游戏，防止同一账号多次登录
            var oldsession = SessionManager.Instance.GetSessionByUserId(dbUser.Id);
            if (oldsession != null)
            {
                oldsession.Leave();
            }

            //==正常登录==

            //登录成功的客户端分配session对象，并且关联conn和session
            var session = SessionManager.Instance.NewSession(dbUser);
            conn.Set<Session>(session);
            session.Conn = conn;

            //发送给客户端的消息
            UserLoginResponse resp = new UserLoginResponse();
            resp.Success = true;
            resp.Message = "登录成功";
            resp.SessionId = session.Id;

            //响应客户端
            conn.Send(resp);
        }

        /// <summary>
        /// 用户创建角色请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _CharacterCreateRequest(Connection conn, CharacterCreateRequest message)
        {
            CharacterCreateResponse resp = new CharacterCreateResponse();
            
            //这里需要安全校验
            DbUser dbUser = conn.Get<Session>().dbUser;

            if (dbUser == null)
            {//未登录的用户，有人试图跳过创建
                Log.Information("未登录的用户，有人试图跳过创建");
                resp.Success = false;
                resp.Message = "未登录,不能创建角色";
                conn.Send(resp);
                return;
            }


            //查询该用户的角色数量是否达到上限
            long roleCount = DbManager.fsql.Select<DbCharacter>().Where(t => t.PlayerId.Equals(dbUser.Id)).Count();
            if (roleCount >= maxRoleCount)
            {
                //角色上限了
                Log.Information("角色上限了");
                resp.Success = false;
                resp.Message = "角色数量最多为4";
                conn.Send(resp);
                return;
            }

            //角色名是否为空
            //你非要问为什么要在这里弄而不是前端弄这个检验，前端的校验是可以被越过的。
            if (string.IsNullOrWhiteSpace(message.Name))
            {
                Log.Information("角色名为空");
                resp.Success = false;
                resp.Message = "角色名不能为空";
                conn.Send(resp);
                return;
            }

            string name = message.Name.Trim();
            //角色名长度限制
            if (name.Length > 7)
            {
                Log.Information("角色名最大长度为7");
                resp.Success = false;
                resp.Message = "角色名最大长度为7";
                conn.Send(resp);
                return;
            }

            //角色名重名
            if (DbManager.fsql.Select<DbCharacter>().Where(t => t.Name.Equals(name)).Count() > 0)
            {
                Log.Information("角色名已存在");
                resp.Success = false;
                resp.Message = "角色名已存在";
                conn.Send(resp);
                return;
            }

            //角色类型有误
            if(message.JobType>=5 || message.JobType < 0)
            {
                Log.Information("角色类型有误："+message.JobType);
                resp.Success = false;
                resp.Message = "请选择角色";
                conn.Send(resp);
                return;
            }

            //存放入数据库中
            var pointDef = DataManager.Instance.revivalPointDefindeDict[0];
            var unitDef = DataManager.Instance.unitDefineDict[message.JobType];
            DbCharacter dbCharacter = new DbCharacter()
            {
                Name = message.Name,
                JobId = message.JobType,
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
                resp.Success = true;
                resp.Message = "创建成功";
                conn.Send(resp);
            }
            else
            {
                resp.Success = false;
                resp.Message = "创建失败";
                conn.Send(resp);
            }


        }

        /// <summary>
        /// 删除角色请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _CharacterDeleteRequest(Connection conn, CharacterDeleteRequest message)
        {
            CharacterDeleteResponse resp = new CharacterDeleteResponse();

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
                resp.Success = true;
                resp.Message = "删除成功";
                conn.Send(resp);
            }
        }

        /// <summary>
        /// 角色列表请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _CharacterListRequest(Connection conn, CharacterListRequest message)
        {

            //获取当前登录的用户id
            DbUser dbUser = conn.Get<Session>().dbUser;

            //防止没登陆
            if (dbUser == null)
            {
                Log.Information("有人尝试未登录访问角色列表");
                return;
            }

            //通过用户id查询角色
            List<DbCharacter> dbCharacterlist =  DbManager.fsql.Select<DbCharacter>().Where(t => t.PlayerId == dbUser.Id).ToList();

            //返回
            CharacterListResponse resp = new CharacterListResponse();
            foreach(var item in dbCharacterlist)
            {
                resp.CharacterList.Add(new NetActor()
                {
                    Id = item.Id,
                    Name = item.Name,
                    Tid = item.JobId,
                    Level = item.Level,
                    Exp = item.Exp,
                    SpaceId = item.SpaceId,
                    Gold = item.Gold,
                    // NetEntity Entity
                });
            }
            conn.Send(resp);
        }

        /// <summary>
        /// 进入游戏请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void _GameEnterRequest(Connection conn, GameEnterRequest msg)
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
