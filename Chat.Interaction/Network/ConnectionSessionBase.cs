using Chat.Common;
using Chat.Interaction.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat.Interaction.Network
{
    public abstract class ConnectionSessionBase<TSend, TSendContent, TRecv, TRecvContent>
        where TSend : IEnvelop<TSendContent>, new()
        where TRecv : IEnvelop<TRecvContent>
    {
        abstract class SessionTask
        {
            protected readonly TSend _packet;

            public SessionTask(TSend packet)
            {
                _packet = packet;
            }

            public void Accomplish(TRecvContent item)
            {
                this.AccomplishImpl(item);
            }

            protected abstract void AccomplishImpl(TRecvContent item);
        }

        class SessionTask<T> : SessionTask
            where T : TRecvContent
        {
            public Task<T> TaskObject { get { return _completionSource.Task; } }

            private readonly TaskCompletionSource<T> _completionSource = new TaskCompletionSource<T>();

            public SessionTask(TSend packet)
                : base(packet)
            {
            }

            protected override void AccomplishImpl(TRecvContent item)
            {
                if (item is T result)
                {
                    _completionSource.SetResult(result);
                }
                else if (item is IErrorInfoContainer error)
                {
                    _completionSource.SetException(new ChatInteractionException(error.Item));
                }
                else
                {
                    _completionSource.SetException(new ApplicationException($"Unexpected data received from server: has {item.GetType().Name} while expecting {typeof(T).Name}"));
                }
            }
        }

        private object _cnnLocker = new object();
        private IConnection<TSend, TRecv> _cnn;
        
        public IConnection<TSend, TRecv> Connection { get { return _cnn; } }

        protected long _packetsCount = 0;
        private readonly object _tasksLocker = new object();
        private Dictionary<long, SessionTask> _tasks = new Dictionary<long, SessionTask>();

        public ConnectionSessionBase(IConnection<TSend, TRecv> cnn)
        {
            _cnn = cnn;
            _cnn.OnData += this.OnData;
        }

        private void OnData(TRecv packet)
        {
            SessionTask task;

            lock (_tasksLocker)
            {
                if (!_tasks.TryGetValue(packet.Id, out task))
                    task = null;
            }

            if (task != null)
            {
                task.Accomplish(packet.Item);
            }
            else
            {
                var result = this.OnAsyncPacket(packet.Item);
                if (result != null)
                    _cnn.Send(new TSend() { Id = packet.Id, Item = result });
            }
        }

        protected abstract TSendContent OnAsyncPacket(TRecvContent packet);

        public void Start()
        {
            _cnn.Start();
        }

        private TSend MakePacket(TSendContent data)
        {
            return new TSend() { Id = Interlocked.Add(ref _packetsCount, 2), Item = data };
        }

        protected Task<T> PostTask<T>(TSendContent data)
            where T : TRecvContent
        {
            var packet = this.MakePacket(data);
            var task = new SessionTask<T>(packet);

            lock (_tasksLocker)
            {
                _tasks.Add(packet.Id, task);
            }

            _cnn.Send(packet);

            return task.TaskObject;
        }

        protected Task SendPacket(TSendContent data)
        {
            _cnn.Send(this.MakePacket(data));

            return Task.CompletedTask;
        }

        protected abstract void DisposeImpl();

        bool _isDisposed = false;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                this.DisposeImpl();
                _cnn.SafeDispose();
            }
        }
    }
}
