// IECGUI/ViewModel/IecConfigViewModel.cs
using IEC.Shared.IECModels;
using IEC.Shared.IECServices;
using IEC61850.Server;
using IECGUI.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class IecConfigViewModel : BaseViewModel
    {
        private readonly IecConfigManagerService _configManager;
        private readonly INavigationService _navigation;

        // ── Selected Items ─────────────────────────────────────
        private RelayConfig _selectedRelay;
        public RelayConfig SelectedRelay
        {
            get => _selectedRelay;
            set
            {
                _selectedRelay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LogicalNodes));
                OnPropertyChanged(nameof(Mappings));
            }
        }

        private LnConfig _selectedLn;
        public LnConfig SelectedLn
        {
            get => _selectedLn;
            set
            {
                _selectedLn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Mappings));
            }
        }

        private DoMappingConfig _selectedMapping;
        public DoMappingConfig SelectedMapping
        {
            get => _selectedMapping;
            set { _selectedMapping = value; OnPropertyChanged(); }
        }

        // ── Collections ────────────────────────────────────────
        public ObservableCollection<RelayConfig> Relays { get; }
            = new ObservableCollection<RelayConfig>();

        public ObservableCollection<LnConfig> LogicalNodes =>
            SelectedRelay != null
                ? new ObservableCollection<LnConfig>(SelectedRelay.LogicalNodes)
                : new ObservableCollection<LnConfig>();

        public ObservableCollection<DoMappingConfig> Mappings =>
            SelectedLn != null
                ? new ObservableCollection<DoMappingConfig>(SelectedLn.Mappings)
                : new ObservableCollection<DoMappingConfig>();

        // ── Dropdown Options ───────────────────────────────────
        public List<string> FcOptions { get; } = new() { "MX", "ST", "CF", "DC" };
        public List<IEC.Shared.IECModels.ValueType> ValueTypeOptions { get; } = new()
        {
            IEC.Shared.IECModels.ValueType.Direct,
            IEC.Shared.IECModels.ValueType.Phase
        };

        // ── Commands ───────────────────────────────────────────
        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }

        // Relay commands
        public ICommand AddRelayCommand { get; }
        public ICommand DeleteRelayCommand { get; }

        // LN commands
        public ICommand AddLnCommand { get; }
        public ICommand DeleteLnCommand { get; }

        // Mapping commands
        public ICommand AddMappingCommand { get; }
        public ICommand DeleteMappingCommand { get; }
        public ICommand LoadDefaultMappingsCommand { get; }

        public IecConfigViewModel(
            INavigationService navigation,
            IecConfigManagerService configManager)
        {
            _navigation = navigation;
            _configManager = configManager;

            BackCommand = new RelayCommand(() => _navigation.NavigateTo<Iec61850MonitorViewModel>());
            SaveCommand = new RelayCommand(Save);

            AddRelayCommand = new RelayCommand(AddRelay);
            DeleteRelayCommand = new RelayCommand(DeleteRelay);

            AddLnCommand = new RelayCommand(AddLn);
            DeleteLnCommand = new RelayCommand(DeleteLn);

            AddMappingCommand = new RelayCommand(AddMapping);
            DeleteMappingCommand = new RelayCommand(DeleteMapping);
            LoadDefaultMappingsCommand = new RelayCommand(LoadDefaultMappings);

            LoadConfig();
        }

        // ── Load ───────────────────────────────────────────────
        private void LoadConfig()
        {
            var config = _configManager.Load();
            Relays.Clear();
            foreach (var relay in config.Relays)
                Relays.Add(relay);

            SelectedRelay = Relays.FirstOrDefault();
            SelectedLn = SelectedRelay?.LogicalNodes.FirstOrDefault();
        }

        // ── Save ───────────────────────────────────────────────
        private void Save()
        {
            var config = new IecConfigRoot
            {
                Relays = Relays.ToList()
            };
            _configManager.Save(config);
        }

        // ── Relay Operations ───────────────────────────────────
        private void AddRelay()
        {
            var relay = new RelayConfig
            {
                RelayId = Relays.Count + 1,
                RelayName = $"Relay-{Relays.Count + 1}",
                IPAddress = "192.168.1.1",
                Port = 102,
                IsEnabled = true,
                PollIntervalMs = 1000,
                LogicalNodes = new List<LnConfig>()
            };
            Relays.Add(relay);
            SelectedRelay = relay;
        }

        private void DeleteRelay()
        {
            if (SelectedRelay == null) return;
            Relays.Remove(SelectedRelay);
            SelectedRelay = Relays.FirstOrDefault();
        }

        // ── Logical Node Operations ────────────────────────────
        private void AddLn()
        {
            if (SelectedRelay == null) return;

            var ln = new LnConfig
            {
                LogicalDevice = "IED_MEAS",
                LogicalNode = "MMXU1",
                FC = "MX",
                Mappings = new List<DoMappingConfig>()
            };

            SelectedRelay.LogicalNodes.Add(ln);
            OnPropertyChanged(nameof(LogicalNodes));
            SelectedLn = ln;
        }

        private void DeleteLn()
        {
            if (SelectedRelay == null || SelectedLn == null) return;
            SelectedRelay.LogicalNodes.Remove(SelectedLn);
            OnPropertyChanged(nameof(LogicalNodes));
            SelectedLn = SelectedRelay.LogicalNodes.FirstOrDefault();
        }

        // ── Mapping Operations ─────────────────────────────────
        private void AddMapping()
        {
            if (SelectedLn == null) return;

            var mapping = new DoMappingConfig
            {
                ParameterName = "NewParameter",
                DOIndex = 0,
                ValueType = IEC.Shared.IECModels.ValueType.Direct,
                PhaseIndex = -1,
                Unit = "",
                IsEnabled = true
            };

            SelectedLn.Mappings.Add(mapping);
            OnPropertyChanged(nameof(Mappings));
            SelectedMapping = mapping;
        }

        private void DeleteMapping()
        {
            if (SelectedLn == null || SelectedMapping == null) return;
            SelectedLn.Mappings.Remove(SelectedMapping);
            OnPropertyChanged(nameof(Mappings));
            SelectedMapping = SelectedLn.Mappings.FirstOrDefault();
        }

        private void LoadDefaultMappings()
        {
            if (SelectedLn == null) return;

            var defaults = IecDefaultConfig.GetDefault()
                                           .LogicalNodes
                                           .FirstOrDefault()
                                           ?.Mappings ?? new List<DoMappingConfig>();

            SelectedLn.Mappings.Clear();
            foreach (var m in defaults)
                SelectedLn.Mappings.Add(m);

            OnPropertyChanged(nameof(Mappings));
        }
    }
}