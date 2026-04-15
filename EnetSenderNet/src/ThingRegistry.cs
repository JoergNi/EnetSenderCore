using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EnetSenderNet
{
    public static class ThingRegistry
    {
        public static readonly Switch Schrank          = new Switch("Schrank", 16);
        public static readonly Blind OfficeStreet      = new Blind("RolloArbeitszimmerStraße", 17);
        public static readonly Blind OfficeGarage      = new Blind("RolloArbeitszimmerGarage", 18);
        public static readonly Blind RaffstoreDining   = new Blind("RaffstoreEssen", 19);
        public static readonly Blind RaffstoreLiving   = new Blind("RaffstoreTerassenTür", 20);
        public static readonly Blind DiningRoom        = new Blind("RolloEssen", 21);
        public static readonly Blind SleepingRoom      = new Blind("RolloSchlafzimmer", 22);
        public static readonly Blind Kitchen           = new Blind("RolloKueche", 23);
        public static readonly Blind LeasRoom          = new Blind("RolloLeasZimmer", 24);
        public static readonly Blind PaulsRoom         = new Blind("RolloPaulsZimmer", 25);
        public static readonly DimmableLight LivingEsszimmer   = new DimmableLight("LichtEsszimmer", 27);
        public static readonly DimmableLight LivingWohnbereich = new DimmableLight("LichtWohnbereich", 28);

        public static readonly IReadOnlyList<Thing> All = new Thing[]
        {
            Schrank, OfficeStreet, OfficeGarage, RaffstoreDining, RaffstoreLiving,
            DiningRoom, SleepingRoom, Kitchen, LeasRoom, PaulsRoom, LivingWohnbereich, LivingEsszimmer
        };

        public static readonly ConcurrentDictionary<int, ThingState> StateCache = new();
    }
}
