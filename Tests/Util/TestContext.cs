using Chat.Interaction;
using Chat.UI.Util;
using Chat.UI.ViewModel;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Util
{

    internal interface IClientTestContext : IDisposable
    {
        MockRepository Mocks { get; }

        Mock<IAppEnv> Env { get; }
        Mock<IClientConnector> ClientConnector { get; }
        Mock<ISessionFabric> SessionFabric { get; }
        Mock<IChatClientSession> ChatSession { get; }
        Mock<IDnsService> DnsService { get; }
    }

    class TestContext
    {

        public static IClientTestContext CreateForClient()
        {
            return new TestContextImpl();
        }
    }

    internal class TestContextBase : IDisposable
    {
        public MockRepository Mocks { get { return _repo; } }

        private readonly MockRepository _repo = new MockRepository(MockBehavior.Strict);

        private readonly Dictionary<Type, Mock> _mocks = new Dictionary<Type, Mock>();

        private static Exception _lastException = null;

        static TestContextBase()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, ea) => _lastException = ea.Exception;
        }

        protected Mock<T> GetMock<T>(Action<Mock<T>> setup = null)
            where T : class
        {
            var mockType = typeof(T);
            if (!_mocks.TryGetValue(mockType, out var mockObj))
            {
                mockObj = this.Mocks.Create<T>();
                _mocks.Add(mockType, mockObj);

                setup?.Invoke((Mock<T>)mockObj);
            }

            return (Mock<T>)mockObj;
        }


        public void Dispose()
        {
            var stack = new StackTrace();
            var currentMethod = MethodInfo.GetCurrentMethod();
            var callerFrame = stack.GetFrames().SkipWhile(f => f.GetMethod() != currentMethod).Skip(1).First();
            var callerOffset = callerFrame.GetILOffset();

            var callerMethod = callerFrame.GetMethod();
            var callerBody = callerMethod.GetMethodBody();

            //if (callerBody.ExceptionHandlingClauses.Any(e => callerOffset >= e.HandlerOffset && callerOffset < e.HandlerOffset + e.HandlerLength))

            // var callerOp = (IlOp<MethodBase>)IlParser.Translate(callerMethod).TakeWhile(o => o.Offset < callerOffset).Last();
            //var callingInstanceTypes = new[] { typeof(Action), typeof(IDisposable), typeof(TestContext) };
            //if (callingInstanceTypes.Any(t => t == callerOp.Arg))

            var exCode = Marshal.GetExceptionCode();
            if (exCode != 0)
            {
                Console.WriteLine("--------- Skipping mock verification due to unhandled exception ------------------------------");
            }
            else
            {
                Console.WriteLine("------------------------ Verifying mock object -----------------------------------------------");
                foreach (var mock in _mocks.Values)
                    mock.VerifyAll();
            }
        }
    }

    internal class TestContextImpl : TestContextBase, IClientTestContext
    {
        public Mock<IAppEnv> Env => this.GetMock<IAppEnv>();

        public Mock<IClientConnector> ClientConnector => this.GetMock<IClientConnector>(m => {
            this.Env.Setup(x => x.Connector).Returns(m.Object);
        });

        public Mock<IDnsService> DnsService => this.GetMock<IDnsService>(m => {
            this.Env.Setup(x => x.Dns).Returns(m.Object);
        });

        public Mock<ISessionFabric> SessionFabric => this.GetMock<ISessionFabric>(m => {
            this.Env.Setup(x => x.SessionFabric).Returns(m.Object);
        });

        public Mock<IChatClientSession> ChatSession => this.GetMock<IChatClientSession>(m => {
            this.SessionFabric.Setup(x => x.OpenSession(It.IsAny<string>(), It.IsAny<ushort>())).Returns(m.Object);
        });

    }
}

