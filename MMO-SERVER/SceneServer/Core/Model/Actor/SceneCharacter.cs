using Common.Summer.Core;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.SceneEntity;
using SceneServer.Net;

namespace SceneServer.Core.Model.Actor
{
    public class SceneCharacter : SceneActor
    {
        private string m_cId;
        private Session m_session;

        public void Init(string sessionId,Connection conn, DBCharacterNode dbChrNode)
        {
            m_cId = dbChrNode.CId;
            m_session = new Session(sessionId, conn);

            var netActorNode = new NetActorNode();
            var transform = new NetTransform();
            var pos = new NetVector3();
            var rotation = new NetVector3();
            var scale = new NetVector3();
            transform.Position = pos;
            transform.Rotation = rotation;
            transform.Scale = scale;
            netActorNode.Transform = transform;
            netActorNode.ProfessionId = dbChrNode.ProfessionId;
            netActorNode.ActorName = dbChrNode.ChrName;
            netActorNode.Level = dbChrNode.Level;
            netActorNode.Exp = dbChrNode.ChrStatus.Exp;
            netActorNode.SceneId = dbChrNode.ChrStatus.CurSceneId;
            netActorNode.NetActorType = NetActorType.Character;
            if(dbChrNode.ChrCombat != null)
            {
                netActorNode.EquippedSkills.AddRange(dbChrNode.ChrCombat.EquippedSkills);
            }
            Init(netActorNode);
        }
        public string SessionId => m_session.SesssionId;
        public void Send(IMessage message)
        {
            m_session.Send(message);
        }
    }
}
