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
            _closetSwitch.TurnOff();
        }
    }
}
