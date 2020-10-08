using Chat.UI.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Client
{
    using Chat.Interaction;
    using Chat.Interaction.Xml;
    using System.Net;
    using Tests.Util;

    [TestClass]
    public class SessionFabricTests
    {
        const string _host = "testhost";
        const ushort _port = 12345;
        static readonly IPAddress _ip = IPAddress.Parse("1.2.3.4");

        [TestMethod]
        public void OpenSessionTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {

                var cnn = ctx.Mocks.Create<IClientToServerConnection>();
                cnn.Setup(c => c.Start());

                ctx.DnsService.Setup(s => s.GetHostAddresses(_host)).Returns(new[] { _ip });
                ctx.ClientConnector.Setup(c => c.Connect(new IPEndPoint(_ip, _port))).Returns(cnn.Object);

                var fabric = new SessionFabric(ctx.Env.Object);

                // Act {
                var session = fabric.OpenSession(_host, _port);
                session.Start();
                // }
            }
        }

        [TestMethod]
        public void DnsTest()
        {
            var dns = new DnsService();
            
            var ips = dns.GetHostAddresses("localhost");

            Assert.IsTrue(ips.Contains(IPAddress.Loopback));
        }
    }
}
