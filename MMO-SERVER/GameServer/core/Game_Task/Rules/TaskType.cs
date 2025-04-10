using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Game_Task.Rules
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TaskType : Attribute
    {
        public int m_mainType;
        public TaskType(int mainType)
        {
            m_mainType = mainType;
        }
    }
}
