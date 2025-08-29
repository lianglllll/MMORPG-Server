using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Reward.Impl
{
    public class SkillRewardHandler : IRewardHandler
    {
        public void GrantReward(RewardData rewardData, GameCharacter chr)
        {
            int skillId = int.Parse(rewardData.Parameters[0]);
            // chr.
        }
    }
}
