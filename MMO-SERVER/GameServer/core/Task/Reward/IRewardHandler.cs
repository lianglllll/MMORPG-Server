using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Reward
{
    public interface IRewardHandler
    {
        void GrantReward(RewardData rewardData, GameCharacter chr);
    }
}
