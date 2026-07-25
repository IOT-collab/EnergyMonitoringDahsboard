using IEC.Shared.IECInterface;
using IEC.Shared.IECServices;
using IEC.Shared.Services;
using IECGUI.Services;
using IECGUI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;

namespace IECGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Services

            // example composition root / startup registration
            services.AddSingleton<MultiEnergyMeterRtuService>();           // RTU concrete
            services.AddSingleton<MultiEnergyMeterTcpService>();        // TCP concrete

            // Register coordinator as the app-level IMultiEnergyMeterService
            services.AddSingleton<IMultiEnergyMeterService>(sp =>
                new MultiEnergyMeterCoordinator(
                    sp.GetRequiredService<MultiEnergyMeterRtuService>(),
                    sp.GetRequiredService<MultiEnergyMeterTcpService>()));
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ConfigurationManagerService>();
            services.AddSingleton<IecConfigManagerService>();

            services.AddSingleton<IMqttClientService, MqttClientService>();
            services.AddTransient<MqttMonitorViewModel>();


            //IEC 618850 services

            services.AddSingleton<IIec61850MeterService, Iec61850MeterService>();
            services.AddTransient<IecConfigViewModel>();


            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<Dashboard1ViewModel>();
            services.AddTransient<EnergyMonitorViewModel>();
            services.AddTransient<EnergyMonitorViewModel2>();            
            services.AddSingleton<ConfigurationViewModel>();
            services.AddSingleton<HomePageViewModel>();
            services.AddTransient<Iec61850MonitorViewModel>();
            services.AddSingleton<ReportViewerViewModel>();
            // Views
            services.AddTransient<MainWindow>();

            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }

}
