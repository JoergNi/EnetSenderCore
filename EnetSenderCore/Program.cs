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
            Blind rolloEssen = new Blind("RolloEssen", 21);

            _closetSwitch.TurnOn();

            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());



            //RunProgram().GetAwaiter().GetResult();

            //StartSingularity(RolloArbeitszimmerGarage);

            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();

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

                var closeOfficeBlindsJob = ActionJob(() => {
                    _blindOfficeGarage.MoveDown();
                    _blindOfficeStreet.MoveDown();
                }).Build();

                var openOfficeBlindsJob = ActionJob(() => {
                    _blindOfficeGarage.MoveUp();
                    _blindOfficeStreet.MoveUp();
                }).Build();



                ITrigger openOfficeBlindsTrigger = TriggerBuilder.Create()
                              .StartAt(DateTimeOffset.Now.AddSeconds(60))
                              .WithSimpleSchedule(x => x.WithIntervalInMinutes(2)
                                                        .WithRepeatCount(1))
                              .Build();

                ITrigger closeOfficeBlindsTrigger = TriggerBuilder.Create()
                             .StartAt(DateTimeOffset.Now.AddSeconds(120))
                             .WithSimpleSchedule(x => x.WithIntervalInMinutes(2)
                                                        .WithRepeatCount(1))
                             .Build();

                await scheduler.ScheduleJob(openOfficeBlindsJob, openOfficeBlindsTrigger);
                await scheduler.ScheduleJob(closeOfficeBlindsJob, closeOfficeBlindsTrigger);



                // and start it off
                await scheduler.Start();

                //// some sleep to show what's happening
                //await Task.Delay(TimeSpan.FromSeconds(60));

                //// and last shut down the scheduler when you are ready to close your program
                //await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                await Console.Error.WriteLineAsync(se.ToString());
            }
        }

   
    }

}
