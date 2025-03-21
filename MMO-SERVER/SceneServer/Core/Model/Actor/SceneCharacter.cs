using Common.Summer.Core;
using Google.Protobuf;
using HS.Protobuf.Common;
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
            m_session.Chr = this;

            var initPos = new NetVector3 { X = dbChrNode.ChrStatus.X, Y = dbChrNode.ChrStatus.Y, Z = dbChrNode.ChrStatus.Z };
            base.Init(initPos, dbChrNode.ProfessionId, dbChrNode.Level);

            // 补充
            m_netActorNode.ActorName = dbChrNode.ChrName;
            m_netActorNode.Exp = dbChrNode.ChrStatus.Exp;
            m_netActorNode.SceneId = dbChrNode.ChrStatus.CurSceneId;
            m_netActorNode.NetActorType = NetActorType.Character;
            if(dbChrNode.ChrCombat != null)
            {
                m_netActorNode.EquippedSkills.AddRange(dbChrNode.ChrCombat.EquippedSkills);
            }
        }
        public string SessionId => m_session.SesssionId;
        public void Send(IMessage message)
        {
            m_session.Send(message);
        }
    }
}
