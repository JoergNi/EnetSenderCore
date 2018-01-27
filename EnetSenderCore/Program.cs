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

                var closeRaffstoresJob = ActionJob(() =>
                {
                    _raffstoreDiningRoom.MoveDown();
                    _raffstoreLivingRoom.MoveDown();
                }).Build();

                var openRaffstoresJob = ActionJob(() =>
                {
                    _raffstoreDiningRoom.MoveUp();
                    _raffstoreLivingRoom.MoveUp();

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


                TimeSpan sunrise = new TimeSpan(8, 0, 0);
                TimeSpan sundown = new TimeSpan(17, 25, 0);
                ScheduleJob(scheduler, openSleepingRoomBlindsJob, sunrise.Add(TimeSpan.FromMinutes(-30.8)));
                ScheduleJob(scheduler, openLivingRoomBlindsJob, sunrise.Add(TimeSpan.FromMinutes(-15.3)));
                ScheduleJob(scheduler, openOfficeBlindsJob, sunrise.Add(TimeSpan.FromMinutes(-3.1)));
                ScheduleJob(scheduler, openRaffstoresJob, sunrise.Add(TimeSpan.FromMinutes(-0.7)));

                ScheduleJob(scheduler, closeRaffstoresJob, sundown.Add(TimeSpan.FromMinutes(3.5)));
                ScheduleJob(scheduler, closeOfficeBlindsJob, sundown.Add(TimeSpan.FromMinutes(5.9)), false);
                ScheduleJob(scheduler, closeLivingRoomBlindsJob, sundown.Add(TimeSpan.FromMinutes(8.7)));
                ScheduleJob(scheduler, closeSleepingRoomBlindsJob, sundown.Add(TimeSpan.FromMinutes(12.2)), false);

                await scheduler.Start();

            }
            catch (SchedulerException se)
            {
                await Console.Error.WriteLineAsync(se.ToString());
            }
        }

        private static async void ScheduleJob(IScheduler scheduler, IJobDetail openOfficeBlindsJob, TimeSpan openOfficeBlindsTime, bool excludeWeekends = true)
        {
            ITrigger openOfficeBlindsTrigger = GetDailyTrigger(openOfficeBlindsTime, excludeWeekends);
            Console.WriteLine("Scheduling job for " + openOfficeBlindsTime);

            await scheduler.ScheduleJob(openOfficeBlindsJob, openOfficeBlindsTrigger);
        }



        private static ITrigger GetDailyTrigger(TimeSpan openOfficeBlindsTime, bool excludeWeekends)
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
                            dailyTimeIntervalScheduleBuilder.StartingDailyAt(TimeOfDay.HourMinuteAndSecondOfDay(openOfficeBlindsTime.Hours, openOfficeBlindsTime.Minutes, openOfficeBlindsTime.Seconds));
                        })
                          .Build();
        }






    }

}
