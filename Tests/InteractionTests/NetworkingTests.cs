using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Chat.Common;
using Chat.Interaction;
using Chat.Interaction.Network;
using Chat.Interaction.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.InteractionTests
{
    [TestClass]
    public class NetworkingTests
    {
        private const ushort _port = 12345;
        private readonly IPEndPoint _localEndPoint = new IPEndPoint(IPAddress.Loopback, _port);

        private IClientConnector<S, R> GetConnector<S, R>()
        {
            return ClientConnector.GetInstance<S, R>();
        }

        private IServerHost<S, R> GetHost<S, R>()
        {
            return ServerHost.GetInstance<S, R>();
        }

        [TestMethod]
        public void TestClientConnection()
        {
            var listener = new TcpListener(_localEndPoint);
            try
            {
                listener.Start();
                var ar = listener.BeginAcceptTcpClient(ar2 => { }, null);

                using (var cnn = this.GetConnector<ClientEnvelopType, ClientEnvelopType>().Connect(_localEndPoint))
                using (var mirror = listener.EndAcceptTcpClient(ar))
                {
                    var mirrored = this.TestConnection(cnn, mirror);

                    Assert.IsTrue(mirrored);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [TestMethod]
        public void TestServerHostOneConnection()
        {
            using (var listener = this.GetHost<ClientEnvelopType, ClientEnvelopType>().CreateListener(_localEndPoint))
            {

                var sink = new TaskCompletionSource<bool>();

                using (var mirror = new TcpClient())
                {
                    listener.OnConnect += cnn => sink.FillWith(() => this.TestConnection(cnn, mirror));
                    listener.Start();
                    mirror.Connect(_localEndPoint);

                    Assert.IsTrue(sink.Task.Result);
                }
            }
        }

        private bool TestConnection(IConnection<ClientEnvelopType, ClientEnvelopType> cnn, TcpClient mirror, DateTime? stampValueSrc = null)
        {
            mirror.NoDelay = true;
            var mirrorStream = mirror.GetStream();
            var mirrorThread = new Thread(() => mirrorStream.CopyTo(mirrorStream)) { IsBackground = true };
            mirrorThread.Start();

            var sink = new TaskCompletionSource<bool>();
            var stampValue = stampValueSrc ?? DateTime.UtcNow;

            cnn.OnData += p => sink.FillWith(() => p.Item is PingRequestType r && r.Stamp == stampValue);
            cnn.Start();
            cnn.Send(this.MakeTestPacket(stampValue));

            return sink.Task.Result;
        }

        private ClientEnvelopType MakeTestPacket(DateTime stampValue)
        {
            return new ClientEnvelopType() { Item = new PingRequestType() { Stamp = stampValue } };
        }

        [TestMethod]
        public void TestClientServerConnection()
        {
            var sink = new TaskCompletionSource<bool>();
            var stampValue = DateTime.UtcNow;

            using (var listener = ServerHost.GetInstance().CreateListener(_localEndPoint))
            {
                IServerToClientConnection scnn;
                listener.OnConnect += c => {
                    scnn = c;
                    scnn.OnData += sp => scnn.Send(new ServerEnvelopType() { Item = new PingResponseType() { RequestStamp = ((PingRequestType)sp.Item).Stamp } });
                    scnn.Start();
                };
                listener.Start();

                using (var cnn = ClientConnector.GetInstance().Connect(_localEndPoint))
                {
                    cnn.OnData += p => sink.FillWith(() => p.Item is PingResponseType r && r.RequestStamp == stampValue);
                    cnn.Start();
                    cnn.Send(this.MakeTestPacket(stampValue));

                    Assert.IsTrue(sink.Task.Result);
                }
            }
        }

        [TestMethod]
        public void TestConnectionAbort1()
        {
            var listener = new TcpListener(_localEndPoint);
            var sink = new TaskCompletionSource<bool>();
            try
            {
                listener.Start();
                var ar = listener.BeginAcceptTcpClient(ar2 => { }, null);

                using (var cnn = this.GetConnector<ClientEnvelopType, ClientEnvelopType>().Connect(_localEndPoint))
                {
                    cnn.OnClosed += () => sink.SetResult(true);
                    cnn.Start();
                    using (var mirror = listener.EndAcceptTcpClient(ar))
                        mirror.GetStream().Write(BitConverter.GetBytes(10), 0, 1);

                    Assert.IsTrue(sink.Task.Result);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [TestMethod]
        public void TestConnectionAbort2()
        {
            var listener = new TcpListener(_localEndPoint);
            var sink = new TaskCompletionSource<bool>();
            try
            {
                listener.Start();
                var ar = listener.BeginAcceptTcpClient(ar2 => { }, null);

                using (var cnn = this.GetConnector<ClientEnvelopType, ClientEnvelopType>().Connect(_localEndPoint))
                {
                    cnn.OnClosed += () => sink.SetResult(true);
                    cnn.Start();
                    using (var mirror = listener.EndAcceptTcpClient(ar))
                        mirror.GetStream().Write(BitConverter.GetBytes(10).Concat(new byte[10]).ToArray(), 0, 5);

                    Assert.IsTrue(sink.Task.Result);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [TestMethod]
        public void TestClientConnectionDelayed()
        {
            var listener = new TcpListener(_localEndPoint);
            try
            {
                listener.Start();
                var ar = listener.BeginAcceptTcpClient(ar2 => { }, null);

                using (var cnn = this.GetConnector<ClientEnvelopType, ClientEnvelopType>().Connect(_localEndPoint))
                using (var mirror = listener.EndAcceptTcpClient(ar))
                {
                    var t = DateTime.Now;
                    for (int i = 0; i < 2000; i++)
                        cnn.Send(this.MakeTestPacket(t));

                    var mirrored = this.TestConnection(cnn, mirror, t);

                    Assert.IsTrue(mirrored);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

    }
}