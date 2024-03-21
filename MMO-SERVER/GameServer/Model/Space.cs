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
        public AOIManager<Entity> AOIManager { get;}

        /// <summary>
        /// 构造函数
        /// </summary>
        public Space(){}
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Space(SpaceDefine spaceDefine)
        {
            def = spaceDefine;
            SpaceId = spaceDefine.SID;
            Name = spaceDefine.Name;
            AOIManager = spaceDefine.SID switch
            {
                0 => new(-99, -73, 200, 226),
                1 => new(0, 0, 1000, 1000),
                _ => throw new NotImplementedException(),
            };
            monsterManager.Init(this);
            spawnManager.Init(this);
            fightManager.Init(this);
            itemManager.Init(this);
        }

        /// <summary>
        /// 推动场景下的各个管理器运行
        /// </summary>
        public void Update()
        {
            spawnManager.Update();
            fightManager.OnUpdate(Time.deltaTime);
        }



        /// <summary>
        /// 新角色进入地图
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="character"></param>
        public void CharaterJoin(Character character)
        {

            //1.将新加入的character交给当前场景来管理
            character.OnEnterSpace(this);
            characterDict[character.EntityId] = character;

            //2.广播给场景中的其他玩家,有新玩家进入
            AOIManager.Enter(character);
            /*
            var resp = new SpaceCharactersEnterResponse();
            resp.SpaceId = this.SpaceId;
            resp.CharacterList.Add(character.Info);
            foreach (var kv in characterDict)
            {
                if (kv.Value.EntityId != character.EntityId)
                {
                    kv.Value.session.Send(resp);
                }
            }
            */

            //3.新上线的玩家需要获取场景中:全部的角色/怪物/物品的信息
            SpaceEnterResponse resp = new SpaceEnterResponse();
            resp.Character = character.Info;
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
            character.session.Send(resp);

            /*
            SpaceEnterResponse spaceEnterResponse = new SpaceEnterResponse();
            spaceEnterResponse.Character = character.Info;
            foreach (var kv in characterDict)
            {
                if (kv.Value.EntityId == character.EntityId) continue;
                spaceEnterResponse.CharacterList.Add(kv.Value.Info);
            }
            foreach (var kv in monsterManager.monsterDict)
            {
                spaceEnterResponse.CharacterList.Add(kv.Value.Info);
            }
            foreach(var kv in itemManager.itemEntityDict)
            {
                spaceEnterResponse.ItemEntityList.Add(kv.Value.NetItemEntity);
            }
            character.session.Send(spaceEnterResponse); 
            */
        }

        /// <summary>
        /// 角色离开地图(客户端离线、切换地图)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="character"></param>
        public void CharacterLeave(Character character)
        {
            characterDict.Remove(character.EntityId);

            AOIManager.Leave(character);

            /* 我们不需要再向整个场景进行广播了
            //广播
            SpaceEntityLeaveResponse resp = new SpaceEntityLeaveResponse();
            resp.EntityId = character.EntityId;
            foreach (var kv in characterDict)
            {
                kv.Value.session.Send(resp);
            }
            */
        }

        /// <summary>
        /// 更新服务器目标entity的信息，并且给其他玩家进行转发
        /// </summary>
        /// <param name="entitySync">位置+状态信息</param>
        public void SyncActor(NEntitySync entitySync,Actor actor)
        {
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


            /*
            foreach (var kv in characterDict)
            {
                if (kv.Value.EntityId == entitySync.Entity.Id)
                {
                    kv.Value.EntityData = entitySync.Entity;
                    kv.Value.State = entitySync.State;
                }
                else
                {
                    SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
                    resp.EntitySync = entitySync;
                    kv.Value.session.Send(resp);
                }
            }
            */
        }

        /// <summary>
        /// 怪物进入地图,广播给场景内的client
        /// </summary>
        /// <param name="monster"></param>
        public void MonsterJoin(Monster monster)
        {
            monster.OnEnterSpace(this);
            AOIManager.Enter(monster);
            /*
            var resp = new SpaceCharactersEnterResponse();
            resp.SpaceId = this.SpaceId;
            resp.CharacterList.Add(monster.Info);

            //广播地图内所有玩家
            Broadcast(resp);
            */
        }

        /// <summary>
        /// 物品进入场景，广播给场景内的client
        /// </summary>
        /// <param name="ie"></param>
        public void ItemJoin(ItemEntity ie)
        {
            AOIManager.Enter(ie);
            /*
            var resp = new SpaceItemEnterResponse();
            resp.NetItemEntity = ie.NetItemEntity;
            Broadcast(resp);
            */
        }

        /// <summary>
        /// 物品离开场景，广播给场景内的client
        /// </summary>
        /// <param name="ie"></param>
        public void ItemLeave(ItemEntity ie)
        {
            if (!itemManager.RemoveItem(ie.EntityId))
            {
                return;
            }
            AOIManager.Leave(ie);
            /*
            //广播
            SpaceEntityLeaveResponse resp = new SpaceEntityLeaveResponse();
            resp.EntityId = ie.EntityId;
            Broadcast(resp);
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
            AOIBroadcast(itemEntity, resp);
            /*
            NetItemEntitySync resp = new NetItemEntitySync();
            resp.NetItemEntity = itemEntity.NetItemEntity;
            Broadcast(resp);
            */
        }


        /// <summary>
        /// 往aoi区域内进行广播(all Character)
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="msg"></param>
        public void AOIBroadcast(Entity entity,IMessage msg)
        {
            var loc = entity.AoiPos;//获取aoi坐标
            var all = AOIManager.GetEntities(loc.x, loc.y);
            foreach (var chr in all.OfType<Character>())
            {
                chr.session.Send(msg);
            }
        }

        /// <summary>
        /// 工具方法广播一个proto消息，给场景的全体玩家
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

            SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
            resp.EntitySync = new NEntitySync();
            resp.EntitySync.Entity = actor.EntityData;
            resp.EntitySync.State = EntityState.Idle;
            resp.EntitySync.Force = true;

            AOIBroadcast(actor,resp);
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
