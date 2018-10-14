using System;
using System.Text;

namespace IServerConnectorStandard
{
    public class ServerSettings
    {

        public int DefaultPort { get; set; }

        public int TimeoutInMs { get; set; }
        public int ConnectRetryCount { get; set; }
        public int QueueingSize { get; set; }
        public int SendRetryCount { get; set; }
        public int RetryTimeoutInMs { get; set; }
        public int EventTriggerTimerinMs { get; set; }
        public bool WithEventTimers { get; set; }
        public int ConnectionMtu { get; set; }
        public int UnstableCounter { get; set; }
        public int SenderBurst { get; set; }
        public String Ip { get; set; }
        public int Port { get; set; }

        public ServerSettings()
        {
            DefaultPort = 0;
            TimeoutInMs = 5000;
            ConnectRetryCount = 3;
            SendRetryCount = 3;
            RetryTimeoutInMs = 30000;
            EventTriggerTimerinMs = 1000;
            WithEventTimers = true;
            ConnectionMtu = 1492;
            UnstableCounter = 5;
            QueueingSize = 1000;
            SenderBurst = 25;
        }
        public override string ToString()
        {
            StringBuilder configStringBuilder = new StringBuilder();

            configStringBuilder.Append("DefaultPort: ");
            configStringBuilder.Append(DefaultPort);
            configStringBuilder.Append("\nTimeoutInMs: ");
            configStringBuilder.Append(TimeoutInMs);
            configStringBuilder.Append("\nConnectRetryCount: ");
            configStringBuilder.Append(ConnectRetryCount);
            configStringBuilder.Append("\nSendRetryCount: ");
            configStringBuilder.Append(SendRetryCount);
            configStringBuilder.Append("\nRetryTimeoutInMs: ");
            configStringBuilder.Append(RetryTimeoutInMs);
            configStringBuilder.Append("\nEventTriggerTimerinMs: ");
            configStringBuilder.Append(EventTriggerTimerinMs);
            configStringBuilder.Append("\nWithEventTimers: ");
            configStringBuilder.Append(WithEventTimers);
            configStringBuilder.Append("\nConnectionMtu: ");
            configStringBuilder.Append(ConnectionMtu);
            configStringBuilder.Append("\nUnstableCounter: ");
            configStringBuilder.Append(UnstableCounter);
            configStringBuilder.Append("\nQueueingSize: ");
            configStringBuilder.Append(QueueingSize);
            configStringBuilder.Append("\nSenderBurst: ");
            configStringBuilder.Append(SenderBurst);

            return configStringBuilder.ToString();
        }
        public static ServerSettings LoadSettingsFromFile(string filename)
        {
            return JsonHelper.JsonObjFromFile<ServerSettings>(filename);
        }

        public static void SaveSettingsFromFile(string filename,ServerSettings obj)
        {
            JsonHelper.JsonObjToFile<ServerSettings>(filename,obj);
        }
    }
}
