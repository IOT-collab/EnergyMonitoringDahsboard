using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    public interface IMqttClientService : IDisposable
    {
        bool IsConnected { get; }

        // Connection
        Task ConnectAsync(string brokerHost, int port = 1883,
                          string username = null, string password = null,
                          bool useTls = false, string certificatePath = null,
                          bool allowUntrustedCertificates = false,
                          string clientId = null, int keepAliveSeconds = 60,
                          bool autoReconnect = true);
        Task DisconnectAsync();

        // Subscribe
        Task SubscribeAsync(string topic);
        Task UnsubscribeAsync(string topic);
        Task PublishAsync(string topic, string payload, int qualityOfService = 0, bool retain = false);

        // Event — jab bhi message aaye, ViewModel ko notify karo
        event EventHandler<MqttMessageReceivedArgs> OnMessageReceived;
    }

    // Event args
    public class MqttMessageReceivedArgs : EventArgs
    {
        public string Topic { get; set; }
        public string Payload { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
