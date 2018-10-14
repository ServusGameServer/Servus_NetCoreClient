using System;
namespace ServusProtobuf
{
    public class ServusProtobufBasicFactory
    {
        public ServusProtobufBasicFactory()
        {

        }

        public static ServusMessage createServusMessage(string gID, long cbID, bool er, ErrorType erT, string erM)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Error = er,
                ErrorType = erT,
                ErrorMessage = erM,
                GameID = gID,
                CallBackID = cbID
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, long cbID, bool er, ErrorType erT, string erM)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Error = er,
                ErrorType = erT,
                ErrorMessage = erM,
                Modul = mod,
                CallBackID = cbID
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(string gID, bool er, ErrorType erT, string erM)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Error = er,
                ErrorType = erT,
                ErrorMessage = erM,
                GameID = gID
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, bool er, ErrorType erT, string erM)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Error = er,
                ErrorType = erT,
                ErrorMessage = erM,
                Modul = mod
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, long cbID, AuthFunctions func)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                CallBackID = cbID,
                Authfunc = func
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, AuthFunctions func)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                Authfunc = func
            };
            return internalServusMsg;
        }

        public static ServusMessage createServusMessage(Modulename mod, long cbID, BasicFunctions func)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                CallBackID = cbID,
                Basicfunc = func
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, BasicFunctions func)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                Basicfunc = func
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, long cbID, QueueFunctions func)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                CallBackID = cbID,
                Queuefunc = func
            };
            return internalServusMsg;
        }
        public static ServusMessage createServusMessage(Modulename mod, QueueFunctions func)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                Queuefunc = func
            };
            return internalServusMsg;
        }

        public static ServusLogin_Self createLoginSelfValue(string mail, string nick, string pw)
        {
            ServusLogin_Self intobj = new ServusLogin_Self
            {
                Email = mail,
                Nickname = nick,
                Password = pw
            };
            return intobj;
        }
        public static ServusLogin_Self createLoginSelfValue(string mail, string pw)
        {
            ServusLogin_Self intobj = new ServusLogin_Self
            {
                Email = mail,
                Password = pw
            };
            return intobj;
        }

        public static ServusLogin_Only createLoginOnlyValue(long id, long key)
        {
            ServusLogin_Only intobj = new ServusLogin_Only
            {
                Id = id,
                Key = key
            };
            return intobj;
        }

        public static ServusMessage createEchoGameMessage(Modulename mod, TestGameFunctions func, string gameID)
        {
            ServusMessage internalServusMsg = new ServusMessage
            {
                Modul = mod,
                TestGamefunc = func,
                GameID = gameID
            };
            return internalServusMsg;
        }


    }
}
