namespace EnetSenderNet
{
    public class DimmableLight : Thing
    {
        public override string ThingType => "dimmer";

        public DimmableLight(string name, int channel, IMobilegateSender sender = null) : base(name, channel, sender)
        {
        }

        public void TurnOn()  => SendDimmerMessage(100);
        public void TurnOff() => SendOnOffMessage(false);

        public void SetBrightness(int value) => SendDimmerMessage(value);  // 0-100

        public void SendOnOffMessage(bool on)
        {
            var message = new EnetOnOffMessage { Channel = Channel, On = on };
            SendCommandMessage(message.GetMessageString());
        }

        public void SendDimmerMessage(int value)
        {
            var message = new EnetDimmerMessage { Channel = Channel, Value = value };
            SendCommandMessage(message.GetMessageString());
        }
    }
}
