using GameServer.Manager;
using HS.Protobuf.Chat;

namespace GameServer.Model
{


    /// <summary>
    /// 每个character一个Chat
    /// </summary>
    public class Chat
    {
        private Character Owner;
        public int localIndex;
        public int worldIndex;
        public int systemIndex;
        public int teamIndex;
        public int guildIndex;

        public Chat(Character chr)
        {
            this.Owner = chr;
        }

        /// <summary>
        /// 后处理，当有任何消息回发到客户端的时候，他就会使用聊天系统来检查有没有新消息
        /// 只要有新消息，就可以把新消息更新出去
        /// </summary>
        /// <param name="res"></param>
        public void PostProcess(ChatResponse res)
        {
            if (res == null)
            {
                res = new ChatResponse();
                res.ResultCode = 0;
            }

            //获取各个频道的信息

            this.localIndex = ChatManager.Instance.GetLocalMessages(this.Owner.CurSpaceId, this.localIndex, res.LocalMessages);
            //this.worldIndex = ChatManager.Instance.GetWorldMessages(this.Owner.SpaceId, this.worldIndex, res.LocalMessages);
            //this.systemIndex = ChatManager.Instance.GetSystemMessages(this.Owner.SpaceId, this.systemIndex, res.LocalMessages);

            /*
            if(this.Owner.Team != null)
            {
                this.teamIndex = ChatManager.Instance.GetTeamMessages(this.Owner.SpaceId, this.systemIndex, res.LocalMessages);
            }
            */
            /*
             if(this.Owner.Guild != null。。。。。。
             */

        }


    }
}
