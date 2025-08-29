using Common.Summer.Time;
using Common.Summer.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterTimerServer.Core
{
    public class TimeMonitor : Singleton<TimeMonitor>
    {
        private HighPrecisionClock highPrecisionClock = new HighPrecisionClock();
        public long GetUnixTimeMilliseconds => highPrecisionClock.GetUnixTimeMilliseconds();

    }
}
