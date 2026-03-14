using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
           var _blindOfficeGarage = new Blind("RolloArbeitszimmerGarage", 18);
           _blindOfficeGarage.MoveUp();
            Thread.Sleep(10000);
    }

     
    }
}