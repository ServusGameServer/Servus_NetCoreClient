using System;
using System.Collections.Generic;
using System.Linq;
using IServerConnectorStandard;
using ServusEmail;
using Xunit;
using Xunit.Abstractions;

namespace ServerConnectorTests
{
    public class ServusEmailTest
    {
        //(private const string Url = "188.40.132.105";
        private const string Url = "127.0.0.1";
        //private const string Url = "192.168.0.99";
        private const double SleepOffset = 2d;
        private const int Port = 4000;
        private readonly ServerConnectorStandardLIB.ServerConnector _connection;
        private readonly List<EmailLoginStates> _receivedEvents;
        private readonly ITestOutputHelper _output;
        private readonly ServusEmailLogin _emailLogin;
        private static readonly Random Random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
        public ServusEmailTest(ITestOutputHelper output)
        {

            this._output = output;
            this._connection = new ServerConnectorStandardLIB.ServerConnector("../../..//conf/email/server.json");
            this._emailLogin = new ServusEmailLogin("../../../conf/email/emailLogin.json");
            this._emailLogin.Settings.Autologin = false;
            _receivedEvents = new List<EmailLoginStates>();
            _emailLogin.EmailLoginStatesChanged += (EmailLoginStates emailState) =>
            {
                _receivedEvents.Add(emailState);
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
        public void EmailInitWrongTest()
        {
            Connect();
            _emailLogin.Init();
            SleepModifier(250);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.ClientError, _receivedEvents[0]);
            Disconnect();
        }

        [Fact]
        public void EmailInitTest()
        {
            Connect();
            _emailLogin.SetServerConnector(_connection);
            _emailLogin.Init();
            SleepModifier(250);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.InitComplete, _receivedEvents[0]);
            Disconnect();
            SleepModifier(500);
        }

        private void EmailInit()
        {
            Connect();
            _emailLogin.SetServerConnector(_connection);
            _emailLogin.Init();
            SleepModifier(250);
            _receivedEvents.Clear();
        }

        [Fact]
        public void EmailRightAccessTest()
        {
            EmailSettings emailSettings = EmailRegisterAcess();
            EmailInit();
            _emailLogin.Settings = emailSettings;
            _emailLogin.Login();
            SleepModifier(250);
            Assert.Equal(2, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.LoginOngoing, _receivedEvents[0]);
            Assert.Equal(EmailLoginStates.LoginComplete, _receivedEvents[1]);
            Disconnect();
        }

        [Fact]
        public void EmailRightAutoAccessTest()
        {
            EmailSettings emailSettings= EmailRegisterAcess();
            EmailRightAutoAccess(emailSettings);
            Disconnect();
        }
        public void EmailRightAutoAccess(EmailSettings intSettings)
        {
            this._emailLogin.Settings = intSettings;
            this._emailLogin.Settings.Autologin = true;
            EmailInit();
            _emailLogin.Login();
            SleepModifier(750);
            Assert.Equal(2, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.LoginOngoing, _receivedEvents[0]);
            Assert.Equal(EmailLoginStates.LoginComplete, _receivedEvents[1]);
            _receivedEvents.Clear();
        }
        [Fact]
        public void EmailWrongAccessTest()
        {
            EmailInit();
            _emailLogin.Settings.Nickname = "FALSE";
            _emailLogin.Settings.Password = "FALSE";
            _emailLogin.Login();
            SleepModifier(250);
            Assert.Equal(2, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.LoginOngoing, _receivedEvents[0]);
            Assert.Equal(EmailLoginStates.LoginError, _receivedEvents[1]);
            Disconnect();
            SleepModifier(500);
        }


        [Fact]
        public void EmailAutoLogoutAfterForcedDisconnect()
        {
            EmailSettings emailSettings = EmailRegisterAcess();
            EmailRightAutoAccess(emailSettings);
            Disconnect();
            SleepModifier(500);
            Assert.Equal(1, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.LogoutComplete, _receivedEvents[0]);
            _receivedEvents.Clear();
        }

        [Fact]
        public void EmailRegisterAccessTest()
        {
            EmailRegisterAcess();
            Disconnect();
        }

        public EmailSettings EmailRegisterAcess(){
            EmailInit();
            _emailLogin.Settings.Email = RandomString(8);
            _emailLogin.Settings.Password = RandomString(8);
            _emailLogin.Settings.Nickname = RandomString(8);
            _emailLogin.Register();
            SleepModifier(500);
            Assert.Equal(3, _receivedEvents.Count);
            Assert.Equal(EmailLoginStates.RegisterOngoing, _receivedEvents[0]);
            Assert.Equal(EmailLoginStates.LoginOngoing, _receivedEvents[1]);
            Assert.Equal(EmailLoginStates.LoginComplete, _receivedEvents[2]);
            Disconnect();
            SleepModifier(500);
            _receivedEvents.Clear();
            return this._emailLogin.Settings;
        }
        private static void SleepModifier(int ms)
        {
            int sleep = Convert.ToInt32(ms * SleepOffset);
            System.Threading.Thread.Sleep(sleep);
        }


    }
}
