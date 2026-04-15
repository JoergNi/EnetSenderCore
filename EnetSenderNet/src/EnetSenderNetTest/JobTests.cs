using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace EnetSenderNetTest
{
    [TestClass]
    public class JobTests
    {
        [TestMethod]
        public void Job_FutureTime_NotFiredOnCheck()
        {
            bool fired = false;
            var job = new Program.Job("test", DateTime.Now.AddHours(1), () => fired = true, false);

            job.Check();

            Assert.IsFalse(fired);
            Assert.IsFalse(job.DoneForToday);
        }

        [TestMethod]
        public void Job_PastTimeAtCreation_MarkedDoneWithoutFiring()
        {
            bool fired = false;
            var job = new Program.Job("test", DateTime.Now.AddSeconds(-60), () => fired = true, false);

            Assert.IsTrue(job.DoneForToday);
            Assert.IsFalse(fired); // constructor must not call the action
        }

        [TestMethod]
        public void Job_Check_FiresWhenTimePassed()
        {
            bool fired = false;
            var job = new Program.Job("test", DateTime.Now.AddHours(1), () => fired = true, false);
            job.Time = DateTime.Now.AddSeconds(-1);

            job.Check();

            Assert.IsTrue(fired);
            Assert.IsTrue(job.DoneForToday);
        }

        [TestMethod]
        public void Job_Check_DoesNotFireTwice()
        {
            int count = 0;
            var job = new Program.Job("test", DateTime.Now.AddHours(1), () => count++, false);
            job.Time = DateTime.Now.AddSeconds(-1);

            job.Check();
            job.Check();

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void Job_Name_IsSet()
        {
            var job = new Program.Job("MyBlind down", DateTime.Now.AddHours(1), () => { }, false);

            Assert.AreEqual("MyBlind down", job.Name);
        }
    }
}
