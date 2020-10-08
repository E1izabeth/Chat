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
   
    public class ServerHost<TSend, TReceive> : IServerHost<TSend, TReceive>
    {
        internal static readonly IServerHost<TSend, TReceive> Instance = new ServerHost<TSend, TReceive>();

        protected ServerHost() { }

        public IConnectionListener<TSend, TReceive> CreateListener(IPEndPoint localEndPoint)
        {
            return new ConnectionListener<TSend, TReceive>(localEndPoint);
        }
    }

    public class ServerHost : ServerHost<ServerEnvelopType, ClientEnvelopType>, IServerHost
    {
        internal static new readonly IServerHost Instance = new ServerHost();

        private ServerHost() { }

        public static IServerHost GetInstance() { return ServerHost.Instance; }
        public static IServerHost<TSend, TReceive> GetInstance<TSend, TReceive>() { return ServerHost<TSend, TReceive>.Instance; }

        IConnectionListener IServerHost.CreateListener(IPEndPoint localEndPoint)
        {
            return new ConnectionListener(localEndPoint);
        }
    }

    public class ConnectionListener<TSend, TReceive> : IConnectionListener<TSend, TReceive>
    {
        public event Action<IConnection<TSend, TReceive>> OnConnect = delegate { };

        protected readonly TcpListener _listener;

        public ConnectionListener(IPEndPoint localEndPoint)
        {
            _listener = new TcpListener(localEndPoint);
        }

        public void Start()
        {
            _listener.Start();
            _listener.BeginAcceptSocket(this.AcceptProc, null);
        }

        private void AcceptProc(IAsyncResult ar)
        {
            var sck = _listener.EndAcceptSocket(ar);

            this.OnConnect(this.CreateConnection(sck));

            _listener.BeginAcceptSocket(this.AcceptProc, null);
        }

        protected virtual IConnection<TSend, TReceive> CreateConnection(Socket sck)
        {
            return new Connection<TSend, TReceive>(sck);
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }

    public class ConnectionListener : ConnectionListener<ServerEnvelopType, ClientEnvelopType>, IConnectionListener
    {
        public new event Action<IServerToClientConnection> OnConnect = delegate { };

        public ConnectionListener(IPEndPoint localEndPoint) : base(localEndPoint) { }

        protected override IConnection<ServerEnvelopType, ClientEnvelopType> CreateConnection(Socket sck)
        {
            var cnn = new ServerToClientConnection(sck);
            this.OnConnect(cnn);
            return cnn;
        }
    }
}
