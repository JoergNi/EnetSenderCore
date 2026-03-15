using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace EnetSenderNetTest
{
    [TestClass]
    public class EnetIntegrationTests
    {
        private ThingState WaitUntilStopped(Blind blind, int timeoutSeconds = 40)
        {
            var deadline = DateTime.Now.AddSeconds(timeoutSeconds);
            ThingState last = null;
            int stableCount = 0;

            while (DateTime.Now < deadline)
            {
                Thread.Sleep(2000);
                var current = blind.GetState();
                if (last != null && current?.Value == last.Value)
                {
                    if (++stableCount >= 2) return current;
                }
                else
                {
                    stableCount = 0;
                }
                last = current;
            }
            return last;
        }

        [TestMethod]
        public void MoveToPositionAndVerifyState()
        {
            var blind = new Blind("RolloArbeitszimmerGarage", 18);

            blind.MoveUp();
            var stateAfterUp = WaitUntilStopped(blind);
            Console.WriteLine($"After MoveUp: Value={stateAfterUp?.Value}, State={stateAfterUp?.State}");
            Assert.AreEqual(0, stateAfterUp?.Value, "Expected blind to be fully up (0%)");

            blind.MoveTo(25);
            var stateAfterMove = WaitUntilStopped(blind);
            Console.WriteLine($"After move to 25%: Value={stateAfterMove?.Value}, State={stateAfterMove?.State}");
            Assert.AreEqual(25, stateAfterMove?.Value, "Expected blind to be at 25%");
        }
    }
}
