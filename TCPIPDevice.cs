using Crestron.SimplSharp;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace _2203SB_RC1_1.Devices
{
    /// <summary>
    /// Class for communicating with Tascam SSR250N
    /// </summary>
    class TCPIPDevice : ITcpDevice, IDisposable
    {
        public string _ipAddress { get; set; }
        public int _port { get; set; }
        public string _userName { get; set; }
        public string _password { get; set; }

        public SimpleTcpClient _tcpClient { get; set; }

        public bool IsConnected => _tcpClient != null && _tcpClient.IsConnected;

        public bool _reconnectInProgress { get; set; }

        public object _lock { get; set; }

        public bool disposedValue { get; set; }

        //Events 
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler OnReconnected;
        public event EventHandler<TcpDeviceDataReceivedEventArgs> OnDataReceived;



        /// <summary>
        /// Dictionary of common commands
        /// </summary>
        public Dictionary<string, string> commands { get; set; }

        #region Constructors

        /// <summary>
        /// Constructor for Tascam SSR250N device
        /// </summary>
        /// <param name="ipAddress">IP Address to connect to</param>
        /// <param name="port">Port number to connect to</param>
        /// <param name="userName">userName if authorization required</param>
        /// <param name="password">password if authorization required</param>
        public TCPIPDevice(string ipAddress, int port, string userName = "", string password = "")
        {

            _ipAddress = ipAddress;
            _port = port;
            _userName = userName;
            _password = password;
            _reconnectInProgress = false;
            _lock = new object();
        }

        #endregion

        public void Connect()
        {

            _tcpClient = new SimpleTcpClient(_ipAddress, _port);

            _tcpClient.Events.Connected += Connected;
            _tcpClient.Events.Disconnected += Disconnected;
            _tcpClient.Events.DataReceived += DataReceived;

            _tcpClient.Keepalive.EnableTcpKeepAlives = true;
            _tcpClient.Keepalive.TcpKeepAliveInterval = 3;
            _tcpClient.Keepalive.TcpKeepAliveTime = 3;
            _tcpClient.Keepalive.TcpKeepAliveRetryCount = 5;
            try
            {
                _tcpClient.Connect();
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Initial connection to device {_ipAddress}:{_port} failed: {ex.Message}");
                ScheduleReconnect();
            }
        }

        public virtual void Connected(object sender, ConnectionEventArgs e){
            CrestronConsole.PrintLine("Connected to TCP/IP device.");

        }

        public void Disconnected(object sender, ConnectionEventArgs e) {
            CrestronConsole.PrintLine("Disconnected from device.");
            OnDisconnected?.Invoke(this, EventArgs.Empty);
            ScheduleReconnect();
        }

        public void DataReceived(object sender, DataReceivedEventArgs e) {
            string msg = Encoding.ASCII.GetString(e.Data.Array, e.Data.Offset, e.Data.Count).Trim();
            OnDataReceived?.Invoke(this, new TcpDeviceDataReceivedEventArgs()
            {
                data = msg
            });
        }

        public void ScheduleReconnect()
        {
            if (_reconnectInProgress) return;

            _reconnectInProgress = true;

            Task.Run(async () =>
            {
                await Task.Delay(5000);
                lock (_lock)
                {
                    try
                    {
                        if (!_tcpClient.IsConnected)
                        {
                            _tcpClient.Connect();
                            OnReconnected?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        CrestronConsole.PrintLine($"Reconnect failed: {ex.Message}");
                        _reconnectInProgress = false;
                        ScheduleReconnect(); // Try again after another delay
                    }
                    finally
                    {
                        _reconnectInProgress = false;
                    }
                }
            });
        }


        public virtual void Send(string command) {
            CrestronConsole.PrintLine("Send, from TCPIPDevice class {0}.", command);

        }

        /// <summary>
        /// Sends the desired command asynchronously 
        /// </summary>
        /// <param name="command"> the key to the command dictionary</param>
        public void SendCommand(string command)
        {
            _ = Task.Run(() =>
            {
                Send(commands[command]);
            });
        }

        /// <summary>
        /// Clean up function that disconnects from the client and disposes the object properly
        /// </summary>
        public void Disconnect()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Disconnect();
                _tcpClient.Dispose();
                _tcpClient = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tcpClient.Events.Connected -= Connected;
                    _tcpClient.Events.Disconnected -= Disconnected;
                    _tcpClient.Events.DataReceived -= DataReceived;
                    Disconnect();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
