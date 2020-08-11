using System;

namespace EnetSenderCore
{
    partial class Program
    {
        public class Job
        {
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

                    Action();
                    DoneForToday = true;
                }
            }

            public Job(DateTime time, Action action)
            {
                Time = time;
                if (Time < DateTime.Now)
                {
                    DoneForToday = true;
                }
                else
                {
                    DoneForToday = false;
                }
                Action = action;
            }

            public Job(DateTime time, Action action, bool ignoreOnWeekends):this(time, action)
            {
                IgnoreOnWeekends = true;
            }
        }



      




    }

}
