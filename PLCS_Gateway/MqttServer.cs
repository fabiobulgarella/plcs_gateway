using MQTTnet;
using MQTTnet.Server;
using System;
using System.Text;

namespace PLCS_Gateway
{
    class MqttServer
    {
        private readonly IMqttServerOptions serverOptions;
        private IMqttServer mqttServer;

        public static bool IsClientConnected = false;

        public MqttServer()
        {
            Console.WriteLine("Initializing MQTT Server...");

            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpointPort(1883);

            serverOptions = optionsBuilder.Build();

            mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.ClientConnected += MqttServer_ClientConnected;
            mqttServer.ClientDisconnected += MqttServer_ClientDisconnected;
            mqttServer.ClientSubscribedTopic += MqttServer_ClientSubscribedTopic;
            mqttServer.ClientUnsubscribedTopic += MqttServer_ClientUnsubscribedTopic;
            mqttServer.ApplicationMessageReceived += MqttServer_ApplicationMessageReceived;

            // Start server
            Start();
        }

        public IMqttServer GetServer()
        {
            return mqttServer;
        }

        public async void Start()
        {
            await mqttServer.StartAsync(serverOptions);
            Console.WriteLine("Broker started!");
        }

        public async void Stop()
        {
            await mqttServer.StopAsync();
            Console.WriteLine("Broker stopped!");
        }

        private void MqttServer_ClientConnected(object sender, MqttClientConnectedEventArgs e)
        {
            IsClientConnected = true;
            Console.WriteLine("Connesso il client -> " + e.Client.ClientId);
        }

        private void MqttServer_ClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            IsClientConnected = false;
            Console.WriteLine("Disconnesso il client -> " + e.Client.ClientId);
        }

        private void MqttServer_ClientSubscribedTopic(object sender, MqttClientSubscribedTopicEventArgs e)
        {
            Console.WriteLine("Sottoscritto -> " + e.TopicFilter.Topic + " dal client -> " + e.ClientId);
        }

        private void MqttServer_ClientUnsubscribedTopic(object sender, MqttClientUnsubscribedTopicEventArgs e)
        {
            Console.WriteLine("Sottoscrizione annullata -> " + e.TopicFilter + " dal client -> " + e.ClientId);
        }

        private void MqttServer_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic != "FEZ49/measurements") return;

            Console.WriteLine();
            Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
            Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
            Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
        }
    }
}
