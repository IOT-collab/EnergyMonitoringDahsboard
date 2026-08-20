using System.Collections.Generic;

namespace IEC.Shared.Models;

public sealed class MqttConfiguration
{
    public string DisplayName { get; set; } = "MQTT Remote Meter Monitor";
    public string Host { get; set; } = "test.mosquitto.org";
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; }
    public string CertificatePath { get; set; } = string.Empty;
    public bool AllowUntrustedCertificates { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int KeepAliveSeconds { get; set; } = 60;
    public bool AutoReconnect { get; set; } = true;
    public string PayloadFormat { get; set; } = "Json";
    public List<MqttSubscriptionConfig> Subscriptions { get; set; } = new()
    {
        new MqttSubscriptionConfig()
    };
    public List<MqttFieldMapping> FieldMappings { get; set; } = new();
}

public sealed class MqttSubscriptionConfig
{
    public string Topic { get; set; } = "testtopic/#";
    public int QualityOfService { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class MqttFieldMapping
{
    public string DisplayName { get; set; } = "Parameter";
    public string JsonPath { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string TopicFilter { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
