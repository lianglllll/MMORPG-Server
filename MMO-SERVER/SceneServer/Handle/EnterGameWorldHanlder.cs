using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Scene;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;

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
            ProtoHelper.Instance.Register<CharacterLeaveSceneResponse>((int)SceneProtocl.CharacterLeaveSceneResp);

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
            var resp = new CharacterLeaveSceneResponse();

            var chr = SceneManager.Instance.SceneCharacterManager.GetSceneCharacterByEntityId(message.EntityId);
            if(chr == null) {
                goto End;       
            }
            SceneManager.Instance.CharacterLeaveScene(message.EntityId);
            resp.CId = chr.Cid;
            resp.SceneSaveDatea = new NeedSaveSceneData();
            resp.SceneSaveDatea.Position = chr.Position;
        End:
            conn.Send(resp);
            return;
        }
    }
}
