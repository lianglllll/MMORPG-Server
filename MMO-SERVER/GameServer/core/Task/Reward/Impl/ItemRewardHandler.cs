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
        public void GrantReward(string rewardString, GameCharacter chr)
        {
            string[] parts = rewardString.Split(':'); // 格式可能是 "ItemID_123:2"
            int itemId = int.Parse(parts[0].Replace("ItemID_", ""));
            int count = int.Parse(parts[1]);
            // chr.Inventory.AddItem(itemId, count);
        }
    }
}
