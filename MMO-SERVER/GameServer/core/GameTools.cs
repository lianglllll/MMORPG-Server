using GameServer.Manager;
using GameServer.Model;

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
