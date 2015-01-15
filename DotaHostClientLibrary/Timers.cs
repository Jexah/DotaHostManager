using System.Timers;

namespace DotaHostClientLibrary
{
    public delegate void TimerCallback();

    public static class Timers
    {
        // Timespan IDs
        public const byte Milliseconds = 0;
        public const byte Seconds = 1;
        public const byte Minutes = 2;
        public const byte Hours = 3;

        public delegate void EndTimer();

        // Create new timeout
        public static EndTimer SetTimeout(int duration, byte type, TimerCallback func)
        {
            int properDuration;
            switch (type)
            {
                case Seconds:
                    properDuration = duration * 1000;
                    break;
                case Minutes:
                    properDuration = duration * 1000 * 60;
                    break;
                case Hours:
                    properDuration = duration * 1000 * 60 * 60;
                    break;
                default:
                    properDuration = duration;
                    break;
            }
            // Creates a new timer with the given duration
            var timer = new Timer(properDuration);

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

            // Returns dispose function, call this to delete the timer.
            return timer.Dispose;
        }

    }
}
