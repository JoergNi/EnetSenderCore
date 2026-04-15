namespace EnetSenderNet
{
    public class Switch : Thing
    {
        public override string ThingType => "switch";

        public Switch(string name, int channel, IMobilegateSender sender = null) : base(name, channel, sender)
        {
        }

        public void TurnOn()  => SendOnOffMessage(true);
        public void TurnOff() => SendOnOffMessage(false);

        public void SendOnOffMessage(bool state)
        {
            var message = new EnetOnOffMessage { Channel = Channel, On = state };
            SendCommandMessage(message.GetMessageString());
        }
    }
}