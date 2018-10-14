using System;
using IServerConnectorStandard;
using ServusProtobuf;

namespace ServusEmail
{
    public class ServusEmailLogin: IServusEmailLogin
    {
        private readonly string _configFilePath;
        public event InternalEmailStatemachine EmailLoginStatesChanged;
        private IServerConnector _internalClient;
        private EmailLoginStates _internalState;
        public bool HasConnection { get; set; }
        private Boolean _lostConnection;
        public string Usertoken { get; set; }
        public string Userid { get; set; }
        private EmailSettings _settings = null;


        public ServusEmailLogin(string configFilePath)
        {
            _configFilePath = configFilePath;
            _settings = EmailSettings.LoadSettingsFromFile(configFilePath);
        }


        public EmailSettings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
            }
        }

        public void ReSaveSettings()
        {
            EmailSettings.SaveSettingsFromFile(_configFilePath,_settings);
        }

        public void DeleteAccount()
        {
            throw new NotImplementedException();
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public void SetServerConnector(IServerConnector client)
        {
            if (client == null)
            {
                changeServerStateStatus(EmailLoginStates.ClientError);
            }
            else
            {
                if(this._internalClient != null) {
                    this._internalClient.RecieveMessage -= _internalClient_recieveMessage;
                    this._internalClient.ServerStateChanged -= InternalClientOnServerStateChanged;
                }
                this._internalClient = client;
                this._internalClient.RecieveMessage += _internalClient_recieveMessage;
                this._internalClient.ServerStateChanged += InternalClientOnServerStateChanged;
            }
        }

        private void InternalClientOnServerStateChanged(ServerStates serverState)
        {
            switch (serverState)
            {
                case ServerStates.Connected:
                    HasConnection = true;
                    if (_lostConnection)
                    {
                        ///TBD
                    }
                    else if (this._settings.Autologin==true){
                        this.Login();
                    }
                    _lostConnection = false;
                    break;
                case ServerStates.ConnectionLost:
                    HasConnection = false;
                    changeServerStateStatus(EmailLoginStates.LoginOngoing);
                    _lostConnection = true;
                    break;
                case ServerStates.Disconnected:
                    HasConnection = false;
                    changeServerStateStatus(EmailLoginStates.LogoutComplete);
                    _lostConnection = true;
                    break;

            }
        }

        internal void _internalClient_recieveMessage(ServusMessage message)
        {
            if(message.Modul == Modulename.AuthSelf){
                if(message.Error == true)
                {
                    HandleErrorMessage(message);
                }
                else
                {
                    HandleNormalMessage(message);
                }
            }
        }

        private void HandleErrorMessage(ServusMessage message)
        {
            /// TDB Check if special handling for each message necessary...
            switch (message.Authfunc)
            {
                case AuthFunctions.AuthLogin:
                    changeServerStateStatus(EmailLoginStates.LoginError);
                    break;
                case AuthFunctions.AuthRegister:
                    changeServerStateStatus(EmailLoginStates.LoginError);
                    break;
                default:
                    break;
            }
        }

        private void HandleNormalMessage(ServusMessage message)
        {
            switch (message.Authfunc)
            {
                case AuthFunctions.AuthLogin:
                    handleReciveMessage_Login(message.ValueBool);
                    break;
                case AuthFunctions.AuthRegister:
                    _settings.ServerUserid = message.ValueInt;
                    this.ReSaveSettings();
                    Login();
                    break;
                default:
                    break;
            }
        }

        internal void handleReciveMessage_Login(bool messageValue)
        {
                if (messageValue)
                {
                    if (_settings.AutoLoginAllowed)
                    {
                        _settings.Autologin = true;
                    }
                    else
                    {
                        _settings.Autologin = false;
                    }
                    this.ReSaveSettings();
                    changeServerStateStatus(EmailLoginStates.LoginComplete);
                }
                else
                {
                    changeServerStateStatus(EmailLoginStates.LoginError);
                }
        }


        internal void changeServerStateStatus(EmailLoginStates newState)
        {
            if (this.EmailLoginStatesChanged != null)
            {
                this.EmailLoginStatesChanged.Invoke(newState);
            }
            this._internalState = newState;
        }

        public void Login()
        {
            changeServerStateStatus(EmailLoginStates.LoginOngoing);
            _internalClient.SendMessage(EmailServusMessageDtoFactory.CreateServusMessageLogin(this._settings.Email, this._settings.Password));
        }
        public void Register()
        {
            changeServerStateStatus(EmailLoginStates.RegisterOngoing);
            _internalClient.SendMessage(EmailServusMessageDtoFactory.CreateServusMessageRegister(this._settings.Email, this._settings.Password, this._settings.Nickname));
        }


        public void Init()
        {
            if (_internalClient == null)
            {
                changeServerStateStatus(EmailLoginStates.ClientError);
            }
            else
            {
                changeServerStateStatus(EmailLoginStates.InitComplete);
            }
        }

        public void LoginExtBegin()
        {
            changeServerStateStatus(EmailLoginStates.LoginOngoing);
        }

        public void LoginExtEnd()
        {
            changeServerStateStatus(EmailLoginStates.LoginError);
        }

        public void GetInitialData()
        {
            changeServerStateStatus(EmailLoginStates.InitialDataRecieving);
        }

        public void SetToWaitingDataOrders()
        {
            changeServerStateStatus(EmailLoginStates.DataOrdersWaitingForNew);
        }

    }
}