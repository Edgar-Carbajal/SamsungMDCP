using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace _2203SB_RC1_1.Devices
{

    /// <summary>
    /// Class for communicating with Samsung TV
    /// </summary>
    class SamsungTvTcp : TCPIPDevice
    {
        private object _statusLock { get; set; }
        private bool _statusLoopRunning = false;

        private Task _statusLoopTask { get; set; }

        private CancellationTokenSource _cts;


        public event EventHandler<SamsungTvTcpEventArgs> onStatusStateChanged;


        public SamsungTvTcp(string ipAddress, int port, string userName, string password) : base(ipAddress, port, userName, password)
        {
            commands = new Dictionary<string, string>() {
                {"Power On", "\xAA\x11\x01\x01\x01\x14" },
                {"Power Off", "\xAA\x11\x01\x01\x00\x13" },
                {"Power Get", "\xAA\x11\x01\x00\x12" }
            };
            _statusLock = new object();
            OnDataReceived += SamsungTvTcp_OnDataReceived;
            //OnDisconnected += SamsungTvTcp_OnDisconnected;
            //_cts = new CancellationTokenSource();

        }

        private void SamsungTvTcp_OnDisconnected(object sender, EventArgs e)
        {
            StopStatusLoop();
        }

        private void SamsungTvTcp_OnDataReceived(object sender, TcpDeviceDataReceivedEventArgs e)
        {
            CrestronConsole.PrintLine("Samsung data received : {0}", e.data);
            bool powerState = ParsePowerStatus(e.data);
            string powerStateString = powerState.ToString();
            onStatusStateChanged?.Invoke(this, new SamsungTvTcpEventArgs() { data = powerStateString });

        

        }
        private bool ParsePowerStatus(string data)
        {
            bool state = false;
            byte[] dataArray = Encoding.UTF8.GetBytes(data);
            byte powerFbByte = dataArray[dataArray.Length - 2];
            if (powerFbByte == 0x01)
            {
                state= true;
            }
            else if(powerFbByte == 0x00)
            {
                state = false;
            }
                return state;
        }


        public override async void Connected(object sender, ConnectionEventArgs e)
        {
            CrestronConsole.PrintLine("Connected to SamsungTv.");
            // add if status feedback loop needed
            //await Task.Delay(5000);
            //RestartStatusLoop();
        }

        public override void Send(string command)
        {
            if (!IsConnected)
            {
                CrestronConsole.PrintLine("Cannot send, not connected.");
                return;
            }

            _tcpClient.Send(command);
        }

        private void StartStatusLoop()
        {
            if (_statusLoopRunning) return;

            _statusLoopRunning = true;

            _statusLoopTask = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    await Task.Delay(5000);
                    lock (_statusLock)
                    {
                        try
                        {
                            Send(commands["Power Get"]);
                        }
                        catch (Exception ex)
                        {
                            CrestronConsole.PrintLine($"Status send failed: {ex.Message}");
                        }
                    }

                }
                _statusLoopRunning = false;
            }, _cts.Token);
        }

        private void StopStatusLoop()
        {
            lock (_statusLock)
            {
                if (_statusLoopRunning) return;

                _cts.Cancel();
                _statusLoopTask.Wait();
                _cts.Dispose();

            }
        }

        public void RestartStatusLoop()
        {
            lock (_statusLock)
            {
                if (_statusLoopRunning)
                {
                    StopStatusLoop();
                }
                StartStatusLoop();
            }
        }

        private string parseStatusState(string data)
        {
            return "";
        }
    }


    /// <summary>
    /// Provides the feedback from the SamsungTv being communicated with
    /// </summary>
    public class SamsungTvTcpEventArgs : EventArgs
    {
        /// <summary>
        /// Data received From samsung device audio recorder
        /// </summary>
        public string data { get; set; }
    }

}
