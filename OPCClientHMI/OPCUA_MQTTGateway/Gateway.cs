using HiveMQtt.Client;
using System.Windows;
using uPLibrary.Networking.M2Mqtt;

namespace OPCClientHMI.OPCUA_MQTTGateway
{
    public class Gateway
    {
        public static MqttClient client = new MqttClient("20.215.243.169");

        public static void Connect()
        {
            string clientID = Guid.NewGuid().ToString();

            client.Connect(clientID);

            if (client.IsConnected) MessageBox.Show("Polaczono z clusterem hivemq");
        }
    }
}
