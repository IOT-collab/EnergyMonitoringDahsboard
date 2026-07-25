using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    public interface IMultiEnergyMeterService : IDisposable
    {
        Task Configure(IEnumerable<MetersConfig> meters);

        // Now returns a meter-centric, parameter-keyed reading object.
        Task<Dictionary<string, MeterReading>> ReadAllAsync();

        Task<MeterReading> ReadOneAsync(string meterName);

        Task DisconnectAll();

        bool HasMeter(string meterName);
    }
}
