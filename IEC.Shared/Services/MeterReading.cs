using System;
using System.Collections.Generic;

namespace IEC.Shared.Services
{
    // Flexible container: ParameterName -> value (as object/double)
    public class MeterReading
    {
        public string MeterName { get; set; }

        // ParameterName -> decoded value
        public Dictionary<string, object> Values { get; } = new();

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}