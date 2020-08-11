using EnetSenderCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EnetSenderCoreTest
{
    [TestClass]
    public class UnitTest1
    {
        private static Switch _closetSwitch = new Switch("Schrank", 16);

        [TestMethod]
        public void TestMethod1()
        {
           var _blindOfficeGarage = new Blind("RolloArbeitszimmerGarage", 25);
           _blindOfficeGarage.MoveHalf();
    }
    }
}
