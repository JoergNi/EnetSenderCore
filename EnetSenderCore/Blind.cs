﻿namespace EnetSenderCore
{
    public class Blind : Thing
    {
        public Blind(string name, int channel) : base(name, channel)
        {
        }

        public void MoveDown()
        {
            ConnectAndSendMessage(() => SendBlindsMessage(100));
        }

        public void MoveHalf()
        {
            ConnectAndSendMessage(() => SendBlindsMessage(50));
        }

        public void MoveThreeQuarters()
        {
            ConnectAndSendMessage(() => SendBlindsMessage(75));
        }

        public void MoveUp()
        {
            ConnectAndSendMessage(() => SendBlindsMessage(0));
        }

        public void SendBlindsMessage(int value)
        {
            var message = new EnetBlindsMessage
            {
                Channel = Channel,
                Value = value
            };
            SendMessage(message.GetMessageString());
        }
    }
}