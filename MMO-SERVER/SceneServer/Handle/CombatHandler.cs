using Serilog;
using Common.Summer.Tools;
using Common.Summer.Net;
using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;
using SceneServer.Utils;
using SceneServer.Net;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;

namespace SceneServer.Handle
{
    public class CombatHandler : Singleton<CombatHandler>
    {
        public override void Init()
        {
            ProtoHelper.Instance.Register<SpellCastRequest>((int)SkillProtocol.SpellCastReq);
            ProtoHelper.Instance.Register<SpellCastResponse>((int)SkillProtocol.SpellCastResp);
            ProtoHelper.Instance.Register<SpellCastFailResponse>((int)SkillProtocol.SpellCastFailResp);

            ProtoHelper.Instance.Register<SceneDeliverRequest>((int)SceneProtocl.SceneDeliverReq);
            ProtoHelper.Instance.Register<SceneDeliverResponse>((int)SceneProtocl.SceneDeliverResp);
            ProtoHelper.Instance.Register<CharacterReviveRequest>((int)SceneProtocl.CharacterReviveReq);
            ProtoHelper.Instance.Register<CharacterReviveResponse>((int)SceneProtocl.CharacterReviveResp);

            MessageRouter.Instance.Subscribe<SpellCastRequest>(HandleSpellCastRequest);
            MessageRouter.Instance.Subscribe<SceneDeliverRequest>(HandleSpaceDeliverRequest);
            MessageRouter.Instance.Subscribe<CharacterReviveRequest>(HanleReviveRequest);
        }


        private void HandleSpellCastRequest(Connection conn, SpellCastRequest message)
        {
            // 将其放入当前场景的战斗管理器的缓冲队列中
            SceneManager.Instance.FightManager.castReqQueue.Enqueue(message.Info);
        }

        private void HanleReviveRequest(Connection conn, CharacterReviveRequest message)
        {
            // chr发的
            var chr = SceneManager.Instance.SceneCharacterManager.GetSceneCharacterByEntityId(message.EntityId);
            if (chr != null && chr.IsDeath)
            {
                chr.Revive();
            }
        }

        private void HandleSpaceDeliverRequest(Connection conn, SceneDeliverRequest message)
        {
            var chr = SceneManager.Instance.SceneCharacterManager.GetSceneCharacterByEntityId(message.EntityId);
            if(chr != null && chr.IsDeath)
            {
                SceneManager.Instance.TransmitTo(chr, message.PointId);
            }
        }
    }
}
