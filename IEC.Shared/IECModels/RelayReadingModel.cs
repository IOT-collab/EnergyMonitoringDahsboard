using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.IECModels
{
    public class RelayReadingModel
    {
        public bool IsOnline { get; set; }
        public string ErrorMessage { get; set; }

        // Frequency
        public float Hz { get; set; }

        // Phase Voltages (Phase to Neutral)
        public float PhV_Neut { get; set; }
        public float PhV_A { get; set; }
        public float PhV_B { get; set; }
        public float PhV_C { get; set; }

        // Line Voltages (Phase to Phase)
        public float PPV_AB { get; set; }
        public float PPV_BC { get; set; }
        public float PPV_CA { get; set; }

        // Currents
        public float A_Neut { get; set; }
        public float A_PhsA { get; set; }
        public float A_PhsB { get; set; }
        public float A_PhsC { get; set; }

        // Power
        public float TotW { get; set; }
        public float TotVA { get; set; }
        public float TotVAr { get; set; }
        public float TotPF { get; set; }
    }
}
