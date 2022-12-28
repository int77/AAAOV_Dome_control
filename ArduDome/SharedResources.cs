using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using ASCOM.Utilities;

namespace ASCOM.AAAOV
{
    public static class SharedResources 
    {
        private static readonly object lockObject = new object();

        private static readonly ASCOM.Utilities.Serial ASCOMSerial = new ASCOM.Utilities.Serial();

        private static int ConnectedClients = 0;

        private static string CMD_START { get { return ":"; } }
        private static string CMD_END { get { return "#"; } }

        private static string CMD_SPACER { get { return " "; } }
        

        /// <summary>
        /// Shared serial port
        /// </summary>
        private static ASCOM.Utilities.Serial SharedSerial
        {
            get
            {
                return ASCOMSerial;
            }
        }

        /// <summary>
        /// Com Port name
        /// </summary>
        public static string COMPortName
        {
            get
            {
                return SharedSerial.PortName;
            }
            set
            {
                if (SharedSerial.Connected)
                {
                    //LogMessage("SharedResources::COMPortName", "NotSupportedException: Serial port already connected");
                    throw new NotSupportedException("Serial port already connected");
                }

                SharedSerial.PortName = value;
                //LogMessage("SharedResources::COMPortName", "New serial port name: {0}", value);
            }
        }

        /// <summary>
        /// number of connections to the shared serial port
        /// </summary>
        public static int Connections
        {
            get
            {
                //LogMessage("Connections", "ConnectedClients: {0}", ConnectedClients);
                return ConnectedClients;
            }
            set
            {
                ConnectedClients = value;
                //LogMessage("Connections", "ConnectedClients new value: {0}", ConnectedClients);
            }
        }

        public static bool Connected
        {
            get
            {
                //LogMessage("SharedResources::Connected", "SharedSerial.Connected: {0}", SharedSerial.Connected);
                return SharedSerial.Connected;
            }
            set
            {
                //                if (SharedSerial.Connected == value) { return; }

                // Check if we are the first client using the shared serial
                if (value)
                {
                    //LogMessage("SharedResources::Connected", "New connection request");

                    if (Connections == 0)
                    {
                        //LogMessage("SharedResources::Connected", "This is the first client");

                        // Check for a valid serial port name
                        if (Array.IndexOf(SharedSerial.AvailableCOMPorts, SharedSerial.PortName) > -1)
                        {
                            lock (lockObject)
                            {
                                // Sets serial parameters
                                SharedSerial.Speed = SerialSpeed.ps9600;
                                SharedSerial.ReceiveTimeout = 5;
                                SharedSerial.Connected = true;

                                Connections++;
                                //LogMessage("SharedResources::Connected", "Connected successfully");
                            }
                        }
                        else
                        {
                            //LogMessage("SharedResources::Connected", "Connection aborted, invalid serial port name");
                        }
                    }
                    else
                    {
                        lock (lockObject)
                        {
                            Connections++;
                            //LogMessage("SharedResources::Connected", "Connected successfully");
                        }
                    }
                }
                else
                {
                    //LogMessage("SharedResources::Connected", "Disconnect request");

                    lock (lockObject)
                    {
                        // Check if we are the last client connected
                        if (Connections == 1)
                        {
                            SharedSerial.ClearBuffers();
                            SharedSerial.Connected = false;
                            //LogMessage("SharedResources::Connected", "This is the last client, disconnecting the serial port");
                        }
                        else
                        {
                            //LogMessage("SharedResources::Connected", "Serial connection kept alive");
                        }

                        Connections--;
                        //LogMessage("SharedResources::Connected", "Disconnected successfully");
                    }
                }
            }
        }

        public static string SendSerialMessage(string message)
        {
            string retval = String.Empty;

            if (SharedSerial.Connected)
            {
                lock (lockObject)
                {
                    SharedSerial.ClearBuffers();
                    SharedSerial.Transmit(CMD_START + message + CMD_END);
                    //LogMessage("SharedResources::SendSerialMessage", "Message: {0}", CMD_START + message + CMD_END);

                    try
                    {
                        retval = SharedSerial.ReceiveTerminated(CMD_END).Replace(CMD_END, String.Empty);
                        //LogMessage("SharedResources::SendSerialMessage", "Message received: {0}", retval);
                    }
                    catch (Exception)
                    {
                        //LogMessage("SharedResources::SendSerialMessage", "Serial timeout exception while receiving data");
                    }

                    //LogMessage("SharedResources::SendSerialMessage", "Message sent: {0} received: {1}", CMD_START + message + CMD_END, retval);
                }
            }
            else
            {
                //throw new NotConnectedException("SendSerialMessage");
                //LogMessage("SharedResources::SendSerialMessage", "NotConnectedException");
            }

            return retval;
        }

        public static void SendSerialMessageBlind(string message)
        {
            if (SharedSerial.Connected)
            {
                lock (lockObject)
                {
                    SharedSerial.Transmit(CMD_START + CMD_SPACER + message + CMD_SPACER + CMD_END);
                    //LogMessage("SharedResources::SendSerialMessage", "Message: {0}", CMD_START + message + CMD_END);
                }
            }
            else
            {
                //throw new NotConnectedException("SendSerialMessageBlind");
                //LogMessage("SharedResources::SendSerialMessageBlind", "NotConnectedException");
            }
        }

    }
}
