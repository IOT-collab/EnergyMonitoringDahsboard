using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Models
{
    public class RegisterConfig
    {
        public string ParameterName { get; set; }

        public ushort RegisterAddress { get; set; }

        public bool IsEnabled { get; set; } = true;

        public string Unit { get; set; }

        public float ScaleFactor { get; set; } = 1;

        public int Length { get; set; } = 2;

        // Changed from string to enum for stronger typing and easier binding
        public RegisterDataType DataType { get; set; } = RegisterDataType.Float;
    }
}
