using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.IECModels
{
    public class IecConfigRoot
    {
        public List<RelayConfig> Relays { get; set; } = new();
    }
}
