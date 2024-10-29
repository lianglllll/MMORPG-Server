using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Buffs.BuffImplement
{
    public class DizzyBuff : BuffBase
    {
        public override BuffDefine GetBuffDefine()
        {
            return DataManager.Instance.buffDefindeDict.GetValueOrDefault(3);
        }

        public override void OnUpdate(float delta)
        {
            Owner.State = ActorState.Dizzy;
        }

        public override void OnGet()
        {
            Owner.SetActorState(ActorState.Dizzy);
        }

        public override void OnLost()
        {
            Owner.SetActorState(ActorState.Idle);
        }

        protected override void OnLevelChange(int change)
        {

        }
    }
}
