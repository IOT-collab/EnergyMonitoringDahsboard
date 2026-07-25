using example2.Explorer;
using IEC61850.Client;
using IEC61850.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace example2
{
    class WriteValueExample
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
                con.Connect(hostname, 102);

                RelayModelBrowser browser = new RelayModelBrowser(con);

                RelayModel model =
                    browser.BuildModel();

                Console.WriteLine();
                Console.WriteLine("Model built successfully.");
                Console.WriteLine();

                foreach (var ld in model.LogicalDevices)
                {
                    Console.WriteLine(ld.Name);

                    foreach (var ln in ld.LogicalNodes)
                    {
                        Console.WriteLine("   " + ln.Name);

                        foreach (var obj in ln.DataObjects)
                        {
                            Console.WriteLine("      " + obj.Name);

                            foreach (var da in obj.Attributes)
                            {
                                Console.WriteLine(
                                    $"         [{da.FC}] {da.Name} ({da.Type})");
                            }
                        }
                    }
                }

                //ReadPoint(con, "IED_1234CTRL/Q0CILO1.EnaCls", FunctionalConstraint.ST);

                //ReadPoint(con, "IED_1234CTRL/Q0CILO1.EnaOpn", FunctionalConstraint.ST);

                //Console.WriteLine("\n========== BREAKER ==========\n");

                //ReadPoint(con,
                //    "IED_1234CTRL/Q0XCBR1.Pos.stVal",
                //    FunctionalConstraint.ST);

                //Console.WriteLine("\n========== SWITCH ==========\n");

                //ReadPoint(con,
                //    "IED_1234CTRL/Q0CSWI1.Pos.stVal",
                //    FunctionalConstraint.ST);

                //Console.WriteLine("\n========== PROTECTION ==========\n");

                //ReadPoint(con,
                //    "IED_1234PROT/PTOC6.Str.stVal",
                //    FunctionalConstraint.ST);

                //ReadPoint(con,
                //    "IED_1234PROT/PTOC6.Op.stVal",
                //    FunctionalConstraint.ST);

                //ReadPoint(con,
                //    "IED_1234PROT/PTRC1.Tr.stVal",
                //    FunctionalConstraint.ST);

                //Console.WriteLine("\n========== MEASUREMENTS ==========\n");

                //ReadPoint(con,
                //    "IED_1234MEAS/MMXU1.Hz.mag.f",
                //    FunctionalConstraint.MX);

                //ReadPoint(con,
                //    "IED_1234MEAS/MMXU1.TotW.mag.f",
                //    FunctionalConstraint.MX);

                //ReadPoint(con,
                //    "IED_1234MEAS/MMXU1.TotVA.mag.f",
                //    FunctionalConstraint.MX);

                //ReadPoint(con,
                //    "IED_1234MEAS/MMXU1.TotPF.mag.f",
                //    FunctionalConstraint.MX);

                //ReadObject( con,"IED_1234PROT/PTOC6.Str",FunctionalConstraint.ST);

                //ReadObject( con,"IED_1234PROT/PTOC6.Op",FunctionalConstraint.ST);

                //ReadObject( con,"IED_1234PROT/PTRC1.Tr", FunctionalConstraint.ST);

                //Console.WriteLine("\n========== PROTECTION Boolean STATUS ==========\n");

                //ReadBooleanStatus(con, "IED_1234PROT/PTOC6.Str");
                //ReadBooleanStatus(con, "IED_1234PROT/PTOC6.Op");

                //ReadBooleanStatus(con, "IED_1234PROT/PTOC7.Str");
                //ReadBooleanStatus(con, "IED_1234PROT/PTOC7.Op");

                //ReadBooleanStatus(con, "IED_1234PROT/PTOC8.Str");
                //ReadBooleanStatus(con, "IED_1234PROT/PTOC8.Op");

                //ReadBooleanStatus(con, "IED_1234PROT/PTOC9.Str");
                //ReadBooleanStatus(con, "IED_1234PROT/PTOC9.Op");

                //ReadBooleanStatus(con, "IED_1234PROT/PTOC18.Str");
                //ReadBooleanStatus(con, "IED_1234PROT/PTOC18.Op");

                //ReadBooleanStatus(con, "IED_1234PROT/PTRC1.Tr");
                //ReadBooleanStatus(con, "IED_1234PROT/PTRC2.Tr");



                //Console.WriteLine("\n========== XCBR ==========\n");

                //ReadStatusObject(con, "IED_1234CTRL/Q0XCBR1.Pos");

                //ReadStatusObject(con, "IED_1234CTRL/Q0XCBR1.BlkCls");

                //ReadStatusObject(con, "IED_1234CTRL/Q0XCBR1.BlkOpn");

                //Console.WriteLine("\n========== XSWI ==========\n");

                //ReadStatusObject(con, "IED_1234CTRL/Q1XSWI1.Pos");

                //ReadStatusObject(con, "IED_1234CTRL/Q1XSWI1.BlkCls");

                //ReadStatusObject(con, "IED_1234CTRL/Q1XSWI1.BlkOpn");

                //Console.WriteLine("\n========== Relay Explorer ==========\n");

                //Explore(con, "IED_1234CTRL/Q1XSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q2XSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q8XSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q9XSWI1.Pos", FunctionalConstraint.ST);

                //Explore(con, "IED_1234CTRL/Q0CSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q1CSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q2CSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q8CSWI1.Pos", FunctionalConstraint.ST);
                //Explore(con, "IED_1234CTRL/Q9CSWI1.Pos", FunctionalConstraint.ST);


                //ControlObject led = con.CreateControlObject("IED_1234CTRL/LLN0.LEDRs");

                //Console.WriteLine("Control Model : " + led.GetControlModel());
                //Console.WriteLine("CtlVal Type   : " + led.GetCtlValType())


                //using (ControlObject cb = con.CreateControlObject("IED_1234CTRL/Q0CSWI1.Pos"))
                //{
                //    Console.WriteLine(cb.GetControlModel());
                //    Console.WriteLine(cb.GetCtlValType());

                //    var before = con.ReadValue(
                //                            "IED_1234CTRL/Q0XCBR1.Pos.stVal",
                //                            FunctionalConstraint.ST);

                //    bool ok = cb.Select();

                //    if (ok)
                //    {
                //        ok = cb.Operate(true);
                //    }


                //    Console.WriteLine(ok);
                //    Console.WriteLine(cb.LastError);

                //    var after = con.ReadValue(
                //                    "IED_1234CTRL/Q0XCBR1.Pos.stVal",
                //                    FunctionalConstraint.ST);


                //    Console.WriteLine($"Before: {before}");
                //    Console.WriteLine($"After: {after}");
                //}


                //using (ControlObject led = con.CreateControlObject("IED_1234CTRL/LLN0.LEDRs"))
                //{
                //    led.SetOrigin("Chat45", OrCat.STATION_CONTROL);

                //    bool result = led.Operate(true);

                //    Console.WriteLine("Result : " + result);
                //    Console.WriteLine("LastError : " + led.LastError);
                //}

                //int n = 0;
                //while (n < 1)
                //{

                //    MmsValue PPV = con.ReadValue("IED_1234CTRL/LLN0", FunctionalConstraint.);

                //    Console.WriteLine("\n");
                //    foreach (MmsValue v in PPV)
                //    {
                //        if (v.Size() > 0 && v != null)
                //            foreach (MmsValue v2 in v.GetElement(0))
                //            {
                //                Console.WriteLine($"{v2}");

                //            }
                //    }
                //    Thread.Sleep(500);
                //    n++;
                //}



                //float setMagF = con.ReadFloatValue("IED_1234MEAS/MMXU1.PPV.phsAB.mag.f", FunctionalConstraint.MX);

                //Console.WriteLine("IED_1234CTRL/Q1CSWI1.Pos.Cancel: " + setMagF);

                //setMagF += 1.0f;

                //con.WriteValue("IED_1234CTRL/Q1CSWI1.Pos.Cancel", FunctionalConstraint.CO, new MmsValue(setMagF));
                //Value -: { PPV}
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00}


                //Value -: { A}

                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00}

                //Value -: { PhV }
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00},
                //{ { { 0} }, { { 0} }, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00}

                //Value -: {Hz}
                //{ NaN}, { NaN}, 0100001000000, 7 / 16 / 2026 11:56:51 AM + 00:00

                //Value -: {TotPF}
                //{ NaN}, { NaN}, 0100001000000, 7 / 16 / 2026 11:56:51 AM + 00:00

                //Value -: {TotVA}
                //{ 0}, { 0}, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00

                //Value -: {TotVAr}
                //{ 0}, { 0}, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00

                //Value -: {TotW}
                //{ 0}, { 0}, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00


                //Value -: { urcbA01 }
                //{, False, False, , 1, 0000000000, 0, 0, 000001, 0, False}

                //Value -: { urcbB01}
                //{ , False, False, , 1, 0000000000, 0, 0, 000001, 0, False}

                //Value -: { NamPlt}

                //{ SIEMENS, 04.43.16, , 160304190901000}\

                //Value -: { Beh}

                //{ 1, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00}

                //Value -: { Health}

                //{ 1, 0000000000000, 7 / 16 / 2026 11:56:51 AM + 00:00}

                //int i = 0;
                //while (i<= 1000)
                //{
                //    Console.Clear();

                //    //PrintPos(con, "IED_1234CTRL/Q0CSWI1.Pos");
                //    //PrintPos(con, "IED_1234CTRL/Q1CSWI1.Pos");
                //    //PrintPos(con, "IED_1234CTRL/Q2CSWI1.Pos");
                //    //PrintPos(con, "IED_1234CTRL/Q8CSWI1.Pos");
                //    //PrintPos(con, "IED_1234CTRL/Q9CSWI1.Pos");


                //    PrintPos(con, "IED_1234CTRL/Q0CSWI1.Pos");
                //    PrintPos(con, "IED_1234CTRL/Q0XCBR1.Pos");

                //    Thread.Sleep(300);
                //    i++;
                //   // Thread.Sleep(500);
                //}


                con.Abort();
            }
            catch (IedConnectionException e)
            {
                Console.WriteLine("IED connection exception: " + e.Message + " err: " + e.GetIedClientError().ToString());
            }

			// release all resources - do NOT use the object after this call!!
			con.Dispose ();
        }

        private static void ReadPoint( IedConnection con,  string reference, FunctionalConstraint fc)
        {
            Console.WriteLine("--------------------------------------------");

            try
            {
                MmsValue value = con.ReadValue(reference, fc);

                Console.WriteLine("Reference : " + reference);
                Console.WriteLine("FC        : " + fc);

                if (value == null)
                {
                    Console.WriteLine("Value : NULL");
                    return;
                }

                Console.WriteLine("Type  : " + value.GetType());
                Console.WriteLine("Value : " + value);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reference : " + reference);
                Console.WriteLine("ERROR : " + ex.Message);
            }
        }

        private static void ReadObject( IedConnection con,  string reference, FunctionalConstraint fc)
        {
            Console.WriteLine("----------------------------------");
            Console.WriteLine(reference);

            try
            {
                MmsValue value = con.ReadValue(reference, fc);

                Console.WriteLine(value);

                if (value == null)
                    return;

                if (value.Size() > 0)
                {
                    Console.WriteLine("Children : " + value.Size());

                    for (int i = 0; i < value.Size(); i++)
                    {
                        Console.WriteLine($"[{i}] {value.GetElement(i)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void ReadBooleanStatus( IedConnection con,  string reference)
        {
            try
            {
                MmsValue value = con.ReadValue(reference, FunctionalConstraint.ST);

                Console.WriteLine("------------------------------------");
                Console.WriteLine(reference);

                if (value == null)
                {
                    Console.WriteLine("NULL");
                    return;
                }

                bool status = value.GetElement(0).GetBoolean();

                Console.WriteLine("Status    : " + status);

                if (value.Size() > 2)
                    Console.WriteLine("Quality   : " + value.GetElement(value.Size() - 2));

                Console.WriteLine("Timestamp : " + value.GetElement(value.Size() - 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine(reference);
                Console.WriteLine(ex.Message);
            }
        }

        private static void ReadStatusObject( IedConnection con,  string reference)
        {
            Console.WriteLine("--------------------------------");
            Console.WriteLine(reference);

            try
            {
                var value = con.ReadValue(reference, FunctionalConstraint.ST);

                Console.WriteLine(value);

                if (value == null)
                    return;

                Console.WriteLine("Type : " + value.GetType());
                Console.WriteLine("Size : " + value.Size());

                for (int i = 0; i < value.Size(); i++)
                    Console.WriteLine($"[{i}] {value.GetElement(i)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Explore(IedConnection con,string reference, FunctionalConstraint fc)
        {
            Console.WriteLine("\n======================================");
            Console.WriteLine(reference);

            try
            {
                MmsValue value = con.ReadValue(reference, fc);

                if (value == null)
                {
                    Console.WriteLine("NULL");
                    return;
                }

                Console.WriteLine("Type : " + value.GetType());
                Console.WriteLine("MMS  : " + value);

                if (value.Size() > 0)
                {
                    Console.WriteLine("Children : " + value.Size());

                    for (int i = 0; i < value.Size(); i++)
                    {
                        MmsValue child = value.GetElement(i);

                        Console.WriteLine(
                            $"[{i}] {child} ({child.GetType()})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void PrintPos(IedConnection con, string reference)
        {
            MmsValue value = con.ReadValue(reference, FunctionalConstraint.ST);

            Console.WriteLine(reference);

            Console.WriteLine("Children : " + value.Size());

            for (int i = 0; i < value.Size(); i++)
            {
                Console.WriteLine($"[{i}] = {value.GetElement(i)}");
            }

            Console.WriteLine();
        }
    }
}
