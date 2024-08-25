using Opc.Ua;

namespace OPCClientHMI.Model.OPCCommunication
{
    public class ReadDataModel
    {
        public NodeId NodeID { get; set; }
        public uint AttributeID { get; set; }

        public string Value { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
