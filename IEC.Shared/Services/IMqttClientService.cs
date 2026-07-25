using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    public interface IMqttClientService : IDisposable
    {
        bool IsConnected { get; }

        // Connection
        Task ConnectAsync(string brokerHost, int port = 1883,
                          string username = null, string password = null);
        Task DisconnectAsync();

        // Subscribe
        Task SubscribeAsync(string topic);
        Task UnsubscribeAsync(string topic);

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