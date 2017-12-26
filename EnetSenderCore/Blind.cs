namespace EnetSenderCore
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

        public void MoveUp()
        {
            ConnectAndSendMessage(() => SendBlindsMessage(0));
        }


        public void SendBlindsMessage(int value)
        {
            string message = Resource1.BlindsValueSetMessage.Replace("{value}", value.ToString());
            SendChannelMessage(message);
        }
    }
}