using System;

namespace EnetSenderNet
{
    partial class Program
    {
        public class Job
        {
            public string Name { get; }
            public Action Action { get; set; }
            public DateTime Time { get; set; }
            public bool DoneForToday { get; set; }

            public bool IgnoreOnWeekends { get; set; }

            internal void Check()
            {
                if (!DoneForToday && IgnoreOnWeekends && (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
                {
                    DoneForToday = true;
                }
                if (!DoneForToday && DateTime.Now > Time)
                {
                    Program.LogNormal($"[Job] {Name} firing (scheduled {Time:HH:mm:ss})");
                    Action();
                    DoneForToday = true;
                }
            }

            private Job(string name, DateTime time, Action action)
            {
                Name = name;
                Time = time;
                if (Time < DateTime.Now)
                {
                    Program.LogNormal($"[Job] {Name} already past ({Time:HH:mm:ss}), skipping");
                    DoneForToday = true;
                }
                else
                {
                    DoneForToday = false;
                }
                Action = action;
            }

            public Job(string name, DateTime time, Action action, bool ignoreOnWeekends) : this(name, time, action)
            {
                IgnoreOnWeekends = ignoreOnWeekends;
            }
        }
    }
}
