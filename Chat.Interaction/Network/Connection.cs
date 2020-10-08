using Chat.Common;
using Chat.Interaction.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Chat.Interaction.Network
{
    class Connection<TSend, TReceive> : IConnection<TSend, TReceive>
    {
        private readonly DisposableList _disposables = new DisposableList();
        private readonly Socket _sck;
        private readonly NetworkStream _stream;
        //private readonly TextWriter _writer;
        //private readonly XmlReader _reader;

        public event Action OnClosed = delegate { };
        public event Action<TReceive> OnData = delegate { };

        private readonly object _sendLock = new object();
        private readonly Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private readonly Thread _recvThread;

        public Connection(Socket sck)
        {
            sck.NoDelay = true;

            _sck = _disposables.Add(sck);
            _stream = _disposables.Add(new NetworkStream(sck));
            //_writer = new StreamWriter(_stream, Encoding.UTF8);
            //_reader = _dis/posables.Add(XmlReader.Create(_stream, new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment, Async = true }));

            _recvThread = new Thread(this.RecvProc);
        }

        public void Start()
        {
            _recvThread.Start();
        }

        private void RecvProc()
        {
            var ms = new MemoryStream();
            while (_stream.TryCopyTo(ms, 4))
            {
                var len = BitConverter.ToInt32(ms.ToArray(), 0);

                ms.SetLength(0);
                if (!_stream.TryCopyTo(ms, len))
                    break;

                ms.Position = 0;
                var xs = new XmlSerializer(typeof(TReceive));
                var obj = (TReceive)xs.Deserialize(ms);

                this.OnData(obj);

                ms.SetLength(0);
            }

            this.OnClosed();
        }

        public void Send(TSend data)
        {
            var ms = new MemoryStream();
            ms.WriteByte(0);
            ms.WriteByte(0);
            ms.WriteByte(0);
            ms.WriteByte(0);
            var xs = new XmlSerializer(typeof(TSend));
            xs.Serialize(ms, data);
            ms.Flush();
            ms.Position = 0;
            ms.Write(BitConverter.GetBytes(ms.Length - 4), 0, 4);
            ms.Flush();
            ms.Position = 0;

            var bytes = ms.ToArray();
            lock (_sendLock)
            {
                _sendQueue.Enqueue(bytes);
                if (_sendQueue.Count == 1)
                    _stream.BeginWrite(bytes, 0, bytes.Length, this.SendProc, null);
            }
        }

        private void SendProc(IAsyncResult ar)
        {
            try
            {
                _stream.EndWrite(ar);
                _stream.Flush();

                lock (_sendLock)
                {
                    _sendQueue.Dequeue();
                    if (_sendQueue.Count > 0)
                    {
                        var bytes = _sendQueue.Peek();
                        _stream.BeginWrite(bytes, 0, bytes.Length, this.SendProc, null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("Sending proc dropped: " + ex.Message);
            }
        }

        //public async void Start()
        //{
        //    var xs = new XmlSerializer(typeof(TReceive));

        //    while (!_reader.EOF && await _reader.ReadAsync())
        //    {
        //        var obj = (TReceive)xs.Deserialize(_reader);
        //        this.OnData(obj);
        //    }
        //}

        //public void Send(TSend data)
        //{
        //    _writer.WriteLine();

        //    using (var w = XmlWriter.Create(_stream, new XmlWriterSettings() { OmitXmlDeclaration = true, CloseOutput = false }))
        //    {
        //        var xs = new XmlSerializer(typeof(TSend));
        //        xs.Serialize(w, data);
        //        w.Flush();
        //    }

        //    _stream.Flush();
        //}

        public void Dispose()
        {
            _disposables.SafeDispose();
        }
    }

    class ClientToServerConnection : Connection<ClientEnvelopType, ServerEnvelopType>, IClientToServerConnection
    {
        public ClientToServerConnection(Socket sck) : base(sck) { }
    }

    class ServerToClientConnection : Connection<ServerEnvelopType, ClientEnvelopType>, IServerToClientConnection
    {
        public ServerToClientConnection(Socket sck) : base(sck) { }
    }
}
