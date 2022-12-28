﻿// This implements a console application that can be used to test an ASCOM driver
//

// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.

#define Dome
// remove this to bypass the code that uses the chooser to select the driver
#define UseChooser

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace ASCOM
{
    class Program
    {
        static void Main(string[] args)
        {
            // Uncomment the code that's required
#if UseChooser
            // choose the device
            string id = ASCOM.DriverAccess.Dome.Choose("");
            if (string.IsNullOrEmpty(id))
                return;
            // create this device
            ASCOM.DriverAccess.Dome device = new ASCOM.DriverAccess.Dome(id);
#else
            // this can be replaced by this code, it avoids the chooser and creates the driver class directly.
            ASCOM.DriverAccess.Dome device = new ASCOM.DriverAccess.Dome("ASCOM.AAAOV.Dome");
#endif
            // now run some tests, adding code to your driver so that the tests will pass.
            // these first tests are common to all drivers.
            Console.WriteLine("name " + device.Name);
            Console.WriteLine("description " + device.Description);
            Console.WriteLine("DriverInfo " + device.DriverInfo);
//            Console.WriteLine("driverVersion " + device.DriverVersion);
//            Console.WriteLine("Supported Actiions:");
//            PrintValues(device.SupportedActions);


        // TODO add more code to test the driver.
        device.Connected = true;


            device.Connected = false;
            Console.WriteLine("Press Enter to finish");
            Console.ReadLine();
        }

        public static void PrintValues(IEnumerable myList)
        {
            foreach (Object obj in myList)
                Console.WriteLine("   {0}", obj);
            Console.WriteLine();
        }
    }
}
