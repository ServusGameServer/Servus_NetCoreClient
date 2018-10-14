using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServusProtobuf;
using IServerConnectorStandard;
using Google.Protobuf;
namespace ServerConnectorStandardLIB
{
    public class ServerConnector : IServerConnector
    {
        public event MessageArrived RecieveMessage;
        public event InternalServerstatemachine ServerStateChanged;
        public event LoggingDebugOutput InternalDebugLogging;

        private Queue<ServusMessage> _internalSenderQueue;
        private IPAddress _ipAdress = null;
        private IPEndPoint _serverEndPoint = null;
        private TcpClient _tcpClient = null;
        private ServerSettings _settings = null;
        private ManualResetEvent _connectResetEvent = new ManualResetEvent(false);
        private Thread _connectionSetupThread;
        private Thread _recieverThread;
        private Thread _pollingConnectionAliveThread;
        private Thread _senderThread;
        private ServerTimer _internalConnectionTimer = new ServerTimer();
        private ServerStates _internalState;
        private string _internalConfigFilePath;
        private ServusMessage _actSenderObj = null;
        private CancellationTokenSource _senderThreadCts;
        private SemaphoreSlim _senderThreadSemaphore;
        private CancellationTokenSource _recieverThreadCts;
        private SemaphoreSlim _recieverThreadSemaphore;
        private CancellationTokenSource _pollingThreadCts;
        private SemaphoreSlim _pollingThreadSemaphore;

        public ServerConnector(string configFilePath)
        {
            _internalConfigFilePath = configFilePath;

        }

        public ServerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }
        public void ConnectWithConfig()
        {
            /// Dummy Values;
            Connect(null, 0, true);
        }
        public void Connect(string url, int port)
        {
            this.Connect(url, port, false);
        }
        private void Connect(string url, int port,bool withConfig)
        {
            Log(LogLevel.Info, "Connect - BEGIN Connect Function");
            Log(LogLevel.Info, "Connect - Load Settings from Path: " + _internalConfigFilePath);
            _settings = ServerSettings.LoadSettingsFromFile(_internalConfigFilePath);
            if (_settings == null)
            {
                this.ChangeServerStateStatus(ServerStates.SettingsError);
                Log(LogLevel.Error, "Connect - Error no Settings loaded");
                return;
            }
            if (withConfig)
            {
                Log(LogLevel.Info, "ConnectWithConfig - IP: " + _settings.Ip);
                Log(LogLevel.Info, "ConnectWithConfig - Port: " + _settings.Port);
                url = _settings.Ip;
                port = _settings.Port;
            }
            Log(LogLevel.Info, "Connect - Settings: " + _settings.ToString());
            _internalSenderQueue = new Queue<ServusMessage>(_settings.QueueingSize);
            if (port <= 0)
            {
                port = this._settings.DefaultPort;
            }
            try
            {
                this._ipAdress = IPAddress.Parse(url);
            }
            catch (Exception ex)
            {
                this.ChangeServerStateStatus(ServerStates.UrlError);
                Log(LogLevel.Error, "Connect - Error Wrong URL given");
                Log(LogLevel.Error, ex.ToString());
                return;
            }
            this._serverEndPoint = new IPEndPoint(this._ipAdress, port);

            this._connectionSetupThread = new Thread(this.InternalConnect);
            this._connectionSetupThread.Start();
            Log(LogLevel.Info, "Connect - END Connect Function");
        }

        private void Log(LogLevel level, String message)
        {
            if (InternalDebugLogging != null)
            {
                InternalDebugLogging.Invoke(level, message);
            }
        }

        public void AbortConnection()
        {
            if (_internalState != ServerStates.Connecting && _internalState != ServerStates.RetryWaiting) return;
            if (this._settings.WithEventTimers && this._internalConnectionTimer != null)
            {
                this._internalConnectionTimer.StopTimer();
            }
            if (this._connectionSetupThread != null && this._connectionSetupThread.IsAlive)
            {
                ///old TODO
                ////this._connectionSetupThread.Abort();
            }
            this.ChangeServerStateStatus(ServerStates.ConnectionFailed);
        }

        internal void InternalConnect()
        {
            this.SetDefaultConnectionSettings();
            this.SetUpConnnection();

        }

        internal void ChangeServerStateStatus(ServerStates newState)
        {
            if (this.ServerStateChanged != null)
            {
                this.ServerStateChanged.Invoke(newState);
            }
            this._internalState = newState;
        }

        internal void SetDefaultConnectionSettings()
        {
            //TODO Needed?
        }
        internal void SetUpConnectionSettings()
        {
            this._tcpClient = new TcpClient();
        }

        internal void SetUpConnnection()
        {
            for (int j = 0; j <= this._settings.SendRetryCount; j++)
            {
                SetUpConnectionSettings();
                if (this._settings.WithEventTimers)
                {
                    this._internalConnectionTimer.StartTimer(this._settings.TimeoutInMs, this._settings.EventTriggerTimerinMs);
                }
                this.ChangeServerStateStatus(ServerStates.Connecting);
                try {
                    Task connectTask = this._tcpClient.ConnectAsync(this._serverEndPoint.Address, this._serverEndPoint.Port);
                    connectTask.Wait(this._settings.TimeoutInMs);
                }
                catch (Exception ex)
                {
                    ///TODO more specific
                    Log(LogLevel.Error, ex.ToString());
                }
                if (this._settings.WithEventTimers)
                {
                    this._internalConnectionTimer.StopTimerAsync();
                }

                try
                {

                    if (this._tcpClient != null)
                    {
                        if (this._tcpClient.Connected)
                        {
                            this.ChangeServerStateStatus(ServerStates.Connected);
                            _pollingThreadCts = new CancellationTokenSource();
                            _pollingThreadSemaphore = new SemaphoreSlim(1);
                            this._pollingConnectionAliveThread = new Thread(this.ConnectionAlivePolling);
                            this._pollingConnectionAliveThread.Start();
                            _recieverThreadCts = new CancellationTokenSource();
                            _recieverThreadSemaphore = new SemaphoreSlim(1);
                            this._recieverThread = new Thread(this.RecieverThread);
                            this._recieverThread.Start();
                            _senderThreadCts = new CancellationTokenSource();
                            _senderThreadSemaphore = new SemaphoreSlim(1);
                            this._senderThread = new Thread(this.SenderThread);
                            this._senderThread.Start();
                            return;
                        }
                    }
                    if (this._tcpClient != null)
                    {
                        if (_tcpClient.Client != null)
                        {
                            this._tcpClient.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }
                this.ChangeServerStateStatus(ServerStates.RetryWaiting);
                if (this._settings.WithEventTimers)
                {
                    this._internalConnectionTimer.StartTimer(this._settings.RetryTimeoutInMs, this._settings.EventTriggerTimerinMs);
                }
                _connectResetEvent.WaitOne(this._settings.RetryTimeoutInMs);
                _connectResetEvent.Reset();
                if (this._settings.WithEventTimers)
                {
                    this._internalConnectionTimer.StopTimer();
                }
            }
            this.ChangeServerStateStatus(ServerStates.ConnectionFailed);
        }

        public void Disconnect()
        {
            Log(LogLevel.Info, "Disconnect - Begin");
            try
            {
                if (this._connectionSetupThread != null && this._connectionSetupThread.IsAlive)
                {
                    ///old TODO
                    ///this._connectionSetupThread.Abort();
                    this._connectionSetupThread = null;
                }
                if (this._settings.WithEventTimers && this._internalConnectionTimer != null)
                {
                   this._internalConnectionTimer.StopTimer();
                }
                if (this._pollingConnectionAliveThread != null && this._pollingConnectionAliveThread.IsAlive)
                {
                    _pollingThreadCts.Cancel();
                    _pollingThreadSemaphore.Wait(250);
                    if (_pollingThreadCts != null) _pollingThreadCts.Dispose();
                    this._pollingConnectionAliveThread = null;
                    _pollingThreadSemaphore = null;
                    _pollingThreadCts = null;
                }
                if (this._recieverThread != null && this._recieverThread.IsAlive)
                {
                    _recieverThreadCts.Cancel();
                    _recieverThreadSemaphore.Wait(250);
                    if (_recieverThreadCts != null) _recieverThreadCts.Dispose();
                    this._recieverThread = null;
                    _recieverThreadSemaphore = null;
                    _recieverThreadCts = null;
                }
                if (this._senderThread != null && this._senderThread.IsAlive)
                {
                    _senderThreadCts.Cancel();
                    lock (_internalSenderQueue)
                    {
                        Monitor.PulseAll(_internalSenderQueue);
                    }
                    _senderThreadSemaphore.Wait(250);
                    if (_senderThreadCts != null) _senderThreadCts.Dispose();
                    this._senderThread = null;
                    _senderThreadSemaphore = null;
                    _senderThreadCts = null;
                }
                try
                {
                    this._tcpClient.Dispose();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Info, "Disconnect - Exception " + ex.Message.ToString());
                }
                this.ChangeServerStateStatus(ServerStates.Disconnected);
                if (_internalSenderQueue != null)
                {
                    _internalSenderQueue.Clear();
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Info, "Disconnect - Exception " + ex.Message.ToString());
            }
            Log(LogLevel.Info, "Disconnect - End");
        }

        internal void ConnectionAlivePolling()
        {
            int unstableCounter = 0;
            _pollingThreadSemaphore.Wait();
            while (true)
            {
                try
                {
                    if (_pollingThreadCts == null)
                    {
                        Log(LogLevel.Info, "Polling-Alive CTS NULL");
                        if (_pollingThreadSemaphore != null) _pollingThreadSemaphore.Release();
                        return;
                    }
                    if(_pollingThreadCts.IsCancellationRequested){
                        Log(LogLevel.Info, "Polling-Alive CTS Release");
                        _pollingThreadSemaphore.Release();
                        return;
                    }
                    bool conStatus = _tcpClient.Client.Poll(0, SelectMode.SelectRead);
                    /// WTF?
                    /// Second Variable for Debuging with Breakpoint TBD Cleanup
                    bool conStatus2 = _tcpClient.Client.Poll(0, SelectMode.SelectWrite);
                    if (!conStatus2 || conStatus)
                    {
                        byte[] buff = new byte[1];
                        if (_tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            unstableCounter += 1;
                        }
                    }
                    else
                    {
                        unstableCounter = 0;
                    }
                    if (unstableCounter > _settings.UnstableCounter)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    ///TODO more specific
                    Log(LogLevel.Error, ex.ToString());
                }
                Thread.Sleep(500);
            }
            this.ChangeServerStateStatus(ServerStates.ConnectionLost);
            Log(LogLevel.Info, "Switch Connection");
            if (this._recieverThread != null && this._recieverThread.IsAlive)
            {
                _recieverThreadCts.Cancel();
                _recieverThreadSemaphore.Wait(250);
                if (_recieverThreadCts != null) _recieverThreadCts.Dispose();
                this._recieverThread = null;
                _recieverThreadSemaphore = null;
                _recieverThreadCts = null;
                ///old TODO
                ///this._recieverThread.Abort();
            }
            if (this._senderThread != null && this._senderThread.IsAlive)
            {
                _senderThreadCts.Cancel();
                _senderThreadSemaphore.Wait(250);
                if (_senderThreadCts != null) _senderThreadCts.Dispose();
                this._senderThread = null;
                _senderThreadSemaphore = null;
                _senderThreadCts = null;
                ///old TODO
                    ///this._senderThread.Abort();
            }
            try
            {
                this._tcpClient.Dispose();
            }
            catch (Exception ex)
            {
                ///TODO more specific
                Log(LogLevel.Error, ex.ToString());
            }

            if (_pollingThreadCts != null) _pollingThreadCts.Dispose();
            _pollingThreadSemaphore.Release();
            ///WTF TO DISCUSS FAST Recon Problems
            Thread.Sleep(500);
            this._connectionSetupThread = new Thread(this.InternalConnect);
            this._connectionSetupThread.Start();
        }

        internal void RecieverThread()
        {
            NetworkStream nwStream = _tcpClient.GetStream();
            _recieverThreadSemaphore.Wait();
            while (true)
            {
                if (_recieverThreadCts == null)
                {
                    Log(LogLevel.Info, "Reciever CTS NULL");
                    if (_recieverThreadSemaphore != null) _recieverThreadSemaphore.Release();
                    return;
                }
                if (_recieverThreadCts.IsCancellationRequested)
                {
                    Log(LogLevel.Info, "Reciever CTS Release");
                    _recieverThreadSemaphore.Release();
                    return;
                }
                int nextRead, length, response = 0;
                byte[] messagebuffer = new byte[4];
                ServusMessage servusMessage = null;
                try
                {
                    if(nwStream.DataAvailable){
                        response = nwStream.Read(messagebuffer, 0, 4);
                    }
                    else
                    {
                        Thread.Sleep(5);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    ///TODO more specific
                    Log(LogLevel.Error, ex.ToString());
                }
                if (response == 0) { Thread.Sleep(10); continue; } /// Else to High CPU Rate on Disconnect
                Array.Reverse(messagebuffer);
                length = BitConverter.ToInt32(messagebuffer, 0);
                System.IO.MemoryStream mStream = new System.IO.MemoryStream(length);
                try
                {
                    while (length > 0)
                    {
                        if (length >= _settings.ConnectionMtu)
                        {
                            nextRead = _settings.ConnectionMtu;
                        }
                        else
                        {
                            nextRead = length;
                        }
                        byte[] buffer = new byte[nextRead];
                        response = nwStream.Read(buffer, 0, nextRead);
                        if (response == 0)
                        {
                            Thread.Sleep(10);/// Else to High CPU Rate on Disconnect
                        }
                        else
                        {

                            mStream.Write(buffer, 0, response);
                            length -= response;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ///TODO more specific
                    Log(LogLevel.Error, ex.ToString());

                }
                if (response == 0) { Thread.Sleep(10); continue; } /// Else to High CPU Rate on Disconnect
                try
                {
                    servusMessage = ServusMessage.Parser.ParseFrom(mStream.ToArray());
                }
                catch (Exception ex)
                {
                    ///TODO more specific
                    Log(LogLevel.Error, ex.ToString());
                }
                if (servusMessage != null && RecieveMessage != null)
                {
                    RecieveMessage.Invoke(servusMessage);
                }
            }
        }



        public void SendMessage(ServusMessage obj)
        {
            if (obj == null)
            {
                Log(LogLevel.Error, "SendMessage - ERROR Gameobject was null!!!!?!");
                return;
            }
            lock (_internalSenderQueue)
            {
                _internalSenderQueue.Enqueue(obj);
                Monitor.PulseAll(_internalSenderQueue);
            }
        }

        private void SenderThread()
        {
            _senderThreadSemaphore.Wait();
            while (true)
            {
                lock (_internalSenderQueue)
                {
                    if (_senderThreadCts == null)
                    {
                        Log(LogLevel.Info, "Sender_1 CTS NULL");
                        if (_senderThreadSemaphore != null) _senderThreadSemaphore.Release();
                        return;
                    }
                    if (_senderThreadCts.IsCancellationRequested)
                    {
                        _senderThreadSemaphore.Release();
                        Log(LogLevel.Info, "Sender_1 CTS Release");
                        return;
                    }
                    while (_internalSenderQueue.Count == 0 && _actSenderObj == null)
                    {
                        Monitor.Wait(_internalSenderQueue);
                        if (_senderThreadCts == null)
                        {
                            Log(LogLevel.Info, "Sender_2 CTS NULL");
                            if (_senderThreadSemaphore != null) _senderThreadSemaphore.Release();
                            return;
                        }
                        if (_senderThreadCts.IsCancellationRequested)
                        {
                            _senderThreadSemaphore.Release();
                            Log(LogLevel.Info, "Sender_2 CTS Release");
                            return;
                        }
                    }
                    if (_actSenderObj == null)
                    {
                        _actSenderObj = _internalSenderQueue.Dequeue();
                    }
                    if (this._internalState == ServerStates.Connected)
                    {
                        try
                        {
                            NetworkStream nwStream = _tcpClient.GetStream();
                            byte[] data = _actSenderObj.ToByteArray();
                            byte[] datalength = BitConverter.GetBytes(data.Length);
                            ///Little Endian Elixir shit
                            Array.Reverse(datalength);
                            nwStream.Write(datalength, 0, 4);
                            nwStream.Flush();
                            nwStream.Write(data, 0, data.Length);
                            nwStream.Flush();
                            _actSenderObj = null;
                        }
                        catch (Exception ex)
                        {
                            Log(LogLevel.Error, "SenderThread - ERROR Gameobject was not transmited but Serverstate Connected with Error: "+ex.Message);
                            Thread.Sleep(_settings.SenderBurst);
                        }
                    }
                    else
                    {
                        Monitor.Wait(_internalSenderQueue);
                    }
                }
            }
        }

        public event ConnectionTimerDelegate ConnectingTimer
        {
            add => this._internalConnectionTimer.ConnectionTimerEvent += value;
            remove => this._internalConnectionTimer.ConnectionTimerEvent -= value;
        }

        public ServerStates GetActServerState()
        {
            return _internalState;
        }

        public void ResaveConfig()
        {
            Log(LogLevel.Info, "ResaveConfig - Start");
            ServerSettings.SaveSettingsFromFile(_internalConfigFilePath, _settings);
            Log(LogLevel.Info, "ResaveConfig - End");

        }

        

        ~ServerConnector()
        {
            Disconnect();
        }

    }
}
