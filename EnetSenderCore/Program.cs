using Chroniton;
using Chroniton.Jobs;
using Chroniton.Schedules;
using System;
using System.Collections.Generic;

namespace EnetSenderCore
{
    class Program
    {
        private IDictionary<string, int> things = new Dictionary<string, int>()
        {
            { "Schrank",16},
            { "RolloArbeitszimmerStraße",17},
            { "RolloArbeitszimmerGarage",18},
            {"RaffstoreEssen",19 },
            {"RolloEssen",21 },
            {"Ding3Wohnzimmer",28 },
            {"Ding4Wohnzimmer",20 },
            {"Ding5Wohnzimmer",27 },
            { "RolloKueche",23},
            { "RolloSchlafzimmer",22},
            { "RolloDisneyZimmer",24},
            { "RolloLeeresZimmer",25},

        }           ;


        static void Main(string[] args)
        {
            var schrankSwitch = new Switch("Schrank", 16);
            Blind rolloArbeitszimmerStraße = new Blind("RolloArbeitszimmerStraße", 17);
            Blind RaffstoreEssen = new Blind("RaffstoreEssen", 19);
            Blind RolloArbeitszimmerStraße = new Blind("RolloArbeitszimmerStraße", 17);
            Blind RolloArbeitszimmerGarage = new Blind("RolloArbeitszimmerGarage", 18);
            Blind rolloEssen = new Blind("RolloEssen", 21);

            var singularity = Singularity.Instance;
            singularity.OnJobError += HandleJobError;
            singularity.OnScheduled += HandleScheduled;
            singularity.OnScheduleError += HandleScheduleError;
            singularity.OnSuccess += HandleSuccess;
            
            ISchedule everyDaySchedule = new MySchedule(new TimeSpan(21,10,0));
            
          //  singularity.ScheduleJob(everyDaySchedule, new SimpleJob((x) => RolloArbeitszimmerGarage.MoveDown()), DateTime.Now.AddMinutes(2));
            singularity.Start();
            singularity.ScheduleJob(everyDaySchedule, new SimpleJob((x) => RolloArbeitszimmerGarage.MoveDown()), true);

            Console.ReadKey();
            
        }

        private static void HandleSuccess(ScheduledJobEventArgs job)
        {
            throw new NotImplementedException();
        }

        private static void HandleScheduleError(ScheduledJobEventArgs job, Exception e)
        {
            throw new NotImplementedException();
        }

        private static void HandleScheduled(ScheduledJobEventArgs job)
        {
            Console.WriteLine("Scheduled at {0}", job.ScheduledTime);
        }

        private static void HandleJobError(ScheduledJobEventArgs job, Exception e)
        {
            throw new NotImplementedException();
        }
    }

    public class MySchedule : ISchedule
    {
        public MySchedule(TimeSpan timeToRun)
        {
            TimeToRun = timeToRun;
        }

        public string Name { get; set; }

        public TimeSpan TimeToRun { get; set; }
        public DateTime NextScheduledTime(IScheduledJob scheduledJob)
        {
            DateTime result = DateTime.Today + TimeToRun;
            if (result < DateTime.Now)
            {
                result = DateTime.Today.AddDays(1) + TimeToRun;
            }
            return result;
        }
    }
}
