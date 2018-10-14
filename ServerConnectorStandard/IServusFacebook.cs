namespace IServerConnectorStandard
{
    public delegate void InternalFbStatemachine(FbStates fbState);
    public enum FbStates
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
    public interface IServusFacebook
    {
        void SetServerConnector(IServerConnector client);
        void LoginExtBegin();
        void LoginExtEnd();
        void Login();
        void Logout();
        void Init();
        void DeleteAccount();
        void ReSaveSettings();
        void GetInitialData();
        void SetToWaitingDataOrders();
        FacebookSettings Settings{ get; set; }
        FbUserdata Fbuserdata { get; set; }
        event InternalFbStatemachine FbLoginStatesChanged;
    }
}
