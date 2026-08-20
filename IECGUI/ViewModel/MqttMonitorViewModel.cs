using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace IECGUI.ViewModel;

public sealed class MqttMonitorViewModel : BaseViewModel
{
    private readonly IMqttClientService _mqttService;
    private readonly MqttConfigurationService _configurationService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private string _statusMessage = "Disconnected — open Configuration to connect.";
    private bool _isConnected;
    private string _selectedPayloadFormat = "Json";
    private string _messageFilter = string.Empty;
    private string _publishTopic = string.Empty;
    private string _publishPayload = string.Empty;
    private int _publishQualityOfService;
    private bool _publishRetain;
    private MqttSubscriptionConfig? _selectedSubscription;
    private MqttFieldMapping? _selectedFieldMapping;

    public MqttConfiguration Configuration => _configurationService.Current;
    public ObservableCollection<MqttSubscriptionConfig> Subscriptions { get; } = new();
    public ObservableCollection<MqttFieldMapping> FieldMappings { get; } = new();
    public ObservableCollection<MqttReadingRow> LiveReadings { get; } = new();
    public ObservableCollection<MqttMessageRecord> MessageHistory { get; } = new();
    public ObservableCollection<string> PayloadFormats { get; } = new(new[] { "PlainText", "Json", "Hex", "Base64" });
    public ObservableCollection<int> QualityOfServiceLevels { get; } = new(new[] { 0, 1, 2 });
    public ICollectionView FilteredMessageHistory { get; }

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedPayloadFormat
    {
        get => _selectedPayloadFormat;
        set
        {
            if (!SetProperty(ref _selectedPayloadFormat, value)) return;
            foreach (var item in MessageHistory) item.SetFormat(value);
        }
    }

    public string MessageFilter
    {
        get => _messageFilter;
        set
        {
            if (!SetProperty(ref _messageFilter, value)) return;
            FilteredMessageHistory.Refresh();
        }
    }

    public string PublishTopic { get => _publishTopic; set => SetProperty(ref _publishTopic, value); }
    public string PublishPayload { get => _publishPayload; set => SetProperty(ref _publishPayload, value); }
    public int PublishQualityOfService { get => _publishQualityOfService; set => SetProperty(ref _publishQualityOfService, value); }
    public bool PublishRetain { get => _publishRetain; set => SetProperty(ref _publishRetain, value); }

    public MqttSubscriptionConfig? SelectedSubscription
    {
        get => _selectedSubscription;
        set => SetProperty(ref _selectedSubscription, value);
    }

    public MqttFieldMapping? SelectedFieldMapping
    {
        get => _selectedFieldMapping;
        set => SetProperty(ref _selectedFieldMapping, value);
    }

    public ICommand BackCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SaveConfigurationCommand { get; }
    public ICommand BrowseCertificateCommand { get; }
    public ICommand AddSubscriptionCommand { get; }
    public ICommand RemoveSubscriptionCommand { get; }
    public ICommand AddFieldMappingCommand { get; }
    public ICommand RemoveFieldMappingCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand ClearHistoryCommand { get; }

    public MqttMonitorViewModel(
        INavigationService navigation,
        IMqttClientService mqttService,
        MqttConfigurationService configurationService,
        IDialogService dialog)
    {
        _navigation = navigation;
        _mqttService = mqttService;
        _configurationService = configurationService;
        _dialog = dialog;

        foreach (var subscription in Configuration.Subscriptions)
            Subscriptions.Add(subscription);
        foreach (var mapping in Configuration.FieldMappings)
            FieldMappings.Add(mapping);
        SelectedPayloadFormat = PayloadFormats.Contains(Configuration.PayloadFormat)
            ? Configuration.PayloadFormat
            : "Json";

        FilteredMessageHistory = CollectionViewSource.GetDefaultView(MessageHistory);
        FilteredMessageHistory.Filter = item =>
        {
            if (item is not MqttMessageRecord record) return false;
            if (string.IsNullOrWhiteSpace(MessageFilter)) return true;
            return record.Topic.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase) ||
                   record.RawPayload.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase);
        };

        BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        ConnectCommand = new RelayCommand(async () => await ConnectAsync());
        DisconnectCommand = new RelayCommand(async () => await DisconnectAsync());
        SaveConfigurationCommand = new RelayCommand(SaveConfiguration);
        BrowseCertificateCommand = new RelayCommand(BrowseCertificate);
        AddSubscriptionCommand = new RelayCommand(AddSubscription);
        RemoveSubscriptionCommand = new RelayCommand(RemoveSubscription);
        AddFieldMappingCommand = new RelayCommand(AddFieldMapping);
        RemoveFieldMappingCommand = new RelayCommand(RemoveFieldMapping);
        PublishCommand = new RelayCommand(async () => await PublishAsync());
        ClearHistoryCommand = new RelayCommand(() =>
        {
            MessageHistory.Clear();
            LiveReadings.Clear();
        });

        _mqttService.OnMessageReceived += OnMessageReceived;
    }

    private void SaveConfiguration()
    {
        Configuration.Subscriptions = Subscriptions.ToList();
        Configuration.FieldMappings = FieldMappings.ToList();
        Configuration.PayloadFormat = SelectedPayloadFormat;
        if (_configurationService.Save())
            StatusMessage = "MQTT configuration saved.";
        else
            StatusMessage = "Unable to save MQTT configuration.";
    }

    private void BrowseCertificate()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select MQTT TLS certificate",
            Filter = "Certificate files (*.cer;*.crt;*.pem;*.pfx)|*.cer;*.crt;*.pem;*.pfx|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            Configuration.CertificatePath = dialog.FileName;
    }

    private void AddSubscription()
    {
        var subscription = new MqttSubscriptionConfig { Topic = "topic/#", IsEnabled = true };
        Subscriptions.Add(subscription);
        SelectedSubscription = subscription;
    }

    private void RemoveSubscription()
    {
        if (SelectedSubscription == null) return;
        Subscriptions.Remove(SelectedSubscription);
        SelectedSubscription = null;
    }

    private void AddFieldMapping()
    {
        var mapping = new MqttFieldMapping { DisplayName = $"Parameter {FieldMappings.Count + 1}" };
        FieldMappings.Add(mapping);
        SelectedFieldMapping = mapping;
    }

    private void RemoveFieldMapping()
    {
        if (SelectedFieldMapping == null) return;
        FieldMappings.Remove(SelectedFieldMapping);
        SelectedFieldMapping = null;
    }

    private async Task ConnectAsync()
    {
        try
        {
            SaveConfiguration();
            StatusMessage = "Connecting...";
            await _mqttService.ConnectAsync(
                Configuration.Host,
                Configuration.Port,
                Configuration.Username,
                Configuration.Password,
                Configuration.UseTls,
                Configuration.CertificatePath,
                Configuration.AllowUntrustedCertificates,
                Configuration.ClientId,
                Configuration.KeepAliveSeconds,
                Configuration.AutoReconnect);

            foreach (var subscription in Subscriptions.Where(s => s.IsEnabled && !string.IsNullOrWhiteSpace(s.Topic)))
                await _mqttService.SubscribeAsync(subscription.Topic);

            IsConnected = true;
            StatusMessage = $"Connected to {Configuration.DisplayName}.";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Connection failed: {ex.Message}";
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            await _mqttService.DisconnectAsync();
            IsConnected = false;
            StatusMessage = "Disconnected.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Disconnect failed: {ex.Message}";
        }
    }

    private async Task PublishAsync()
    {
        if (string.IsNullOrWhiteSpace(PublishTopic))
        {
            StatusMessage = "Enter a publish topic.";
            return;
        }

        try
        {
            await _mqttService.PublishAsync(PublishTopic, PublishPayload, PublishQualityOfService, PublishRetain);
            AddHistory(new MqttMessageRecord
            {
                Direction = "Published",
                Topic = PublishTopic,
                RawPayload = PublishPayload,
                ReceivedAt = DateTime.Now
            });
            StatusMessage = "Message published.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Publish failed: {ex.Message}";
        }
    }

    private void OnMessageReceived(object? sender, MqttMessageReceivedArgs e)
    {
        RunOnUi(() =>
        {
            AddHistory(new MqttMessageRecord
            {
                Direction = "Received",
                Topic = e.Topic,
                RawPayload = e.Payload,
                ReceivedAt = e.ReceivedAt
            });
            UpdateLiveReading(e);
        });
    }

    private void AddHistory(MqttMessageRecord record)
    {
        record.SetFormat(SelectedPayloadFormat);
        MessageHistory.Add(record);
        while (MessageHistory.Count > 200)
            MessageHistory.RemoveAt(0);
        FilteredMessageHistory.Refresh();
    }

    private void UpdateLiveReading(MqttMessageReceivedArgs message)
    {
        var mappings = FieldMappings
            .Where(m => m.IsEnabled && (string.IsNullOrWhiteSpace(m.TopicFilter) || TopicMatches(m.TopicFilter, message.Topic)))
            .ToList();
        if (mappings.Count == 0) return;

        try
        {
            using var document = JsonDocument.Parse(message.Payload);
            var row = new MqttReadingRow { Topic = message.Topic, Timestamp = message.ReceivedAt };
            foreach (var mapping in mappings)
            {
                if (TryGetJsonValue(document.RootElement, mapping.JsonPath, out var value))
                {
                    row.Fields.Add(new MqttFieldValue
                    {
                        Name = mapping.DisplayName,
                        Value = value,
                        Unit = mapping.Unit
                    });
                }
            }

            if (row.Fields.Count == 0) return;
            var existing = LiveReadings.FirstOrDefault(r => r.Topic == row.Topic);
            if (existing != null)
                LiveReadings[LiveReadings.IndexOf(existing)] = row;
            else
                LiveReadings.Add(row);
        }
        catch (JsonException)
        {
            // The message can still be inspected in the raw history.
        }
    }

    private static bool TryGetJsonValue(JsonElement root, string path, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;

        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current.ValueKind != JsonValueKind.Object) return false;
            var property = current.EnumerateObject().FirstOrDefault(p =>
                string.Equals(p.Name, segment, StringComparison.OrdinalIgnoreCase));
            if (property.Equals(default(JsonProperty))) return false;
            current = property.Value;
        }

        value = current.ValueKind == JsonValueKind.String
            ? current.GetString() ?? string.Empty
            : current.ToString();
        return true;
    }

    private static bool TopicMatches(string filter, string topic)
    {
        var filterParts = filter.Split('/');
        var topicParts = topic.Split('/');
        for (var i = 0; i < filterParts.Length; i++)
        {
            if (filterParts[i] == "#") return true;
            if (i >= topicParts.Length) return false;
            if (filterParts[i] != "+" && !string.Equals(filterParts[i], topicParts[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return filterParts.Length == topicParts.Length;
    }

    public void Cleanup()
    {
        _mqttService.OnMessageReceived -= OnMessageReceived;
        _ = _mqttService.DisconnectAsync();
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }
}

public sealed class MqttReadingRow
{
    public string Topic { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ObservableCollection<MqttFieldValue> Fields { get; } = new();
}

public sealed class MqttFieldValue
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public sealed class MqttMessageRecord : BaseViewModel
{
    private string _formattedPayload = string.Empty;

    public string Direction { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string FormattedPayload
    {
        get => _formattedPayload;
        private set => SetProperty(ref _formattedPayload, value);
    }

    public void SetFormat(string format)
    {
        FormattedPayload = format switch
        {
            "Hex" => Convert.ToHexString(Encoding.UTF8.GetBytes(RawPayload)),
            "Base64" => Convert.ToBase64String(Encoding.UTF8.GetBytes(RawPayload)),
            "Json" => FormatJson(RawPayload),
            _ => RawPayload
        };
    }

    private static string FormatJson(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return payload;
        }
    }
}
