using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;
using GameServer.Core;
using GameServer.Combat;
using GameServer.Manager;
using Serilog;
using GameServer.Buffs;
using GameServer.core;
using GameServer.Combat.Skill;
using Google.Protobuf.Collections;
using AOIMap;

namespace GameServer.Model
{

    public class Actor : Entity
    {
        public UnitDefine Define;                                                                   //actor的define数据(静态数据)
        public NetActor _info  = new NetActor();                                                     //actor的NetActor数据(动态数据)
        public EntityState State;                                                                   //actor动画状态：跑、走、跳、
        public UnitState unitState;                                                                 //actor活动状态:死亡、空闲、战斗
        public Attributes Attr = new Attributes();                                                  //actor属性
        public SkillManager skillManager;                                                           //actor技能管理器
        public Spell spell;                                                                         //actor技能释放器
        public BuffManager buffManager;                                                             //actor的buff管理器

        public Skill curentSkill;                                                                   //actor当前正在使用的技能

        public bool IsDeath => unitState == UnitState.Dead;            
        
        public NetActor Info
        {
            get
            {
                //更新actor中的buff网络信息
                _info.BuffsList.Clear();
                foreach (BuffBase item in buffManager.buffs)
                {
                    _info.BuffsList.Add(item.Info);
                }

                return _info;
            }
            set
            {
                _info = value;
            }
        }

        /// <summary>
        /// chrId
        /// </summary>
        public int Id { 
            get { return _info.Id; } 
            set { _info.Id = value; } 
        }                                                         
        
        public string Name { 
            get { return _info.Name; } 
            set { _info.Name = value; } 
        }

        /// <summary>
        /// actor的类型：角色、怪物、npc
        /// </summary>
        public EntityType Type { 
            get { return _info.EntityType; } 
            set { _info.EntityType = value; }
        }                                                 
        
        public int SpaceId
        {
            get
            {
                return _info.SpaceId;
            }
            set
            {
                _info.SpaceId = value;
            }
        }

        public float Hp { get { return _info.Hp; } set { _info.Hp = value; } }
        public float Mp { get { return _info.Mp; } set { _info.Mp = value; } }
        public int Level { get => _info.Level; set => _info.Level = value; }
        public long Exp { get => _info.Exp; set => _info.Exp = value; }
        public long Gold { get => _info.Gold; set => _info.Gold = value; }
        public int Speed { get => _info.Speed; set => _info.Speed = value; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <param name="TID"></param>
        /// <param name="level"></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        public Actor( EntityType type,int TID,int level,Vector3Int position, Vector3Int direction) : base(position, direction)
        {
            //加载define的默认数据
            this.Define = DataManager.Instance.unitDefineDict[TID];     //todo 可能会空，可能会有人篡改json配置文件
            
            //更新NetActor网络对象的信息
            this._info.Tid = TID;                                        //TID:区分相同实体类型，不同身份。可以通过tid去找define
            this._info.EntityType = type;                                //unit类型
            this._info.Entity = this.EntityData;                         //entity的基本数据：pos dir speed entityId
            this._info.Name = Define.Name;                               //defind中的角色默认名字，可以给子类进行覆盖
            this._info.Hp = (int)this.Define.HPMax;                      //hp
            this._info.Mp = (int)this.Define.MPMax;                      //mp
            this._info.Level = level;                                    //level

            //给当前actor添加相对应的组件
            this.skillManager = new SkillManager(this);                 //技能管理器
            this.spell = new Spell(this);                               //技能施法器
            this.buffManager = new BuffManager(this);                   //buff管理器
            this.Attr.Init(this);                                       //actor属性初始化

            //再初始化
            _info.Hp = Attr.final.HPMax;
            _info.Mp = Attr.final.MPMax;
            _info.HpMax = Attr.final.HPMax;
            _info.MpMax = Attr.final.MPMax;
            _info.Speed = Attr.final.Speed;

        }

        /// <summary>
        /// 推动actor再mmo世界的运行
        /// </summary>
        public override void Update()
        {
            this.skillManager.Update();//驱动技能系统
            this.buffManager.OnUpdate(Time.deltaTime);
        }

        /// <summary>
        /// actor进入某个场景，更新自己记录的space信息
        /// </summary>
        /// <param name="space"></param>
        public void OnEnterSpace(Space space)
        {
            if(currentSpace!= null && space != null)
            {
                EntityManager.Instance.ChangeSpace(this, currentSpace.SpaceId, space.SpaceId);
            }
            this.currentSpace = space;
            SpaceId = space.SpaceId;
        }

        /// <summary>
        /// 当前actor收到扣血通知
        /// </summary>
        /// <param name="damage"></param>
        public void RecvDamage(Damage damage)
        {
            //无敌
            if (buffManager.HasBuffByBid(5))
            {
                damage.IsImmune = true;
                damage.Amount = 0;
            }

            //添加广播，一个伤害发生了
            currentSpace.fightManager.damageQueue.Enqueue(damage);
            //扣血
            if(Hp > damage.Amount)
            {
                SetHp(Hp - damage.Amount);
            }
            else
            {
                Die(damage.AttackerId);
            }

            AfterRecvDamage(damage);
        }

        /// <summary>
        /// 受伤后处理
        /// </summary>
        /// <param name="damage"></param>
        protected virtual void AfterRecvDamage(Damage damage)
        {
        }

        /// <summary>
        /// 设置actor的hp，并广播
        /// </summary>
        /// <param name="hp"></param>
        public void SetHp(float hp)
        {

            if (Mathf.Approximately(Hp, hp)) return;
            if(hp <= 0)
            {
                hp = 0;
            }
            if (hp > Attr.final.HPMax)
            {
                hp = Attr.final.HPMax;
            }
            float oldValue = Hp;
            Hp = hp;

            //发包
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Hp,
                OldValue = new() { FloatValue = oldValue },
                NewValue = new() { FloatValue = Hp },
            };
            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 设置actor的mp，并广播
        /// </summary>
        /// <param name="mp"></param>
        public void SetMP(float mp)
        {
            if (Mathf.Approximately(Mp, mp)) return;
            if (mp <= 0)
            {
                mp = 0;
            }
            if (mp > Attr.final.MPMax)
            {
                mp = Attr.final.MPMax;
            }
            float oldValue = Mp;
            Mp = mp;

            //发包
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Mp,
                OldValue = new() { FloatValue = oldValue },
                NewValue = new() { FloatValue = Mp },
            };
            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 设置动画状态,并广播
        /// </summary>
        /// <param name="state"></param>
        public virtual void SetEntityState(EntityState state)
        {
            this.State = state;
            var resp = new SpaceEntitySyncResponse();
            resp.EntitySync = new NEntitySync();
            resp.EntitySync.Entity = EntityData;
            resp.EntitySync.Force = true;
            resp.EntitySync.State = state;
            currentSpace.AOIBroadcast(this,resp);
        }

        /// <summary>
        /// 设置actor的unitstate,并广播
        /// </summary>
        /// <param name="unitState"></param>
        protected void SetState(UnitState unitState)
        {
            if (this.unitState == unitState) return;
            UnitState oldValue = this.unitState;
            this.unitState = unitState;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.State,
                OldValue = new() { StateValue = oldValue },
                NewValue = new() { StateValue = unitState } 
            };
            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 设置actor的等级,并广播
        /// </summary>
        protected void SetLevel(int level)
        {
            if (Level == level) return;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Level,
                OldValue = new() { IntValue = Level },
                NewValue = new() { IntValue = level }
            };
            Level = level;
            //广播通知
            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
            //属性刷新
            Attr.Reload();
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="value"></param>
        protected void SetSpeed(int value)
        {
            if (this.Speed == value) return;
            int oldValue = this.Speed;
            this.Speed = value;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Speed,
                OldValue = new() { IntValue = oldValue },
                NewValue = new() { IntValue = value },
            };
            currentSpace?.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 设置生命上限
        /// </summary>
        /// <param name="value"></param>
        protected void SetHpMax(float value)
        {
            if (Mathf.Approximately(_info.HpMax, value)) return;

            float old = _info.HpMax;
            _info.HpMax = value;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Hpmax,
                OldValue = new() { FloatValue = old },
                NewValue = new() { FloatValue = value },
            };
            currentSpace?.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 设置法力上限
        /// </summary>
        /// <param name="value"></param>
        protected void SetMpMax(float value)
        {
            if (Mathf.Approximately(_info.MpMax, value)) return;
            float old = _info.MpMax;
            _info.MpMax = value;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Mpmax,
                OldValue = new() { FloatValue = old },
                NewValue = new() { FloatValue = value },
            };
            currentSpace?.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 当前actor死亡
        /// </summary>
        /// <param name="killerID"></param>
        public virtual void Die(int killerID)
        {
            if (IsDeath) return;

            SetHp(0);
            SetState(UnitState.Dead);
            SetEntityState(EntityState.Death);

            OnAfterDie(killerID);
        }


        /// <summary>
        /// 死亡后的处理
        /// </summary>
        /// <param name="killerID"></param>
        protected virtual void OnAfterDie(int killerID)
        {
            buffManager.RemoveAllBuff();
        }

        /// <summary>
        /// actor复活
        /// </summary>
        public virtual void Revive()
        {

        }


        /// <summary>
        /// 复活后处理
        /// </summary>
        /// <param name="killerID"></param>
        protected virtual void OnAfterRevive()
        {

        }

        /// <summary>
        /// 传送
        /// </summary>
        public virtual void TransmitSpace(Space targetSpace,Vector3Int pos,Vector3Int dir = new Vector3Int())
        {
            if (this is not Character chr) return;
            //传送的不是同一场景
            if(currentSpace != targetSpace)
            {
                //1.退出当前场景
                currentSpace.CharacterLeave(chr);
                //设置坐标
                var tempSpace = currentSpace;
                currentSpace = null;
                chr.Position = pos;
                chr.Direction = dir;
                currentSpace = tempSpace;
                //2.进入新场景
                targetSpace.CharaterJoin(chr);

            }
            //传送的是同一场景
            else
            {
                currentSpace.Transmit(this, pos, dir);
            }
        }

        //查看actor的健康状态：生命、蓝条、debuffer
        //不健康的时候返回false
        public virtual bool ActorHealth()
        {
            if (IsDeath) return true;

            if(Hp < _info.HpMax || Mp < _info.MpMax)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 休息时恢复状态
        /// </summary>
        public virtual void RestoreHealthState()
        {
            if (ActorHealth()) return;
            SetHp(Hp + _info.HpMax * 0.1f);
            SetMP(Mp + _info.MpMax * 0.1f);
        }

        /// <summary>
        /// 属性发生变化时同步
        /// </summary>
        public void SyncAttributes()
        {
            SetSpeed(Attr.final.Speed);
            SetHpMax(Attr.final.HPMax);
            SetMpMax(Attr.final.MPMax);

        }

    }
}
