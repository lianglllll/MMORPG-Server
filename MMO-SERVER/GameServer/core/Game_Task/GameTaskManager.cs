using Common.Summer.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Game_Task
{

    public class GameTask
    {
        private TaskDefine def;
        private object taskProgressData;    
    }


    public class GameTaskChain
    {
        int taskChainId;


    }

    public class GameTaskManager : Singleton<GameTaskManager>
    {



        // 获取某个玩家的全部任务数据
        // 保存某个玩家全部的任务数据
    }
}
