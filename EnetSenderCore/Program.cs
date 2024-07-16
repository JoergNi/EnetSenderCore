using CoordinateSharp;
using Quartz;
using Quartz.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EnetSenderCore
{
    internal partial class Program
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
            { "RolloLeasZimmer",24},
            { "RolloPaulsZimmer",25},

        };
        private static Switch _closetSwitch = new Switch("Schrank", 16);
        private static Blind _blindOfficeStreet = new Blind("RolloArbeitszimmerStraße", 17);
        private static Blind _blindOfficeGarage = new Blind("RolloArbeitszimmerGarage", 18);
        private static Blind _blindDiningRoom = new Blind("RolloEssen", 21);
        private static Blind _blindKitchen = new Blind("RolloKueche", 23);
        private static Blind _blindPaulsRoom = new Blind("RolloPaulsZimmer", 25);
        private static Blind _blindLeasRoom = new Blind("RolloLeasZimmer", 24);
        private static Blind _blindSleepingRoom = new Blind("RolloSchlafzimmer", 22);

        private static Blind _raffstoreDiningRoom = new Blind("RaffstoreEssen", 19);
        private static Blind _raffstoreLivingRoom = new Blind("RaffstoreTerassenTür", 20);

        public static DateTime LastInitTime { get; private set; }

        public static IList<Job> Jobs = new List<Job>();

        public static JobBuilder ActionJob(Action action)
        {
            return JobBuilder
                .Create<RunActionJob>()
                .SetJobData(new JobDataMap{
                    { "action", action}
                });
        }

        private static void Main(string[] args)
        {
            Console.WriteLine(typeof(Program).Assembly.GetName().Version);
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            // RunProgram().GetAwaiter().GetResult();

            while (true)
            {
                if (LastInitTime.Date < DateTime.Now.Date)
                {
                    Initialize();
                }

                foreach (Job job in Jobs)
                {
                    job.Check();
                }
                Thread.Sleep(5000);
            }

        }

        private static void Initialize()
        {
            LastInitTime = DateTime.Now;
            Coordinate c = new Coordinate();
            c.Latitude = new CoordinatePart(50.921210, CoordinateType.Lat, c);
            c.Longitude = new CoordinatePart(7.086539, CoordinateType.Long, c);
            c.GeoDate = LastInitTime;
            DateTime localSunRise = new DateTime(c.CelestialInfo.SunRise.Value.Ticks, DateTimeKind.Utc).ToLocalTime();
            DateTime localSunSet = new DateTime(c.CelestialInfo.SunSet.Value.Ticks, DateTimeKind.Utc).ToLocalTime();
            Console.WriteLine(localSunRise);
            Console.WriteLine(localSunSet);

            var closeOfficeBlindsJob = new Job(Min(localSunSet.AddMinutes(10), TimeSpan.FromHours(20)), () =>
            {
                _blindOfficeGarage.MoveDown();
                _blindOfficeStreet.MoveDown();
            },false);
            Jobs.Add(closeOfficeBlindsJob);

            var openOfficeBlindsJob = new Job(Max(localSunRise.AddMinutes(10), TimeSpan.FromHours(8.25)), () =>
            {
                _blindOfficeGarage.MoveUp();
                _blindOfficeStreet.MoveUp();
            }, false);
            Jobs.Add(openOfficeBlindsJob);

            var closeLivingRoomBlindsJob = new Job(Min(localSunSet.AddMinutes(8), TimeSpan.FromHours(22)), () =>
            {
                _blindKitchen.MoveDown();
                _blindDiningRoom.MoveDown();
                Thread.Sleep(1000);
                _blindKitchen.MoveDown();
                _blindDiningRoom.MoveDown();
            }, false);
            Jobs.Add(closeLivingRoomBlindsJob);

            var openLivingRoomBlindsJob = new Job(Max(localSunRise.AddMinutes(8), TimeSpan.FromHours(7.45)), () =>
            {
                _blindKitchen.MoveUp();
                Thread.Sleep(1000);
                _blindDiningRoom.MoveUp();
                _blindKitchen.MoveUp();
                Thread.Sleep(1000);
                _blindDiningRoom.MoveUp();
            }, false);
            Jobs.Add(openLivingRoomBlindsJob);

            var closeSleepingRoomBlindsJob = new Job(Min(localSunSet, TimeSpan.FromHours(22)), () =>
            {
                _blindSleepingRoom.MoveDown();
            }, false);
            Jobs.Add(closeSleepingRoomBlindsJob);

            var closePaulsRoomBlindsJob = new Job(Min(localSunSet.AddMinutes(2), TimeSpan.FromHours(22)), () =>
            {
                _blindPaulsRoom.MoveDown();
            }, false);
            Jobs.Add(closePaulsRoomBlindsJob);

            var closeLeasRoomBlindsJob = new Job(Min(localSunSet.AddMinutes(1), TimeSpan.FromHours(22)), () =>
            {
                _blindLeasRoom.MoveDown();
            }, false);
            Jobs.Add(closeLeasRoomBlindsJob);

            var openSleepingRoomBlindsJob = new Job(Max(localSunRise.AddMinutes(10), TimeSpan.FromHours(10)), () =>
            {
                _blindSleepingRoom.MoveUp();

            }, false);
            Jobs.Add(openSleepingRoomBlindsJob);

            var closeRaffstoreLivingRoomJob = new Job(Min(localSunSet.AddMinutes(4), TimeSpan.FromHours(23)), () =>
            {
                _raffstoreLivingRoom.MoveDown();
            }, false);
            Jobs.Add(closeRaffstoreLivingRoomJob);

            var closeRaffstoreDiningRoomJob = new Job(Min(localSunSet.AddMinutes(3), TimeSpan.FromHours(22)), () =>
            {
                _raffstoreDiningRoom.MoveDown();
            }, false);
            Jobs.Add(closeRaffstoreDiningRoomJob);

            var openRaffstoresJob = new Job(Max(localSunRise.AddMinutes(10), TimeSpan.FromHours(7.6)), () =>
            {
                _raffstoreDiningRoom.MoveUp();
                _raffstoreLivingRoom.MoveUp();

            }, false);
            Jobs.Add(openRaffstoresJob);

            bool isSummer = LastInitTime.Month > 3 && LastInitTime.Month < 10;
            bool isHot = false;


            if (isSummer)
            {
               
                var halfBlindsJob = new Job(DateTime.Today.AddHours(13.5), () =>
                {
                    _blindKitchen.MoveHalf();
                    _blindOfficeStreet.MoveHalf();
                    _blindDiningRoom.MoveHalf();
                }, false);
                Jobs.Add(halfBlindsJob);
                if (isHot)
                {
                    var southRoomsShader = new Job(DateTime.Today.AddHours(9), () =>
                    {
                        _blindPaulsRoom.MoveThreeQuarters();
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        _blindLeasRoom.MoveThreeQuarters();
                    }, false);
                    Jobs.Add(southRoomsShader);
                    var southRoomsOpen = new Job(DateTime.Today.AddHours(17.5), () =>
                    {
                        _blindPaulsRoom.MoveUp();
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        _blindLeasRoom.MoveUp();
                    }, false);
                    Jobs.Add(southRoomsOpen);
                }

            }

        }

        private static DateTime Min(DateTime dateTime, TimeSpan timeSpan)
        {
            return new DateTime(Math.Min(dateTime.Ticks, DateTime.Today.Add(timeSpan).Ticks));
        }

        private static DateTime Max(DateTime dateTime, TimeSpan timeSpan)
        {
            return new DateTime(Math.Max(dateTime.Ticks, DateTime.Today.Add(timeSpan).Ticks));
        }
    }

}
