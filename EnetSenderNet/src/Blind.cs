namespace EnetSenderNet
{
    public class Blind : Thing
    {
        public override string ThingType => "blind";

        public Blind(string name, int channel, IMobilegateSender sender = null) : base(name, channel, sender)
        {
        }

        public void MoveDown()   => SendBlindsMessage(100);
        public void MoveHalf()   => SendBlindsMessage(50);
        public void MoveThreeQuarters() => SendBlindsMessage(75);
        public void MoveUp()     => SendBlindsMessage(0);
        public void MoveTo(int value)   => SendBlindsMessage(value);

        public void SendBlindsMessage(int value)
        {
            var message = new EnetBlindsMessage { Channel = Channel, Value = value };
            SendCommandMessage(message.GetMessageString());
        }
    }
}