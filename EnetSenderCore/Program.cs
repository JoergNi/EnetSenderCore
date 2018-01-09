using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

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

        };
        private static Switch _closetSwitch = new Switch("Schrank", 16);
        private static Blind _blindOfficeStreet = new Blind("RolloArbeitszimmerStraße", 17);
        private static Blind _blindOfficeGarage = new Blind("RolloArbeitszimmerGarage", 18);
        private static Blind _blindDiningroom = new Blind("RolloEssen", 21);
        private static Blind _blindKitchen = new Blind("RolloKueche", 23);
        private static Blind _blindSleepingRoom = new Blind("RolloSchlafzimmer", 22);


        public static JobBuilder ActionJob(Action action)
        {
            return JobBuilder
                .Create<RunActionJob>()
                .SetJobData(new JobDataMap{
                    { "action", action}
                });
        }

        static void Main(string[] args)
        {
            Blind RaffstoreEssen = new Blind("RaffstoreEssen", 19);

            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            RunProgram().GetAwaiter().GetResult();

            Console.WriteLine("Press x key to close the application");

            while(Console.ReadKey().KeyChar!='x');

            //TODO Logger mit datum
            //TODO Exception loggen
        }



        private static async Task RunProgram()
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();

                var closeOfficeBlindsJob = ActionJob(() =>
                {
                    //_blindOfficeGarage.MoveDown();
                    _blindOfficeStreet.MoveDown();
                }).Build();

                var openOfficeBlindsJob = ActionJob(() =>
                {
                    //_blindOfficeGarage.MoveUp();
                    _blindOfficeStreet.MoveUp();
                }).Build();

                var closeLivingRoomBlindsJob = ActionJob(() =>
                {
                    _blindKitchen.MoveDown();
                    _blindDiningroom.MoveDown();
                }).Build();

                var openLivingRoomBlindsJob = ActionJob(() =>
                {
                    _blindKitchen.MoveUp();
                    _blindDiningroom.MoveUp();
                }).Build();

                var closeSleepingRoomBlindsJob = ActionJob(() =>
                {
                    _blindSleepingRoom.MoveDown();
                }).Build();

                var openSleepingRoomBlindsJob = ActionJob(() =>
                {
                    _blindSleepingRoom.MoveUp();

                }).Build();


                //TimeSpan now = DateTime.Now.TimeOfDay;

                //ScheduleJob(scheduler, openSleepingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(20)));
                //ScheduleJob(scheduler, openLivingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(40)));
                //ScheduleJob(scheduler, openOfficeBlindsJob, now.Add(TimeSpan.FromSeconds(50)));


                //ScheduleJob(scheduler, closeOfficeBlindsJob, now.Add(TimeSpan.FromSeconds(100)));
                //ScheduleJob(scheduler, closeLivingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(110)));
                //ScheduleJob(scheduler, closeSleepingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(120)));

                ScheduleJob(scheduler, openSleepingRoomBlindsJob, new TimeSpan(7, 33, 0));
                ScheduleJob(scheduler, openLivingRoomBlindsJob, new TimeSpan(7, 52, 0));
                ScheduleJob(scheduler, openOfficeBlindsJob, new TimeSpan(8, 0, 11));

                ScheduleJob(scheduler, closeOfficeBlindsJob, new TimeSpan(16, 32, 0));
                ScheduleJob(scheduler, closeLivingRoomBlindsJob, new TimeSpan(16, 44, 0));
                ScheduleJob(scheduler, closeSleepingRoomBlindsJob, new TimeSpan(17, 1, 0));

                await scheduler.Start();

            }
            catch (SchedulerException se)
            {
                await Console.Error.WriteLineAsync(se.ToString());
            }
        }

        private static async void ScheduleJob(IScheduler scheduler, IJobDetail openOfficeBlindsJob, TimeSpan openOfficeBlindsTime)
        {
            ITrigger openOfficeBlindsTrigger = GetDailyTrigger(openOfficeBlindsTime);
            Console.WriteLine("Scheduling job for "+openOfficeBlindsTime);

            await scheduler.ScheduleJob(openOfficeBlindsJob, openOfficeBlindsTrigger);
        }

        private static ITrigger GetDailyTrigger(TimeSpan openOfficeBlindsTime)
        {
            return TriggerBuilder.Create()
                          .StartAt(DateTimeOffset.Now.Date.Add(openOfficeBlindsTime))
                          .WithSimpleSchedule(x => x.WithIntervalInHours(24)
                                                    .RepeatForever())
                          .Build();
        }

    }

}
