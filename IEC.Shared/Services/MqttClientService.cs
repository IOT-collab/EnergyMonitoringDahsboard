using MQTTnet;
using MQTTnet.Client;
using IEC.Shared.Models;
using System;
using System.Threading.Tasks;

namespace IEC.Shared.Services
{
    public class MqttClientService : IMqttClientService
    {
        private IMqttClient _client;
        private MqttClientOptions _options;

        public bool IsConnected => _client?.IsConnected ?? false;

        public event EventHandler<MqttMessageReceivedArgs> OnMessageReceived;

        public MqttClientService()
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _client.ApplicationMessageReceivedAsync += OnMessageReceivedHandler;

            // Auto reconnect
            _client.DisconnectedAsync += async e =>
            {
                if (e.ClientWasConnected)
                {
                    await Task.Delay(3000);
                    try { await _client.ConnectAsync(_options); }
                    catch { }
                }
            };
        }

        public async Task ConnectAsync(string brokerHost, int port = 1883,
                                        string username = null, string password = null)
        {
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerHost, port)
                .WithClientId($"IECGUI_{Guid.NewGuid()}")
                .WithCleanSession();

            if (!string.IsNullOrEmpty(username))
                builder = builder.WithCredentials(username, password);

            _options = builder.Build();

            await _client.ConnectAsync(_options);
        }

        public async Task SubscribeAsync(string topic)
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic))
                .Build();

            await _client.SubscribeAsync(subscribeOptions);
        }

        public async Task UnsubscribeAsync(string topic)
        {
            var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter(topic)
                .Build();

            await _client.UnsubscribeAsync(unsubscribeOptions);
        }

        private Task OnMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = e.ApplicationMessage.ConvertPayloadToString();

            OnMessageReceived?.Invoke(this, new MqttMessageReceivedArgs
            {
                Topic = topic,
                Payload = payload,
                ReceivedAt = DateTime.Now
            });

            return Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            if (_client.IsConnected)
                await _client.DisconnectAsync();
        }

        public void Dispose()
        {
            _client?.DisconnectAsync().Wait();
            _client?.Dispose();
        }
    }
}