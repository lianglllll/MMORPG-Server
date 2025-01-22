using HS.Protobuf.SceneEntity;
using SceneServer.Utils;
using System.Collections.Generic;

namespace GameServer.Buffs.BuffImplement
{
    public class DizzyBuff : BuffBase
    {
        public override BuffDefine GetBuffDefine()
        {
            return StaticDataManager.Instance.buffDefineDict.GetValueOrDefault(3);
        }

        public override void Update(float delta)
        {
            // Owner.State = ActorState.Dizzy;
        }

        public override void OnGet()
        {
            // Owner.SetActorState(ActorState.Dizzy);
        }

        public override void OnLost()
        {
            // Owner.SetActorState(ActorState.Idle);
        }

        protected override void OnLevelChange(int change)
        {

        }
    }
}
