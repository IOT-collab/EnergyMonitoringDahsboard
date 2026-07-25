using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IEC.Shared.IECModels
{
    public class RelayConfig
    {
        public int RelayId { get; set; }
        public string RelayName { get; set; } = "New Relay";
        public string IPAddress { get; set; } = "192.168.1.1";
        public int Port { get; set; } = 102;
        public bool IsEnabled { get; set; } = true;
        public int PollIntervalMs { get; set; } = 1000;

        // Logical Node config
        public List<LnConfig> LogicalNodes { get; set; } = new();
    }
}