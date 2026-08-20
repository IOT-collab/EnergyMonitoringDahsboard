using IEC.Shared.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IEC.Shared.Services;

public class MqttClientService : IMqttClientService
{
    private readonly IMqttClient _client;
    private MqttClientOptions? _options;
    private bool _manualDisconnect;
    private bool _autoReconnect = true;

    public bool IsConnected => _client.IsConnected;

    public event EventHandler<MqttMessageReceivedArgs>? OnMessageReceived;

    public MqttClientService()
    {
        _client = new MqttFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedHandler;
        _client.DisconnectedAsync += async e =>
        {
            if (e.ClientWasConnected && !_manualDisconnect && _autoReconnect && _options != null)
            {
                await Task.Delay(3000);
                try { await _client.ConnectAsync(_options); }
                catch { /* retry can occur on a later connection attempt */ }
            }
        };
    }

    public async Task ConnectAsync(string brokerHost, int port = 1883,
        string? username = null, string? password = null,
        bool useTls = false, string? certificatePath = null,
        bool allowUntrustedCertificates = false, string? clientId = null,
        int keepAliveSeconds = 60, bool autoReconnect = true)
    {
        _autoReconnect = autoReconnect;
        var host = NormalizeHost(brokerHost, ref useTls);

        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(string.IsNullOrWhiteSpace(clientId) ? $"IECGUI_{Guid.NewGuid():N}" : clientId)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(Math.Clamp(keepAliveSeconds, 5, 3600)))
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(username))
            builder = builder.WithCredentials(username, password);

        if (useTls)
        {
            builder = builder.WithTlsOptions(tls =>
            {
                tls.UseTls();
                tls.WithAllowUntrustedCertificates(allowUntrustedCertificates);
                tls.WithIgnoreCertificateChainErrors(allowUntrustedCertificates);
                tls.WithIgnoreCertificateRevocationErrors(allowUntrustedCertificates);

                if (!string.IsNullOrWhiteSpace(certificatePath) && File.Exists(certificatePath))
                {
                    var certificate = new X509Certificate2(certificatePath);
                    tls.WithTrustChain(new X509Certificate2Collection(certificate));
                }
            });
        }

        _options = builder.Build();
        if (_client.IsConnected)
        {
            // Suppress the disconnected callback's reconnect loop while replacing options.
            _manualDisconnect = true;
            await _client.DisconnectAsync();
        }
        _manualDisconnect = false;
        await _client.ConnectAsync(_options);
    }

    public async Task DisconnectAsync()
    {
        _manualDisconnect = true;
        if (_client.IsConnected)
            await _client.DisconnectAsync();
    }

    public async Task SubscribeAsync(string topic)
    {
        if (!_client.IsConnected)
            throw new InvalidOperationException("MQTT client is not connected.");

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter.WithTopic(topic))
            .Build();
        await _client.SubscribeAsync(options);
    }

    public async Task UnsubscribeAsync(string topic)
    {
        if (!_client.IsConnected)
            return;

        var options = new MqttClientUnsubscribeOptionsBuilder()
            .WithTopicFilter(topic)
            .Build();
        await _client.UnsubscribeAsync(options);
    }

    public async Task PublishAsync(string topic, string payload, int qualityOfService = 0, bool retain = false)
    {
        if (!_client.IsConnected)
            throw new InvalidOperationException("MQTT client is not connected.");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload ?? string.Empty)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)Math.Clamp(qualityOfService, 0, 2))
            .WithRetainFlag(retain)
            .Build();
        await _client.PublishAsync(message);
    }

    private Task OnMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        OnMessageReceived?.Invoke(this, new MqttMessageReceivedArgs
        {
            Topic = e.ApplicationMessage.Topic,
            Payload = e.ApplicationMessage.ConvertPayloadToString(),
            ReceivedAt = DateTime.Now
        });
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _manualDisconnect = true;
        try { _client.DisconnectAsync().GetAwaiter().GetResult(); }
        catch { /* shutdown should not crash the application */ }
        _client.Dispose();
    }

    private static string NormalizeHost(string brokerHost, ref bool useTls)
    {
        var host = (brokerHost ?? string.Empty).Trim();
        if (host.StartsWith("mqtts://", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("ssl://", StringComparison.OrdinalIgnoreCase))
        {
            useTls = true;
            host = host[(host.IndexOf("://", StringComparison.Ordinal) + 3)..];
        }
        else if (host.StartsWith("mqtt://", StringComparison.OrdinalIgnoreCase) ||
                 host.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            host = host[(host.IndexOf("://", StringComparison.Ordinal) + 3)..];
        }
        return host.TrimEnd('/');
    }
}
