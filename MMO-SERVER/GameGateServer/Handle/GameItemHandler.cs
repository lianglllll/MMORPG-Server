using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using HS.Protobuf.Backpack;
using HS.Protobuf.Chat;
using HS.Protobuf.GameTask;
using HS.Protobuf.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameGateServer.Handle
{
    public class GameItemHandler : Singleton<GameItemHandler>
    {
        public override void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetItemInventoryDataRequest>((int)BackpackProtocol.GetItemInventoryDataReq);
            ProtoHelper.Instance.Register<GetItemInventoryDataResponse>((int)BackpackProtocol.GetItemInventoryDataResp);
            ProtoHelper.Instance.Register<ChangeItemPositionRequest>((int)BackpackProtocol.ChangeItemPositionReq);
            ProtoHelper.Instance.Register<ChangeItemPositionResponse>((int)BackpackProtocol.ChangeItemPositionResp);
            ProtoHelper.Instance.Register<UseItemRequest>((int)BackpackProtocol.UseItemReq);
            ProtoHelper.Instance.Register<UseItemResponse>((int)BackpackProtocol.UseItemResp);
            ProtoHelper.Instance.Register<DiscardItemRequest>((int)BackpackProtocol.DiscardItemReq);
            ProtoHelper.Instance.Register<DiscardItemResponse>((int)BackpackProtocol.DiscardItemResp);
            ProtoHelper.Instance.Register<WearEquipRequest>((int)BackpackProtocol.WearEquipReq);
            ProtoHelper.Instance.Register<WearEquipResponse>((int)BackpackProtocol.WearEquipResp);
            ProtoHelper.Instance.Register<UnloadEquipRequest>((int)BackpackProtocol.UnloadEquipReq);
            ProtoHelper.Instance.Register<UnloadEquipResponse>((int)BackpackProtocol.UnloadEquipResp);
            ProtoHelper.Instance.Register<PickUpSceneItemRequest>((int)SceneProtocl.PickupSceneItemReq);
            ProtoHelper.Instance.Register<PickupSceneItemResponse>((int)SceneProtocl.PickupSceneItemResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetItemInventoryDataRequest>(HandleGetItemInventoryDataRequest);
            MessageRouter.Instance.Subscribe<GetItemInventoryDataResponse>(HandleGetItemInventoryDataResponse);
            MessageRouter.Instance.Subscribe<ChangeItemPositionRequest>(HandleChangeItemPositionRequest);
            MessageRouter.Instance.Subscribe<ChangeItemPositionResponse>(HandleChangeItemPositionResponse);
            MessageRouter.Instance.Subscribe<UseItemRequest>(HandleUseItemRequest);
            MessageRouter.Instance.Subscribe<UseItemResponse>(HandleUseItemResponse);
            MessageRouter.Instance.Subscribe<DiscardItemRequest>(HandleDiscardItemRequest);
            MessageRouter.Instance.Subscribe<DiscardItemResponse>(HandleDiscardItemResponse);
            MessageRouter.Instance.Subscribe<WearEquipRequest>(HandleWearEquipRequest);
            MessageRouter.Instance.Subscribe<WearEquipResponse>(HandleWearEquipRequest);
            MessageRouter.Instance.Subscribe<UnloadEquipRequest>(HandleUnloadEquipRequest);
            MessageRouter.Instance.Subscribe<UnloadEquipResponse>(HandleUnloadEquipRequest);
            MessageRouter.Instance.Subscribe<PickUpSceneItemRequest>(HandlePickUpSceneItemRequest);
            MessageRouter.Instance.Subscribe<PickupSceneItemResponse>(HandlePickupSceneItemResponse);
        }

        private void HandleGetItemInventoryDataRequest(Connection conn, GetItemInventoryDataRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleGetItemInventoryDataResponse(Connection conn, GetItemInventoryDataResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void HandleChangeItemPositionRequest(Connection conn, ChangeItemPositionRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleChangeItemPositionResponse(Connection conn, ChangeItemPositionResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void HandleUseItemRequest(Connection conn, UseItemRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleUseItemResponse(Connection conn, UseItemResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void HandleDiscardItemRequest(Connection conn, DiscardItemRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleDiscardItemResponse(Connection conn, DiscardItemResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void HandleWearEquipRequest(Connection conn, WearEquipRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleWearEquipRequest(Connection conn, WearEquipResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void HandleUnloadEquipRequest(Connection conn, UnloadEquipRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleUnloadEquipRequest(Connection conn, UnloadEquipResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void HandlePickUpSceneItemRequest(Connection conn, PickUpSceneItemRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            ServersMgr.Instance.SendToSceneServer(session.curSceneId, message);
        End:
            return;
        }
        private void HandlePickupSceneItemResponse(Connection conn, PickupSceneItemResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

    }
}
