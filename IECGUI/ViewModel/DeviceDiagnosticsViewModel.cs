using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IECGUI.ViewModel
{

    public sealed class DeviceDiagnosticsViewModel : BaseViewModel
    {
        private readonly ConfigurationManagerService _configuration;
        private readonly IMultiEnergyMeterService _devices;
        private readonly McSlmpDeviceService _mcService;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialog;
        private MetersConfig _selectedDevice;
        private string _status = "Select a device and read its configured mappings.";
        private bool _isBusy;

        public ObservableCollection<MetersConfig> Devices { get; } = new();
        public ObservableCollection<DeviceTagRowViewModel> Tags { get; } = new();

        public MetersConfig SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (!SetProperty(ref _selectedDevice, value)) return;
                LoadTags();
                Status = value == null
                    ? "Select a device and read its configured mappings."
                    : $"{value.MeterName}: ready to read.";
            }
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public ICommand ReadCommand { get; }
        public ICommand WriteCommand { get; }
        public ICommand BackCommand { get; }

        public DeviceDiagnosticsViewModel(
            ConfigurationManagerService configuration,
            IMultiEnergyMeterService devices,
            McSlmpDeviceService mcService,
            INavigationService navigation,
            IDialogService dialog)
        {
            _configuration = configuration;
            _devices = devices;
            _mcService = mcService;
            _navigation = navigation;
            _dialog = dialog;

            foreach (var meter in (_configuration.Configuration.Meters ?? new())
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.MeterName))
                .OrderBy(m => m.MeterName))
            {
                Devices.Add(meter);
            }

            ReadCommand = new RelayCommand(async () => await ReadSelectedAsync());
            WriteCommand = new RelayCommand<DeviceTagRowViewModel>(async row => await WriteAsync(row));
            BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());

            SelectedDevice = Devices.FirstOrDefault();
        }

        private void LoadTags()
        {
            Tags.Clear();
            if (SelectedDevice == null) return;

            var protocol = SelectedDevice.Communication?.Protocol ?? ProtocolsType.ModbusRtu;
            foreach (var mapping in (SelectedDevice.Registers ?? new())
                .Where(r => r != null && r.IsEnabled))
            {
                Tags.Add(new DeviceTagRowViewModel(mapping, protocol));
            }

            if (Tags.Count == 0)
                Status = $"{SelectedDevice.MeterName}: no enabled mappings are configured.";
        }

        private async Task ReadSelectedAsync()
        {
            if (IsBusy || SelectedDevice == null) return;
            IsBusy = true;
            try
            {
                Status = $"Reading {SelectedDevice.MeterName}...";
                var reading = await _devices.ReadOneAsync(SelectedDevice.MeterName);
                foreach (var tag in Tags)
                {
                    tag.CurrentValue = "--";
                    if (TryGetValue(reading, tag.LookupKey, out var value))
                        tag.CurrentValue = FormatValue(value);
                }

                Status = string.IsNullOrWhiteSpace(reading.CommunicationError)
                    ? $"Read completed at {DateTime.Now:HH:mm:ss}."
                    : reading.CommunicationError;
            }
            catch (Exception ex)
            {
                Status = $"Read failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task WriteAsync(DeviceTagRowViewModel tag)
        {
            if (IsBusy || SelectedDevice == null || tag == null || !tag.CanWrite)
                return;

            if (!TryParseValue(tag, out var value, out var error))
            {
                _dialog.ShowWarning(error);
                return;
            }

            IsBusy = true;
            try
            {
                var protocol = SelectedDevice.Communication?.Protocol ?? ProtocolsType.ModbusRtu;
                if (protocol == ProtocolsType.McSlmp)
                {
                    await _mcService.WriteAsync(SelectedDevice.MeterName, tag.WriteKey, value);
                }
                else if (protocol is ProtocolsType.ModbusRtu or ProtocolsType.ModbusTcp)
                {
                    if (tag.Mapping.DataArea == ModbusDataArea.Coil)
                    {
                        await _devices.WriteCoilAsync(SelectedDevice.MeterName,
                            tag.Mapping.RegisterAddress, Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                    }
                    else if (tag.Mapping.DataArea == ModbusDataArea.HoldingRegister)
                    {
                        if (!TryGetSingleWord(tag, value, out var word, out error))
                        {
                            _dialog.ShowWarning(error);
                            return;
                        }
                        await _devices.WriteRegisterAsync(SelectedDevice.MeterName,
                            tag.Mapping.RegisterAddress, word);
                    }
                    else
                    {
                        _dialog.ShowWarning("Input registers and discrete inputs are read-only.");
                        return;
                    }
                }
                else
                {
                    _dialog.ShowWarning("Write support for OPC UA/OPC DA is not enabled yet. This mapping is read-only.");
                    return;
                }

                tag.ValueToWrite = string.Empty;
                Status = $"Write completed for {tag.ParameterName} at {DateTime.Now:HH:mm:ss}.";
                IsBusy = false;
                await ReadSelectedAsync();
            }
            catch (Exception ex)
            {
                Status = $"Write failed: {ex.Message}";
                _dialog.ShowWarning(Status);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool TryGetValue(MeterReading reading, string key, out object value)
        {
            if (reading?.Values != null && reading.Values.TryGetValue(key, out value))
                return value != null;

            var match = reading?.Values?.FirstOrDefault(x =>
                string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            value = match?.Value;
            return value != null;
        }

        private static string FormatValue(object value)
        {
            if (value is bool boolean)
                return boolean ? "ON" : "OFF";
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "--";
        }

        private static bool TryParseValue(DeviceTagRowViewModel tag, out object value, out string error)
        {
            var text = tag.ValueToWrite?.Trim() ?? string.Empty;
            error = string.Empty;
            value = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Enter a value before pressing WRITE.";
                return false;
            }

            try
            {
                if (tag.Mapping.DataType == RegisterDataType.Bool || tag.Mapping.DataArea == ModbusDataArea.Coil)
                {
                    if (text.Equals("1") || text.Equals("on", StringComparison.OrdinalIgnoreCase) || text.Equals("true", StringComparison.OrdinalIgnoreCase))
                        value = true;
                    else if (text.Equals("0") || text.Equals("off", StringComparison.OrdinalIgnoreCase) || text.Equals("false", StringComparison.OrdinalIgnoreCase))
                        value = false;
                    else throw new FormatException("Use ON/OFF, TRUE/FALSE, or 1/0 for a Boolean value.");
                    return true;
                }

                value = tag.Mapping.DataType switch
                {
                    RegisterDataType.Byte => byte.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.SByte => sbyte.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.Int16 => short.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.UInt16 => ushort.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.Int32 => int.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.UInt32 => uint.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.Int64 => long.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.UInt64 => ulong.Parse(text, CultureInfo.InvariantCulture),
                    RegisterDataType.Float => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                    RegisterDataType.Double => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                    RegisterDataType.AsciiString => text,
                    _ => short.Parse(text, CultureInfo.InvariantCulture)
                };
                return true;
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                error = $"Invalid {tag.DataType} value: {ex.Message}";
                return false;
            }
        }

        private static bool TryGetSingleWord(DeviceTagRowViewModel tag, object value, out ushort word, out string error)
        {
            word = 0;
            error = string.Empty;
            if (tag.Mapping.Length > 1 || tag.Mapping.DataType is RegisterDataType.Float or RegisterDataType.Double or RegisterDataType.Int32 or RegisterDataType.UInt32 or RegisterDataType.Int64 or RegisterDataType.UInt64)
            {
                error = "This Modbus screen currently writes one 16-bit holding register at a time. Configure a single-word integer mapping first.";
                return false;
            }

            try
            {
                word = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                error = $"Value must fit in one unsigned 16-bit register: {ex.Message}";
                return false;
            }
        }
    }

    public sealed class DeviceTagRowViewModel : ObservableObjectVM
    {
        private string _currentValue = "--";
        private string _valueToWrite = string.Empty;

        public RegisterConfig Mapping { get; }
        public ProtocolsType Protocol { get; }
        public string ParameterName => string.IsNullOrWhiteSpace(Mapping.ParameterName)
            ? $"Register {Mapping.RegisterAddress}"
            : Mapping.ParameterName.Trim();
        public string DataType => Mapping.DataType.ToString();
        public string Unit => Mapping.Unit ?? string.Empty;
        public string Address { get; }
        public string LookupKey => string.IsNullOrWhiteSpace(Mapping.ParameterName)
            ? Mapping.RegisterAddress.ToString(CultureInfo.InvariantCulture)
            : Mapping.ParameterName.Trim();
        public string WriteKey => LookupKey;
        public bool CanWrite { get; }

        public string CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        public string ValueToWrite
        {
            get => _valueToWrite;
            set => SetProperty(ref _valueToWrite, value);
        }

        public DeviceTagRowViewModel(RegisterConfig mapping, ProtocolsType protocol)
        {
            Mapping = mapping;
            Protocol = protocol;
            Address = protocol switch
            {
                ProtocolsType.McSlmp => $"{mapping.McDevice}{mapping.RegisterAddress}",
                ProtocolsType.OpcUa or ProtocolsType.OpcDa => mapping.Address ?? "(not configured)",
                _ => $"{mapping.DataArea} {mapping.RegisterAddress}"
            };
            CanWrite = protocol == ProtocolsType.McSlmp
                ? mapping.McAccess != McAccessMode.Read
                : protocol is ProtocolsType.ModbusRtu or ProtocolsType.ModbusTcp
                    && mapping.DataArea is ModbusDataArea.Coil or ModbusDataArea.HoldingRegister;
        }
    }
}