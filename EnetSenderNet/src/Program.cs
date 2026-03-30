using CoordinateSharp;
using EnetSenderNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EnetSenderNet
{
    internal partial class Program
    {
        private static readonly WeatherService _weather = new WeatherService(50.921210, 7.086539);

        public static DateTime LastInitTime { get; private set; }
        public static IList<Job> Jobs = new List<Job>();

        public static string FirmwareVersion { get; private set; } = "unknown";
        public static string HardwareVersion { get; private set; } = "unknown";
        public static string EnetVersion     { get; private set; } = "unknown";
        public static int[]  DeviceTypes     { get; private set; } = Array.Empty<int>();

        private static void Main(string[] args)
        {
            Console.WriteLine(typeof(Program).Assembly.GetName().Version);
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            QueryVersion();
            QueryAllChannels();
            LogThingStates();

            Task.Run(RunScheduler);
            Task.Run(RefreshStateCache);

            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/version", () => typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

            app.MapGet("/diagnostics", () => new
            {
                addonVersion  = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                firmware      = FirmwareVersion,
                hardware      = HardwareVersion,
                enet          = EnetVersion,
                deviceTypes   = DeviceTypes.Select((type, index) => new { channel = index, type }).Where(x => x.type != 0),
                things        = ThingRegistry.All.Select(t => new
                {
                    channel  = t.Channel,
                    name     = t.Name,
                    type     = t.ThingType,
                    hwType   = DeviceTypes.Length > t.Channel ? DeviceTypes[t.Channel] : -1,
                    state    = ThingRegistry.StateCache.TryGetValue(t.Channel, out var s) ? s : null
                })
            });

            app.MapGet("/things", () => ThingRegistry.All.Select(t => new
            {
                channel = t.Channel,
                name = t.Name,
                type = t.ThingType,
                state = ThingRegistry.StateCache.TryGetValue(t.Channel, out var s) ? s : null
            }));

            app.MapPost("/things/{channel}/up", (int channel) =>
            {
                var thing = ThingRegistry.All.FirstOrDefault(t => t.Channel == channel);
                if (thing is Blind b) b.MoveUp();
                else if (thing is Switch sw) sw.TurnOff();
                else if (thing is DimmableLight dl) dl.TurnOff();
                else return Results.NotFound();
                Task.Run(() => RefreshChannel(thing));
                return Results.Ok();
            });

            app.MapPost("/things/{channel}/down", (int channel) =>
            {
                var thing = ThingRegistry.All.FirstOrDefault(t => t.Channel == channel);
                if (thing is Blind b) b.MoveDown();
                else if (thing is Switch sw) sw.TurnOn();
                else if (thing is DimmableLight dl) dl.TurnOn();
                else return Results.NotFound();
                Task.Run(() => RefreshChannel(thing));
                return Results.Ok();
            });

            app.MapPost("/things/{channel}/position/{value}", (int channel, int value) =>
            {
                var thing = ThingRegistry.All.FirstOrDefault(t => t.Channel == channel);
                if (thing is Blind b)
                {
                    b.MoveTo(value);
                    Task.Run(() => RefreshChannel(thing));
                    return Results.Ok();
                }
                return Results.BadRequest("Not a blind");
            });

            app.MapPost("/things/{channel}/brightness/{value}", (int channel, int value) =>  // value 0-100
            {
                var thing = ThingRegistry.All.FirstOrDefault(t => t.Channel == channel);
                if (thing is DimmableLight dl)
                {
                    dl.SetBrightness(value);
                    Task.Run(() => RefreshChannel(thing));
                    return Results.Ok();
                }
                return Results.BadRequest("Not a dimmable light");
            });

            app.Run("http://0.0.0.0:8080");
        }

        private static void RefreshChannel(Thing thing)
        {
            // Immediate read — hardware reports correct state right after a command
            var initial = thing.GetState();
            if (initial != null && initial.Value >= 0)
                ThingRegistry.StateCache[thing.Channel] = initial;

            // Then poll until stable (covers blinds that are still moving)
            string lastState = $"{initial?.Value}:{initial?.State}";
            int unchanged = 0;
            while (unchanged < 2)
            {
                Thread.Sleep(3000);
                var state = thing.GetState();
                if (state == null) break;
                if (state.Value >= 0)
                {
                    ThingRegistry.StateCache[thing.Channel] = state;
                    string key = $"{state.Value}:{state.State}";
                    if (key == lastState) unchanged++;
                    else unchanged = 0;
                    lastState = key;
                }
            }
        }

        private static void RunScheduler()
        {
            while (true)
            {
                if (LastInitTime.Date < DateTime.Now.Date)
                    Initialize();

                foreach (Job job in Jobs)
                    job.Check();

                Thread.Sleep(5000);
            }
        }

        private static readonly HashSet<int> _refreshingChannels = new HashSet<int>();

        private static void RefreshStateCache()
        {
            while (true)
            {
                foreach (var thing in ThingRegistry.All)
                {
                    var state = thing.GetState();
                    if (state != null && state.Value >= 0)
                    {
                        bool changed = !ThingRegistry.StateCache.TryGetValue(thing.Channel, out var cached)
                            || cached.Value != state.Value || cached.State != state.State;
                        ThingRegistry.StateCache[thing.Channel] = state;
                        if (changed)
                        {
                            lock (_refreshingChannels)
                            {
                                if (_refreshingChannels.Add(thing.Channel))
                                    Task.Run(() => { RefreshChannel(thing); lock (_refreshingChannels) { _refreshingChannels.Remove(thing.Channel); } });
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
                Thread.Sleep(60000);
            }
        }

        private static void QueryVersion()
        {
            var request = new EnetCommandMessage { Command = "VERSION_REQ" };
            string response = ThingRegistry.OfficeGarage.SendRequest(request.GetMessageString());
            Console.WriteLine("VERSION_RES: " + response);
            try
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(response.Trim().TrimEnd('\r', '\n'));
                FirmwareVersion = json["FIRMWARE"]?.ToString() ?? "unknown";
                HardwareVersion = json["HARDWARE"]?.ToString() ?? "unknown";
                EnetVersion     = json["ENET"]?.ToString()     ?? "unknown";
            }
            catch { Console.WriteLine("Failed to parse VERSION_RES"); }
        }

        private static void QueryAllChannels()
        {
            var request = new EnetCommandMessage { Command = "GET_CHANNEL_INFO_ALL_REQ" };
            string response = ThingRegistry.OfficeGarage.SendRequest(request.GetMessageString());
            Console.WriteLine("GET_CHANNEL_INFO_ALL response:");
            Console.WriteLine(response);
            try
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(response.Trim().TrimEnd('\r', '\n'));
                DeviceTypes = json["DEVICES"]?.ToObject<int[]>() ?? Array.Empty<int>();
            }
            catch { Console.WriteLine("Failed to parse GET_CHANNEL_INFO_ALL_RES"); }
        }

        private static void LogThingStates()
        {
            foreach (var thing in ThingRegistry.All)
            {
                var state = thing.GetState();
                Console.WriteLine($"[Ch{thing.Channel:D2}] {thing.Name,-30} Value={state?.Value,3}  State={state?.State}");
                Thread.Sleep(500);
            }
        }

        private static void Initialize()
        {
            LastInitTime = DateTime.Now;
            Jobs = new List<Job>();
            Coordinate c = new Coordinate(50.921210, 7.086539, LastInitTime);
            DateTime localSunRise = new DateTime(c.CelestialInfo.SunRise.Value.Ticks, DateTimeKind.Utc).ToLocalTime();
            DateTime localSunSet = new DateTime(c.CelestialInfo.SunSet.Value.Ticks, DateTimeKind.Utc).ToLocalTime();
            Console.WriteLine(localSunRise);
            Console.WriteLine(localSunSet);

            Jobs.Add(new Job(Min(localSunSet.AddMinutes(10), TimeSpan.FromHours(20)), () =>
            {
                ThingRegistry.OfficeGarage.MoveDown();
                ThingRegistry.OfficeStreet.MoveDown();
            }, false));

            Jobs.Add(new Job(Max(localSunRise.AddMinutes(10), TimeSpan.FromHours(8.25)), () =>
            {
                ThingRegistry.OfficeGarage.MoveUp();
                ThingRegistry.OfficeStreet.MoveUp();
            }, false));

            Jobs.Add(new Job(Min(localSunSet.AddMinutes(8), TimeSpan.FromHours(22)), () =>
            {
                ThingRegistry.Kitchen.MoveDown();
                ThingRegistry.DiningRoom.MoveDown();
                Thread.Sleep(1000);
                ThingRegistry.Kitchen.MoveDown();
                ThingRegistry.DiningRoom.MoveDown();
            }, false));

            Jobs.Add(new Job(Max(localSunRise.AddMinutes(8), TimeSpan.FromHours(7.45)), () =>
            {
                ThingRegistry.Kitchen.MoveUp();
                Thread.Sleep(1000);
                ThingRegistry.DiningRoom.MoveUp();
                ThingRegistry.Kitchen.MoveUp();
                Thread.Sleep(1000);
                ThingRegistry.DiningRoom.MoveUp();
            }, false));

            Jobs.Add(new Job(Min(localSunSet, TimeSpan.FromHours(22)), () =>
            {
                ThingRegistry.SleepingRoom.MoveDown();
            }, false));

            Jobs.Add(new Job(Min(localSunSet.AddMinutes(2), TimeSpan.FromHours(22)), () =>
            {
                ThingRegistry.PaulsRoom.MoveDown();
            }, false));

            Jobs.Add(new Job(Min(localSunSet.AddMinutes(1), TimeSpan.FromHours(22)), () =>
            {
                ThingRegistry.LeasRoom.MoveDown();
            }, false));

            Jobs.Add(new Job(DateTime.Today.AddHours(9), () =>
            {
                ThingRegistry.LeasRoom.MoveUp();
            }, false));

            Jobs.Add(new Job(Min(localSunSet.AddMinutes(4), TimeSpan.FromHours(23)), () =>
            {
                ThingRegistry.RaffstoreLiving.MoveDown();
            }, false));

            Jobs.Add(new Job(Min(localSunSet.AddMinutes(3), TimeSpan.FromHours(22)), () =>
            {
                ThingRegistry.RaffstoreDining.MoveDown();
            }, false));

            Jobs.Add(new Job(Max(localSunRise.AddMinutes(10), TimeSpan.FromHours(10)), () =>
            {
                ThingRegistry.RaffstoreDining.MoveUp();
                ThingRegistry.RaffstoreLiving.MoveUp();
            }, false));

            bool isSummer = LastInitTime.Month > 3 && LastInitTime.Month < 10;
            bool isHot = _weather.IsHot();

            if (isSummer)
            {
                Jobs.Add(new Job(DateTime.Today.AddHours(13.5), () =>
                {
                    ThingRegistry.Kitchen.MoveHalf();
                    ThingRegistry.OfficeStreet.MoveHalf();
                    ThingRegistry.DiningRoom.MoveHalf();
                }, false));

                if (isHot)
                {
                    Jobs.Add(new Job(DateTime.Today.AddHours(9), () =>
                    {
                        ThingRegistry.PaulsRoom.MoveThreeQuarters();
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        ThingRegistry.LeasRoom.MoveThreeQuarters();
                    }, false));

                    Jobs.Add(new Job(DateTime.Today.AddHours(17.5), () =>
                    {
                        ThingRegistry.PaulsRoom.MoveUp();
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        ThingRegistry.LeasRoom.MoveUp();
                    }, false));
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
