using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Reward
{
    public class RewardParser
    {
        private Dictionary<string, IRewardHandler> _handlers = new Dictionary<string, IRewardHandler>();

        public void RegisterHandler(string prefix, IRewardHandler handler)
        {
            _handlers[prefix] = handler;
        }

        public void GrantRewards(string rewardString, GameCharacter chr)
        {
            if (string.IsNullOrEmpty(rewardString)) return;
            string[] rewards = rewardString.Split(';');
            foreach (var reward in rewards)
            {
                string[] parts = reward.Split(':');
                if (parts.Length < 2) continue;
                string prefix = parts[0];
                if (_handlers.TryGetValue(prefix, out var handler))
                {
                    handler.GrantReward(parts[1], chr);
                }
            }
        }
    }
}
