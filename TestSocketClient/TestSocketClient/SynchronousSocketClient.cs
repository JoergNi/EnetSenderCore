using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TestSocketClient
{
    public class SynchronousSocketClient
    {
        private Socket _sender;
        private byte[] _bytes;

        public void SendOnOffMessage(int channel, bool state)
        {
            string message = Resource1.ValueSetMessageTemplate.Replace("{state}", state ? "ON" : "OFF");
            SendChannelMessage(channel, message);
        }

        public void SendBlindsMessage(int channel, int value)
        {
            string message = Resource1.BlindsValueSetMessage.Replace("{value}", value.ToString());
            SendChannelMessage(channel, message);
        }

        public void SendChannelMessage(int channel, string message)
        {
            message = message.Replace("{channel}", channel.ToString());
            SendMessage(message);
        }

        public void SendMessage(string message)
        {

            byte[] msg = Encoding.ASCII.GetBytes(message);

            // Send the data through the socket.
            int bytesSent = _sender.Send(msg);
            Console.WriteLine("Sent message = {0}", message);

        }

        public void ReceiveMessage()
        {

            // Receive the response from the remote device.
            int bytesRec = _sender.Receive(_bytes);
            Console.WriteLine("Received message = {0}", Encoding.ASCII.GetString(_bytes, 0, bytesRec));
        }

        public void StartClient()
        {

            

            // Data buffer for incoming data.
            _bytes = new byte[1024];

            // Connect to a remote device.

            // Establish the remote endpoint for the socket.
            // This example uses port 11000 on the local computer.
            IPHostEntry ipHostInfo = Dns.Resolve("192.168.178.34");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 9050);

            // Create a TCP/IP  socket.
            _sender = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                _sender.Connect(remoteEP);

                Console.WriteLine("Socket connected to {0}", _sender.RemoteEndPoint.ToString());
                int channel = 16;
                // Encode the data string into a byte array.
                SendChannelMessage(channel,Resource1.SignIn);
                SendChannelMessage(channel, Resource1.SignIn);
                SendOnOffMessage(channel, false);
                //SendOnOffMessage(16, false);
                //ReceiveMessage();
                SendChannelMessage(channel, Resource1.SignOff);
                //ReceiveMessage();


                // Release the socket.
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