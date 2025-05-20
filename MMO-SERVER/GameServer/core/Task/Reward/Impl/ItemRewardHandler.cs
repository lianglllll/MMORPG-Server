using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Reward.Impl
{
    public class ItemRewardHandler : IRewardHandler
    {
        public void GrantReward(RewardData rewardData, GameCharacter chr)
        {
            int itemId  = int.Parse(rewardData.Parameters[0]);
            int count   = int.Parse(rewardData.Parameters[1]);
            chr.BackPackManager.AddGameItem(itemId, count);
        }
    }
}
