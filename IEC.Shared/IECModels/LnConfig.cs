using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IEC.Shared.IECModels
{
    public class LnConfig
    {
        public string LogicalDevice { get; set; } = "IED_1234MEAS";
        public string LogicalNode { get; set; } = "MMXU1";
        public string FC { get; set; } = "MX";

        // DO Mappings
        public List<DoMappingConfig> Mappings { get; set; } = new();
    }
}