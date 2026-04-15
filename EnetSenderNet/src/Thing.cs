using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EnetSenderNet
{
    public class ThingState
    {
        public int Value { get; set; }       // 0-100 for position-aware actors, 101=down, 102=up for others
        public string State { get; set; }    // "OFF"/"ALL_OFF" = up, "ON"/"ALL_ON" = down
        public bool IsPositionAware => Value >= 0 && Value <= 100;
        public bool IsUp => State == "OFF" || State == "ALL_OFF";
    }

    /// <summary>Abstraction over the Mobilegate TCP transport. Injected for unit testing.</summary>
    public interface IMobilegateSender
    {
        /// <summary>Send a single request and return the full response (used by GetState, health check).</summary>
        string Send(string message, int receiveTimeoutMs = 3000);

        /// <summary>
        /// Open a session (sign-in × 2 + commandMessage + sign-out) against the given channel.
        /// Used by all movement/on/off/brightness commands.
        /// </summary>
        void SendCommand(string commandMessage, int channel, string thingName);
    }

    public abstract class Thing
    {
        public virtual string ThingType => "thing";
        private static readonly string ServerIp   = GetConfig("enet_host", "ENET_HOST", "192.168.178.34");
        private static readonly int    ServerPort = int.TryParse(GetConfig("enet_port", "ENET_PORT", "9050"), out var p) ? p : 9050;

        // Priority: /data/options.json (HA addon) → env var (local dev) → hardcoded default
        private static string GetConfig(string jsonKey, string envVar, string defaultValue)
        {
            const string optionsFile = "/data/options.json";
            if (File.Exists(optionsFile))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(optionsFile));
                    if (doc.RootElement.TryGetProperty(jsonKey, out var val))
                        return val.ToString();
                }
                catch { }
            }
            return Environment.GetEnvironmentVariable(envVar) ?? defaultValue;
        }

        private static readonly Regex ValueRegex = new("\"VALUE\":\"(-?\\d+)\"", RegexOptions.Compiled);
        private static readonly Regex StateRegex = new("\"STATE\":\"([^\"]+)\"", RegexOptions.Compiled);

        private readonly IMobilegateSender _mobilegate;

        public string Name { get; }
        public int Channel { get; }

        public Thing(string name, int channel, IMobilegateSender sender = null)
        {
            Name = name;
            Channel = channel;
            _mobilegate = sender ?? new SocketMobilegateSender(ServerIp, ServerPort);
        }

        public string SendRequest(string message, int receiveTimeoutMs = 3000) =>
            _mobilegate.Send(message, receiveTimeoutMs);

        public ThingState GetState()
        {
            var signIn = new EnetCommandMessage { Channel = Channel, Command = "ITEM_VALUE_SIGN_IN_REQ" };
            string response = _mobilegate.Send(signIn.GetMessageString(), receiveTimeoutMs: 500);

            var valueMatch = ValueRegex.Match(response);
            var stateMatch = StateRegex.Match(response);

            if (!valueMatch.Success || !stateMatch.Success)
                return null;

            return new ThingState
            {
                Value = int.Parse(valueMatch.Groups[1].Value),
                State = stateMatch.Groups[1].Value
            };
        }

        public static int RetryDelayMs = 1000;

        protected void SendCommandMessage(string commandMessage)
        {
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _mobilegate.SendCommand(commandMessage, Channel, Name);
                    return;
                }
                catch (SocketException se)
                {
                    Program.LogNormal($"SocketException on ch{Channel} ({Name}), attempt {attempt}/{maxAttempts}: {se.Message}");
                    if (attempt < maxAttempts)
                        System.Threading.Thread.Sleep(RetryDelayMs);
                    else
                    {
                        Program.LogNormal($"SendCommand failed after {maxAttempts} attempts: ch{Channel} ({Name})");
                        Program.OnCommandFailed?.Invoke($"ch{Channel} ({Name}) command failed after {maxAttempts} attempts");
                    }
                }
            }
        }

        // ── Real TCP implementation ──────────────────────────────────────────

        private sealed class SocketMobilegateSender : IMobilegateSender
        {
            private readonly string _ip;
            private readonly int _port;

            public SocketMobilegateSender(string ip, int port) { _ip = ip; _port = port; }

            private Socket Connect()
            {
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(new IPEndPoint(IPAddress.Parse(_ip), _port));
                return sock;
            }

            public string Send(string message, int receiveTimeoutMs = 3000)
            {
                try
                {
                    var sock = Connect();
                    sock.ReceiveTimeout = receiveTimeoutMs;
                    sock.Send(Encoding.ASCII.GetBytes(message));

                    var buffer = new byte[65536];
                    var sb = new StringBuilder();
                    try { while (true) { int n = sock.Receive(buffer); if (n == 0) break; sb.Append(Encoding.ASCII.GetString(buffer, 0, n)); } }
                    catch (SocketException) { }
                    sock.Close();
                    return sb.ToString();
                }
                catch (SocketException)
                {
                    return string.Empty;
                }
            }

            public void SendCommand(string commandMessage, int channel, string thingName)
            {
                var sock = Connect();
                var buf = new byte[1024];

                var signIn = new EnetCommandMessage { Channel = channel, Command = "ITEM_VALUE_SIGN_IN_REQ" };
                string signInStr = signIn.GetMessageString();

                sock.Send(Encoding.ASCII.GetBytes(signInStr)); sock.Receive(buf);
                sock.Send(Encoding.ASCII.GetBytes(signInStr)); sock.Receive(buf);
                sock.Send(Encoding.ASCII.GetBytes(commandMessage)); sock.Receive(buf);

                var signOut = new EnetCommandMessage { Channel = channel, Command = "ITEM_VALUE_SIGN_OUT_REQ" };
                sock.Send(Encoding.ASCII.GetBytes(signOut.GetMessageString())); sock.Receive(buf);

                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
            }
        }
    }
}
