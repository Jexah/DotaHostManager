
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

            System.Timers.Timer timer = new System.Timers.Timer(properDuration);
            timer.Elapsed += (sender, e) => { func(); timer.Dispose(); };
            timer.Enabled = true;
        }
    }
}
