using System.Timers;

namespace DotaHostClientLibrary
{
    public delegate void TimerCallback();

    public static class Timers
    {
        // Timespan IDs
        public const byte MILLISECONDS = 0;
        public const byte SECONDS = 1;
        public const byte MINUTES = 2;
        public const byte HOURS = 3;

        public delegate void endTimer();

        // Create new timeout
        public static endTimer setTimeout(int duration, byte type, TimerCallback func)
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
            // Creates a new timer with the given duration
            Timer timer = new System.Timers.Timer(properDuration);

            // Sets the elapsed function
            timer.Elapsed += (sender, e) =>
            {
                // Call the function given from the function argument
                func();

                // Dispose of the timer when we're finished
                timer.Dispose();
            };

            // Enables the timer
            timer.Enabled = true;

            return timer.Dispose;
        }

    }
}
