using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Chat.Common;
using Chat.Interaction;
using Chat.Interaction.Xml;
using Chat.Service.Db;
using Chat.Service.Util;

namespace Chat.Service.Impl
{
    public interface IChatUser
    {
        DbUserInfo Info { get; }
        int SessionsCount { get; }

        void RegisterSession(IChatClientSession session);
        void UnregisterSession(IChatClientSession session);
    }

    public interface IChatUserContext
    {
        DbUserInfo Info { get; }

        void ChangeEmail(ChangeEmailSpecType spec);
        void ChangePassword(ChangePasswordSpecType spec);
        void RequestActivation(RequestActivationSpecType spec);
        void PostMessage(PostMessageSpecType spec);
        void Activate(ActivateSpecType node);
        void PostVoiceMessage(PostVoiceMessageSpecType node);
    }

    internal class ChatUser : IChatUser, IChatUserContext
    {
        private readonly object _sessionsLock = new object();
        private readonly List<IChatClientSession> _sessions = new List<IChatClientSession>();

        public int SessionsCount { get { return _sessions.Count; } }

        private readonly object _userInfoLock = new object();
        private readonly DbUserInfo _userInfo;
        private readonly IChatService _service;

        public DbUserInfo Info { get { return _userInfo; } }

        public ChatUser(DbUserInfo userInfo, IChatService service)
        {
            _userInfo = userInfo;
            _service = service;
        }

        public void RegisterSession(IChatClientSession session)
        {
            lock (_sessionsLock)
                _sessions.Add(session);
        }

        public void UnregisterSession(IChatClientSession session)
        {
            lock (_sessionsLock)
                _sessions.Remove(session);
        }

        public void Send(ServerEnvelopContentType data)
        {
            IEnumerable<IChatClientSession> sessions;
            lock (_sessionsLock)
                sessions = _sessions.ToArray();

            foreach (var session in sessions)
                session.Send(data);
        }

        void IChatUserContext.ChangeEmail(ChangeEmailSpecType spec)
        {
            lock (_userInfoLock)
            {
                _service.ProfileManager.SetEmail(_userInfo, spec);
            }
        }

        void IChatUserContext.ChangePassword(ChangePasswordSpecType spec)
        {
            lock (_userInfoLock)
            {
                _service.ProfileManager.SetPassword(_userInfo, spec);
            }
        }

        public void RequestActivation(RequestActivationSpecType spec)
        {
            lock (_userInfo)
            {
                _service.ProfileManager.RequestActivation(_userInfo, spec);
            }
        }

        void IChatUserContext.Activate(ActivateSpecType spec)
        {
            if (this.Info.Activated)
                throw new ApplicationException("Your profile already activated");

            lock (_userInfo)
            {
                _service.ProfileManager.Activate(_userInfo, spec.Token);
            }
        }

        void IChatUserContext.PostMessage(PostMessageSpecType spec)
        {
            if (!this.Info.Activated)
                throw new ApplicationException("Your profile is not activated");

            _service.PostMessage(this, spec);
        }

        void IChatUserContext.PostVoiceMessage(PostVoiceMessageSpecType node)
        {
            if (!this.Info.Activated)
                throw new ApplicationException("Your profile is not activated");

            _service.PostVoiceMessage(this, node);
        }

        public void DeliverPacket(ServerEnvelopContentType packet)
        {
            IChatClientSession[] sessions;

            lock (_sessionsLock)
                sessions = _sessions.ToArray();


            sessions.ForEach(s => s.Send(packet));
        }

        //void IChatUserContext.StartChat()
        //{
        //    // _service.StartChat(this);
        //}
    }
}
