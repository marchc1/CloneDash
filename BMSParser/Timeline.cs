// Credit: https://github.com/Mushus/bms-parser

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BMS
{
    public class Timeline
    {
        List<Event> events;
        internal Timeline(BMS bms)
        {
            var sortObjects = new List<IObject>(bms.Objects);
            sortObjects.Sort(
                (a, b) =>
                    a.Measure != b.Measure ?
                    a.Measure - b.Measure :
                    a.Channel - b.Channel
            );

            // NOTE: sortObject は 小節でソートされているので、
            // 最後の小節を調べれば最大の小節が取れる
            var maxMeasure =
                sortObjects.Count > 0 ?
                sortObjects[sortObjects.Count - 1].Measure :
                0;

            var eventTimeline = new List<Event>();

            var bpm = bms.BPM;
            double timeSecond = 0;
            for (var measure = 0; measure <= maxMeasure; measure++)
            {
                var measuredObj = sortObjects.FindAll(v => v.Measure == measure);
                double beat = 1;
                double measureDuration = calcMeasureDuration(bpm, beat);
                foreach (var obj in measuredObj)
                {
                    if (obj is BPMObject bmsObj)
                    {
                        bpm = bmsObj.Data;
                        measureDuration = calcMeasureDuration(bpm, beat);
                    }

                    if (obj is BeatObject beatObj)
                    {
                        beat = beatObj.Data;
                        measureDuration = calcMeasureDuration(bpm, beat);
                    }

                    if (obj is KeyObject keyObject)
                    {
                        var notes = keyObject.Data;
                        for (var i = 0; i < notes.Length; i++)
                        {
                            var reference = notes[i];
                            if (reference == 0) continue;
                            var millisecond = Convert.ToInt64((measureDuration * (double)i / (double)notes.Length + timeSecond) * 1000);
                            var channel = keyObject.Channel;
                            eventTimeline.Add(new Event(millisecond, channel, reference));
                        }
                    }
                }

                timeSecond += measureDuration;
            }
            eventTimeline.Sort((a, b) =>
                    a.Millisecond != b.Millisecond ?
                    (int)(a.Millisecond - b.Millisecond) :
                    a.Channel - b.Channel);
            this.events = eventTimeline;
        }
        private double calcMeasureDuration(double bpm, double beat)
        {
            return 4 * 60 * beat / bpm;
        }

        public ReadOnlyCollection<Event> Events()
        {
            return this.events.AsReadOnly();
        }
    }
}