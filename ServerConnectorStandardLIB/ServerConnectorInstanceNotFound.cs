using System;
namespace ServerConnectorStandardLIB
{
    public class ServerConnectorInstanceNotFound : Exception
    {
        public ServerConnectorInstanceNotFound()
        {
        }

        public ServerConnectorInstanceNotFound(string message)
            : base(message)
        {
        }

        public ServerConnectorInstanceNotFound(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}