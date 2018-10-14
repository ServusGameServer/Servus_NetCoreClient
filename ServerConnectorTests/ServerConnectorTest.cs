using System;
using System.Collections.Generic;
using System.Linq;
using IServerConnectorStandard;
using Xunit;
using Xunit.Abstractions;
using ServusProtobuf;

namespace ServerConnectorTests
{
    public class ServerConnectorTests
    {
        //private const string Url = "188.40.132.105";
        private const string Url = "127.0.0.1";
        //private const string Url = "192.168.0.99";
        public const double SleepOffset = 2d;
        private const int Port = 4000;
        private readonly ITestOutputHelper _output;
        private static readonly Random Random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

      
        private ServerConnectorStandardLIB.ServerConnector _connection;
        private List<ServusMessage> _receivedEvents;
        public ServerConnectorTests(ITestOutputHelper output)
        {
            this._output = output;
        }
        private void PrepareFirstServerConnector()
        {
            _connection = new ServerConnectorStandardLIB.ServerConnector("../../../conf/single/server.json");
        }


        [Fact]
        public void TestCreateNewInstance()
        {
            PrepareFirstServerConnector();
            Assert.NotNull(_connection);
        }


        [Fact]
        public void TestConnect()
        {
            PrepareFirstServerConnector();
            _connection.Connect(Url, Port);
            sleepModifier(250);
            Assert.NotNull(_connection);
            Assert.Equal(ServerStates.Connected, _connection.GetActServerState());
            _connection.Disconnect();
            _connection = null;
        }
        [Fact]
        public void TestonnectWrongAdress()
        {
            PrepareFirstServerConnector();
            _connection.Connect("127.0.0.2", Port);
            System.Threading.Thread.Sleep(5500);
            Assert.NotNull(_connection);
            Assert.Equal(ServerStates.RetryWaiting, _connection.GetActServerState());
            _connection.Disconnect();
            _connection = null;
        }
        [Fact]
        public void TestConnectDisconnect()
        {
            PrepareFirstServerConnector();
            Assert.NotNull(_connection);
            _connection.Connect(Url, Port);
            sleepModifier(1000);
            Assert.Equal(ServerStates.Connected, _connection.GetActServerState());
            _connection.Disconnect();
            sleepModifier(1000);
            Assert.Equal(ServerStates.Disconnected, _connection.GetActServerState());
            _connection.Disconnect();
            _connection = null;

        }
        private void Connect()
        {
            PrepareFirstServerConnector();
            Assert.NotNull(_connection);
            _connection.Connect(Url, Port);
            sleepModifier(250);
            Assert.NotNull(_connection);
            Assert.Equal(ServerStates.Connected, _connection.GetActServerState());
        }
        private void Disconnect()
        {
            _receivedEvents.Clear();
            _connection.Disconnect();
            sleepModifier(1000);
            Assert.Equal(ServerStates.Disconnected, _connection.GetActServerState());
            _connection.Disconnect();
            _connection = null;

        }
        private void sleepModifier(int ms)
        {
            int sleep =Convert.ToInt32(ms * SleepOffset);
            System.Threading.Thread.Sleep(sleep);
        }

        private ServusLogin_Only RegisterOnly()
        {
           
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthRegister);
            msg.ValueString = "Dummy";
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.True(_receivedEvents[0].ValueOnlyAuth.Id > 0);
            Assert.False(_receivedEvents[0].Error);
            ServusLogin_Only returnVal = _receivedEvents[0].ValueOnlyAuth;
            _receivedEvents.Clear();
            return returnVal;
        }
        [Fact]
        public void TestServerRegisterOnly()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            RegisterOnly();
            Disconnect();
        }

        [Fact]
        public void TestServerSimpleNoAuth()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Unknown, BasicFunctions.BasicUnkown);
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.Equal(_receivedEvents[0].ErrorType, ErrorType.ErrorNoAuth);
            Assert.True(_receivedEvents[0].Error);
            _receivedEvents.Clear();
            Disconnect();
       }

        [Fact]
        public void TestServerSimpleWrongModule()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Unknown, BasicFunctions.BasicUnkown);
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.Equal(_receivedEvents[0].ErrorType,ErrorType.ErrorGeneric);
            Assert.True(_receivedEvents[0].Error);
            _receivedEvents.Clear();
            Disconnect();
        }

        private void LoginViaOnly(ServusLogin_Only loginData)
        {
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthLogin);
            msg.ValueOnlyAuth = loginData;
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.True(_receivedEvents[0].ValueBool);
            Assert.False(_receivedEvents[0].Error);
            _receivedEvents.Clear();
        }
        [Fact]
        public void TestServerJoinloginViaOnly()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj= RegisterOnly();
            LoginViaOnly(obj);
        }

        [Fact]
        public void TestServerJoinloginViaOnlyWrongId()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            obj.Id = 0;
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthLogin);
            msg.ValueOnlyAuth = obj;
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].ValueBool);
            Assert.False(_receivedEvents[0].Error);
            _receivedEvents.Clear();
            Disconnect();
        }

        [Fact]
        public void TestServerJoinloginViaOnlyWrongKey()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            obj.Key = 12345;
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthLogin);
            msg.ValueOnlyAuth = obj;
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].ValueBool);
            Assert.False(_receivedEvents[0].Error);
            _receivedEvents.Clear();
            Disconnect();
        }


       
        [Theory]
        [InlineData(10,250)]
        [InlineData(1000, 500)]
        [InlineData(100000, 1000)]
        [InlineData(10000000, 6000)]
        //[InlineData(100000000, 10000)] --> Changes needed on Elix side
        public void TestServerTestModuleSendData(int countStringLength,int sleepTime)
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            string strToSend = RandomString(countStringLength);
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.TestEcho, BasicFunctions.BasicEcho);
            msg.ValueString=strToSend;
            _connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].ValueBool);
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(strToSend, _receivedEvents[0].ValueString);
            Disconnect();
        }
        [Fact]
        public void TestServerTestModuleTestModulCallbackId()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            string strToSend = RandomString(10000);
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.TestEcho, BasicFunctions.BasicEcho);
            msg.CallBackID = 123456789L;
            msg.ValueString = strToSend;
            _connection.SendMessage(msg);
            sleepModifier(100);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(strToSend, _receivedEvents[0].ValueString);
            Assert.Equal(123456789L, _receivedEvents[0].CallBackID);
            Disconnect();
        }
        [Fact]
        public void TestServerTestModuleLoginModulCallbackId()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthRegister);
            msg.ValueString = "Dummy";
            msg.CallBackID = 123456789L;
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.True(_receivedEvents[0].ValueOnlyAuth.Id > 0);
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(123456789L, _receivedEvents[0].CallBackID);
            _receivedEvents.Clear();
            Disconnect();
        }
        /// <summary>
        /// Game with only one player:-)
        /// </summary>
        [Fact]
        public void TestServerTestJoinQueue()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) =>
            {
                _receivedEvents.Add(m);
            };
            JoinQueue();
            Disconnect();
        }

   
        [Fact]
        public void TestServerTestJoinWrongQueueName()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) =>
            {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueJoin);
            msg.ValueString = "testGame2";
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.Equal(_receivedEvents[0].ErrorType, ErrorType.ErrorNoGameQueueFound);
            Assert.True(_receivedEvents[0].Error);
            Disconnect();
        }

        private string JoinQueue()
        {
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueJoin);
            msg.ValueString = "testGame_1p";
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(3, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.Equal(typeof(ServusMessage), _receivedEvents[1].GetType());
            Assert.Equal(typeof(ServusMessage), _receivedEvents[2].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.False(_receivedEvents[1].Error);
            Assert.False(_receivedEvents[2].Error);
            Assert.Equal(QueueFunctions.QueueGameid, _receivedEvents[1].Queuefunc);
            Assert.True(null != _receivedEvents[1].ValueString);
            Assert.Equal(TestGameFunctions.TgBegin, _receivedEvents[0].TestGamefunc);
            Assert.Equal("Dummy", _receivedEvents[0].ValueString);
            Assert.Equal(QueueFunctions.QueueJoin, _receivedEvents[2].Queuefunc);
            string returnString = _receivedEvents[1].ValueString;
            _receivedEvents.Clear();
            return returnString;
        }
        [Theory]
        [InlineData(10, 100)]
        [InlineData(1000, 250)]
        [InlineData(100000, 1000)]
        [InlineData(10000000, 6000)]
        public void TestServerTestEchoGame(int countStringLength, int sleepTime)
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) =>
            {
                _receivedEvents.Add(m);
            };
            string gameId = JoinQueue();
            string strToSend = RandomString(countStringLength);
            ServusMessage msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho, gameId);
            msg.ValueString = strToSend;
            _connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(strToSend, _receivedEvents[0].ValueString);
            Assert.Equal(gameId, _receivedEvents[0].GameID);
            Disconnect();
        }

        [Fact]
        public void TestServerTestEchoGameWrongGameId()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) =>
            {
                _receivedEvents.Add(m);
            };
            string gameId = JoinQueue();
            gameId += "A";
            ServusMessage msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho, gameId);
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.True(_receivedEvents[0].Error);
            Assert.Equal(_receivedEvents[0].ErrorType, ErrorType.ErrorNoGameFound);
            Disconnect();
        }

        [Fact]
        public void TestServerTestEchoGameWrongGameIdNoQueue()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) =>
            {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            string gameID = "nope";
            ServusMessage msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho, gameID);
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.True(_receivedEvents[0].Error);
            Assert.Equal(_receivedEvents[0].ErrorType, ErrorType.ErrorNoGameFound);
            Disconnect();
        }

        [Theory]
        [InlineData(10, 100)]
        [InlineData(1000, 250)]
        [InlineData(100000, 1000)]
        [InlineData(10000000, 6000)]
        public void TestServerTestEchoTwoGameMultiple(int countStringLength, int sleepTime)
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) =>
            {
                _receivedEvents.Add(m);
            };
            string gameId = JoinQueue();

            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueJoin);
            msg.ValueString = "testGame_1p";
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(3, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.Equal(typeof(ServusMessage), _receivedEvents[1].GetType());
            Assert.Equal(typeof(ServusMessage), _receivedEvents[2].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.False(_receivedEvents[1].Error);
            Assert.False(_receivedEvents[2].Error);
            Assert.Equal(QueueFunctions.QueueGameid, _receivedEvents[1].Queuefunc);
            Assert.True(null != _receivedEvents[1].ValueString);
            Assert.Equal(TestGameFunctions.TgBegin, _receivedEvents[0].TestGamefunc);
            Assert.Equal("Dummy", _receivedEvents[0].ValueString);
            Assert.Equal(QueueFunctions.QueueJoin, _receivedEvents[2].Queuefunc);
            string gameId2 = _receivedEvents[1].ValueString;
            _receivedEvents.Clear();
            ///First Gameid
            string strToSend = RandomString(countStringLength);
            msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho, gameId);
            msg.ValueString = strToSend;
            _connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(strToSend, _receivedEvents[0].ValueString);
            Assert.Equal(gameId, _receivedEvents[0].GameID);
            _receivedEvents.Clear();
            /// Second Gameid
            strToSend = RandomString(countStringLength);
            msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho, gameId2);
            msg.ValueString = strToSend;
            _connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(strToSend, _receivedEvents[0].ValueString);
            Assert.Equal(gameId2, _receivedEvents[0].GameID);
            /// First again
            _receivedEvents.Clear();
            strToSend = RandomString(countStringLength);
            msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho, gameId);
            msg.ValueString = strToSend;
            _connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.False(_receivedEvents[0].Error);
            Assert.Equal(strToSend, _receivedEvents[0].ValueString);
            Assert.Equal(gameId, _receivedEvents[0].GameID);
            Disconnect();
        }


        [Fact]
        public void TestServerTestJoinQueueError()
        {
            Connect();
            _receivedEvents = new List<ServusMessage>();
            _connection.RecieveMessage += (ServusMessage m) => {
                _receivedEvents.Add(m);
            };
            ServusLogin_Only obj = RegisterOnly();
            LoginViaOnly(obj);
            ServusMessage msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueUnkown);
            _connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEvents[0].GetType());
            Assert.Equal(_receivedEvents[0].ErrorType, ErrorType.ErrorWrongmethod);
            Assert.True(_receivedEvents[0].Error);
            Disconnect();
        }
    }
}