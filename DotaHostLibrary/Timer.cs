using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DotaHostLibrary
{

    public delegate void TimerCallback(); 
    public static class Timer
    {
        public const byte MILLISECONDS = 0;
        public const byte SECONDS = 1;
        public const byte MINUTES = 2;
        public const byte HOURS = 3;

        public static void newTimer(int duration, byte type, TimerCallback func)
        {
            System.Timers.Timer timer;
            int properDuration;
            switch (type)
            {
                case SECONDS:
                    properDuration = duration * 1000;
                    break;
                case MINUTES:
                    properDuration = duration * 1000 * 60;
                    break;
                case HOURS:
                    properDuration = duration * 1000 * 60 * 60;
                    break;
                default:
                    properDuration = duration;
                    break;
            }

            timer = new System.Timers.Timer();
            timer.Elapsed += (sender, e) => { func(); timer.Dispose(); };
        }
    }
}
