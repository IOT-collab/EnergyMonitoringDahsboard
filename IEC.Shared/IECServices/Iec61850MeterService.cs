using IEC.Shared.IECInterface;
using IEC.Shared.Models;
using IEC61850.Client;
using IEC61850.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IEC.Shared.IECModels;

namespace IEC.Shared.IECServices
{
    public class Iec61850MeterService : IIec61850MeterService
    {
        private IedConnection _con;
        private string _hostname;
        private int _port;
        private readonly object _lock = new object();

        public bool IsConnected { get; private set; }

        public void Connect(string hostname, int port = 102)
        {
            _hostname = hostname;
            _port = port;

            _con = new IedConnection();
            _con.Connect(hostname, port);
            IsConnected = true;
        }

        public Task<RelayReadingModel> ReadAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        MmsValue mmxu = _con.ReadValue(
                            "IED_1234MEAS/MMXU1",
                            FunctionalConstraint.MX);

                        return new RelayReadingModel
                        {
                            IsOnline = true,

                            // DO[0..3] = TotPF, TotVA, TotVAr, TotW
                            TotPF = GetDirectValue(mmxu.GetElement(3)),
                            TotVA = GetDirectValue(mmxu.GetElement(0)),
                            TotVAr = GetDirectValue(mmxu.GetElement(1)),
                            TotW = GetDirectValue(mmxu.GetElement(2)),

                            // DO[4] = Hz
                            Hz = GetDirectValue(mmxu.GetElement(4)),

                            // DO[5] = PPV
                            PPV_AB = GetPhaseValue(mmxu.GetElement(5).GetElement(0)),
                            PPV_BC = GetPhaseValue(mmxu.GetElement(5).GetElement(1)),
                            PPV_CA = GetPhaseValue(mmxu.GetElement(5).GetElement(2)),

                            // DO[6] = PhV
                            PhV_Neut = GetPhaseValue(mmxu.GetElement(6).GetElement(0)),
                            PhV_A = GetPhaseValue(mmxu.GetElement(6).GetElement(1)),
                            PhV_B = GetPhaseValue(mmxu.GetElement(6).GetElement(2)),
                            PhV_C = GetPhaseValue(mmxu.GetElement(6).GetElement(3)),

                            // DO[7] = A (Current)
                            A_Neut = GetPhaseValue(mmxu.GetElement(7).GetElement(0)),
                            A_PhsA = GetPhaseValue(mmxu.GetElement(7).GetElement(1)),
                            A_PhsB = GetPhaseValue(mmxu.GetElement(7).GetElement(2)),
                            A_PhsC = GetPhaseValue(mmxu.GetElement(7).GetElement(3)),
                        };
                    }
                    catch (Exception ex)
                    {
                        IsConnected = false;
                        return new RelayReadingModel
                        {
                            IsOnline = false,
                            ErrorMessage = ex.Message
                        };
                    }
                }
            });
        }

        private float GetDirectValue(MmsValue doVal)
        {
            return (float)doVal.GetElement(1)
                               .GetElement(0)
                               .ToDouble();
        }

        private float GetPhaseValue(MmsValue phaseChild)
        {
            return (float)phaseChild.GetElement(1)
                                    .GetElement(0)
                                    .GetElement(0)
                                    .ToDouble();
        }

        public void Disconnect()
        {
            try { _con?.Release(); } catch { }
            IsConnected = false;
        }

        public void Dispose() => Disconnect();
    }
}
