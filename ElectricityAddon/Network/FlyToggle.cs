using ProtoBuf;

namespace ElectricityAddon.Network;
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class FlyToggle
    {
        public string toggle;
        public float savedspeed;
        public string savedaxis;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class FlyResponse
    {
        public string response;
    }

