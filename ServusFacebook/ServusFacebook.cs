using System;
using System.Collections.Generic;
using System.Linq;
using IServerConnectorStandard;
using Newtonsoft.Json.Linq;
using ServusProtobuf;

namespace ServusFacebook
{
    public class ServusFacebook : IServusFacebook
    {
        private readonly string _configFilePath;
        public event InternalFbStatemachine FbLoginStatesChanged;
        private IServerConnector _internalClient;
        private FbStates _internalState;
        public bool HasConnection { get; private set; }
        private Boolean _lostConnection;
        public FbUserdata Fbuserdata { get; set; }

        public ServusFacebook(string configFilePath)
        {
            _configFilePath = configFilePath;
            Settings = FacebookSettings.LoadSettingsFromFile(configFilePath);
            Fbuserdata = new FbUserdata();
        }


        public FacebookSettings Settings { get; set; } = null;

        public void ReSaveSettings()
        {
            FacebookSettings.SaveSettingsFromFile(_configFilePath,Settings);
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
                ChangeServerStateStatus(FbStates.ClientError);
            }
            else
            {
                if (this._internalClient != null)
                {
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

                    }
                    _lostConnection = false;
                    break;
                case ServerStates.ConnectionLost:
                    HasConnection = false;
                    _lostConnection = true;
                    break;

            }
        }

        internal void _internalClient_recieveMessage(ServusMessage message)
        {
            switch (message.Modul)
            {
                case Modulename.AuthFb:
                    if (message.Error == true)
                    {
                        ChangeServerStateStatus(FbStates.LoginError);
                    }
                    else
                    {
                        HandleNormalAuthMessage(message);
                    }
                    break;
                case Modulename.AuthUserdata:
                    if (message.Error == true)
                    {
                        ChangeServerStateStatus(FbStates.LoginError);
                    }
                    else
                    {
                        HandleNormalUserDataMessage(message);
                    }
                    break;
                default:
                    break;
            }
          
        }

        public void HandleNormalUserDataMessage(ServusMessage message)
        {
            switch (message.Authfunc)
            {
                case AuthFunctions.AuthPicture:
                    if(message.ValueUDataPicture.ValueBytes.IsEmpty && message.ValueUDataPicture.Reason == USERDATA_REASONS.NoPic) {
                        ChangeServerStateStatus(FbStates.InitialDataComplete);
                        /// TDB Default pic...
                        break;
                    }
                    Fbuserdata.Picture = message.ValueUDataPicture.ValueBytes.ToArray<byte>();
                    ChangeServerStateStatus(FbStates.InitialDataComplete);
                    break;
                default:
                    break;
            }
        }

        private void HandleNormalAuthMessage(ServusMessage message)
        {
            switch (message.Authfunc)
            { 
                case AuthFunctions.AuthLogin:
                    if (message.ValueFBAuthResp.LoginSucessful)
                    {
                        Settings.Autologin = true;
                        this.ReSaveSettings();
                        ChangeServerStateStatus(FbStates.LoginComplete);
                        break;
                    }
                    switch (message.ValueFBAuthResp.Reason)
                    {
                        case FB_REASONS.IdNotMatchedToToken:
                            ChangeServerStateStatus(FbStates.LoginError);
                            break;
                        case FB_REASONS.IdNotFound:
                            Register();
                            break;
                        default:
                            ChangeServerStateStatus(FbStates.LoginError);
                            break;
                    }
                    break;
                case AuthFunctions.AuthRegister:
                    Settings.FbToken = message.ValueFBAuth.Token;
                    Settings.ServerUserid = message.ValueFBAuth.FbId;
                    if (CheckLoginData())
                    {
                        this.ReSaveSettings();
                        Login();
                    }
                    break;
                default:
                    ChangeServerStateStatus(FbStates.LoginError);
                    break;
            }
        }

        private void handleReciveMessage_Picture(object messageValue)
        {
            throw new NotImplementedException();
            /* if (messageValue is Newtonsoft.Json.Linq.JObject)
             {
                 foreach (KeyValuePair<string, JToken> k_Value in (JObject) messageValue)
                 {
                     switch (k_Value.Key)
                     {
                         case "picture":
                             Fbuserdata.Picture = k_Value.Value.ToObject<byte[]>();
                             changeServerStateStatus(FBStates.InitialDataComplete);
                             break;
                         case "id":
                             ///Nothing to do;
                             break;
                     }
                 }
             }
             else if (messageValue is string)
             {
                 switch ((string)messageValue)
                 {
 
                     case "no_pic_for_id":
                     case "no_pic_for_id_file_error":
                         ///TBD
                         break;
                     default:
                         ///TDB
                         ///changeServerStateStatus(FBStates.LoginError);
                         break;
                 }
             }
                 else
             {
                 /// TBD ERROR
             }*/
        }

        internal void ChangeServerStateStatus(FbStates newState)
        {
            if (this.FbLoginStatesChanged != null)
            {
                this.FbLoginStatesChanged.Invoke(newState);
            }
            this._internalState = newState;
        }

        private bool CheckLoginData()
		{
			if (String.IsNullOrEmpty(Settings.FbToken) || String.IsNullOrEmpty(Settings.FbUserid))
			{
				this.ChangeServerStateStatus(FbStates.LoginError);
				return false;
			}
			return true;
		}

        public void Login()
        {
            ChangeServerStateStatus(FbStates.LoginOngoing);
            _internalClient.SendMessage(FbServusMessageDtoFactory.CreateServusMessageLogin(Settings.FbUserid,Settings.FbToken));
        }
        private void Register()
        {
            ChangeServerStateStatus(FbStates.RegisterOngoing);
            _internalClient.SendMessage(FbServusMessageDtoFactory.CreateServusMessageRegister(Settings.FbUserid, Settings.FbToken));

        }

        private void GetSelfPicture()
        {
            _internalClient.SendMessage(FbServusMessageDtoFactory.CreateServusMessage(Settings.ServerUserid));
        }

        public void Init()
        {
            ChangeServerStateStatus(_internalClient == null ? FbStates.ClientError : FbStates.InitComplete);
        }

        public void LoginExtBegin()
        {
            ChangeServerStateStatus(FbStates.LoginOngoing);
        }

        public void LoginExtEnd()
        {
            ChangeServerStateStatus(FbStates.LoginError);
        }

        public void GetInitialData()
        {
            ChangeServerStateStatus(FbStates.InitialDataRecieving);
            GetSelfPicture();
        }

        public void SetToWaitingDataOrders()
        {
            ChangeServerStateStatus(FbStates.DataOrdersWaitingForNew);
        }
    }


}
