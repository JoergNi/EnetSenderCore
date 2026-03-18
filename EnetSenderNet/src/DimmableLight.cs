namespace EnetSenderNet
{
    public class DimmableLight : Thing
    {
        public override string ThingType => "dimmer";

        public DimmableLight(string name, int channel) : base(name, channel)
        {
        }

        public void TurnOn()
        {
            ConnectAndSendMessage(() => SendDimmerMessage(100));
        }

        public void TurnOff()
        {
            ConnectAndSendMessage(() => SendOnOffMessage(false));
        }

        public void SendOnOffMessage(bool on)
        {
            var message = new EnetOnOffMessage
            {
                Channel = Channel,
                On = on
            };
            SendMessage(message.GetMessageString());
        }

        public void SetBrightness(int value)  // 0-100
        {
            ConnectAndSendMessage(() => SendDimmerMessage(value));
        }

        public void SendDimmerMessage(int value)
        {
            var message = new EnetDimmerMessage
            {
                Channel = Channel,
                Value = value
            };
            SendMessage(message.GetMessageString());
        }
    }
}
