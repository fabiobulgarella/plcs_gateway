using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PLCS_Gateway
{
    class Bridge
    {
        private readonly IMqttClient mqttClient;
        private readonly IMqttServer mqttServer;
        private readonly ConcurrentQueue<String> ackQueue;
        private readonly ConcurrentQueue<MqttApplicationMessage> messageQueue;

        private Thread senderThread;
        private Thread notifyerThread;

        private bool shouldStop;

        public Bridge(IMqttClient client, IMqttServer server)
        {
            shouldStop = false;
            mqttClient = client;
            mqttServer = server;

            ackQueue = new ConcurrentQueue<String>();
            messageQueue = new ConcurrentQueue<MqttApplicationMessage>();

            mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;
            mqttServer.ApplicationMessageReceived += MqttServer_ApplicationMessageReceived;

            senderThread = new Thread(SenderWorker);
            notifyerThread = new Thread(NotifyerWorker);
            senderThread.Start();
            notifyerThread.Start();
        }

        public void Stop()
        {
            shouldStop = true;
        }

        private async void SenderWorker()
        {
            while (!shouldStop)
            {
                if (mqttClient.IsConnected)
                {
                    if (messageQueue.TryDequeue(out MqttApplicationMessage message))
                    {
                        if (await SendToAmazonAsync(message))
                        {
                            String msgName = (message.Topic == "FEZ49/config") ? "configuration" : GetMeasureName(message.Payload);
                            Console.WriteLine("Message \"" + msgName + "\" sent correctly to AWS IoT");
                        }
                        else
                            messageQueue.Enqueue(message);
                    }
                }

                Thread.Sleep(800);
            }
        }

        private async void NotifyerWorker()
        {
            while (!shouldStop)
            {
                if (MqttServer.IsClientConnected)
                {
                    if (ackQueue.TryDequeue(out String message))
                    {
                        if (await SendToFez(message))
                            Console.WriteLine("Ack for message \"" + message + "\" sent correctly to FEZ");
                        else
                            ackQueue.Enqueue(message);
                    }
                }

                Thread.Sleep(800);
            }
        }

        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic != "FEZ49/measurements") return;

            String measureName = GetMeasureName(e.ApplicationMessage.Payload);

            if (measureName != null)
            {
                ackQueue.Enqueue(measureName);
            }
        }

        private void MqttServer_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == "FEZ49/measurements" || e.ApplicationMessage.Topic == "FEZ49/config")
            {
                messageQueue.Enqueue(e.ApplicationMessage);
            }
        }

        private async Task<bool> SendToAmazonAsync(MqttApplicationMessage message)
        {
            message.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;

            try
            {
                await mqttClient.PublishAsync(message);
                return true;
            }
            catch
            {
                Console.WriteLine("An error occurred while publishing a message to Amazon IoT!");
                return false;
            }
        }

        private async Task<bool> SendToFez(String message)
        {
            try
            {
                await mqttServer.PublishAsync("FEZ49/acknowledgments", message, MqttQualityOfServiceLevel.ExactlyOnce);
                return true;
            }
            catch
            {
                Console.WriteLine("An error occurred while publishing a message to FEZ!");
                return false;
            }
        }

        private String GetMeasureName(byte[] payload)
        {
            String fileData = Encoding.UTF8.GetString(payload);
            int index = fileData.IndexOf("iso_timestamp");

            if (index != -1)
            {
                String timestamp = fileData.Substring(index + 17, 19);
                String[] splitted = timestamp.Split('T');
                String[] date = splitted[0].Split('-');
                String[] time = splitted[1].Split(':');
                return date[0] + date[1] + date[2] + "T" + time[0] + time[1] + time[2];
            }

            return null;
        }
    }
}
