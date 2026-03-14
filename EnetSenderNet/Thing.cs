using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

    public abstract class Thing
    {
        private const string ServerIp = "192.168.178.34";
        private const int ServerPort = 9050;

        private static readonly Regex ValueRegex = new("\"VALUE\":\"(-?\\d+)\"", RegexOptions.Compiled);
        private static readonly Regex StateRegex = new("\"STATE\":\"([^\"]+)\"", RegexOptions.Compiled);

        private Socket _sender;

        public string Name { get; }
        public int Channel { get; }

        public Thing(string name, int channel)
        {
            Name = name;
            Channel = channel;
        }

        private void Connect()
        {
            _sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sender.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort));
            Console.WriteLine("Socket connected to {0}", _sender.RemoteEndPoint.ToString());
        }

        public void SendMessage(string message)
        {
            _sender.Send(Encoding.ASCII.GetBytes(message));
            Console.WriteLine("Sent message = {0}", message.Trim());
        }

        public void ReceiveMessage()
        {
            var buffer = new byte[1024];
            int bytesRec = _sender.Receive(buffer);
            Console.WriteLine("Received message = {0}", Encoding.ASCII.GetString(buffer, 0, bytesRec).Trim());
        }

        public string SendRequest(string message, int receiveTimeoutMs = 3000)
        {
            Connect();
            _sender.ReceiveTimeout = receiveTimeoutMs;
            SendMessage(message);

            var buffer = new byte[65536];
            var sb = new StringBuilder();
            try { while (true) { int n = _sender.Receive(buffer); if (n == 0) break; sb.Append(Encoding.ASCII.GetString(buffer, 0, n)); } }
            catch (SocketException) { }
            _sender.Close();
            return sb.ToString();
        }

        public ThingState GetState()
        {
            var signIn = new EnetCommandMessage { Channel = Channel, Command = "ITEM_VALUE_SIGN_IN_REQ" };
            string response = SendRequest(signIn.GetMessageString(), receiveTimeoutMs: 500);

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

        public void ConnectAndSendMessage(Action action)
        {
            try
            {
                Connect();

                var signInMessage = new EnetCommandMessage { Channel = Channel, Command = "ITEM_VALUE_SIGN_IN_REQ" };
                string signInMessageString = signInMessage.GetMessageString();

                SendMessage(signInMessageString);
                ReceiveMessage();
                SendMessage(signInMessageString);
                ReceiveMessage();

                action();
                ReceiveMessage();

                var signOutMessage = new EnetCommandMessage { Channel = Channel, Command = "ITEM_VALUE_SIGN_OUT_REQ" };
                SendMessage(signOutMessage.GetMessageString());
                ReceiveMessage();

                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
        }
    }
}
