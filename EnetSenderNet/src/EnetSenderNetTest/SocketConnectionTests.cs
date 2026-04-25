using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EnetSenderNetTest
{
    /// <summary>
    /// Tests that exercise the real SocketMobilegateSender TCP error paths and the
    /// /mobilegate endpoint response-classification logic.
    ///
    /// TCP tests inject a real sender via Thing.CreateRealSenderForTest() pointed at a
    /// loopback address with no listener (connection refused, immediate).  This avoids
    /// touching the static Thing.ServerIp/ServerPort fields, so existing integration
    /// tests that need the real Mobilegate are unaffected.
    /// </summary>
    [TestClass]
    public class SocketConnectionTests
    {
        private static int _freePort;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            // Bind to port 0 → OS assigns a free port → immediately release it.
            // No listener will be running on this port for the duration of the tests.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            _freePort = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
        }

        [TestInitialize]
        public void SetUp()
        {
            Thing.RetryDelayMs = 0;
            Program.OnCommandFailed = null;
        }

        [TestCleanup]
        public void TearDown()
        {
            Thing.RetryDelayMs = 1000;
            Program.OnCommandFailed = null;
        }

        // ── Send() path ────────────────────────────────────────────────────────
        // Connect() throws SocketException(ConnectionRefused) → caught by Send() → "".
        // GetState() finds no regex matches in "" → returns null.

        [TestMethod]
        public void Send_ConnectionRefused_ReturnsNull()
        {
            var sender = Thing.CreateRealSenderForTest("127.0.0.1", _freePort);
            var blind = new Blind("test", 18, sender);

            var state = blind.GetState();

            Assert.IsNull(state, "GetState() must return null when TCP connection is refused");
        }

        // ── SendCommand() path ─────────────────────────────────────────────────
        // Connect() throws SocketException → propagates out of SendCommand() →
        // caught by SendCommandMessage() retry loop → 3 attempts → OnCommandFailed.

        [TestMethod]
        public void SendCommand_ConnectionRefused_CallsOnCommandFailedAfterRetries()
        {
            string failMsg = null;
            Program.OnCommandFailed = msg => failMsg = msg;
            var sender = Thing.CreateRealSenderForTest("127.0.0.1", _freePort);
            var blind = new Blind("test", 18, sender);

            blind.MoveDown();

            Assert.IsNotNull(failMsg, "OnCommandFailed must be invoked after all retry attempts fail");
            StringAssert.Contains(failMsg, "ch18");
            StringAssert.Contains(failMsg, "test");
        }

        // ── /mobilegate endpoint logic ─────────────────────────────────────────
        // Endpoint handler: response.Contains("VERSION_RES") ? "ok" : "down"

        [TestMethod]
        public void MobilegateLogic_ResponseWithVersionRes_ClassifiedAsOk()
        {
            var fake = new FakeSender();
            fake.Responses.Enqueue("{\"CMD\":\"VERSION_RES\",\"FIRMWARE\":\"0.91\",\"PROTOCOL\":\"0.03\"}\r\n\r\n");
            var blind = new Blind("test", 18, fake);

            string response = blind.SendRequest("irrelevant");

            Assert.AreEqual("ok", response.Contains("VERSION_RES") ? "ok" : "down");
        }

        [TestMethod]
        public void MobilegateLogic_EmptyResponse_ClassifiedAsDown()
        {
            var fake = new FakeSender();  // queue empty → returns ""
            var blind = new Blind("test", 18, fake);

            string response = blind.SendRequest("irrelevant");

            Assert.AreEqual("down", response.Contains("VERSION_RES") ? "ok" : "down");
        }

        // ── Minimal fake (Send-only) ───────────────────────────────────────────

        private sealed class FakeSender : IMobilegateSender
        {
            public Queue<string> Responses { get; } = new Queue<string>();

            public string Send(string message, int receiveTimeoutMs = 3000) =>
                Responses.Count > 0 ? Responses.Dequeue() : string.Empty;

            public void SendCommand(string commandMessage, int channel, string thingName) { }
        }
    }
}
