using Chat.UI.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Client
{
    using Chat.Interaction.Xml;
    using Chat.UI.Util;
    using Tests.Util;

    [TestClass]
    public class SessionViewModelTests
    {
        const string _login1 = "tester1";
        const string _login2 = "tester2";

        const string _text1 = "11111111";
        const string _text2 = "22222222";

        [TestMethod]
        public void OnMessageTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var session = new SessionViewModel(null);

                session.OnMessage(new ChatMessageInfoType() {
                    Stamp = DateTime.Now,
                    Text = _text1,
                    AuthorUserInfo = new UserProfileInfoType() {
                        Login = _login1
                    }
                });
                session.OnMessage(new ChatMessageInfoType() {
                    Stamp = DateTime.Now,
                    Text = _text2,
                    AuthorUserInfo = new UserProfileInfoType() {
                        Login = _login2
                    }
                });


                Assert.IsTrue(session.Messages.Count == 2);
                Assert.IsTrue(session.Messages[0] is MessageItem m1 && m1.Data.Text == _text1);
                Assert.IsTrue(session.Messages[1] is MessageItem m2 && m2.Data.Text == _text2);
            }
        }

        [TestMethod]
        public void OnUserStatus()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var session = new SessionViewModel(null);
                session.OnUserStatus(new UserProfileInfoType() {
                    Login = _login1,
                    IsOnline = true,
                    IsActivated = true,
                });
                session.OnUserStatus(new UserProfileInfoType() {
                    Login = _login2,
                    IsOnline = true,
                    IsActivated = false,
                });

                Assert.AreEqual(session.Contacts.Count, 2);
                Assert.AreEqual(session.Contacts[0].Login, _login1);
                Assert.AreEqual(session.Contacts[0].IsActivated, true);
                Assert.AreEqual(session.Contacts[0].IsOnline, true);
                Assert.AreEqual(session.Contacts[1].Login, _login2);
                Assert.AreEqual(session.Contacts[1].IsActivated, false);
                Assert.AreEqual(session.Contacts[1].IsOnline, true);

                session.OnUserStatus(new UserProfileInfoType() {
                    Login = _login1,
                    IsOnline = false,
                    IsActivated = false,
                });
             
                Assert.AreEqual(session.Contacts.Count, 1);
             
                Assert.AreEqual(session.Contacts[0].Login, _login2);

                session.OnUserStatus(new UserProfileInfoType() {
                    Login = _login2,
                    IsOnline = true,
                    IsActivated = false,
                });

                Assert.AreEqual(session.Contacts.Count, 1);
                Assert.AreEqual(session.Contacts[0].Login, _login2);
                Assert.AreEqual(session.Contacts[0].IsOnline, true);
            }
        }

        [TestMethod]
        public void SendMessage()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                var clientSession = ctx.Mocks.Create<IChatClientSession>(Moq.MockBehavior.Strict);
                clientSession.Setup(s => s.SendMessage(_text1)).Returns(Task.CompletedTask);

                var session = new SessionViewModel(clientSession.Object);

                using (var msgCmdWatcher = EventWatcher.For(session.SendMessageCommand)
                                                       .Expecting(c => c.Accomplished += null))
                {
                    session.MessageToSendText = _text1;
                    session.SendMessageCommand.Execute(null);

                    msgCmdWatcher.WaitForAllRequiredEventsOrThrow();

                    Assert.IsTrue(string.IsNullOrWhiteSpace(session.MessageToSendText));
                }
            }
        }
    }
}

