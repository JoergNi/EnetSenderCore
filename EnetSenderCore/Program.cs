using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
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
        private static Blind _blindDiningRoom = new Blind("RolloEssen", 21);
        private static Blind _blindKitchen = new Blind("RolloKueche", 23);
        private static Blind _blindSleepingRoom = new Blind("RolloSchlafzimmer", 22);

        private static Blind _raffstoreDiningRoom = new Blind("RaffstoreEssen", 19);
        private static Blind _raffstoreLivingRoom = new Blind("RaffstoreTerassenTür", 20);

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
            Console.WriteLine(typeof(Program).Assembly.GetName().Version);
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            RunProgram().GetAwaiter().GetResult();

            Console.WriteLine("Press x key to close the application");

            while (Console.ReadKey().KeyChar != 'x') ;


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
                    _blindOfficeGarage.MoveDown();
                    _blindOfficeStreet.MoveDown();
                }).Build();

                var openOfficeBlindsJob = ActionJob(() =>
                {
                    _blindOfficeGarage.MoveUp();
                    _blindOfficeStreet.MoveUp();
                }).Build();

                var closeLivingRoomBlindsJob = ActionJob(() =>
                {
                    _blindKitchen.MoveDown();
                    _blindDiningRoom.MoveDown();
                }).Build();

                var openLivingRoomBlindsJob = ActionJob(() =>
                {
                    _blindKitchen.MoveUp();
                    Thread.Sleep(100);
                    _blindDiningRoom.MoveUp();
                }).Build();

                var closeSleepingRoomBlindsJob = ActionJob(() =>
                {
                    _blindSleepingRoom.MoveDown();
                }).Build();

                var openSleepingRoomBlindsJob = ActionJob(() =>
                {
                    _blindSleepingRoom.MoveUp();

                }).Build();

                var closeRaffstoreLivingRoomJob = ActionJob(() =>
                {
                    _raffstoreLivingRoom.MoveDown();
                }).Build();

                var closeRaffstoreDiningRoomJob = ActionJob(() =>
                {
                    _raffstoreDiningRoom.MoveDown();
                }).Build();

                var openRaffstoresJob = ActionJob(() =>
                {
                   _raffstoreDiningRoom.MoveUp();
                    _raffstoreLivingRoom.MoveUp();

                }).Build();

                var halfBlindsJob = ActionJob(() =>
                {
                    _blindKitchen.MoveHalf();
                    _blindOfficeStreet.MoveHalf();
                    _blindDiningRoom.MoveHalf();
                }).Build();

                //TimeSpan now = DateTime.Now.TimeOfDay;

                //ScheduleJob(scheduler, openSleepingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(20)));
                //ScheduleJob(scheduler, openLivingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(40)));
                //ScheduleJob(scheduler, openOfficeBlindsJob, now.Add(TimeSpan.FromSeconds(50)));
                //ScheduleJob(scheduler, openRaffstoresJob, now.Add(TimeSpan.FromSeconds(60)));

                //ScheduleJob(scheduler, closeRaffstoresJob, now.Add(TimeSpan.FromSeconds(90)));
                //ScheduleJob(scheduler, closeOfficeBlindsJob, now.Add(TimeSpan.FromSeconds(100)));
                //ScheduleJob(scheduler, closeLivingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(110)));
                //ScheduleJob(scheduler, closeSleepingRoomBlindsJob, now.Add(TimeSpan.FromSeconds(120)));


                TimeSpan sunrise = new TimeSpan(7, 50, 0);
                TimeSpan sundown = new TimeSpan(19, 10, 0);

                ScheduleJob(scheduler, openSleepingRoomBlindsJob, TimeSpan.FromHours(10), false);
                ScheduleJob(scheduler, openLivingRoomBlindsJob, sunrise.Add(TimeSpan.FromMinutes(-15.3)), false);
                ScheduleJob(scheduler, openOfficeBlindsJob, sunrise.Add(TimeSpan.FromMinutes(-3.1)), false);
                ScheduleJob(scheduler, openRaffstoresJob, sunrise.Add(TimeSpan.FromMinutes(-0.7)), false);

              //  ScheduleJob(scheduler, halfBlindsJob, TimeSpan.FromHours(15), false);

                ScheduleJob(scheduler, closeRaffstoreDiningRoomJob, sundown.Add(TimeSpan.FromMinutes(1.5)), false);
                ScheduleJob(scheduler, closeRaffstoreLivingRoomJob, sundown.Add(TimeSpan.FromMinutes(2.2)), false);
                ScheduleJob(scheduler, closeOfficeBlindsJob, sundown.Add(TimeSpan.FromMinutes(5.9)), false);
                ScheduleJob(scheduler, closeLivingRoomBlindsJob, sundown.Add(TimeSpan.FromMinutes(8.7)), false);
                ScheduleJob(scheduler, closeSleepingRoomBlindsJob, sundown.Add(TimeSpan.FromMinutes(12.2)), false);

                await scheduler.Start();

            }
            catch (SchedulerException se)
            {
                await Console.Error.WriteLineAsync(se.ToString());
            }
        }

        private static async void ScheduleJob(IScheduler scheduler, IJobDetail job, TimeSpan time, bool excludeWeekends = true)
        {
            ITrigger trigger = GetDailyTrigger(time, excludeWeekends);
            Console.WriteLine("Scheduling job for " + time);

            await scheduler.ScheduleJob(job, trigger);
        }



        private static ITrigger GetDailyTrigger(TimeSpan time, bool excludeWeekends)
        {
            return TriggerBuilder.Create()
                          .WithDailyTimeIntervalSchedule(s =>
                        {
                            DailyTimeIntervalScheduleBuilder dailyTimeIntervalScheduleBuilder = s.WithIntervalInHours(24);
                            if (excludeWeekends)
                            {
                                dailyTimeIntervalScheduleBuilder.OnMondayThroughFriday();
                            }
                            else
                            {
                                dailyTimeIntervalScheduleBuilder.OnEveryDay();
                            }
                            dailyTimeIntervalScheduleBuilder.StartingDailyAt(TimeOfDay.HourMinuteAndSecondOfDay(time.Hours, time.Minutes, time.Seconds));
                        })
                          .Build();
        }






    }

}
