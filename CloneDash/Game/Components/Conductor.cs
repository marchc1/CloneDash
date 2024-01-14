using Raylib_cs;

namespace CloneDash.Game.Components
{
    /// <summary>
    /// Currently unused, and may be a bad idea to try implementing?
    /// </summary>
    public struct TempoChange {
        public double Time;
        public double BPM;

        public TempoChange(double time, float bpm) {
            this.Time = time;
            this.BPM = bpm;
        }
    }

    public class Conductor : DashGameComponent
    {
        public List<TempoChange> TempoChanges { get; private set; } = [];

        /// <summary>
        /// The current music playhead, adjusted for inaccuracies.
        /// </summary>
        public double Time => currentInaccurateTime;

        /// <summary>
        /// Offsets the conductor time
        /// </summary>
        public double PreStartTime { get; set; } = 5;

        public double BPM => GetTempoAtTime(Time); 

        // What was the last music time that Raylib reported?
        private float lastTimeFromFunctionCall = 0;
        // What is the current time + the frametime values
        private float currentInaccurateTime = 0;

        public Conductor(DashGame game) : base(game) {

        }

        public TempoChange GetTempoChangeAtTime(double time) {
            if (TempoChanges.Count == 0)
                throw new Exception("No tempo changes found in DashGame (likely a DashSheet import error)");

            if(TempoChanges.Count == 1)
                return TempoChanges[0];

            TempoChange last = TempoChanges[0];
            foreach(var tempoChange in TempoChanges) {
                if (time >= tempoChange.Time)
                    last = tempoChange;
                else
                    break;
            }

            return last;
        }
        /// <summary>
        /// Gets the current BPM from the song position
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetTempoAtTime(double time) => GetTempoChangeAtTime(time).BPM;

        public bool firstTick = true;
        public override void OnTick() {
            if (firstTick) {
                currentInaccurateTime = (float)-PreStartTime;
            }
            if (currentInaccurateTime < 0) {
                currentInaccurateTime += Raylib.GetFrameTime();
            }
            else {
                Game.Music.Update();

                var now = Game.Music.Playhead;
                var paused = Game.Music.Paused;

                if (lastTimeFromFunctionCall != now && !paused) {
                    lastTimeFromFunctionCall = now;
                    currentInaccurateTime = now;
                }
                else {
                    if (!paused)
                        currentInaccurateTime += Game.FrameTime;
                }
            }
            firstTick = false;
        }

        /// <summary>
        /// Returns how long 1/<paramref name="division"/> of a note takes, in seconds. By default, <paramref name="division"/> is set to 4, which is a quarter note.
        /// </summary>
        /// <param name="division"></param>
        /// <returns></returns>
        public float NoteDivisorToSeconds(float division = 4) => (60 / (float)BPM) * (4 / division);

        /// <summary>
        /// Returns a value between zero to one, where zero is the current beat starting, and one is the current beat ending.<br></br>
        /// <paramref name="division"/> is by default set to 4, which means you'll get a 0-1 value for each quarter note.
        /// </summary>
        /// <param name="division"></param>
        /// <returns></returns>
        public float NoteDivisorRealtime(float division = 4) {
            var div2sec = NoteDivisorToSeconds(division);
            return ((float)Time % div2sec) / div2sec;
        }

        public override void OnDrawScreenSpace(float width, float height) {
            float howLongUntilFinishedPlaying = Game.Music.Playhead / Game.Music.Length;
            Graphics.SetDrawColor(200, 225, 235, 255);
            Graphics.DrawRectangle(0, height - 4, width * howLongUntilFinishedPlaying, 4);
        }
    }
}
