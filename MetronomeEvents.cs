using System;

namespace Anywhen
{
    [Serializable]
    public struct MetronomeTickEvent
    {
        public int TickRate;
        public double ScheduledTime;
        public int Count;
    }
}
