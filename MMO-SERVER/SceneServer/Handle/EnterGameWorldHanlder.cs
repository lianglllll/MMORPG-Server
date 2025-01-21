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
            ProtoHelper.Instance.Register<SelfCharacterEnterSceneResponse>((int)SceneProtocl.CharacterEnterSceneReq);
            ProtoHelper.Instance.Register<OtherCharacterEnterSceneResponse>((int)SceneProtocl.CharacterEnterSceneReq);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<CharacterEnterSceneRequest>(_HandleCharacterEnterSceneRequest);


            return true;
        }
        private void _HandleCharacterEnterSceneRequest(Connection conn, CharacterEnterSceneRequest message)
        {
            SceneManager.Instance.AddSceneCharacter(message.DbChrNode);
        }
    }
}
