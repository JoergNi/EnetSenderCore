using EnetSenderNet;
using System.IO;
using System.Threading;

namespace EnetSenderNetTest
{
    [TestClass]
    public class UnitTest1
    {
        private static Switch _closetSwitch = new Switch("Schrank", 16);

        [TestMethod]
        public void TestMethod1()
        {
            var _blindOfficeGarage = new Blind("RolloArbeitszimmerGarage", 11);
            _blindOfficeGarage.MoveHalf();
        }
    }
}