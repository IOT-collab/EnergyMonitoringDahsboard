using System;
using System.Collections.Generic;

using IEC61850.Client;
using IEC61850.Common;

namespace example1
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

			int port = 102;

			if (args.Length > 1)
				port = Int32.Parse(args [1]);

            Console.WriteLine("Connect to " + hostname);

            try
            {
                con.Connect(hostname, port);

   //             List<string> serverDirectory = con.GetServerDirectory(false);

   //             foreach (string entry in serverDirectory)
   //             {
   //                 Console.WriteLine("LD: " + entry);
   //             }


			//	List<string> lnDirectory = con.GetLogicalNodeDirectory("IED_1234MEAS/MMXU1", ACSIClass.ACSI_CLASS_DATA_SET);

   //             foreach (string entry in lnDirectory)
   //             {
   //                 Console.WriteLine("Dataset: " + entry);
   //             }

			////	string vendor = con.ReadStringValue ("simpleIOGenericIO/LLN0.NamPlt.vendor", FunctionalConstraint.DC);
   //          //   Console.WriteLine ("Vendor: " + vendor);

   //             /* read FCDO */
   //             MmsValue value = con.ReadValue("IED_1234MEAS/MMXU1.PPV", FunctionalConstraint.MX);

   //             if (value.GetType() == MmsType.MMS_STRUCTURE)
   //             {
   //                 Console.WriteLine("Value is of complex type");

   //                 for (int i = 0; i < value.Size(); i++)
   //                 {
   //                     Console.WriteLine("  element: " + value.GetElement(i).GetType());
   //                     if (value.GetElement(i).GetType() == MmsType.MMS_UTC_TIME)
   //                     {
   //                         Console.WriteLine("   -> " + value.GetElement(i).GetUtcTimeAsDateTimeOffset());
   //                     }
   //                 }
   //             }

   //             DataSet dataSet = con.ReadDataSetValues("IED_1234MEAS/MMXU1.PPV", null);

   //             Console.WriteLine("Read data set " + dataSet.GetReference());

   //             /* read multiple variables (WARNING: this is not IEC 61850 standard compliant but might
   //              * be supported by most servers).
   //              */
   //             MmsConnection mmsConnection = con.GetMmsConnection();

   //             MmsValue result = mmsConnection.ReadMultipleVariables("IED_1234MEAS/MMXU1", new List<string>() {
   //                 "PPV", "A", "PhV","Hz"
   //             });

   //             Console.WriteLine(result.ToString());

                con.Abort();
            }
            catch (IedConnectionException e)
            {
				Console.WriteLine(e.Message);
                Console.WriteLine("Numeric error code: " + e.GetErrorCode());
                Console.WriteLine("IedClientError: " + e.GetIedClientError());

            }

            System.Threading.Thread.Sleep(2000);

			// release all resources - do NOT use the object after this call!!
			con.Dispose ();
        }
    }
}
