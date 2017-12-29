namespace EnetSenderCore
{
    public class Switch : Thing
    {
        public Switch(string name, int channel) : base(name, channel)
        {
        }

        public void TurnOn()
        {
            ConnectAndSendMessage(() => SendOnOffMessage(true));
        }

        public void TurnOff()
        {
            ConnectAndSendMessage(() => SendOnOffMessage(false));
        }

        public void SendOnOffMessage(bool state)
        {
            var message = new EnetOnOffMessage
            {
                Channel = Channel,
                On = state
            };
            SendChannelMessage(message.GetMessageString());
        }

    }
}