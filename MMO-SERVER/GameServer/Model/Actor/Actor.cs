using HS.Protobuf.Game.Backpack;
using GameServer.Combat;
using GameServer.Manager;
using GameServer.Buffs;
using GameServer.core;
using Common.Summer.Core;
using HS.Protobuf.SceneEntity;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;

namespace GameServer.Model
{

    public class Actor : Entity
    {
        public UnitDefine Define;                                                                   //actor的define数据    (静态数据)
        private NetActor _info  = new NetActor();                                                   //actor的NetActor数据  (动态数据)
        public Attributes Attr = new Attributes();                                                  //actor属性
        public ActorMode actorMode;                                                                 //actor模式：用于描述角色在某一段时间内的大范围，通常包括：空闲、战斗
        public ActorCombatMode actorCombatMode;                                                     //actor战斗模式:空手、武器、御剑飞行
        public ActorState State;                                                                    //actor动作状态：用于描述角色在特定时刻所执行的具体动作或行为。通常包括：Idle、Run、Walk、Jump、Attack 等。
        public SkillManager skillManager;                                                           //actor技能管理器
        public Spell spell;                                                                         //actor技能释放器
        public Skill curentSkill;                                                                   //actor当前正在使用的技能
        public BuffManager buffManager;                                                             //actor的buff管理器

        public bool IsDeath => actorMode == ActorMode.Dead;            
        
        public NetActor Info
        {
            get
            {
                return _info;
            }
            set
            {
                _info = value;
            }
        }

        public int AcotrId { 
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
        public ActorType Type { 
            get { return _info.ActorType; } 
            set { _info.ActorType = value; }
        }                                                 
        
        public int CurSpaceId
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
        public Actor(ActorType type,int TID,int level,Vector3Int position, Vector3Int direction) : base(position, direction)
        {
            //加载define的默认数据
            this.Define = DataManager.Instance.unitDefineDict[TID];     //todo 可能会空，可能会有人篡改json配置文件
            
            //更新NetActor网络对象的信息
            this._info.Tid = TID;                                        //TID:区分相同实体类型，不同身份。可以通过tid去找define
            this._info.ActorType = type;                                //unit类型
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

        public override void Update()
        {
            this.skillManager.Update();
            this.buffManager.OnUpdate(MyTime.deltaTime);
        }


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
        protected void SetActorMode(ActorMode actorMode)
        {
            if (this.actorMode == actorMode) return;
            ActorMode oldValue = this.actorMode;
            this.actorMode = actorMode;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Mode,
                OldValue = new() { ModeValue = oldValue },
                NewValue = new() { ModeValue = actorMode } 
            };
            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }
        protected void SetActorCombatMode(ActorCombatMode actorCombatMode)
        {
            if (this.actorCombatMode == actorCombatMode) return;
            ActorCombatMode oldValue = this.actorCombatMode;
            this.actorCombatMode = actorCombatMode;
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.CombatMode,
                OldValue = new() { CombatModeValue = oldValue },
                NewValue = new() { CombatModeValue = actorCombatMode }
            };
            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }
        public virtual void SetActorState(ActorState state)
        {
            this.State = state;
            var resp = new SpaceEntitySyncResponse();
            resp.EntitySync = new NEntitySync();
            resp.EntitySync.Entity = EntityData;
            resp.EntitySync.Force = true;
            resp.EntitySync.State = state;
            currentSpace.AOIBroadcast(this,resp,true);
        }

        public void RecvDamage(Damage damage)
        {
            //由技能和buff发出，当前actor收到扣血通知，本过程由Scheduler单线程调用，没有并发问题。

            //无敌
            if (buffManager.HasBuffByBid(5))
            {
                damage.IsImmune = true;
                damage.Amount = 0;
            }

            //扣血，属性更新
            if(Hp > damage.Amount)
            {
                SetHp(Hp - damage.Amount);
            }
            else
            {
                OnDeath(damage.AttackerId);
            }

            //添加广播，一个伤害发生了。
            currentSpace.fightManager.damageQueue.Enqueue(damage);

            AfterRecvDamage(damage);
        }
        protected virtual void AfterRecvDamage(Damage damage)
        {

        }
        public virtual void OnDeath(int killerID)
        {
            if (IsDeath) return;
            SetHp(0);
            SetActorMode(ActorMode.Dead);
            SetActorState(ActorState.Death);

            OnAfterDeath(killerID);
        }
        protected virtual void OnAfterDeath(int killerID)
        {
            buffManager.RemoveAllBuff();
            var act = GameTools.GetActorByEntityId(killerID);
            if(act is Character chr)
            {
                chr.AddKillCount();
            }
        }
        /// <summary>
        /// actor复活
        /// </summary>
        public virtual void Revive()
        {

        }
        protected virtual void OnAfterRevive()
        {

        }

        /// <summary>
        /// 属性更新回调
        /// </summary>
        public void UpdateAttributes()
        {
            //问就是netinfo中暂时只有这几个
            SetSpeed(Attr.final.Speed);
            SetHpMax(Attr.final.HPMax);
            SetMpMax(Attr.final.MPMax);
        }
        /// <summary>
        /// actor进入某个场景，更新自己记录的space信息
        /// </summary>
        /// <param name="space"></param>
        public void OnEnterSpace(Space space)
        {
            if(currentSpace!= null && space != null)
            {
                EntityManager.Instance.EntityChangeSpace(this, currentSpace.SpaceId, space.SpaceId);
            }
            this.currentSpace = space;
            CurSpaceId = space.SpaceId;
        }

        // 自动回血调用，这里暂时只有monster调用，本过程由Scheduler单线程调用，没有并发问题。
        public virtual bool Check_HpAndMp_Needs()
        {
            if(Hp < _info.HpMax || Mp < _info.MpMax)
            {
                return true;
            }
            return false;
        }
        public virtual void Restore_HpAndMp()
        {
            SetHp(Hp + _info.HpMax * 0.1f);
            SetMP(Mp + _info.MpMax * 0.1f);
        }

        /// <summary>
        /// 传送
        /// </summary>
        public  void TransmitTo(Space targetSpace,Vector3Int pos,Vector3Int dir = new Vector3Int())
        {
            if (this is not Character chr) return;
            currentSpace.TransmitTo(targetSpace,this,pos,dir);
        }

    }
}
