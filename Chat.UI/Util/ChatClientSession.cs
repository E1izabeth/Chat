using Chat.Common;
using Chat.Interaction;
using Chat.Interaction.Network;
using Chat.Interaction.Xml;
using Chat.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat.UI.Util
{
    public interface IChatClientSession : IDisposable
    {
        void Start();

        void SetContext(ISessionContext context);

        Task<UserProfileInfoType> Login(string login, string password);
        Task Register(string email, string login, string password);
        Task Restore(string login, string email);
        Task SendMessage(string messageToSendText);
        
        Task RequestActivation();
        Task Activate(string messageToSendText);
        Task SendVoiceMessage(byte[] oggData);
    }

    class ChatClientSession : ConnectionSessionBase<ClientEnvelopType, ClientEnvelopContentType, ServerEnvelopType, ServerEnvelopContentType>, IChatClientSession, IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>
    {
        public event Action<ErrorInfoType> OnRemoteError;

        public ISessionContext Context { get; private set; }

        public ChatClientSession(IClientToServerConnection cnn)
            : base(cnn)
        {

        }

        protected override ClientEnvelopContentType OnAsyncPacket(ServerEnvelopContentType packet)
        {
            try
            {
                return packet.Apply(this);
            }
            catch (Exception ex)
            {
                return new ClientErrorInfoType() { Item = ex.MakeErrorInfo() };
            }
        }

        public void SetContext(ISessionContext context)
        {
            if (this.Context != null)
                throw new InvalidOperationException();

            this.Context = context;
        }

        public async Task<UserProfileInfoType> Login(string login, string password)
        {
            return await this.PostTask<UserProfileInfoType>(new LoginSpecType() { Login = login, Password = password });
        }

        public async Task Register(string email, string login, string password)
        {
            await this.PostTask<OkType>(new RegisterSpecType() { Email = email, Login = login, Password = password });
        }

        public async Task Restore(string login, string email)
        {
            await this.PostTask<OkType>(new ResetPasswordSpecType() { Login = login, Email = email });
        }

        public async Task SendMessage(string text)
        {
            await this.PostTask<OkType>(new PostMessageSpecType() {
                Text = text
            });
        }

        public async Task SendVoiceMessage(byte[] data)
        {
            await this.PostTask<OkType>(new PostVoiceMessageSpecType() {
                OggData = data
            });
        }

        public async Task RequestActivation()
        {
            await this.PostTask<OkType>(new RequestActivationSpecType());
        }

        public async Task Activate(string token)
        {
            await this.PostTask<OkType>(new ActivateSpecType() { Token = token });
        }

        public async Task<TimeSpan> Ping()
        {
            var response = await this.PostTask<PingResponseType>(new PingRequestType() { Stamp = DateTime.UtcNow });
            return DateTime.UtcNow - response.RequestStamp;
        }

        protected override void DisposeImpl()
        {
        }

        #region IServerEnvelopContentTypeVisitor<ClientEnvelopContentType> implementation

        ClientEnvelopContentType IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>.VisitChatMessageInfoType(ChatMessageInfoType node)
        {
            this.Context?.OnMessage(node);
            return null;
        }

        ClientEnvelopContentType IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>.VisitPingResponseType(PingResponseType node)
        {
            return null;
        }

        ClientEnvelopContentType IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>.VisitUserProfileInfoType(UserProfileInfoType node)
        {
            this.Context?.OnUserStatus(node);
            return null;
        }
        
        ClientEnvelopContentType IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>.VisitOkType(OkType node)
        {
            System.Diagnostics.Debug.Print("Unexpected OK received.");
            return null;
        }

        ClientEnvelopContentType IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>.VisitServerErrorInfoType(ServerErrorInfoType node)
        {
            this.OnRemoteError?.Invoke(node.Item);
            return null;
        }

        ClientEnvelopContentType IServerEnvelopContentTypeVisitor<ClientEnvelopContentType>.VisitChatVoiceMessageDataType(ChatVoiceMessageDataType node)
        {
            this.Context?.OnVoiceMessage(node);
            return null;
        }

        #endregion
    }
}
