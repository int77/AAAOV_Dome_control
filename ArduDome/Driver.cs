//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Dome driver for AAAOV
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Dome interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.
#define Dome

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.IO;
using System.IO.Ports;

namespace ASCOM.AAAOV
{
    //
    // Your driver's DeviceID is ASCOM.AAAOV.Dome
    //
    // The Guid attribute sets the CLSID for ASCOM.AAAOV.Dome
    // The ClassInterface/None attribute prevents an empty interface called
    // _AAAOV from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Dome Driver for AAAOV.
    /// </summary>
    [Guid("cd3e76cd-ad8a-4426-b19c-5f32949c113f")]
    [ClassInterface(ClassInterfaceType.None)]
    public abstract class Dome : IDomeV2
    {
        protected virtual string GET_STATUS { get { return "G"; } }
        protected virtual string GET_POSITION { get { return "GP"; } }
        protected virtual string FIND_HOME { get { return "F"; } }
        protected virtual string SET_HOME { get { return "A"; } }
        protected virtual string MOVE_SHUTTER { get { return "O"; } }
        protected virtual string SLEW { get { return "S"; } }


        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.AAAOV.Dome";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "ASCOM Dome Driver for AAAOV";

        public abstract bool ConnectionState { get; set; }

        // Constants used for Profile persistence
        internal static string comPortProfileName = "COM Port"; 
        internal static string comPortDefault = "COM3";

        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "true";

        internal static string ParkAzimuthProfileName = "Park Azimuth";
        internal static string ParkAzimuthDefault="130";

        internal static string HomeAzimuthProfileName = "Home Azimuth";
        internal static string HomeAzimuthDefault = "143";

        internal static string _ComPort;
        internal static double _ParkAzimuth;
        internal static double _HomeAzimuth;

        /// <summary>
        /// Private variables to hold different states
        /// </summary>
        private bool _connectedState;
        private bool _isSlewing;
        private bool _isAtHome;
        private bool _isParked;
        private bool _isSynced;
        private bool _isSlaved;
        private ShutterState _ShutterStatus = ShutterState.shutterClosed;
        private double _Azimuth;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal TraceLogger tl;

        /// <summary>
        /// Initializes a new instance of the <see cref="AAAOV"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Dome()
        {
            tl = new TraceLogger("", "AAAOV");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            LogMessage("Dome", "Starting initialisation");

            _connectedState = false; // Initialise connected to false
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro-utilities object
            //TODO: Implement your additional construction here

            LogMessage("Dome", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE IDomeV2 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(tl))
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and does not wait for a response.
        /// Optionally, protocol framing characters may be added to the string before transmission. 
        /// 
        /// Can throw a not implemented exception
        /// Namespace: ASCOM.DeviceInterface
        /// Assembly:  ASCOM.DeviceInterfaces(in ASCOM.DeviceInterfaces.dll) Version: 6.0.0.0 (6.4.1.2695)
        /// </summary>
        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            LogMessage("CommandBlind", "Command: {0}, raw: {1}", command, raw.ToString());

            if (raw)
            {
                SharedResources.SendSerialMessageBlind(command);
            }
            else
            {
                switch (command)
                {
                    case "FindHome":
                        SharedResources.SendSerialMessageBlind(FIND_HOME);
                        break;
                    case "OpenShutter":
                        SharedResources.SendSerialMessageBlind(MOVE_SHUTTER+" 1" );
                        break;
                    case "CloseShutter":
                        SharedResources.SendSerialMessageBlind(MOVE_SHUTTER + " 0");
                        break;
                    case "SlewToAzimuth":
                        SharedResources.SendSerialMessageBlind(SLEW );
                        break;


                    default:
                        throw new MethodNotImplementedException("MethodNotImplementedException: CommandBlind");
                }
            }
        }

        public bool CommandBool(string command, bool raw)
        {
            //CheckConnected("CommandBool");
            // TODO The optional CommandBool method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBool must send the supplied command to the mount, wait for a response and parse this to return a True or False value

            // string retString = CommandString(command, raw); // Send the command and wait for the response
            // bool retBool = XXXXXXXXXXXXX; // Parse the returned string and create a boolean True / False value
            // return retBool; // Return the boolean value to the client

            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            //CheckConnected("CommandString");
            // TODO The optional CommandString method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandString must send the supplied command to the mount and wait for a response before returning this to the client

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the trace logger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
        }

        public bool Connected
        {
            get
            {
                return ConnectionState;
            }
            set
            {
                if (value == ConnectionState) { return; }

                if (value)
                {
                    LogMessage("Connected", "Starting a new serial connection");

                    // Check if we are the first client using the shared serial
                    if (SharedResources.Connections == 0)
                    {
                        LogMessage("Connected", "We are the first connected client, setting serial port name");
                        SharedResources.COMPortName = _ComPort;
                    }

                    SharedResources.Connected = true;

                    try
                    {
                        //
                        // Loops until the firmware reports a successful connection 
                        //
                        bool ready = false;
                        string hex;

                        do
                        {
                            try
                            {
                                hex = SharedResources.SendSerialMessage(GET_STATUS);
                                if (hex.Length > 0) { ready = true; }
                            }
                            catch (Exception) { ready = false; }

                        } while (!ready);

                        LogMessage("Connected", "Firmware ready");
                    }
                    catch (Exception e)
                    {
                        LogMessage("Connected", "Exception: {0}", e.Message);
                        ConnectionState = false;
                        return;
                    }

                    // Check if we are the first client using the shared serial
                    if (SharedResources.Connections == 1)
                    {
                        InitDevice();
                    }

                    ConnectionState = true;
                    LogMessage("Connected", "Connected successfully");
                }
                else
                {
                    LogMessage("Connected", "Disconnecting the serial connection");
                    ConnectionState = false;
                    SharedResources.Connected = false;
                    LogMessage("Connected", "Disconnected successfully");
                }
            }
        }


        private void InitDevice()
        {
            //
            // Init device
            //
            //string Speed = String.Empty;
            //CommandBlind(string.Format(SET_SPEED, Speed), true);
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
                        this.Azimuth = Double.Parse(com_args[1]);
                        break;
                    case "SHUTTER":
                        if (com_args[1] == "OPEN") this.ShutterStatus = ShutterState.shutterOpen;
                        if (com_args[1] == "CLOSED") this.ShutterStatus = ShutterState.shutterClosed;
                        if (com_args[1] == "OPENING") this.ShutterStatus = ShutterState.shutterOpening;
                        if (com_args[1] == "CLOSING") this.ShutterStatus = ShutterState.shutterClosing;
                        break;
                    case "SYNCED":
                        this.Synced = true;
                        break;
                    case "NOTSYNCED":
                        this.Synced = false;
                        break;
                    case "PARKED":
                        this.AtPark = true;
                        break;
                    case "ATHOME":
                        this.AtHome = true;
                        break;
                    case "CONNECTED":
                        this._connectedState = true;
                        break;
                    case "SLEWING":
                        this.Slewing = true;
                        //LogMessage("Slewing status:", this.Slewing.ToString());
                        break;
                    case "STOP":
                        this.Slewing = false;
                        //LogMessage("Slewing status: ", this.Slewing.ToString());
                        break;
                    default:
                        break;
                }
            }
        }


        public string Description
        {
            // TODO customise this device description
            get
            {
                LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "2");
                return Convert.ToInt16("2");
            }
        }

        public string Name
        {
            get
            {
                string name = "AAAOV_Dome";
                LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region IDome Implementation
 
        public double Altitude
        {
            get
            {
                //LogMessage("Altitude Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("Altitude", false);
            }
        }

        public bool AtHome
        {
            get
            {
                return this._isAtHome;
            }
            set
            {
                this._isAtHome = value;
            }
        }

        public bool AtPark
        {
            get { return this._isParked; }
            set { this._isParked = value; }
        }

        public double Azimuth
        {
            get { return this._Azimuth; }
            set { this._Azimuth = value; }

        }

        public ShutterState ShutterStatus
        {
            get { return this._ShutterStatus; }
            set { this._ShutterStatus = value; }
        }

        public bool Slewing
        {
            get { return this._isSlewing; }
            set { this._isSlewing = value; }

        }

        public bool Synced
        {
            get { return this._isSynced; }
            set { this._isSynced = value; }

        }


        public bool Slaved
        {
            get { return this._isSlaved; }
            set { this._isSlaved = value; }
        }

        public bool CanFindHome
        {
            get
            {
                return true;
            }
        }

        public bool CanPark
        {
            get
            {
                return true;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                return false;
            }
        }

        public bool CanSetAzimuth
        {
            get
            {
                return true;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return true;
            }
        }

        public bool CanSetShutter
        {
            get
            {
                return true;
            }
        }

        public bool CanSlave
        {
            get
            {
                return true;
            }
        }

        public bool CanSyncAzimuth
        {
            get
            {
                return true;
            }
        }

        public void AbortSlew()
        {
            //CheckConnected("AbortSlew");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.Abort);
            LogMessage("AbortSlew", "Completed");
        }

        public void CloseShutter()
        {
            CheckConnected("CloseShutter");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.OpenCloseShutter, 0);
            LogMessage("CloseShutter", "Shutter has been closed");
        }

        public void FindHome()
        {
            CheckConnected("FindHome");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.FindHome);
            this.AtHome = true;
            LogMessage("FindHome", "At Home");
        }

        public void OpenShutter()
        {
            CheckConnected("OpenShutter");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.OpenCloseShutter, 1);
            LogMessage("OpenShutter", "Shutter has been opened");
        }

        public void Park()
        {
            CheckConnected("Park");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.Park);
            this.AtPark = true;
            LogMessage("Park", "Parking dome");
        }

        public void SetPark()
        {
            CheckConnected("SetPark");
            _ParkAzimuth = this.Azimuth;
            SerialConnection.SendCommand(SharedSerial.SerialCommand.SetPark, this.Azimuth);
            LogMessage("SetPark", "Setting Park Azimuth");
        }

        public void SlewToAltitude(double Altitude)
        {
            throw new ASCOM.MethodNotImplementedException("SlewToAltitude");
        }

        public void SlewToAzimuth(double Azimuth)
        {
            CheckConnected("SlewToAzimuth");
            this.AtPark = false;
            this.AtHome = false;
            if (Azimuth > 360 || Azimuth < 0)
                throw new ASCOM.InvalidValueException("Azimuth out of range");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.Slew, Azimuth);
            LogMessage("SlewToAzimuth", $"Slewing to azimuth ({Azimuth})");
        }

        public void SyncToAzimuth(double Azimuth)
        {
            CheckConnected("SyncToAzimuth");
            this.Synced = false;
            if (Azimuth > 360 || Azimuth < 0)
                throw new ASCOM.InvalidValueException("Azimuth out of range");
            SerialConnection.SendCommand(SharedSerial.SerialCommand.SyncToAzimuth, Azimuth);
            LogMessage("SyncToAzimuth", $"Syncing to azimuth ({Azimuth})");
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Dome";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                if (SerialConnection != null)
                {
                    this._connectedState = false;
                    SerialConnection.SendCommand(SharedSerial.SerialCommand.ReadStatus);
                    utilities.WaitForMilliseconds(200);
                    return _connectedState;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                LogMessage("CheckConnected", $"Connection lost in ({message})");
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                _ComPort = driverProfile.GetValue(driverID, comPortProfileName, string.Empty, comPortDefault);
                _ParkAzimuth = Convert.ToDouble(driverProfile.GetValue(driverID, ParkAzimuthProfileName,string.Empty,ParkAzimuthDefault));
                _HomeAzimuth = Convert.ToDouble(driverProfile.GetValue(driverID, HomeAzimuthProfileName, string.Empty,HomeAzimuthDefault));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, comPortProfileName, _ComPort.ToString());
                driverProfile.WriteValue(driverID, ParkAzimuthProfileName, _ParkAzimuth.ToString());
                driverProfile.WriteValue(driverID, HomeAzimuthProfileName, _HomeAzimuth.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            tl.LogMessage(identifier, msg);
        }
        #endregion
    }
}
