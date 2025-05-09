using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Reward.Impl
{
    public class SkillChainRewardHandler : IRewardHandler
    {
        public void GrantReward(RewardData rewardData, GameCharacter chr)
        {
            int skillChainId = int.Parse(rewardData.Parameters[0]);
            // chr.
        }
    }
}
