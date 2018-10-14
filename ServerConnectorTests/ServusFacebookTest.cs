using System;
using System.Collections.Generic;
using System.Linq;
using IServerConnectorStandard;
using Xunit;
using Xunit.Abstractions;

namespace ServerConnectorTests
{
    public class ServusFacebookTest
    {
        //private const string Url = "188.40.132.105";
        private const string Url = "127.0.0.1";
        //private const string Url = "192.168.0.99";
        private const double SleepOffset = 2d;
        private const int Port = 4000;
        private readonly ServerConnectorStandardLIB.ServerConnector _connection;
        private readonly List<FbStates> _receivedEvents;
        private readonly ITestOutputHelper _output;
        private readonly IServusFacebook _facebookLogin;
        private static Random _random = new Random();
       
        public ServusFacebookTest(ITestOutputHelper output)
        {

            this._output = output;
            this._connection = new ServerConnectorStandardLIB.ServerConnector("../../../conf/facebook/server.json");
            this._facebookLogin = new ServusFacebook.ServusFacebook("../../../conf/facebook/facebookLogin.json");
            this._facebookLogin.Settings.Autologin = false;
            _receivedEvents = new List<FbStates>();
            _facebookLogin.FbLoginStatesChanged += (FbStates fBLoginStates) =>
            {
                _receivedEvents.Add(fBLoginStates);
            };
        }
        private void Connect()
        {
            Assert.NotNull(_connection);
            _connection.Connect(Url, Port);
            SleepModifier(250);
            Assert.NotNull(_connection);
            Assert.Equal(ServerStates.Connected, _connection.GetActServerState());
        }
        private void Disconnect()
        {
            _receivedEvents.Clear();
            _connection.Disconnect();
            SleepModifier(1000);
            Assert.Equal(ServerStates.Disconnected, _connection.GetActServerState());
        }

        [Fact]
        public void FbInitWrongTest()
        {
            Connect();
            _facebookLogin.Init();
            System.Threading.Thread.Sleep(250);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(FbStates.ClientError, _receivedEvents[0]);
            Disconnect();
        }

        [Fact]
        public void FbInitTest()
        {
            Connect();
            _facebookLogin.SetServerConnector(_connection);
            _facebookLogin.Init();
            SleepModifier(250);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(FbStates.InitComplete, _receivedEvents[0]);
            Disconnect();
            SleepModifier(500);
        }

        private void FbInit()
        {
            Connect();
            _facebookLogin.SetServerConnector(_connection);
            _facebookLogin.Init();
            SleepModifier(250);
            _receivedEvents.Clear();
        }

		[Fact]
        public void FbwRightAccessTest()
        {
            FbInit();
			_facebookLogin.Settings.FbUserid = "106019590338239";
            _facebookLogin.Settings.FbToken = "EAARSBAF8qvYBAIhZC0qOZAhf7fL6sJyQo5AXpvBW5cwsRueOzNjrVCZAFrw4Btd37Np5CFfWTZBpljdWxoV5PgTAoEgPUZBrzGuh7YVKy5aYJnudW3mCGxcdqIppU1GZB3V1JPFDfd2MRRkybBbQfzYSNpvZBLKFjSOIWytce1GCYarr5CuZCEXBaidFE3dhn5wAaqr6EnBoZAj1ZCHIzmmex7GGMvcSETySxwZCBjTFDccY7rFYcIoaf7W";
            _facebookLogin.Login();
            SleepModifier(5000); // High because of FB
			if (_receivedEvents.Count == 4)
            {
                Assert.Equal(4, _receivedEvents.Count);
                Assert.Equal(FbStates.LoginOngoing, _receivedEvents[0]);
                Assert.Equal(FbStates.RegisterOngoing, _receivedEvents[1]);
				Assert.Equal(FbStates.LoginOngoing, _receivedEvents[2]);
				Assert.Equal(FbStates.LoginComplete, _receivedEvents[3]);
            }
			else
            {
                Assert.Equal(2, _receivedEvents.Count);
                Assert.Equal(FbStates.LoginOngoing, _receivedEvents[0]);
				Assert.Equal(FbStates.LoginComplete, _receivedEvents[1]);
            }
            Disconnect();
            SleepModifier(500);
        }

       
        [Fact]
        public void FbWrongAccessTest()
        {
            FbInit();
            _facebookLogin.Settings.FbUserid = "FALSE";
            _facebookLogin.Settings.FbToken = "FALSE";
            _facebookLogin.Login();
            SleepModifier(5000); // High because of FB
            Assert.Equal(3, _receivedEvents.Count);
            Assert.Equal(FbStates.LoginOngoing, _receivedEvents[0]);
			Assert.Equal(FbStates.RegisterOngoing, _receivedEvents[1]);
			Assert.Equal(FbStates.LoginError, _receivedEvents[2]);
            Disconnect();
            SleepModifier(500);
        }
        private static void SleepModifier(int ms)
        {
            int sleep = Convert.ToInt32(ms * SleepOffset);
            System.Threading.Thread.Sleep(sleep);
        }
    }
}
