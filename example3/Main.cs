using IEC61850.Client;
using IEC61850.Common;
using System;
using System.Collections.Generic;

namespace example3
{
	class MainClass
    {
        public static void Main (string[] args)
        {
            IedConnection con = new IedConnection ();

            string hostname;

            if (args.Length > 0)
                hostname = args[0];
            else
                hostname = "172.168.1.2";

            Console.WriteLine("Connect to " + hostname);


            try
            {
				IsoConnectionParameters parameters = con.GetConnectionParameters();

//				parameters.SetRemoteAddresses(new byte[] { 0x00, 0x01 }, new byte[] {0x00, 0x01}, new byte[] {0x00, 0x01, 0x02, 0x03});

				con.ConnectTimeout = 10000;

                con.MaxPduSize = 1200;

                con.Connect(hostname, 102);


                //MmsValue mmxu = con.ReadValue("IED_1234MEAS/MMXU1", FunctionalConstraint.MX);

                //Console.WriteLine("=== STRUCTURE DEBUG ===");
                //for (int i = 0; i < mmxu.Size(); i++)
                //{
                //    MmsValue doVal = mmxu.GetElement(i);
                //    Console.WriteLine($"\nDO[{i}] Type={doVal.GetType()} Size={doVal.Size()}");

                //    for (int j = 0; j < doVal.Size(); j++)
                //    {
                //        MmsValue child = doVal.GetElement(j);
                //        Console.WriteLine($"  [{j}] Type={child.GetType()} Size={(child.GetType() == MmsType.MMS_STRUCTURE ? child.Size().ToString() : "N/A")} Val={child}");

                //        if (child.GetType() == MmsType.MMS_STRUCTURE)
                //        {
                //            for (int k = 0; k < child.Size(); k++)
                //            {
                //                MmsValue grandchild = child.GetElement(k);
                //                Console.WriteLine($"    [{k}] Type={grandchild.GetType()} Val={grandchild}");

                //                if (grandchild.GetType() == MmsType.MMS_STRUCTURE)
                //                {
                //                    for (int m = 0; m < grandchild.Size(); m++)
                //                    {
                //                        MmsValue ggchild = grandchild.GetElement(m);
                //                        Console.WriteLine($"      [{m}] Type={ggchild.GetType()} Val={ggchild}");
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}



                MmsValue mmxu = con.ReadValue("IED_1234MEAS/MMXU1", FunctionalConstraint.MX);

                // ---- Helper: Direct DO se mag value nikalna ----
                // Structure: { instMag{f}, mag{f}, q, t }
                //                  [0]      [1]
                // Hum [1] (mag) lete hain → [0] → [0] = float
                float GetDirectValue(MmsValue doVal)
                {
                    return (float)doVal.GetElement(1)   // mag
                                       .GetElement(0)   // {f}                                       
                                       .ToDouble();
                }

                // ---- Helper: Phase child se mag value nikalna ----
                // Structure: { instMag{f}, mag{f}, q, t }
                //                  [0]      [1]
                float GetPhaseValue(MmsValue phaseChild)
                {
                    return (float)phaseChild.GetElement(1)  // mag
                                            .GetElement(0)  // {f}
                                            .GetElement(0)  // f
                                            .ToDouble();
                }

                Console.WriteLine($"\n{"Element",-10} {"Child",-10} {"Value",12}");
                Console.WriteLine(new string('-', 35));

                // DO[0..3] = TotPF, TotVA, TotVAr, TotW (direct)
                var directDOs = new[] { "TotPF", "TotVA", "TotVAr", "TotW" };
                for (int i = 0; i <= 3; i++)
                {
                    float val = GetDirectValue(mmxu.GetElement(i));
                    Console.WriteLine($"{directDOs[i],-10} {"",-10} {val,12:F4}");
                }

                // DO[4] = Hz (direct)
                {
                    float val = GetDirectValue(mmxu.GetElement(4));
                    Console.WriteLine($"{"Hz",-10} {"",-10} {val,12:F4}");
                }

                // DO[5] = PPV (3 phases: phsAB, phsBC, phsCA)
                {
                    Console.WriteLine($"\n{"PPV",-10}");
                    var phases = new[] { "phsAB", "phsBC", "phsCA" };
                    MmsValue ppv = mmxu.GetElement(5);
                    for (int j = 0; j < 3; j++)
                    {
                        float val = GetPhaseValue(ppv.GetElement(j));
                        Console.WriteLine($"  {"",-8} {phases[j],-10} {val,12:F4}");
                    }
                }

                // DO[6] = PhV (4 phases: neut, phsA, phsB, phsC)
                {
                    Console.WriteLine($"\n{"PhV",-10}");
                    var phases = new[] { "neut", "phsA", "phsB", "phsC" };
                    MmsValue phv = mmxu.GetElement(6);
                    for (int j = 0; j < 4; j++)
                    {
                        float val = GetPhaseValue(phv.GetElement(j));
                        Console.WriteLine($"  {"",-8} {phases[j],-10} {val,12:F4}");
                    }
                }

                // DO[7] = A (4 phases: neut, phsA, phsB, phsC)
                {
                    Console.WriteLine($"\n{"A",-10}");
                    var phases = new[] { "neut", "phsA", "phsB", "phsC" };
                    MmsValue a = mmxu.GetElement(7);
                    for (int j = 0; j < 4; j++)
                    {
                        float val = GetPhaseValue(a.GetElement(j));
                        Console.WriteLine($"  {"",-8} {phases[j],-10} {val,12:F4}");
                    }
                }




                con.Release();
            }
            catch (IedConnectionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Error code: " + e.GetErrorCode());
                Console.WriteLine("IedClientError: " + e.GetIedClientError());
            }

            // release all resources - do NOT use the object after this call!!
            con.Dispose ();
        }
    }
}
