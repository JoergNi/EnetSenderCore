using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace EnetSenderNetTest
{
    [TestClass]
    public class JobLogFilterTests
    {
        [TestMethod]
        public void FilterJobLog_ExcludesEntriesBeforeCutoff()
        {
            var entries = new[]
            {
                "2026-04-01 10:00:00 [Job] old entry",
                "2026-04-10 10:00:00 [Job] boundary entry",
                "2026-04-13 20:00:00 [Job] recent entry"
            };

            var result = Program.FilterJobLog(entries, new DateTime(2026, 4, 10)).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.IsFalse(result.Any(l => l.Contains("old entry")));
        }

        [TestMethod]
        public void FilterJobLog_IncludesEntriesOnCutoffDate()
        {
            var entries = new[]
            {
                "2026-04-10 00:00:01 [Job] early",
                "2026-04-10 23:59:59 [Job] late"
            };

            var result = Program.FilterJobLog(entries, new DateTime(2026, 4, 10)).ToList();

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void FilterJobLog_AllEntriesOld_ReturnsEmpty()
        {
            var entries = new[] { "2020-01-01 00:00:00 [Job] ancient" };

            var result = Program.FilterJobLog(entries, DateTime.Now.AddDays(-10)).ToList();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void FilterJobLog_EmptyInput_ReturnsEmpty()
        {
            var result = Program.FilterJobLog(Array.Empty<string>(), DateTime.Now).ToList();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void FilterJobLog_AllEntriesRecent_ReturnsAll()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var entries = new[]
            {
                $"{today} 08:00:00 [Job] morning",
                $"{today} 20:00:00 [Job] evening"
            };

            var result = Program.FilterJobLog(entries, DateTime.Now.AddDays(-10)).ToList();

            Assert.AreEqual(2, result.Count);
        }
    }
}
