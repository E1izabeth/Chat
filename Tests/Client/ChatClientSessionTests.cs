using Chat.UI.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chat.Interaction;
using Chat.Interaction.Xml;
using Chat.UI.ViewModel;
using Moq;
using Tests.Util;
using TestContext = Tests.Util.TestContext;

namespace Tests.Client
{

    [TestClass]
    public class ChatClientSessionTests
    {
        const string _email = "email";
        const string _login = "login";
        const string _pwd = "pwd";
        const string _text = "testtext";

        [TestMethod]
        public void PingTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var dueTime = TimeSpan.FromSeconds(5);

                var cnn = ctx.Mocks.Create<IClientToServerConnection>();
                cnn.Setup(c => c.Send(It.Is<ClientEnvelopType>(o => o.Item is PingRequestType)))
                   .Callback<ClientEnvelopType>(o => cnn.Raise(c => c.OnData += null, new ServerEnvelopType() {
                       Id = o.Id,
                       Item = new PingResponseType() { RequestStamp = (o.Item as PingRequestType).Stamp - dueTime }
                   }));

                var session = new ChatClientSession(cnn.Object);

                var now = DateTime.UtcNow;
                var task = session.Ping();

                task.Wait();

                Assert.IsTrue(task.Result.Seconds == dueTime.Seconds);
            }
        }

        [TestMethod]
        public void LoginTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var dueTime = TimeSpan.FromSeconds(5);

                var cnn = ctx.Mocks.Create<IClientToServerConnection>();
                cnn.Setup(c => c.Send(It.Is<ClientEnvelopType>(o => o.Item is LoginSpecType)))
                   .Callback<ClientEnvelopType>(o => cnn.Raise(c => c.OnData += null, new ServerEnvelopType() {
                       Id = o.Id,
                       Item = new OkType()
                   }));

                var session = new ChatClientSession(cnn.Object);

                var now = DateTime.UtcNow;
                var task = session.Login(_login, _pwd);

                task.Wait();

                Assert.IsTrue(task.IsCompleted);
            }
        }

        [TestMethod]
        public void RegisterTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var dueTime = TimeSpan.FromSeconds(5);

                var cnn = ctx.Mocks.Create<IClientToServerConnection>();
                cnn.Setup(c => c.Send(It.Is<ClientEnvelopType>(o => o.Item is RegisterSpecType)))
                   .Callback<ClientEnvelopType>(o => cnn.Raise(c => c.OnData += null, new ServerEnvelopType() {
                       Id = o.Id,
                       Item = new OkType()
                   }));

                var session = new ChatClientSession(cnn.Object);

                var now = DateTime.UtcNow;
                var task = session.Register(_email, _login, _pwd);

                task.Wait();

                Assert.IsTrue(task.IsCompleted);
            }
        }

        [TestMethod]
        public void SendMessage()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var dueTime = TimeSpan.FromSeconds(5);

                var cnn = ctx.Mocks.Create<IClientToServerConnection>();
                cnn.Setup(c => c.Send(It.Is<ClientEnvelopType>(o => o.Item is PostMessageSpecType)))
                   .Callback<ClientEnvelopType>(o => {
                       cnn.Raise(c => c.OnData += null, new ServerEnvelopType() {
                           Id = o.Id,
                           Item = new OkType()
                       });
                       cnn.Raise(c => c.OnData += null, new ServerEnvelopType() {
                           Id = o.Id,
                           Item = new ChatMessageInfoType() {
                               Text = ((o as ClientEnvelopType).Item as PostMessageSpecType).Text
                           }
                       });
                   });

                var sessionContext = ctx.Mocks.Create<ISessionContext>();
                sessionContext.Setup(s => s.OnMessage(It.IsAny<ChatMessageInfoType>()));

                var session = new ChatClientSession(cnn.Object);
                session.SetContext(sessionContext.Object);

                var now = DateTime.UtcNow;
                var task = session.SendMessage(_text);

                task.Wait();

                Assert.IsTrue(task.IsCompleted);
            }
        }


    }
}
