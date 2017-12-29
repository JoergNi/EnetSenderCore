using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace EnetSenderCore
{
    [DataContract]
    public class EnetCommandMessage
    {
        //{"CMD":"ITEM_VALUE_SIGN_IN_REQ","PROTOCOL":"0.03","ITEMS":[{channel}],"TIMESTAMP":"1421948265"}
        [DataMember(Name = "CMD")]
        public string Command { get; set; }

        [DataMember(Name = "PROTOCOL")]
        public string Protocol { get { return "0.03"; } }


        [DataMember(Name = "TIMESTAMP")]
        public string Timestamp { get { return "1421948265"; } }

        [DataMember(Name = "ITEMS", IsRequired = false)]
        public virtual IList<int> Items { get { return new List<int> { Channel }; } }

        public int Channel { get; set; }


        public string GetMessageString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })
                            + "\r\n\r\n";
        }
    }

    [DataContract]
    public class EnetOnOffMessage : EnetCommandMessage
    {
        public EnetOnOffMessage()
        {
            Command = "ITEM_VALUE_SET";
        }
        // [DataMember(Name = "ITEMS")]
        public override IList<int> Items { get { return null; } }

        public bool On { get; set; }
        // {"NUMBER":{channel},"STATE":"{state}"}

        [DataMember(Name = "VALUES")]
        public IList<SwitchState> Values { get { return new List<SwitchState> { new SwitchState { Number = Channel, State = On ? "ON" : "OFF" } }; } }
    }

    [DataContract]
    public class EnetBlindsMessage : EnetCommandMessage
    {

        //"VALUES":[{"STATE":"VALUE_BLINDS","VALUE":{value},"NUMBER":{channel}}

        public EnetBlindsMessage()
        {
            Command = "ITEM_VALUE_SET";
        }
        // [DataMember(Name = "ITEMS")]
        public override IList<int> Items { get { return null; } }

        public int Value { get; set; }
        // {"NUMBER":{channel},"STATE":"{state}"}

        [DataMember(Name = "VALUES")]
        public IList<BlindState> Values { get { return new List<BlindState> { new BlindState { Number = Channel, Value = Value } }; } }
    }


    [DataContract]
    public class BlindState
    {
        [DataMember(Name = "NUMBER")]
        public int Number { get; set; }

        [DataMember(Name = "STATE")]
        public string State        {            get { return "VALUE_BLINDS"; }        }


        [DataMember(Name = "VALUE")]
        public int Value { get; set; }
    }


    [DataContract]
    public class SwitchState
    {
        [DataMember(Name = "NUMBER")]
        public int Number { get; set; }

        [DataMember(Name = "STATE")]
        public string State { get; set; }
    }


}