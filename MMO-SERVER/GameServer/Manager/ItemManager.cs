using GameServer.Model;
using Proto;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
    public class ItemManager : Singleton<ItemManager>
    {
        public bool ItemUse(ItemUseRequest message)
        {
            //安全校验
            Entity entity = EntityManager.Instance.GetEntity(message.EntityId);
            if (entity == null) return false;
            if (!((EntityManager.Instance.GetEntity(message.EntityId)) is Character chr)) return false;

            //让chr使用
            return chr.UseItem(message.SlotIndex,message.Count);
        }

    }
}
