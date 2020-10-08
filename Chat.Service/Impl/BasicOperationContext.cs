using Chat.Common;
using Chat.Service.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Service.Impl
{

    public interface IBasicOperationContext : IDisposable
    {
        IChatDbContext Db { get; }
    }

    class BasicOperationContext : IDisposable, IBasicOperationContext
    {
        protected readonly DisposableList _disposables = new DisposableList();

        private readonly IChatService _svcCtx;

        IChatDbContext _dbContext = null;
        public IChatDbContext Db { get { return _dbContext ?? (_dbContext = this.OpenDb()); } }

        // private TransactionScope _transaction = null;

        public BasicOperationContext(IChatService svcCtx)
        {
            _svcCtx = svcCtx;
        }

        private IChatDbContext OpenDb()
        {
            var ctx = _svcCtx.OpenDb();
            _disposables.Add(ctx.Raw.Connection);
            _disposables.Add(ctx.Raw);
            //_transaction = _disposables.Add(new TransactionScope(
            //    TransactionScopeOption.Required,
            //    new TransactionOptions
            //    {
            //        IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
            //    }
            //));
            return ctx;
        }

        public void Dispose()
        {
            //if (_transaction != null)
            //    _transaction.Complete();

            _disposables.SafeDispose();
        }
    }
}
