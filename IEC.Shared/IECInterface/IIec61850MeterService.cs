using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IEC.Shared.IECModels;

namespace IEC.Shared.IECInterface
{
    public interface IIec61850MeterService : IDisposable
    {
        bool IsConnected { get; }
        void Connect(string hostname, int port = 102);
        Task<RelayReadingModel> ReadAsync();

        Task ConnectAllAsync(List<RelayConfig> relays);
        Task<Dictionary<int, RelayReadingModel>> ReadAllAsync();
        Task<RelayReadingModel> ReadOneAsync(int relayId);
        void Disconnect();

      
    }
}
