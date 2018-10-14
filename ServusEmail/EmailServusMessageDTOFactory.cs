using ServusProtobuf;
namespace ServusEmail
{
    public static class EmailServusMessageDtoFactory
    {
        public static ServusMessage CreateServusMessageLogin(string email, string pw)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = Modulename.AuthSelf,
                Authfunc = AuthFunctions.AuthLogin
            };
            internalServusMsg.ValueSelfAuth = new ServusLogin_Self();
            internalServusMsg.ValueSelfAuth.Email = email;
            internalServusMsg.ValueSelfAuth.Password = pw;
            return internalServusMsg;
        }
        public static ServusMessage CreateServusMessageRegister(string email, string pw, string nickname)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = Modulename.AuthSelf,
                Authfunc = AuthFunctions.AuthRegister
            };
            internalServusMsg.ValueSelfAuth = new ServusLogin_Self();
            internalServusMsg.ValueSelfAuth.Email = email;
            internalServusMsg.ValueSelfAuth.Password = pw;
            internalServusMsg.ValueSelfAuth.Nickname = nickname;
            return internalServusMsg;
        }
    }
}