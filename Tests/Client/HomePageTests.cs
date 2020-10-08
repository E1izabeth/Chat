using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chat.UI.Util;
using Chat.UI.View;
using Chat.UI.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Client
{
    using Chat.Interaction.Xml;
    using Moq;
    using Tests.Util;

    [TestClass]
    public class HomePageTests
    {
        private const string _login = "login", _password = "password", _email = "email@mail.ru";

        [TestMethod]
        public void LoginSuccessTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                ctx.ChatSession.Setup(s => s.Login(_login, _password)).Returns(Task.FromResult(new UserProfileInfoType()));
                ctx.ChatSession.Setup(s => s.Start());
                ctx.ChatSession.Setup(s => s.SetContext(It.IsAny<ISessionContext>()));

                var home = new HomeViewModel() { Env = ctx.Env.Object };
                home.Mode = HomeScreenMode.Login;
                home.Login = _login;
                home.Password = _password;
                home.Password2 = _password;
                home.Email = _email;
                home.Email2 = _email;

                using (var loginCommandWatcher = EventWatcher.For(home.LoginCommand)
                                                            .Expecting(c => c.Accomplished += null))
                {
                    home.LoginCommand.Execute(null);

                    loginCommandWatcher.WaitForAllRequiredEventsOrThrow();


                    Assert.AreEqual(home.Mode, HomeScreenMode.Login);
                    Assert.IsTrue(home.IsLoggedIn);
                    Assert.IsNotNull(home.CurrentSession);
                }
            }
        }

        [TestMethod]
        public void LoginFailTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                ctx.ChatSession.Setup(s => s.Login(_login, _password)).Returns(Task.FromException<UserProfileInfoType>(new ApplicationException()));
                ctx.ChatSession.Setup(s => s.Start());

                var home = new HomeViewModel() { Env = ctx.Env.Object };
                home.Login = _login;
                home.Password = _password;
                home.Password2 = _password;
                home.Email = _email;
                home.Email2 = _email;

                using (var loginCommandWatcher = EventWatcher.For(home.LoginCommand)
                                                            .Expecting(c => c.Accomplished += null))
                {

                    home.LoginCommand.Execute(null);

                    loginCommandWatcher.WaitForAllRequiredEventsOrThrow();

                    Assert.IsFalse(home.IsLoggedIn);
                    Assert.IsNull(home.CurrentSession);
                }
            }
        }

        [TestMethod]
        public void RegisterSuccessTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                ctx.ChatSession.Setup(s => s.Register(_email, _login, _password)).Returns(Task.CompletedTask);
                ctx.ChatSession.Setup(s => s.Start());

                var home = new HomeViewModel() { Env = ctx.Env.Object };
                home.Login = _login;
                home.Password = _password;
                home.Password2 = _password;
                home.Email = _email;
                home.Email2 = _email;

                using (var registerCommandWatcher = EventWatcher.For(home.RegisterCommand)
                                                            .Expecting(c => c.Accomplished += null))
                {

                    home.RegisterCommand.Execute(null);

                    registerCommandWatcher.WaitForAllRequiredEventsOrThrow();
                }
            }
        }

        [TestMethod]
        public void RegisterFailTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {

                var home = new HomeViewModel() { Env = ctx.Env.Object };
                home.Login = _login;
                home.Password = _password;
                home.Password2 = "invalidpwds";
                home.Email = _email;
                home.Email2 = _email;

                using (var registerCommandWatcher = EventWatcher.For(home.RegisterCommand)
                                                            .Expecting(c => c.Accomplished += null))
                {

                    home.RegisterCommand.Execute(null);

                    registerCommandWatcher.WaitForAllRequiredEventsOrThrow();
                }
            }
        }

        [TestMethod]
        public void RestoreSuccessTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {
                ctx.ChatSession.Setup(s => s.Restore(_login, _email)).Returns(Task.CompletedTask);
                ctx.ChatSession.Setup(s => s.Start());

                var home = new HomeViewModel() { Env = ctx.Env.Object };
                home.Login = _login;
                home.Password = _password;
                home.Password2 = _password;
                home.Email = _email;
                home.Email2 = _email;

                using (var restoreCommandWatcher = EventWatcher.For(home.RestoreCommand)
                                                            .Expecting(c => c.Accomplished += null))
                {

                    home.RestoreCommand.Execute(null);

                    restoreCommandWatcher.WaitForAllRequiredEventsOrThrow();
                }
            }
        }

        [TestMethod]
        public void RestoreFailTest()
        {
            using (var ctx = TestContext.CreateForClient())
            {

                var home = new HomeViewModel() { Env = ctx.Env.Object };
                home.Login = _login;
                home.Password = _password;
                home.Password2 = _password;
                home.Email = _email;
                home.Email2 = "invalid@mail.ru";

                using (var restoreCommandWatcher = EventWatcher.For(home.RestoreCommand)
                                                            .Expecting(c => c.Accomplished += null))
                {

                    home.RestoreCommand.Execute(null);

                    restoreCommandWatcher.WaitForAllRequiredEventsOrThrow();
                }
            }
        }
    }
}
