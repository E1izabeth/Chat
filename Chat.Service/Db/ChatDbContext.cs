﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Service.Db
{
    public interface IChatDbContext
    {
        IUsersRepository Users { get; }

        IDbContext Raw { get; }

        void SubmitChanges();
    }

    public interface IUsersRepository
    {
        void AddUser(DbUserInfo user);
        DbUserInfo GetUserById(long userId);
        DbUserInfo FindUserByLoginKey(string loginKey);
        DbUserInfo FindUserByTokenKey(string key);
    }

    class ChatDbContext : RepositoryImpl, IChatDbContext
    {
        public IUsersRepository Users { get; }
        //public IResourcesRepository Resources { get; }
        //public ITopicsRepository Topics { get; }
        //public ISubscriptionsRepository Subscriptions { get; }

        public IDbContext Raw { get { return _db; } }

        public ChatDbContext(DbContext dbContext) : base(dbContext)
        {
            this.Users = new UsersRepositoryImpl(dbContext);
            //this.Resources = new ResourcesRepository(dbContext);
            //this.Topics = new TopicsRepositoryImpl(dbContext);
            //this.Subscriptions = new SubscriptionsRepository(dbContext);
        }

        public void SubmitChanges()
        {
            _db.SubmitChanges();
        }
    }


    class RepositoryImpl
    {
        protected readonly DbContext _db;

        public RepositoryImpl(DbContext db)
        {
            _db = db;
        }
    }

    class UsersRepositoryImpl : RepositoryImpl, IUsersRepository
    {
        public UsersRepositoryImpl(DbContext db) : base(db) { }

        public void AddUser(DbUserInfo user)
        {
            _db.Users.InsertOnSubmit(user);
        }

        public DbUserInfo GetUserById(long userId)
        {
            return _db.Users.First(u => u.Id == userId);
        }

        public DbUserInfo FindUserByLoginKey(string loginKey)
        {
            return _db.Users.FirstOrDefault(u => u.LoginKey == loginKey);
        }

        public DbUserInfo FindUserByTokenKey(string key)
        {
            return _db.Users.FirstOrDefault(u => u.LastToken == key);
        }
    }
}
