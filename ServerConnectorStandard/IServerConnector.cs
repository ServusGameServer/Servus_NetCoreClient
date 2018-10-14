using System;
using ServusProtobuf;
namespace IServerConnectorStandard
{
    public delegate void MessageArrived(ServusMessage message);
    public delegate void ConnectionTimerDelegate(int actTimming);
    public delegate void InternalServerstatemachine (ServerStates serverState);
    public delegate void LoggingDebugOutput(LogLevel level,String message);
    public enum ServerStates
    {
        None,
        UrlError,
        SettingsError,
        Connecting,
        RetryWaiting,
        Connected,
        ConnectionFailed,
        ConnectionLost,
        Disconnected,
        Tbd/// In Porgress no final Use
    };

    public enum LogLevel
    {
        None=0,
        Info=1,
        Error=2,
        All =3
    }

    public interface IServerConnector
    {
        void Connect(String url, int port);
        void ResaveConfig(); //More likely a game dev Option(Backendchange in APP)
        void ConnectWithConfig();
        void Disconnect();
        void AbortConnection();
        void SendMessage(ServusMessage message);
        ServerSettings Settings { get; set; }
        ServerStates GetActServerState();
        event MessageArrived RecieveMessage;
        event ConnectionTimerDelegate ConnectingTimer;
        event InternalServerstatemachine ServerStateChanged;
        event LoggingDebugOutput InternalDebugLogging;
    }
}
