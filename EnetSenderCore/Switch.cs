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
            string message = Resource1.ValueSetMessageTemplate.Replace("{state}", state ? "ON" : "OFF");
            SendChannelMessage(message);
        }

    }
}