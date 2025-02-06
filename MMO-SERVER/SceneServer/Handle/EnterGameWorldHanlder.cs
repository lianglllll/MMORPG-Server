using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Scene;
using SceneServer.Core.Scene;

namespace SceneServer.Handle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private int m_curWorldId;
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();
        
        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<CharacterEnterSceneRequest>((int)SceneProtocl.CharacterEnterSceneReq);
            ProtoHelper.Instance.Register<SelfCharacterEnterSceneResponse>((int)SceneProtocl.SelfCharacterEnterSceneResp);
            ProtoHelper.Instance.Register<OtherEntityEnterSceneResponse>((int)SceneProtocl.OtherEntityEnterSceneResp);
            ProtoHelper.Instance.Register<CharacterLeaveSceneRequest>((int)SceneProtocl.CharacterLeaveSceneReq);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<CharacterEnterSceneRequest>(_HandleCharacterEnterSceneRequest);
            MessageRouter.Instance.Subscribe<CharacterLeaveSceneRequest>(_HandleCharacterLeaveSceneRequest);

            return true;
        }

        private void _HandleCharacterEnterSceneRequest(Connection conn, CharacterEnterSceneRequest message)
        {
            SceneManager.Instance.CharacterEnterScene(conn, message);
        }

        private void _HandleCharacterLeaveSceneRequest(Connection conn, CharacterLeaveSceneRequest message)
        {
            SceneManager.Instance.CharacterLeaveScene(message.EntityId);
        }

    }
}
