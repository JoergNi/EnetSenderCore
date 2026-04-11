using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace EnetSenderNetTest
{
    /// <summary>
    /// Unit tests using a fake Mobilegate — no TCP connection required.
    /// </summary>
    [TestClass]
    public class MobilegateTests
    {
        // ── Fake implementation ──────────────────────────────────────────────

        private class FakeMobilegate : IMobilegateSender
        {
            /// <summary>Responses returned in order by Send(). Returns "" when empty.</summary>
            public Queue<string> Responses { get; } = new Queue<string>();

            /// <summary>All command messages passed to SendCommand(), in order.</summary>
            public List<string> CommandMessages { get; } = new List<string>();

            public string Send(string message, int receiveTimeoutMs = 3000) =>
                Responses.Count > 0 ? Responses.Dequeue() : string.Empty;

            public void SendCommand(string commandMessage, int channel) =>
                CommandMessages.Add(commandMessage);
        }

        // Helpers to build realistic Mobilegate ITEM_UPDATE_IND responses
        private static string SignInResponse(int channel, int value, string state) =>
            $"{{\"CMD\":\"ITEM_VALUE_SIGN_IN_RES\",\"PROTOCOL\":\"0.03\",\"ITEMS\":[{channel}],\"TIMESTAMP\":\"1421948265\"}}\r\n" +
            $"{{\"CMD\":\"ITEM_UPDATE_IND\",\"NUMBER\":{channel},\"STATE\":\"{state}\",\"VALUE\":\"{value}\",\"PROTOCOL\":\"0.03\"}}\r\n\r\n";

        private static string EmptyResponse() => string.Empty;

        private static string UndefinedResponse(int channel) =>
            $"{{\"CMD\":\"ITEM_VALUE_SIGN_IN_RES\",\"PROTOCOL\":\"0.03\",\"ITEMS\":[{channel}],\"TIMESTAMP\":\"1421948265\"}}\r\n" +
            $"{{\"CMD\":\"ITEM_UPDATE_IND\",\"NUMBER\":{channel},\"STATE\":\"UNDEFINED\",\"PROTOCOL\":\"0.03\"}}\r\n\r\n";

        // ── GetState tests ───────────────────────────────────────────────────

        [TestMethod]
        public void GetState_ParsesPositionAndState()
        {
            var fake = new FakeMobilegate();
            fake.Responses.Enqueue(SignInResponse(18, 75, "ON"));
            var blind = new Blind("test", 18, fake);

            var state = blind.GetState();

            Assert.IsNotNull(state);
            Assert.AreEqual(75, state.Value);
            Assert.AreEqual("ON", state.State);
        }

        [TestMethod]
        public void GetState_FullyUp_IsUpTrue()
        {
            var fake = new FakeMobilegate();
            fake.Responses.Enqueue(SignInResponse(18, 0, "OFF"));
            var blind = new Blind("test", 18, fake);

            var state = blind.GetState();

            Assert.IsNotNull(state);
            Assert.AreEqual(0, state.Value);
            Assert.IsTrue(state.IsUp);
        }

        [TestMethod]
        public void GetState_ReturnsNullOnEmptyResponse()
        {
            var fake = new FakeMobilegate();
            fake.Responses.Enqueue(EmptyResponse());
            var blind = new Blind("test", 18, fake);

            Assert.IsNull(blind.GetState());
        }

        [TestMethod]
        public void GetState_ReturnsNullWhenNoValueField()
        {
            // UNDEFINED response has STATE but no VALUE — both regexes must match
            var fake = new FakeMobilegate();
            fake.Responses.Enqueue(UndefinedResponse(22));
            var blind = new Blind("test", 22, fake);

            Assert.IsNull(blind.GetState());
        }

        [TestMethod]
        public void GetState_NegativeValuePassedThrough()
        {
            // Mobilegate sometimes returns VALUE:-1 for non-position-aware devices;
            // GetState returns the state object; callers filter Value < 0.
            var fake = new FakeMobilegate();
            fake.Responses.Enqueue(SignInResponse(17, -1, "ON"));
            var blind = new Blind("test", 17, fake);

            var state = blind.GetState();

            Assert.IsNotNull(state);
            Assert.AreEqual(-1, state.Value);
            Assert.IsFalse(state.IsPositionAware);
        }

        // ── Blind command tests ──────────────────────────────────────────────

        [TestMethod]
        public void Blind_MoveDown_SendsValue100()
        {
            var fake = new FakeMobilegate();
            var blind = new Blind("test", 18, fake);

            blind.MoveDown();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"VALUE\":100");
            StringAssert.Contains(fake.CommandMessages[0], "\"STATE\":\"VALUE_BLINDS\"");
        }

        [TestMethod]
        public void Blind_MoveUp_SendsValue0()
        {
            var fake = new FakeMobilegate();
            var blind = new Blind("test", 18, fake);

            blind.MoveUp();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"VALUE\":0");
        }

        [TestMethod]
        public void Blind_MoveTo_SendsCorrectValue()
        {
            var fake = new FakeMobilegate();
            var blind = new Blind("test", 18, fake);

            blind.MoveTo(35);

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"VALUE\":35");
        }

        [TestMethod]
        public void Blind_MoveHalf_SendsValue50()
        {
            var fake = new FakeMobilegate();
            var blind = new Blind("test", 18, fake);

            blind.MoveHalf();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"VALUE\":50");
        }

        // ── Switch command tests ─────────────────────────────────────────────

        [TestMethod]
        public void Switch_TurnOn_SendsStateOn()
        {
            var fake = new FakeMobilegate();
            var sw = new Switch("test", 16, fake);

            sw.TurnOn();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"STATE\":\"ON\"");
        }

        [TestMethod]
        public void Switch_TurnOff_SendsStateOff()
        {
            var fake = new FakeMobilegate();
            var sw = new Switch("test", 16, fake);

            sw.TurnOff();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"STATE\":\"OFF\"");
        }

        // ── DimmableLight command tests ──────────────────────────────────────

        [TestMethod]
        public void DimmableLight_SetBrightness_SendsCorrectValue()
        {
            var fake = new FakeMobilegate();
            var light = new DimmableLight("test", 27, fake);

            light.SetBrightness(75);

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"VALUE\":75");
            StringAssert.Contains(fake.CommandMessages[0], "\"STATE\":\"ON\"");
        }

        [TestMethod]
        public void DimmableLight_TurnOff_SendsStateOff()
        {
            var fake = new FakeMobilegate();
            var light = new DimmableLight("test", 27, fake);

            light.TurnOff();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"STATE\":\"OFF\"");
        }

        [TestMethod]
        public void DimmableLight_TurnOn_SendsValue100()
        {
            var fake = new FakeMobilegate();
            var light = new DimmableLight("test", 27, fake);

            light.TurnOn();

            Assert.AreEqual(1, fake.CommandMessages.Count);
            StringAssert.Contains(fake.CommandMessages[0], "\"VALUE\":100");
        }
    }
}
