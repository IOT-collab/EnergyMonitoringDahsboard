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

        public CommunicationConfig Communication { get; set; }
            = new CommunicationConfig();

        public ObservableCollection<RegisterConfig> Registers { get; set; }
            = new ObservableCollection<RegisterConfig>();
    }
}
