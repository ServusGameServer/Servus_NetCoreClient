using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IServerConnectorStandard;
using ServerConnectorStandardLIB;
using ServusProtobuf;
using Xunit;
using Xunit.Abstractions;

namespace ServerConnectorTests
{
    public class ServerConnectorMultiTests
    {
        public ServerConnectorMultiTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        //private const string Url = "188.40.132.105";

        private const string Url = "127.0.0.1";
        //private const string Url = "192.168.0.99";
        private const double SleepOffset = 2d;
        private const int Port = 4000;
        private readonly ITestOutputHelper _output;
        private static readonly Random Random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }


        private ServerConnector _connectionFirst;
        private ServerConnector _connectionSecond;
        private List<ServusMessage> _receivedEventsFirstCon;
        private List<ServusMessage> _receivedEventsSecondCon;

        private void PrepareFirstServerConnector()
        {
            _connectionFirst = new ServerConnector("../../../conf/multi/server.json");
        }

        private void PrepareSecondServerConnector()
        {
            _connectionSecond = new ServerConnector("../../../conf/multi/server.json");
        }

        private void KillAll()
        {
            _connectionFirst.Disconnect();
            _connectionSecond.Disconnect();
        }

        private void ConnectBoth()
        {
            PrepareFirstServerConnector();
            PrepareSecondServerConnector();
            Assert.NotNull(_connectionFirst);
            Assert.NotNull(_connectionSecond);
            Assert.NotSame(_connectionFirst, _connectionSecond);
            Connect(_connectionFirst);
            Connect(_connectionSecond);
        }

        private void DisconnectBoth()
        {
            Disconnect(_connectionFirst);
            Disconnect(_connectionSecond);
            KillAll();
        }

        public void Echo1PGame(int countStringLength, int sleepTime, string gameId, ServerConnector connection,
            List<ServusMessage> receivedEvents)
        {
            var strToSend = RandomString(countStringLength);
            var msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho,
                gameId);
            msg.ValueString = strToSend;
            connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.False(receivedEvents[0].Error);
            Assert.Equal(strToSend, receivedEvents[0].ValueString);
            Assert.Equal(gameId, receivedEvents[0].GameID);
        }

        public void Echo2PGame(int countStringLength, int sleepTime, string gameId, ServerConnector connection,
            string strToSend)
        {
            var msg = ServusProtobufBasicFactory.createEchoGameMessage(Modulename.Directgame, TestGameFunctions.TgEcho,
                gameId);
            msg.ValueString = strToSend;
            connection.SendMessage(msg);
            sleepModifier(sleepTime);
        }

        [Theory]
        [InlineData(10, 250)]
        [InlineData(1000, 1250)]
        [InlineData(100000, 2500)]
        [InlineData(10000000, 7000)]
        public void PlayOneGameEach(int countStringLength, int sleepTime)
        {
            Loginboth();
            var gameId1 = joinQueue1P(_connectionFirst, _receivedEventsFirstCon, "Con1");
            var gameId2 = joinQueue1P(_connectionSecond, _receivedEventsSecondCon, "Con2");
            Echo1PGame(countStringLength, sleepTime, gameId1, _connectionFirst, _receivedEventsFirstCon);
            Echo1PGame(countStringLength, sleepTime, gameId2, _connectionSecond, _receivedEventsSecondCon);
            DisconnectBoth();
        }

        [Theory]
        [InlineData(10, 250)]
        [InlineData(1000, 1250)]
        [InlineData(100000, 2500)]
        [InlineData(10000000, 7000)]
        public void PlayGameTogehter(int countStringLength, int sleepTime)
        {
            Loginboth();
            joinQueue2P_First(_connectionFirst, _receivedEventsFirstCon);
            var gameId2 = joinQueue2P_Second(_connectionSecond, _receivedEventsSecondCon, "Con1");
            Assert.Equal(2, _receivedEventsFirstCon.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEventsFirstCon[0].GetType());
            Assert.Equal(typeof(ServusMessage), _receivedEventsFirstCon[1].GetType());
            Assert.False(_receivedEventsFirstCon[0].Error);
            Assert.False(_receivedEventsFirstCon[1].Error);
            Assert.Equal(QueueFunctions.QueueGameid, _receivedEventsFirstCon[1].Queuefunc);
            Assert.True(null != _receivedEventsFirstCon[1].ValueString);
            Assert.Equal(TestGameFunctions.TgBegin, _receivedEventsFirstCon[0].TestGamefunc);
            Assert.Equal("Con2", _receivedEventsFirstCon[0].ValueString);
            var gameId1 = _receivedEventsFirstCon[1].ValueString;
            Assert.Equal(gameId1, gameId2);
            _receivedEventsFirstCon.Clear();
            _receivedEventsSecondCon.Clear();
            var oneToTwo = RandomString(countStringLength);
            var twoToOne = RandomString(countStringLength);

            /// From 1 to 2
            Echo2PGame(countStringLength, sleepTime, gameId1, _connectionFirst, oneToTwo);
            /// From 2 to 1
            Echo2PGame(countStringLength, sleepTime, gameId2, _connectionSecond, twoToOne);

            /// Con1 Check
            Assert.Equal(1, _receivedEventsFirstCon.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEventsFirstCon[0].GetType());
            Assert.False(_receivedEventsFirstCon[0].Error);
            Assert.Equal(twoToOne, _receivedEventsFirstCon[0].ValueString);
            Assert.Equal(gameId1, _receivedEventsFirstCon[0].GameID);
            /// Con2 Check
            Assert.Equal(1, _receivedEventsSecondCon.Count);
            Assert.Equal(typeof(ServusMessage), _receivedEventsSecondCon[0].GetType());
            Assert.False(_receivedEventsSecondCon[0].Error);
            Assert.Equal(oneToTwo, _receivedEventsSecondCon[0].ValueString);
            Assert.Equal(gameId1, _receivedEventsSecondCon[0].GameID);
            _receivedEventsFirstCon.Clear();
            _receivedEventsSecondCon.Clear();
            DisconnectBoth();
        }

        [Theory]
        [InlineData(10, 100)]
        [InlineData(1000, 500)]
        [InlineData(100000, 2500)]
        [InlineData(10000000, 10000)]
        //[InlineData(100000000, 10000)] --> Changes needed on Elix side
        public void TestModulEach(int countStringLength, int sleepTime)
        {
            Loginboth();
            SendModuleDate(countStringLength, sleepTime, _connectionFirst, _receivedEventsFirstCon);
            SendModuleDate(countStringLength, sleepTime, _connectionSecond, _receivedEventsSecondCon);
            DisconnectBoth();
        }

        private string joinQueue1P(ServerConnector connection, List<ServusMessage> receivedEvents, string name)
        {
            var msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueJoin);
            msg.ValueString = "testGame_1p";
            connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(3, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.Equal(typeof(ServusMessage), receivedEvents[1].GetType());
            Assert.Equal(typeof(ServusMessage), receivedEvents[2].GetType());
            Assert.False(receivedEvents[0].Error);
            Assert.False(receivedEvents[1].Error);
            Assert.False(receivedEvents[2].Error);
            Assert.Equal(QueueFunctions.QueueGameid, receivedEvents[1].Queuefunc);
            Assert.True(null != receivedEvents[1].ValueString);
            Assert.Equal(TestGameFunctions.TgBegin, receivedEvents[0].TestGamefunc);
            Assert.Equal(name, receivedEvents[0].ValueString);
            Assert.Equal(QueueFunctions.QueueJoin, receivedEvents[2].Queuefunc);
            var returnString = receivedEvents[1].ValueString;
            receivedEvents.Clear();
            return returnString;
        }

        private string joinQueue2P_Second(ServerConnector connection, List<ServusMessage> receivedEvents, string name)
        {
            var msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueJoin);
            msg.ValueString = "testGame_2p";
            connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(3, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.Equal(typeof(ServusMessage), receivedEvents[1].GetType());
            Assert.Equal(typeof(ServusMessage), receivedEvents[2].GetType());
            Assert.False(receivedEvents[0].Error);
            Assert.False(receivedEvents[1].Error);
            Assert.False(receivedEvents[2].Error);
            Assert.Equal(QueueFunctions.QueueGameid, receivedEvents[1].Queuefunc);
            Assert.True(null != receivedEvents[1].ValueString);
            Assert.Equal(TestGameFunctions.TgBegin, receivedEvents[0].TestGamefunc);
            Assert.Equal(name, receivedEvents[0].ValueString);
            Assert.Equal(QueueFunctions.QueueJoin, receivedEvents[2].Queuefunc);
            var returnString = receivedEvents[1].ValueString;
            receivedEvents.Clear();
            return returnString;
        }

        private void joinQueue2P_First(ServerConnector connection, List<ServusMessage> receivedEvents)
        {
            var msg = ServusProtobufBasicFactory.createServusMessage(Modulename.Queue, QueueFunctions.QueueJoin);
            msg.ValueString = "testGame_2p";
            connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.False(receivedEvents[0].Error);
            Assert.Equal(QueueFunctions.QueueJoin, receivedEvents[0].Queuefunc);
            receivedEvents.Clear();
        }

        private void SendModuleDate(int countStringLength, int sleepTime, ServerConnector connection,
            List<ServusMessage> receivedEvents)
        {
            var strToSend = RandomString(countStringLength);
            var msg = ServusProtobufBasicFactory.createServusMessage(Modulename.TestEcho, BasicFunctions.BasicEcho);
            msg.ValueString = strToSend;
            connection.SendMessage(msg);
            sleepModifier(sleepTime);
            Assert.Equal(1, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.False(receivedEvents[0].ValueBool);
            Assert.False(receivedEvents[0].Error);
            Assert.Equal(strToSend, receivedEvents[0].ValueString);
        }

        private void Loginboth()
        {
            ConnectBoth();
            _receivedEventsFirstCon = new List<ServusMessage>();
            _connectionFirst.RecieveMessage += m => { _receivedEventsFirstCon.Add(m); };
            _receivedEventsSecondCon = new List<ServusMessage>();
            _connectionSecond.RecieveMessage += m => { _receivedEventsSecondCon.Add(m); };

            var con1Login = RegisterOnly(_connectionFirst, _receivedEventsFirstCon, "Con1");
            var con2Login = RegisterOnly(_connectionSecond, _receivedEventsSecondCon, "Con2");
            LoginViaOnly(_connectionFirst, _receivedEventsFirstCon, con1Login);
            LoginViaOnly(_connectionSecond, _receivedEventsSecondCon, con2Login);
        }

        private void LoginViaOnly(ServerConnector connection, List<ServusMessage> receivedEvents,
            ServusLogin_Only loginData)
        {
            var msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthLogin);
            msg.ValueOnlyAuth = loginData;
            connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.True(receivedEvents[0].ValueBool);
            Assert.False(receivedEvents[0].Error);
            receivedEvents.Clear();
        }

        private ServusLogin_Only RegisterOnly(ServerConnector connection, List<ServusMessage> receivedEvents,
            string name)
        {
            var msg = ServusProtobufBasicFactory.createServusMessage(Modulename.AuthOnly, AuthFunctions.AuthRegister);
            msg.ValueString = name;
            connection.SendMessage(msg);
            sleepModifier(500);
            Assert.Equal(1, receivedEvents.Count);
            Assert.Equal(typeof(ServusMessage), receivedEvents[0].GetType());
            Assert.True(receivedEvents[0].ValueOnlyAuth.Id > 0);
            Assert.False(receivedEvents[0].Error);
            var returnVal = receivedEvents[0].ValueOnlyAuth;
            receivedEvents.Clear();
            return returnVal;
        }

        private void Connect(ServerConnector connection)
        {
            Assert.NotNull(connection);
            connection.Connect(Url, Port);
            sleepModifier(250);
            Assert.NotNull(connection);
            Assert.Equal(ServerStates.Connected, connection.GetActServerState());
        }

        private void Disconnect(ServerConnector connection)
        {
            connection.Disconnect();
            sleepModifier(1000);
            Assert.Equal(ServerStates.Disconnected, connection.GetActServerState());
        }

        private void sleepModifier(int ms)
        {
            var sleep = Convert.ToInt32(ms * SleepOffset);
            Thread.Sleep(sleep);
        }


        [Fact]
        public void ConnectWithTwoCons()
        {
            PrepareFirstServerConnector();
            PrepareSecondServerConnector();
            Assert.NotNull(_connectionFirst);
            Assert.NotNull(_connectionSecond);
            Assert.NotSame(_connectionFirst, _connectionSecond);
            Connect(_connectionFirst);
            Connect(_connectionSecond);
            KillAll();
        }

        [Fact]
        public void DisconnectWithTwo()
        {
            ConnectBoth();
            Disconnect(_connectionFirst);
            Disconnect(_connectionSecond);
            KillAll();
        }

        [Fact]
        public void JoinOneGameEach()
        {
            Loginboth();
            var gameId1 = joinQueue1P(_connectionFirst, _receivedEventsFirstCon, "Con1");
            var gameId2 = joinQueue1P(_connectionSecond, _receivedEventsSecondCon, "Con2");
            Assert.NotSame(gameId1, gameId2);
            DisconnectBoth();
        }

        [Fact]
        public void LoginBoth()
        {
            ConnectBoth();
            _receivedEventsFirstCon = new List<ServusMessage>();
            _connectionFirst.RecieveMessage += m => { _receivedEventsFirstCon.Add(m); };
            _receivedEventsSecondCon = new List<ServusMessage>();
            _connectionSecond.RecieveMessage += m => { _receivedEventsSecondCon.Add(m); };
            var con1Login = RegisterOnly(_connectionFirst, _receivedEventsFirstCon, "Con1");
            var con2Login = RegisterOnly(_connectionSecond, _receivedEventsSecondCon, "Con2");
            LoginViaOnly(_connectionFirst, _receivedEventsFirstCon, con1Login);
            LoginViaOnly(_connectionSecond, _receivedEventsSecondCon, con2Login);
            DisconnectBoth();
        }

        [Fact]
        public void TestCreateTwoInstances()
        {
            PrepareFirstServerConnector();
            PrepareSecondServerConnector();
            Assert.NotNull(_connectionFirst);
            Assert.NotNull(_connectionSecond);
            Assert.NotSame(_connectionFirst, _connectionSecond);
            KillAll();
        }
    }
}