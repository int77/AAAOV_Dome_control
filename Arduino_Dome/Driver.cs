//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Dome driver for Arduino
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Dome interface version: 1.0
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	1.0.0	Initial edit, from ASCOM Dome Driver template
// --------------------------------------------------------------------------------
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Reflection;
using System.Threading;

using ASCOM;
using ASCOM.Utilities;
using ASCOM.Interface;

namespace ASCOM.Arduino
{
    //
    // Your driver's ID is ASCOM.Arduino.Dome
    //
    // The Guid attribute sets the CLSID for ASCOM.Arduino.Dome
    // The ClassInterface/None addribute prevents an empty interface called
    // _Dome from being created and used as the [default] interface
    //
    [Guid("387409ed-6827-46d7-8b61-a61a724281e0")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Dome : IDome
    {
        //
        // Driver ID and descriptive string that shows in the Chooser
        //
        public static string s_csDriverID = "ASCOM.Arduino.Dome";
        public static string s_csDriverDescription = "Arduino Dome";

        private ArduinoSerial SerialConnection;

        private Util HC = new Util();

        private Config Config = new Config();

        //
        // Constructor - Must be public for COM registration!
        //
        public Dome()
        {
            // TODO Implement your additional construction here

            //SerialConnection.SendCommand(ArduinoSerial.SerialCommand.SetPark, Config.ParkAzimuth);
            //SerialConnection.SendCommand(ArduinoSerial.SerialCommand.SetHome, Config.HomeAzimuth);
        }

        #region ASCOM Registration
        //
        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        private static void RegUnregASCOM(bool bRegister)
        {
            Helper.Profile P = new Helper.Profile();
            P.DeviceTypeV = "Dome";					//  Requires Helper 5.0.3 or later
            if (bRegister)
                P.Register(s_csDriverID, s_csDriverDescription);
            else
                P.Unregister(s_csDriverID);
            try										// In case Helper becomes native .NET
            {
                Marshal.ReleaseComObject(P);
            }
            catch (Exception) { }
            P = null;
        }

        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }
        #endregion

        //
        // PUBLIC COM INTERFACE IDome IMPLEMENTATION
        //

        #region IDome Members

        public void AbortSlew()
        {
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.Abort);
        }

        public double Altitude
        {
            get { throw new PropertyNotImplementedException("Altitude", false); }
        }

        public bool AtHome
        {
            //get { throw new PropertyNotImplementedException("AtHome", false); }

            get { return this.Config.AtHome; }
        }

        public bool AtPark
        {
            get { return this.Config.Parked; }
        }

        public double Azimuth
        {
            get { return this.Config.Azimuth; }
        }

        public bool CanFindHome
        {
            get { return true; }
        }

        public bool CanPark
        {
            get { return true; }
        }

        public bool CanSetAltitude
        {
            get { return false; }
        }

        public bool CanSetAzimuth
        {
            get { return true; }
        }

        public bool CanSetPark
        {
            get { return true; }
        }

        public bool CanSetShutter
        {
            get { return true; }
        }

        public bool CanSlave
        {
            get { return true; }
        }

        public bool CanSyncAzimuth
        {
            get { return true; }
        }



        public void CommandBlind(string Command)
        {
            // TODO Replace this with your implementation
            throw new MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string Command)
        {
            // TODO Replace this with your implementation
            throw new MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string Command)
        {
            // TODO Replace this with your implementation
            throw new MethodNotImplementedException("CommandString");
        }

        public bool Connected
        {
            get { return this.Config.Link; }
            set 
            {
                switch(value)
                {
                    case true:
                        this.Config.Link = this.ConnectDome();
                        break;
                    case false:
                        this.Config.Link = !this.DisconnectDome();
                        break;
                }
            }
        }

        private bool ConnectDome()
        {
            try
            {
                SerialConnection = new ArduinoSerial();
                SerialConnection.CommandQueueReady += new ArduinoSerial.CommandQueueReadyEventHandler(SerialConnection_CommandQueueReady);
                HC.WaitForMilliseconds(2000);
                SerialConnection.SendCommand(ArduinoSerial.SerialCommand.SetPark, Config.ParkAzimuth);
                SerialConnection.SendCommand(ArduinoSerial.SerialCommand.SetHome, Config.HomeAzimuth);
                CheckConnected("ConnectDome");
                return true;
            }
            catch (Exception)
            {

                //dl.LogMessage("Connected Set", $"Port {SerialConnection.PortName} cannot be opened ({e.Message})");
                throw new NotConnectedException("Can't connect to the device");
                //return false;
            }
        }

        private bool DisconnectDome()
        {
            if (SerialConnection.IsOpen) 
            {
                SerialConnection.Close();
            }
            return true;
        }

        private void CheckConnected(string message)
        {

            this.Config.Link = false;
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.ReadStatus);
            HC.WaitForMilliseconds(200);

            if (!this.Config.Link)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        void SerialConnection_CommandQueueReady(object sender, EventArgs e)
        {
            while (SerialConnection.CommandQueue.Count > 0)
            {
                string[] com_args = ((string)SerialConnection.CommandQueue.Pop()).Split(' ');

                string command = com_args[0];

                switch (command)
                {
                    case "P":
                        this.Config.Azimuth = Int32.Parse(com_args[1]);
                        this.Config.IsSlewing = false;
                        break;
                    case "SHUTTER":
                        if (com_args[1] == "OPEN") this.Config.ShutterStatus = ShutterState.shutterOpen;
                        if (com_args[1] == "CLOSED") this.Config.ShutterStatus = ShutterState.shutterClosed;
                        if (com_args[1] == "OPENING") this.Config.ShutterStatus = ShutterState.shutterOpening;
                        if (com_args[1] == "CLOSING") this.Config.ShutterStatus = ShutterState.shutterClosing;
                        break;
                    case "SYNCED":
                        this.Config.Synced = true;
                        break;
                    case "PARKED":
                        this.Config.Parked = true;
                        break;
                    case "ATHOME":
                        this.Config.AtHome = true;
                        break;
                    case "CONNECTED":
                        this.Config.Link = true;
                        break;
                    case "SLEWING":
                        this.Config.IsSlewing = true;
                        this.Config.AtHome = false;
                        this.Config.Parked = false;
                        break;
                    default:
                        break;
                }
            }
        }

        public string Description
        {
            get { return "Arduino Dome"; }
        }

        public string DriverInfo
        {
            get { return "1.0"; }
        }

        public void FindHome()
        {
            //this.Config.IsSlewing = true;

            CheckConnected("FindHome");
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.FindHome);
//            HC.WaitForMilliseconds(500);

//            while (this.Config.IsSlewing)
//                HC.WaitForMilliseconds(100);
            //throw new MethodNotImplementedException("FindHome");
        }

        public short InterfaceVersion
        {
            get { return 1; }
        }

        public string Name
        {
            get { return "Arduino Dome"; }
        }

        public void OpenShutter()
        {
            CheckConnected("OpenShutter");
            //this.Config.ShutterStatus = ShutterState.shutterOpening;
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.OpenCloseShutter,1);
            //HC.WaitForMilliseconds(500);

            //while (this.Config.ShutterStatus == ShutterState.shutterOpening)
                //HC.WaitForMilliseconds(100);
        }

        public void CloseShutter()
        {
            CheckConnected("CloseShutter");
            //this.Config.ShutterStatus = ShutterState.shutterClosing;
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.OpenCloseShutter,0);
            //HC.WaitForMilliseconds(500);

            //while (this.Config.ShutterStatus == ShutterState.shutterClosing)
                //HC.WaitForMilliseconds(100);
        }

        public void Park()
        {
            //this.Config.IsSlewing = true;
            CheckConnected("Park");
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.Park);
            //HC.WaitForMilliseconds(500);

            //while (!this.Config.Parked)
            //    HC.WaitForMilliseconds(100);
        }

        public void SetPark()
        {
            this.Config.ParkAzimuth = this.Config.Azimuth;
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.SetPark, Azimuth);
        }

        public void SetupDialog()
        {
            SetupDialogForm F = new SetupDialogForm();
            F.ShowDialog();
        }

        public ShutterState ShutterStatus
        {
            get { return this.Config.ShutterStatus; }
        }

        public bool Slaved
        {
            get { return this.Config.Slaved; }
            set { this.Config.Slaved = value; }
        }

        public void SlewToAltitude(double Altitude)
        {
            throw new MethodNotImplementedException("SlewToAltitude");
        }

        public void SlewToAzimuth(double Azimuth)
        {
            if (Azimuth > 360 || Azimuth < 0)
                throw new Exception("Out of range");
            CheckConnected("SlewToAzimuth");
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.Slew, Azimuth);
//            HC.WaitForMilliseconds(500);

//            while (this.Config.IsSlewing)
//                HC.WaitForMilliseconds(100);
        }

        public bool Slewing
        {
            get { return this.Config.IsSlewing; }
        }

        public void SyncToAzimuth(double Azimuth)
        {
            this.Config.Synced = false;
            if (Azimuth > 360 || Azimuth < 0)
                throw new Exception("Out of range");
            SerialConnection.SendCommand(ArduinoSerial.SerialCommand.SyncToAzimuth, Azimuth);

 //           while (!this.Config.Synced)
 //               HC.WaitForMilliseconds(100);
        }

        #endregion
    }
}
