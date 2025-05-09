using Common.Summer.Tools;
using GameServer.Core.Model;
using GameServer.Core.Task.Reward.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Reward
{
    public class TaskRewardParser : Singleton<TaskRewardParser>
    {
        private Dictionary<string, IRewardHandler> _handlers = new Dictionary<string, IRewardHandler>();

        public void Init()
        {
            _handlers["Item"]       = new ItemRewardHandler();
            _handlers["SkillChain"] = new SkillChainRewardHandler();
            _handlers["Skill"]      = new SkillRewardHandler();
        }

        public void GrantRewards(RewardData rewardData, GameCharacter chr)
        {
            if (_handlers.TryGetValue(rewardData.rewardType, out var handler))
            {
                handler.GrantReward(rewardData, chr);
            }
        }
    }
}
