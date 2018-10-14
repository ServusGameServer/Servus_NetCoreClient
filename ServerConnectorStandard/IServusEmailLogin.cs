namespace IServerConnectorStandard
{
    public delegate void InternalEmailStatemachine(EmailLoginStates emailState);
    public enum EmailLoginStates
    {
        None,
        InitComplete,
        LoginOngoing,
        RegisterOngoing,
        ClientError,
        LoginError,
        RightError,
        DeleteComplete,
        LogoutComplete,
        LoginComplete,
        InitialDataRecieving,
        InitialDataPending,
        InitialDataComplete,
        DataOrdersWaitingForNew,
        DataOrdersRecieving,
        DataOrdersPending,
        DataOrdersAvaiable,
        PermsError,
        Tbd/// In Porgress no final Use
    };
    public interface IServusEmailLogin
    {
            void SetServerConnector(IServerConnector client);
            void LoginExtBegin();
            void LoginExtEnd();
            void Login();
            void Register();
            void Logout();
            void Init();
            void DeleteAccount();
            void ReSaveSettings();
            void GetInitialData();
            void SetToWaitingDataOrders();
            EmailSettings Settings{ get; set; }
            string Usertoken { get; set; }
            string Userid { get; set; }
            event InternalEmailStatemachine EmailLoginStatesChanged;
        }
    }

