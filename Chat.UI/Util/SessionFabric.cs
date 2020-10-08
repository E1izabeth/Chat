using Chat.Interaction;
using Chat.Interaction.Xml;
using Chat.UI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat.UI.ViewModel
{
    public interface ISessionFabric
    {
        IChatClientSession OpenSession(string serviceHost, ushort servicePort);
    }

    class SessionFabric : ISessionFabric
    {
        public IAppEnv Env { get; }

        public SessionFabric(IAppEnv env)
        {
            this.Env = env;
        }

        public IChatClientSession OpenSession(string serviceHost, ushort servicePort)
        {
            return new ChatClientSession(this.OpenConnection(serviceHost, servicePort));
        }

        private IClientToServerConnection OpenConnection(string serviceHost, ushort servicePort)
        {
            var ips = this.Env.Dns.GetHostAddresses(serviceHost);
            var errors = new List<Exception>();

            foreach (var ip in ips)
            {
                try
                {
                    return this.Env.Connector.Connect(new IPEndPoint(ip, servicePort));
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }

            throw new AggregateException(errors);
        }
    }

    public interface IDnsService
    {
        IPAddress[] GetHostAddresses(string serviceHost);
    }

    public class DnsService : IDnsService
    {
        public IPAddress[] GetHostAddresses(string serviceHost)
        {
            return Dns.GetHostAddresses(serviceHost);
        }
    }
}
