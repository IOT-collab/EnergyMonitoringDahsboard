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
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class ConfigurationViewModel : BaseViewModel
    {
        private readonly ConfigurationManagerService _config;
        private readonly INavigationService _navigation;
        private MetersConfig _selectedMeter;
        private readonly IDialogService _dialogService;

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

        // New: Load default registers command
        public ICommand LoadDefaultRegistersCommand { get; }

        public ObservableCollection<RegisterDataType> DataTypes { get; } =
            new ObservableCollection<RegisterDataType>(
                Enum.GetValues(typeof(RegisterDataType)).Cast<RegisterDataType>());

        // Protocol list for the Protocol ComboBox
        public ObservableCollection<ProtocolsType> Protocols { get; } =
            new ObservableCollection<ProtocolsType>(
                Enum.GetValues(typeof(ProtocolsType)).Cast<ProtocolsType>());

        // New: expose enum values for WordOrder so the ComboBox can bind
        public ObservableCollection<RegisterWordOrder> WordOrder { get; } =
            new ObservableCollection<RegisterWordOrder>(
                Enum.GetValues(typeof(RegisterWordOrder)).Cast<RegisterWordOrder>());

        // New: available COM ports and refresh command
        public ObservableCollection<string> AvailableComPorts { get; } = new();
        public ICommand RefreshComPortsCommand { get; }

        // New: Baud rate choices (common values)
        public ObservableCollection<int> BaudRates { get; } = new()
        {
            1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200
        };

        public ObservableCollection<string> Parity { get; } = new()
        {
         "Even","Odd","None"
        };

        public ObservableCollection<int> DataBits { get; } = new()
        {
         7,8
        };

        public ObservableCollection<int> StopBits { get; } = new()
        {
         0,1,2
        };

        // New: Slave ID choices (1..255)
        public ObservableCollection<byte> SlaveIds { get; } = new ObservableCollection<byte>(
            Enumerable.Range(1, 255).Select(i => (byte)i));

        public ConfigurationViewModel(INavigationService navigation, ConfigurationManagerService config , IDialogService dialogService)
        {
            _config = config;
            _navigation = navigation;
            _dialogService = dialogService;
            Meters = new ObservableCollection<MetersConfig>(
                _config.Configuration.Meters);

            AddMeterCommand =
                new RelayCommand(AddMeter);

            DeleteMeterCommand =
                new RelayCommand(DeleteMeter);

            SaveCommand =
                new RelayCommand(Save);

            SaveRegisterCommand =
                new RelayCommand(Save);

            RefreshComPortsCommand = new RelayCommand(RefreshComPorts);

            // populate available ports at startup
            RefreshComPorts();

            //Register mapping Tab commands->
            AddRegisterCommand = new RelayCommand(AddRegister);
            DeleteRegisterCommand = new RelayCommand(DeleteRegister);

            // Load default registers command
            LoadDefaultRegistersCommand = new RelayCommand(LoadDefaultRegisters);

            MenuCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        }

        private void RefreshComPorts()
        {
            try
            {
                Console.WriteLine("Refreshing COM Ports...");

                var ports = SerialPort.GetPortNames();

                Console.WriteLine($"Found {ports.Length} ports");

                AvailableComPorts.Clear();

                foreach (var port in ports.OrderBy(x => x))
                {
                    Console.WriteLine(port);
                    AvailableComPorts.Add(port);
                }
            }
            catch (Exception ex)
            {
                
                _dialogService.ShowMessage(ex.ToString(), "COM Port Error");
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
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = "None",
                    SlaveId = 5,
                    StopBits = 1,
                    IpAddress = "127.0.0.1",
                    TcpPort = 502,
                    WordOrder = RegisterWordOrder.LowHigh
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

        // Load default register mapping into the selected meter (or all meters if nothing selected)
        private void LoadDefaultRegisters()
        {
            var defaults = new List<RegisterConfig>
            {
                new RegisterConfig { ParameterName = "Voltage A-N", RegisterAddress = 3020, DataType = RegisterDataType.Float, Unit = "V", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Voltage B-N", RegisterAddress = 3022, DataType = RegisterDataType.Float, Unit = "V", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Voltage C-N", RegisterAddress = 3024, DataType = RegisterDataType.Float, Unit = "V", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Voltage L-N Avg", RegisterAddress = 3026, DataType = RegisterDataType.Float, Unit = "V", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Current A", RegisterAddress = 3000, DataType = RegisterDataType.Float, Unit = "A", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Current B", RegisterAddress = 3002, DataType = RegisterDataType.Float, Unit = "A", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Current C", RegisterAddress = 3004, DataType = RegisterDataType.Float, Unit = "A", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Current Avg", RegisterAddress = 3010, DataType = RegisterDataType.Float, Unit = "A", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Total Active Power", RegisterAddress = 3060, DataType = RegisterDataType.Float, Unit = "kW", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Total Reactive Power", RegisterAddress = 3068, DataType = RegisterDataType.Float, Unit = "kVAR", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Total Apparent Power", RegisterAddress = 3076, DataType = RegisterDataType.Float, Unit = "kVA", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Frequency", RegisterAddress = 3110, DataType = RegisterDataType.Float, Unit = "Hz", ScaleFactor = 1, Length = 2, IsEnabled = true },
                new RegisterConfig { ParameterName = "Power Factor", RegisterAddress = 3084, DataType = RegisterDataType.Float, Unit = "", ScaleFactor = 1, Length = 2, IsEnabled = true }
            };

            if (SelectedMeter != null)
            {
                SelectedMeter.Registers.Clear();
                foreach (var r in defaults)
                    SelectedMeter.Registers.Add(r);
            }
            else
            {
                foreach (var meter in Meters)
                {
                    meter.Registers.Clear();
                    foreach (var r in defaults)
                        meter.Registers.Add(new RegisterConfig
                        {
                            ParameterName = r.ParameterName,
                            RegisterAddress = r.RegisterAddress,
                            DataType = r.DataType,
                            Unit = r.Unit,
                            ScaleFactor = r.ScaleFactor,
                            Length = r.Length,
                            IsEnabled = r.IsEnabled
                        });
                }
            }
        }
    }
}
