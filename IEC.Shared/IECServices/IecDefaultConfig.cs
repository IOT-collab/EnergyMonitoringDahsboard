using IEC.Shared.IECModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.IECServices
{
    public static class IecDefaultConfig
    {
        public static RelayConfig GetDefault() => new RelayConfig
        {
            RelayId = 1,
            RelayName = "Siemens 7SJ661",
            IPAddress = "172.168.1.2",
            Port = 102,
            IsEnabled = true,
            PollIntervalMs = 1000,
            LogicalNodes = new List<LnConfig>
            {
                new LnConfig
                {
                    LogicalDevice = "IED_1234MEAS",
                    LogicalNode   = "MMXU1",
                    FC            = "MX",
                    Mappings = new List<DoMappingConfig>
                    {
                        new DoMappingConfig { ParameterName="TotPF",  DOIndex=0, ValueType=IECModels.ValueType.Direct, Unit="",   IsEnabled=true },
                        new DoMappingConfig { ParameterName="TotVA",  DOIndex=1, ValueType=IECModels.ValueType.Direct, Unit="kVA",IsEnabled=true },
                        new DoMappingConfig { ParameterName="TotVAr", DOIndex=2, ValueType=IECModels.ValueType.Direct, Unit="kVAr",IsEnabled=true},
                        new DoMappingConfig { ParameterName="TotW",   DOIndex=3, ValueType=IECModels.ValueType.Direct, Unit="kW", IsEnabled=true },
                        new DoMappingConfig { ParameterName="Hz",     DOIndex=4, ValueType=IECModels.ValueType.Direct, Unit="Hz", IsEnabled=true },

                        new DoMappingConfig { ParameterName="PPV_AB", DOIndex=5, ValueType=IECModels.ValueType.Phase, PhaseIndex=0, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="PPV_BC", DOIndex=5, ValueType=IECModels.ValueType.Phase, PhaseIndex=1, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="PPV_CA", DOIndex=5, ValueType=IECModels.ValueType.Phase, PhaseIndex=2, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="PhV_Neut",DOIndex=6,ValueType=IECModels.ValueType.Phase, PhaseIndex=0, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="PhV_A",  DOIndex=6, ValueType=IECModels.ValueType.Phase, PhaseIndex=1, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="PhV_B",  DOIndex=6, ValueType=IECModels.ValueType.Phase, PhaseIndex=2, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="PhV_C",  DOIndex=6, ValueType=IECModels.ValueType.Phase, PhaseIndex=3, Unit="V", IsEnabled=true },
                        new DoMappingConfig { ParameterName="A_Neut", DOIndex=7, ValueType=IECModels.ValueType.Phase, PhaseIndex=0, Unit="A", IsEnabled=true },
                        new DoMappingConfig { ParameterName="A_PhsA", DOIndex=7, ValueType=IECModels.ValueType.Phase, PhaseIndex=1, Unit="A", IsEnabled=true },
                        new DoMappingConfig { ParameterName="A_PhsB", DOIndex=7, ValueType=IECModels.ValueType.Phase, PhaseIndex=2, Unit="A", IsEnabled=true },
                        new DoMappingConfig { ParameterName="A_PhsC", DOIndex=7, ValueType=IECModels.ValueType.Phase, PhaseIndex=3, Unit="A", IsEnabled=true },
                    }
                }
            }
        };
    }
}