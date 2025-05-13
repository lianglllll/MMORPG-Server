using HS.Protobuf.SceneEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.WoodenDummy.Impl
{
    public class WoodenDummyMonsterAIState_Idle : WoodenDummyMonsterAIState
    {
        public override void Enter()
        {
            monsterAI.Monster.ChangeActorStateAndSend(NetActorState.Idle);
        }
    }
}
