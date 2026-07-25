using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class ConfigurationViewModel : BaseViewModel
    {
        private readonly ConfigurationManagerService _config;

        private readonly INavigationService _navigation;

        private MetersConfig _selectedMeter;
        
        public MetersConfig SelectedMeter
        {
            get => _selectedMeter;
            set => SetProperty(ref _selectedMeter, value);
        }

        private RegisterConfig _selectedRegister;

        public RegisterConfig SelectedRegister
        {
            get => _selectedRegister;
            set => SetProperty(ref _selectedRegister, value);
        }

        public ObservableCollection<RegisterConfig> Registers => SelectedMeter?.Registers;

        public ObservableCollection<MetersConfig> Meters { get; }

        public ICommand MenuCommand { get; }

        public ICommand AddMeterCommand { get; }
        public ICommand DeleteMeterCommand { get; }
        public ICommand SaveCommand { get; }

        public ICommand AddRegisterCommand { get; }
        public ICommand DeleteRegisterCommand { get; }

        public ICommand EditRegisterCommand { get; }

        public ICommand SaveRegisterCommand { get; }

        public ObservableCollection<RegisterDataType> DataTypes { get; } =
            new ObservableCollection<RegisterDataType>(
                Enum.GetValues(typeof(RegisterDataType)).Cast<RegisterDataType>());

        // Protocol list for the Protocol ComboBox
        public ObservableCollection<ProtocolsType> Protocols { get; } =
            new ObservableCollection<ProtocolsType>(
                Enum.GetValues(typeof(ProtocolsType)).Cast<ProtocolsType>());

        // New: available COM ports and refresh command
        public ObservableCollection<string> AvailableComPorts { get; } = new();
        public ICommand RefreshComPortsCommand { get; }

        // New: Baud rate choices (common values)
        public ObservableCollection<int> BaudRates { get; } = new()
        {
            1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200
        };

        // New: Slave ID choices (1..255)
        public ObservableCollection<byte> SlaveIds { get; } = new ObservableCollection<byte>(
            Enumerable.Range(1, 255).Select(i => (byte)i));

        public ConfigurationViewModel(INavigationService navigation, ConfigurationManagerService config)
        {
            _config =config;
            _navigation = navigation;
            Meters = new ObservableCollection<MetersConfig>(
                _config.Configuration.Meters);

            AddMeterCommand =
                new RelayCommand(AddMeter);

            DeleteMeterCommand =
                new RelayCommand(DeleteMeter);

            SaveCommand =
                new RelayCommand(Save);

            RefreshComPortsCommand = new RelayCommand(RefreshComPorts);

            // populate available ports at startup
            RefreshComPorts();

            //Register mapping Tab commands->

            AddRegisterCommand = new RelayCommand(AddRegister);

            DeleteRegisterCommand = new RelayCommand(DeleteRegister);
                  

            MenuCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        }

        private void RefreshComPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames()
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                AvailableComPorts.Clear();
                foreach (var p in ports)
                    AvailableComPorts.Add(p);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshComPorts error: {ex.Message}");
            }
        }

        // Add Communincation Confiuration//
        private void AddMeter()
        {
            var defaultPort = AvailableComPorts.FirstOrDefault() ?? "COM1";

            var meter = new MetersConfig()
            {
                MeterId = Meters.Count + 1,
                MeterName = $"Meter-{Meters.Count + 1}",
                Communication = new CommunicationConfig() 
                { 
                    Protocol = ProtocolsType.ModbusRtu,
                    ComPort = defaultPort, 
                    BaudRate = 19200, 
                    DataBits = 8, 
                    Parity = "Even", 
                    SlaveId = 10, 
                    StopBits = 1,
                    IpAddress = "127.0.0.1",
                    TcpPort = 502
                }
            };

            Meters.Add(meter);

            SelectedMeter = meter;
        }


        private void DeleteMeter()
        {
            if (SelectedMeter == null)
                return;

            Meters.Remove(SelectedMeter);
        }

        private void Save()
        {
            _config.Configuration.Meters.Clear();

            foreach (var meter in Meters)
                _config.Configuration.Meters.Add(meter);

            _config.Save();
        }

        //Add Register Mapping//

        private void AddRegister()
        {
            if (SelectedMeter == null)
                return;

            var reg = new RegisterConfig()
            {
                ParameterName = "Voltage A-N",
                RegisterAddress = 3020,
                DataType = RegisterDataType.Float, // use enum
                Unit = "V",
                ScaleFactor = 1,
                Length = 2,
                IsEnabled = true
            };

            SelectedMeter.Registers.Add(reg);

            SelectedRegister = reg;
        }

        private void DeleteRegister()
        {
            if (SelectedRegister == null)
                return;

            SelectedMeter.Registers.Remove(SelectedRegister);
        }
    }
}
