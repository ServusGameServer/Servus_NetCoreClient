using ServusProtobuf;
namespace ServusFacebook
{
    public static class FbServusMessageDtoFactory
    {
            
        public static ServusMessage CreateServusMessageLogin(string fbId, string token)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = Modulename.AuthFb,
                Authfunc = AuthFunctions.AuthLogin,
                ValueFBAuth = new ServusLogin_FB {FbId = fbId, Token = token}
            };
            return internalServusMsg;
        }
        public static ServusMessage CreateServusMessageRegister(string fbId, string token)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = Modulename.AuthFb,
                Authfunc = AuthFunctions.AuthRegister,
                ValueFBAuth = new ServusLogin_FB {FbId = fbId, Token = token}
            };
            return internalServusMsg;
        }
        public static ServusMessage CreateServusMessage(string playerid)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = Modulename.AuthUserdata, Authfunc = AuthFunctions.AuthPicture, ValueString = playerid
            };
            return internalServusMsg;
        }
    }
}
