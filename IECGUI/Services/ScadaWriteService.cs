using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.ViewModel;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace IECGUI.Services;

public sealed class ScadaWriteService
{
    private readonly ConfigurationManagerService _configuration;
    private readonly IMultiEnergyMeterService _devices;
    private readonly McSlmpDeviceService _mc;

    public ScadaWriteService(ConfigurationManagerService configuration, IMultiEnergyMeterService devices, McSlmpDeviceService mc)
    {
        _configuration = configuration;
        _devices = devices;
        _mc = mc;
    }

    public async Task WriteToggleAsync(ScadaWidgetViewModel widget, bool desired)
    {
        if (widget == null || string.IsNullOrWhiteSpace(widget.DeviceName) || string.IsNullOrWhiteSpace(widget.ParameterName))
            throw new InvalidOperationException("Configure a device and parameter before using this button.");

        var meter = _configuration.Configuration.Meters?.FirstOrDefault(x =>
            string.Equals(x.MeterName, widget.DeviceName, StringComparison.OrdinalIgnoreCase));
        var mapping = meter?.Registers?.FirstOrDefault(x =>
            string.Equals(x.ParameterName, widget.ParameterName, StringComparison.OrdinalIgnoreCase));
        if (mapping == null && ushort.TryParse(widget.ParameterName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var address))
            mapping = meter?.Registers?.FirstOrDefault(x => x.RegisterAddress == address);
        if (meter == null || mapping == null)
            throw new InvalidOperationException("The selected parameter mapping was not found.");

        var protocol = meter.Communication?.Protocol ?? ProtocolsType.ModbusRtu;
        switch (protocol)
        {
            case ProtocolsType.McSlmp:
                if (mapping.McAccess == McAccessMode.Read)
                    throw new InvalidOperationException("This MC/SLMP mapping is read-only.");
                await _mc.WriteAsync(meter.MeterName, string.IsNullOrWhiteSpace(mapping.ParameterName)
                    ? mapping.RegisterAddress.ToString(CultureInfo.InvariantCulture)
                    : mapping.ParameterName, desired).ConfigureAwait(false);
                break;
            case ProtocolsType.ModbusRtu:
            case ProtocolsType.ModbusTcp:
                if (mapping.DataArea == ModbusDataArea.Coil)
                    await _devices.WriteCoilAsync(meter.MeterName, mapping.RegisterAddress, desired).ConfigureAwait(false);
                else if (mapping.DataArea == ModbusDataArea.HoldingRegister)
                    await _devices.WriteRegisterAsync(meter.MeterName, mapping.RegisterAddress, (ushort)(desired ? 1 : 0)).ConfigureAwait(false);
                else
                    throw new InvalidOperationException("Only coils and holding registers can be written from a SCADA button.");
                break;
            default:
                throw new InvalidOperationException("Write buttons currently support Modbus and MC/SLMP mappings only.");
        }
    }
}
