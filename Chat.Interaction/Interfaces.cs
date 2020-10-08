using Chat.Common;
using Chat.Interaction.Xml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Interaction
{
    public interface IChat
    {

    }

    public interface IClientConnector<TSend, TReceive>
    {
        IConnection<TSend, TReceive> Connect(IPEndPoint endPoint);
    }

    public interface IClientConnector : IClientConnector<ClientEnvelopType, ServerEnvelopType>
    {
        new IClientToServerConnection Connect(IPEndPoint endPoint);
    }

    public interface IConnection<TSend, TReceive> : IDisposable
    {
        event Action OnClosed;
        event Action<TReceive> OnData;

        void Send(TSend data);
        void Start();
    }

    public interface IClientToServerConnection : IConnection<ClientEnvelopType, ServerEnvelopType> { }
    public interface IServerToClientConnection : IConnection<ServerEnvelopType, ClientEnvelopType> { }

    public interface IConnectionListener<TSend, TReceive> : IDisposable
    {
        event Action<IConnection<TSend, TReceive>> OnConnect;
        void Start();
    }

    public interface IConnectionListener : IConnectionListener<ServerEnvelopType, ClientEnvelopType>
    {
        new event Action<IServerToClientConnection> OnConnect;
    }

    public interface IServerHost<TSend, TReceive>
    {
        IConnectionListener<TSend, TReceive> CreateListener(IPEndPoint endPoint);
    }

    public interface IServerHost : IServerHost<ServerEnvelopType, ClientEnvelopType>
    {
        new IConnectionListener CreateListener(IPEndPoint endPoint);
    }


}
