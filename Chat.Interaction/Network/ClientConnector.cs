using Chat.Interaction.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Interaction.Network
{
    public class ClientConnector<TSend, TReceive> : IClientConnector<TSend, TReceive>
    {
        internal static readonly IClientConnector<TSend, TReceive> Instance = new ClientConnector<TSend, TReceive>();

        protected ClientConnector() { }

        public IConnection<TSend, TReceive> Connect(IPEndPoint remoteEndPoint)
        {
            return new Connection<TSend, TReceive>(this.ConnectImpl(remoteEndPoint));
        }

        protected Socket ConnectImpl(IPEndPoint remoteEndPoint)
        {
            var sck = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sck.Connect(remoteEndPoint);
            return sck;
        }
    }

    public class ClientConnector : ClientConnector<ClientEnvelopType, ServerEnvelopType>, IClientConnector
    {
        internal static new readonly IClientConnector Instance = new ClientConnector();

        private ClientConnector() { }

        public static IClientConnector GetInstance() { return ClientConnector.Instance; }
        public static IClientConnector<TSend, TReceive> GetInstance<TSend, TReceive>() { return ClientConnector<TSend, TReceive>.Instance; }

        public new IClientToServerConnection Connect(IPEndPoint remoteEndPoint)
        {
            return new ClientToServerConnection(this.ConnectImpl(remoteEndPoint));
        }
    }
}
