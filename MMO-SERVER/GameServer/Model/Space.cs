using Serilog;
using Summer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;
using GameServer.Manager;
using GameServer.core;
using GameServer.Combat;
using Summer;
using Google.Protobuf;
using GameServer.Core;
using AOIMap;
using GameServer.Database;
using AOI;
using System.Security.Policy;
using GameServer.Utils;
using MySqlX.XDevAPI;
using System.Runtime.ConstrainedExecution;

namespace GameServer.Model
{
    /// <summary>
    /// 空间、地图、场景
    /// </summary>
    public class Space
    { 
        public int SpaceId { get; set; }
        public string Name { get; set; }
        public SpaceDefine def { get; set; }

        private Dictionary<int, Character> characterDict = new Dictionary<int, Character>();        //当前地图中所有的Character<角色id，角色引用>
        public MonsterManager monsterManager = new MonsterManager();                                //怪物管理器，负责当前场景的怪物创建和销毁
        public SpawnManager spawnManager = new SpawnManager();                                      //怪物孵化器，负责怪物的孵化
        public FightManager fightManager = new FightManager();                                      //战斗管理器，负责技能、投射物、伤害、actor信息的更新
        public ItemEntityManager itemManager = new ItemEntityManager();                             //物品管理器，管理场景中出现的物品

        public AoiZone aoiZone = new AoiZone(0.001f,0.001f);                    //十字链表空间(unity坐标系)
        private System.Numerics.Vector2 viewArea = new(Config.Server.AoiViewArea, Config.Server.AoiViewArea);

        /// <summary>
        /// 构造函数
        /// </summary>
        public Space(){}
        public Space(SpaceDefine spaceDefine)
        {
            def = spaceDefine;
            SpaceId = spaceDefine.SID;
            Name = spaceDefine.Name;
            monsterManager.Init(this);
            spawnManager.Init(this);
            fightManager.Init(this);
            itemManager.Init(this);
            //AOIManager = new AOIManager<Entity>(spaceDefine.Area[0], spaceDefine.Area[1], spaceDefine.Area[2], spaceDefine.Area[3]);

        }

        public void Update()
        {
            spawnManager.Update();
            fightManager.OnUpdate(Time.deltaTime);
        }

        /// <summary>
        ///  entity加入space
        /// </summary>
        public void CharaterJoin(Character character)
        {
            //1.将新加入的character交给当前场景来管理
            character.OnEnterSpace(this);
            characterDict[character.EntityId] = character;

            //2.加入aoi空间
            aoiZone.Enter(character);
            
            //3.新上线的玩家需要获取场景中:全部的角色/怪物/物品的信息
            SpaceEnterResponse resp = new SpaceEnterResponse();
            resp.Character = character.Info;
            var views = aoiZone.FindViewEntity(character.EntityId);
            foreach (var ent in views)
            {
                if (ent is Actor acotr)
                {
                    resp.CharacterList.Add(acotr.Info);
                }
                else if (ent is ItemEntity item)
                {
                    resp.ItemEntityList.Add(item.NetItemEntity);
                }
            }
            character.session.Send(resp);

            //4.通知附近玩家
            var resp2 = new SpaceCharactersEnterResponse();
            resp2.SpaceId = this.SpaceId;
            resp2.CharacterList.Add(character.Info);
            foreach (var cc in views.OfType<Character>())
            {
                cc.session.Send(resp2);
            }



            /*
            var loc = character.AoiPos;//获取aoi坐标
            var all = AOIManager.GetEntities(loc.x, loc.y);
            foreach ( var ent in all)
            {
                if(ent is Actor acotr)
                {
                    resp.CharacterList.Add(acotr.Info);
                }
                else if(ent is ItemEntity item)
                {
                    resp.ItemEntityList.Add(item.NetItemEntity);
                }
            }
            //3.广播给场景中的其他玩家,有新玩家进入
            //AOIManager.Enter(character);
            */
        }
        public void MonsterJoin(Monster monster)
        {
            monster.OnEnterSpace(this);
            aoiZone.Enter(monster);
            //通知附近的玩家
            var resp = new SpaceCharactersEnterResponse();
            resp.SpaceId = SpaceId; //场景ID
            resp.CharacterList.Add(monster.Info);
            var views = aoiZone.FindViewEntity(monster.EntityId);
            foreach (var cc in views.OfType<Character>())
            {
                cc.session.Send(resp);
            }

        }
        public void ItemJoin(ItemEntity ie)
        {
            aoiZone.Enter(ie);
            //通知附近的玩家
            var resp = new SpaceItemEnterResponse();
            resp.NetItemEntity = ie.NetItemEntity;
            var views = aoiZone.FindViewEntity(ie.EntityId);
            foreach (var cc in views.OfType<Character>())
            {
                cc.session.Send(resp);
            }
        }

        /// <summary>
        ///  entity离开space
        /// </summary>
        public void CharacterLeave(Character character)
        {
            //space清理
            characterDict.Remove(character.EntityId);

            //告诉其他人
            var views = aoiZone.FindViewEntity(character.EntityId);
            SpaceEntityLeaveResponse resp = new SpaceEntityLeaveResponse();
            resp.EntityId = character.EntityId;
            foreach (var cc in views.OfType<Character>())
            {
                cc.session.Send(resp);
            }

            //退出aoi空间
            aoiZone.Exit(character.EntityId);

        }
        public void ItemLeave(ItemEntity ie)
        {
            //space清理
            if (!itemManager.RemoveItem(ie.EntityId))
            {
                return;
            }

            //告诉其他人
            var views = aoiZone.FindViewEntity(ie.EntityId);
            SpaceEntityLeaveResponse resp = new SpaceEntityLeaveResponse();
            resp.EntityId = ie.EntityId;
            foreach (var cc in views.OfType<Character>())
            {
                cc.session.Send(resp);
            }

            //退出aoi空间
            aoiZone.Exit(ie.EntityId);
        }

        /// <summary>
        /// 更新的信息给其他玩家进行转发
        /// </summary>
        /// <param name="entitySync">位置+状态信息</param>
        public void SyncActor(NEntitySync entitySync,Actor actor)
        {
            var loc = actor.AoiPos;
            var handle = aoiZone.Refresh(actor.EntityId, loc.x, loc.y, viewArea);   //更新aoi空间里面我们的坐标

            //广播给附近的玩家
            var units = EntityManager.Instance.GetEntitiesByIds(handle.All); 
            SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
            resp.EntitySync = entitySync;
            foreach (var chr in units.OfType<Character>())
            {
                chr.session.Send(resp);
            }
            
            //新进入视野的单位，双向通知
            var resp2 = new SpaceCharactersEnterResponse();
            resp2.SpaceId = this.SpaceId; 
            foreach (var key in handle.Newly)
            {
                Actor target = (Actor)EntityManager.Instance.GetEntityById((int)key);

                //如果对方是玩家，自己进入对方视野
                if (target is Character chr2)
                {   
                    resp2.CharacterList.Clear();
                    resp2.CharacterList.Add(target.Info);
                    chr2.session.Send(resp2);
                }

                //告诉自己
                if(actor is Character chr3)
                {
                    resp2.CharacterList.Clear();
                    resp2.CharacterList.Add(target.Info);
                    chr3.session.Send(resp2);
                }

            }

            //远离的角色
            var resp3 = new SpaceEntityLeaveResponse();
            foreach (var key in handle.Leave)
            {
                Actor target = (Actor)EntityManager.Instance.GetEntityById((int)key);

                //如果对方是玩家，自己离开对方视野
                if (actor != null && actor is Character chr2)
                {
                    resp3.EntityId = actor.EntityId;
                    chr2.session.Send(resp3);
                }
                //如果自己是玩家，对方离开自己视野
                if (actor is Character chr3)
                {
                    resp3.EntityId = (int)key;
                    chr3.session.Send(resp3);
                }
            }


            /*
            //aoi区域广播
            var loc = actor.AoiPos;//获取aoi坐标
            var all = AOIManager.GetEntities(loc.x, loc.y);

            SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
            resp.EntitySync = entitySync;

            foreach (var chr in all.OfType<Character>())
            {
                if(chr.EntityId != actor.EntityId)
                {
                    chr.session.Send(resp);
                }
            }
            */
        }

        /// <summary>
        /// 更新itementity的信息，向其他玩家进行转发
        /// </summary>
        /// <param name="sycn"></param>
        public void SyncItemEntity(ItemEntity itemEntity)
        {
            NetItemEntitySync resp = new NetItemEntitySync();
            resp.NetItemEntity = itemEntity.NetItemEntity;
            AOIBroadcast(itemEntity, resp,true);
        }

        /// <summary>
        /// 往aoi九宫格内进行广播(all Character)一个proto消息
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="msg"></param>
        public void AOIBroadcast(Entity entity,IMessage msg, bool includeSelf = false)
        {
            var all = aoiZone.FindViewEntity(entity.EntityId, includeSelf);
            foreach (var chr in all.OfType<Character>())
            {
                chr.session.Send(msg);
            }
        }

        /// <summary>
        /// 广播一个proto消息给场景的全体玩家
        /// </summary>
        /// <param name="msg"></param>
        public void Broadcast(IMessage msg)
        {
            foreach(var kv in characterDict)
            {
                kv.Value.session.Send(msg);
            }
        }


        /// <summary>
        /// 场景内传送
        /// </summary>
        public void Transmit(Actor actor,Vector3Int pos, Vector3Int dir = new Vector3Int())
        {
            actor.Position = pos;
            actor.Direction = dir;

            //通知客户端，需要强制到达这个地点
            //否则，客户端会用原来的pos来覆盖新的位置
            SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
            resp.EntitySync = new NEntitySync();
            resp.EntitySync.Entity = actor.EntityData;
            resp.EntitySync.State = EntityState.Idle;
            resp.EntitySync.Force = true;
            AOIBroadcast(actor,resp,true);

        }

        /// <summary>
        /// 寻找场景中最近的复活点
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Vector3Int SearchNearestRevivalPoint(Character chr)
        {
            float comparativetGap = float.MaxValue;
            Vector3Int pos = chr.Position;
            foreach(var pointId in def.RevivalPointS)
            {
                var pointDef = DataManager.Instance.revivalPointDefindeDict[pointId];
                if (pointDef == null) continue;
                var tempPos = new Vector3Int(pointDef.X, pointDef.Y, pointDef.Z);
                var gap = Vector3Int.Distance(chr.Position, tempPos);   
                if(gap < comparativetGap)
                {
                    comparativetGap = gap;
                    pos = tempPos;
                }
            }
            return pos;
        }
    }
}
