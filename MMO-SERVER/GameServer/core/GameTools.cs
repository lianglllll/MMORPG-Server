using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core
{
    public class GameTools
    {


        public static Actor GetActorByEntityId(int entityId)
        {
            return EntityManager.Instance.GetEntityById(entityId) as Actor;
        }


        public static Entity GetEntityByEntityId(int entityId)
        {
            return EntityManager.Instance.GetEntityById(entityId);
        }

    }
}
