using Chat.Interaction;
using Chat.Interaction.Network;
using Chat.Interaction.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Service.Impl
{
    public interface IChatClientSession : IDisposable
    {
        event Action OnClosed;

        void Send(ServerEnvelopContentType data);
    }

    class ChatClientSession : ConnectionSessionBase<ServerEnvelopType, ServerEnvelopContentType, ClientEnvelopType, ClientEnvelopContentType>, 
                              IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>, IChatClientSession
    {
        public LinkedListNode<ChatClientSession> Node { get; set; }

        public event Action<ErrorInfoType> OnRemoteError;
        public event Action OnClosed;

        private IChatUserContext _userContext;
        public IChatUserContext UserContext
        {
            get
            {
                if (_userContext == null)
                    throw new InvalidOperationException("Unauthorized access prohibited");

                return _userContext;
            }
        }

        public IChatService Service { get; }
        
        public ChatClientSession(IServerToClientConnection cnn, IChatService service)
            : base(cnn)
        {
            _packetsCount++;
            this.Service = service;
        }

        private void ValidateNotLoggedIn()
        {
            if (_userContext != null)
                throw new InvalidOperationException("Session already in use");
        }

        protected override ServerEnvelopContentType OnAsyncPacket(ClientEnvelopContentType packet)
        {
            try
            {
                return packet.Apply(this);
            }
            catch (Exception ex)
            {
                return new ServerErrorInfoType() { Item = ex.MakeErrorInfo() };
            }
        }

        protected override void DisposeImpl()
        {
            this.OnClosed?.Invoke();
        }

        public void Send(ServerEnvelopContentType data)
        {
            this.SendPacket(data);
        }

        #region IClientEnvelopContentTypeVisitor<ServerEnvelopContentType> implementation

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitChangeEmailSpecType(ChangeEmailSpecType node)
        {
            this.UserContext.ChangeEmail(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitChangePasswordSpecType(ChangePasswordSpecType node)
        {
            this.UserContext.ChangePassword(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitClientErrorInfoType(ClientErrorInfoType node)
        {
            return null;
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitLoginSpecType(LoginSpecType node)
        {
            this.ValidateNotLoggedIn();
            _userContext = this.Service.PerformLogin(this, node);
            return new UserProfileInfoType() {
                Login = _userContext.Info.Login,
                IsActivated = _userContext.Info.Activated,
                IsOnline = true,
            };
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitPingRequestType(PingRequestType node)
        {
            return new PingResponseType() { RequestStamp = node.Stamp };
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitPostMessageSpecType(PostMessageSpecType node)
        {
            this.UserContext.PostMessage(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitPostVoiceMessageSpecType(PostVoiceMessageSpecType node)
        {
            this.UserContext.PostVoiceMessage(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitRegisterSpecType(RegisterSpecType node)
        {
            this.ValidateNotLoggedIn();
            this.Service.PerformRegistration(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitRequestActivationSpecType(RequestActivationSpecType node)
        {
            this.UserContext.RequestActivation(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitResetPasswordSpecType(ResetPasswordSpecType node)
        {
            this.ValidateNotLoggedIn();
            this.Service.ResetPassword(node);
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitStartChatSpecType(StartChatSpecType node)
        {
            // this.UserContext.StartChat();
            return new OkType();
        }

        ServerEnvelopContentType IClientEnvelopContentTypeVisitor<ServerEnvelopContentType>.VisitActivateSpecType(ActivateSpecType node)
        {
            var x = this.UserContext;
            this.UserContext.Activate(node);

            return new OkType();
        }

        #endregion
    }
}
