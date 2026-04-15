using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
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
        public void Diagnostics_VersionReq_ReturnsFirmwareHardwareEnet()
        {
            var thing = new Blind("probe", 18);
            var request = new EnetCommandMessage { Command = "VERSION_REQ" };
            string response = thing.SendRequest(request.GetMessageString());
            Console.WriteLine("VERSION_RES: " + response);

            var json = JObject.Parse(response.Trim());
            Assert.AreEqual("VERSION_RES", json["CMD"]?.ToString(), "Expected CMD=VERSION_RES");
            StringAssert.Matches(json["FIRMWARE"]?.ToString(), new System.Text.RegularExpressions.Regex(@"\d+\.\d+"), "FIRMWARE should be a version string");
            Assert.IsFalse(string.IsNullOrEmpty(json["HARDWARE"]?.ToString()), "HARDWARE should be non-empty");
            Assert.IsFalse(string.IsNullOrEmpty(json["ENET"]?.ToString()),     "ENET should be non-empty");
        }

        [TestMethod]
        public void Diagnostics_GetChannelInfoAll_Returns40DeviceTypes()
        {
            var thing = new Blind("probe", 18);
            var request = new EnetCommandMessage { Command = "GET_CHANNEL_INFO_ALL_REQ" };
            string response = thing.SendRequest(request.GetMessageString());
            Console.WriteLine("GET_CHANNEL_INFO_ALL_RES: " + response);

            var json = JObject.Parse(response.Trim());
            Assert.AreEqual("GET_CHANNEL_INFO_ALL_RES", json["CMD"]?.ToString());
            var devices = json["DEVICES"]?.ToObject<int[]>();
            Assert.IsNotNull(devices, "DEVICES array should be present");
            Assert.AreEqual(40, devices.Length, "Expected 40 channel entries");
            Console.WriteLine("Non-zero channels: " + string.Join(", ", System.Linq.Enumerable.Range(0, devices.Length).Where(i => devices[i] != 0).Select(i => $"ch{i}=type{devices[i]}")));
        }

        //[TestMethod]
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
