using MQTTnet;
using MQTTnet.Client;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace PLCS_Gateway
{
    class MqttClient
    {
        //private const String AMAZON_HOST = "a1nm63yhp2skng.iot.eu-west-1.amazonaws.com";
        private const String AMAZON_HOST = "a2lzki3obz1dhu.iot.eu-west-1.amazonaws.com";

        private readonly IMqttClientOptions clientOptions;
        private readonly IMqttClient mqttClient;

        private Thread connectionThread;
        private bool keepTrying;

        public MqttClient()
        {
            Console.WriteLine("Initializing MQTT Client...");

            // Create TCP based options using the builder.
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId("FEZ49-Gateway")
                .WithTcpServer(AMAZON_HOST, 8883)
                .WithProtocolVersion(MQTTnet.Serializer.MqttProtocolVersion.V311)
                .WithCommunicationTimeout(new TimeSpan(0, 0, 30))
                .WithKeepAlivePeriod(new TimeSpan(0, 0, 10))
                .WithCleanSession();

            // Get Certificates
            X509Certificate2 rootCA = new X509Certificate2("Certificates\\Root-CA.pem");
            X509Certificate2 clientCert = new X509Certificate2("Certificates\\plcs_certificate.pfx", "fez49");

            // Setup TLS connection
            optionsBuilder.WithTls(false, false, false, rootCA.Export(X509ContentType.SerializedCert), clientCert.Export(X509ContentType.SerializedCert));

            clientOptions = optionsBuilder.Build();
            mqttClient = new MqttFactory().CreateMqttClient();
            mqttClient.Connected += MqttClient_Connected;
            mqttClient.Disconnected += MqttClient_Disconnected;

            // Start connection thread
            keepTrying = true;
            StartConnectionThread();
        }

        public IMqttClient GetClient()
        {
            return mqttClient;
        }

        private void StartConnectionThread()
        {
            connectionThread = new Thread(Connect);
            connectionThread.Start();
        }

        private async void Connect()
        {
            while (keepTrying)
            {
                if (!mqttClient.IsConnected)
                {
                    Thread.Sleep(5000);

                    try
                    {
                        await mqttClient.ConnectAsync(clientOptions);
                        if (mqttClient.IsConnected)
                        {
                            await mqttClient.SubscribeAsync("FEZ49/measurements");
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("An error occurred while trying to connect to Amazon Iot Core. Trying again in few seconds...");
                    }
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }
        }

        public async void Stop()
        {
            keepTrying = false;

            if (connectionThread.IsAlive)
                connectionThread.Join();

            if (mqttClient.IsConnected)
            {
                try
                {
                    await mqttClient.DisconnectAsync();
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occurred while trying to disconnect from Amazon Iot Core.");
                }
            }
        }

        private void MqttClient_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connesso ad Amazon IOT");
        }

        private void MqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnesso da Amazon IOT");
        }
    }
}
