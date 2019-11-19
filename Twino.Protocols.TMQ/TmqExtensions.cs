using Twino.Core;
using Twino.Core.Protocols;

namespace Twino.Protocols.TMQ
{
    public static class TmqExtensions
    {
        public static ITwinoServer UseTmq(this ITwinoServer server, IProtocolConnectionHandler<TmqMessage> handler)
        {
            TwinoTmqProtocol protocol = new TwinoTmqProtocol(server, handler);
            server.UseProtocol(protocol);
            return server;
        }

        public static ITwinoServer UseTmq(this ITwinoServer server, TmqMessageHandler action)
        {
            TmqMethodHandler handler = new TmqMethodHandler(action);
            TwinoTmqProtocol protocol = new TwinoTmqProtocol(server, handler);
            server.UseProtocol(protocol);
            return server;
        }
    }
}