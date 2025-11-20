using Crestron.SimplSharp;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace _2203SB_RC1_1.Devices
{

    /// <summary>
    /// Provides the feedback from the TCP device beoing communicated with
    /// </summary>
    public class TcpDeviceDataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Data received from tcp device
        /// </summary>
        public string data { get; set; }
    }
    /// <summary>
    /// Class for communicating with Tascam SSR250N
    /// </summary>
    public interface ITcpDevice 
    {
        string _ipAddress { get; set; }
        int _port { get; set; }
        string _userName { get; set; }
        string _password { get; set; }

        SimpleTcpClient _tcpClient { get; set; }

        bool IsConnected { get; }

        bool _reconnectInProgress { get; set; }

        object _lock { get; set; }
        Dictionary<string, string> commands { get; set; }

        bool disposedValue { get; set; }

        //Events 
        event EventHandler OnDisconnected;
        event EventHandler OnReconnected;
        event EventHandler<TcpDeviceDataReceivedEventArgs> OnDataReceived;

        void Connect();

        void Connected(object sender, ConnectionEventArgs e);

        void Disconnected(object sender, ConnectionEventArgs e);
        void DataReceived(object sender, DataReceivedEventArgs e);

        void ScheduleReconnect();

        void Send(string command);

        void SendCommand(string command);

        void Disconnect();
    }
}
