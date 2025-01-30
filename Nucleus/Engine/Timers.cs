namespace Nucleus.Engine
{
    public class Timer {
        public double LastRun { get; set; }
        public double Delay { get; set; }
        public int MaxRepetitions { get; set; }
        public int Repetitions { get; set; } = 0;
        public Action? Method { get; set; }
    }
    public class TimerManagement(Level level)
    {
        private Dictionary<ThreadExecutionTime, List<Timer>> Timers = [];

        /// <summary>
        /// Creates a simple timer, which executes in <paramref name="delay"/> seconds.
        /// <br></br>
        /// <br></br>
        /// This is thread-safe; operates similarly to 
        /// <see cref="MainThread.RunASAP(Action, ThreadExecutionTime)"/> where a <see cref="ThreadExecutionTime"/> 
        /// (by default, <see cref="ThreadExecutionTime.BeforeFrame"/>) controls where in the game loop this timer will be ran.
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="on"></param>
        /// <param name="exTime"></param>
        /// <returns></returns>
        public Timer Simple(float delay, Action on, ThreadExecutionTime exTime = ThreadExecutionTime.BeforeFrame) {
            Timer t = new Timer();

            t.LastRun = level.Realtime;
            t.Delay = delay;
            t.MaxRepetitions = 1;
            t.Method = on;

            Timers.TryAdd(exTime, []);
            Timers[exTime].Add(t);

            return t;
        }

        public void Run(ThreadExecutionTime exTime) {
            var now = level.Realtime;
            List<Timer> toRemove = [];

            Timers.TryGetValue(exTime, out var timers);
            if (timers == null) return;

            foreach (Timer timer in timers) {
                if(now - timer.LastRun > timer.Delay) {
                    timer.Method?.Invoke();
                    timer.Repetitions += 1;
                    timer.LastRun = now;

                    if (timer.Repetitions >= timer.MaxRepetitions)
                        toRemove.Add(timer);
                }
            }

            foreach (var t in toRemove)
                timers.Remove(t);
        }
    }
}
