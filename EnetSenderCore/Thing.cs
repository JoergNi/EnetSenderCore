﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace EnetSenderCore
{

    public abstract class Thing
    {
        private Socket _sender;
        private byte[] _bytes = new byte[1024];
       

        public string Name { get; }
        public int Channel { get; }

        public Thing(string name, int channel)
        {
            Name = name;
            Channel = channel;
        }
       
        public void SendMessage(string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);

            // Send the data through the socket.
            int bytesSent = _sender.Send(msg);
            Console.WriteLine("Sent message = {0}", message.Trim());
        }

        public void ReceiveMessage()
        {
            // Receive the response from the remote device.
            int bytesRec = _sender.Receive(_bytes);
            Console.WriteLine("Received message = {0}", Encoding.ASCII.GetString(_bytes, 0, bytesRec).Trim());
        }


        public void ConnectAndSendMessage(Action action)
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.178.34");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 9050);

            // Create a TCP/IP  socket.
            _sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                _sender.Connect(remoteEP);
                Console.WriteLine("Socket connected to {0}", _sender.RemoteEndPoint.ToString());

                var signInMessage = new EnetCommandMessage()
                {
                    Channel = Channel,
                    Command = "ITEM_VALUE_SIGN_IN_REQ"
                };
                string signInMessageString = signInMessage.GetMessageString();

                SendMessage(signInMessageString);
                ReceiveMessage();
                SendMessage(signInMessageString);
                ReceiveMessage();

                action();                  
                ReceiveMessage();

                var signOutMessage = new EnetCommandMessage()
                {
                    Channel = Channel,
                    Command = "ITEM_VALUE_SIGN_OUT_REQ"
                };
                string signOutMessageString = signOutMessage.GetMessageString();

                SendMessage(signOutMessageString);
                ReceiveMessage();

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