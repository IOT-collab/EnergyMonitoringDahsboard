namespace IEC.Shared.Models
{
    public class ScreenPermissions
    {
        public bool SldView { get; set; } = true;
        public bool EnergyMonitor { get; set; } = true;
        public bool GaugeView { get; set; } = true;
        public bool DeviceConfiguration { get; set; }
        public bool RelayMonitor { get; set; } = true;
        public bool RemoteView { get; set; } = true;
        public bool Reports { get; set; } = true;
        public bool Alarms { get; set; } = true;
        public bool UserConfiguration { get; set; }

        public static ScreenPermissions ForRole(UserRole role) => new()
        {
            DeviceConfiguration = role == UserRole.Admin,
            UserConfiguration = role == UserRole.Admin
        };
    }
}
