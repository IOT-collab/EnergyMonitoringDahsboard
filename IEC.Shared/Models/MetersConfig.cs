using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Models
{
    public class MetersConfig
    {
        public int MeterId { get; set; }

        public string MeterName { get; set; }

        public string Section { get; set; } = "General";
        public string UtilityRoom { get; set; } = "Main Utility Room";

        // Existing configuration files do not contain this property, so the
        // default keeps all previously configured meters enabled.
        public bool IsEnabled { get; set; } = true;

        public CommunicationConfig Communication { get; set; }
            = new CommunicationConfig();

        public ObservableCollection<RegisterConfig> Registers { get; set; }
            = new ObservableCollection<RegisterConfig>();
    }
}
