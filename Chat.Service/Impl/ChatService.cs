using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Chat;
using Chat.Common;
using Chat.Interaction;
using Chat.Interaction.Network;
using Chat.Interaction.Xml;
using Chat.Service.Db;
using Chat.Service.Util;

namespace Chat.Service.Impl
{
    public interface IChatService : IDisposable
    {
        ChatServiceConfiguration Configuration { get; }
        ISecureRandom SecureRandom { get; }
        IProfileManager ProfileManager { get; }

        IBasicOperationContext OpenLocalContext();
        IChatDbContext OpenDb();
        void SendMail(string targetEmail, string subject, string text);

        IChatUserContext PerformLogin(IChatClientSession session, LoginSpecType spec);
        void PerformRegistration(RegisterSpecType spec);
        void ResetPassword(ResetPasswordSpecType node);

        void PostMessage(IChatUser chatUser, PostMessageSpecType spec);
        void PostVoiceMessage(IChatUser chatUser, PostVoiceMessageSpecType node);
        //void StartChat(ChatUser chatUser);
    }

    public class ChatService : IChatService
    {
        public ChatServiceConfiguration Configuration { get; private set; }
        public ISecureRandom SecureRandom { get; private set; }

        public IProfileManager ProfileManager { get; }

        private readonly DisposableList _disposables = new DisposableList();
        private readonly object _userContextsLock = new object();
        private readonly Dictionary<string, ChatUser> _usersByLogin = new Dictionary<string, ChatUser>();
        private readonly object _sessionsLock = new object();
        private readonly LinkedList<ChatClientSession> _sessions = new LinkedList<ChatClientSession>();

        public ChatService(ChatServiceConfiguration configuration)
        {
            this.Configuration = configuration;
            this.SecureRandom = new SecureRandom();

            this.Initialize();

            this.ProfileManager = new ProfileManager(this);

            AppDomain.CurrentDomain.FirstChanceException += (sender, ea) => System.Diagnostics.Debug.Print(ea.Exception.ToString());

            var listener = _disposables.Add(ServerHost.GetInstance().CreateListener(new IPEndPoint(IPAddress.Any, configuration.ServicePort)));
            listener.OnConnect += this.OnConnectionProc;
            listener.Start();
        }

        public IChatDbContext OpenDb()
        {
            var cnn = new SqlConnection(this.Configuration.DbConnectionString);
            cnn.InfoMessage += (sneder, ea) => {
                System.Diagnostics.Debug.Print(ea.Source + ": " + ea.Message);
                ea.Errors.OfType<SqlError>().ToList().ForEach(e => {
                    System.Diagnostics.Debug.Print(e.Source + " (" + e.Procedure + ") : " + e.Message);
                });
            };
            cnn.Open();

            var ctx = new DbContext(cnn);
            ctx.Log = new DebugTextWriter();
            return new ChatDbContext(ctx);
        }

        public void SendMail(string targetEmail, string subject, string text)
        {
            // "FromName<FromLogin@host>"
            using (MailMessage mm = new MailMessage(this.Configuration.SmtpLogin, targetEmail))
            {
                mm.Subject = subject;
                mm.Body = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
 <head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
  <title>" + subject + @"</title>
</head>
<body>
" + text + @"
</body>
</html>";
                mm.IsBodyHtml = true;
                mm.Headers["Content-Type"] = "text/html; charset=utf-8";
                mm.BodyEncoding = Encoding.UTF8;

                using (SmtpClient sc = new SmtpClient(this.Configuration.SmtpServerHost, this.Configuration.SmtpServerPort))
                {
                    sc.PickupDirectoryLocation = this.Configuration.SmtpPickupDirectoryLocation;
                    sc.EnableSsl = this.Configuration.SmtpUseSsl;
                    sc.DeliveryMethod = this.Configuration.SmtpDeliveryMethod;
                    sc.UseDefaultCredentials = this.Configuration.SmtpUseDefaultCredentials;
                    sc.Credentials = new NetworkCredential(this.Configuration.SmtpLogin, this.Configuration.SmtpPassword);
                    sc.Send(mm);
                }
            }
        }

        const string _asciifyPrefix = "!asciify";

        public void PostMessage(IChatUser chatUser, PostMessageSpecType spec)
        {
            var text = spec.Text;

            if (text.ToLower().StartsWith(_asciifyPrefix))
            {
                var url = text.Substring(_asciifyPrefix.Length).Trim();
                using (var wc = new WebClient())
                {
                    var data = wc.DownloadData(url);
                    using (var bitmap = new Bitmap(new MemoryStream(data)))
                    {
                        var asciify = new ASCIIfy();
                        text = asciify.GetAsciiString(bitmap, new Rectangle(0, 0, 4, 8), 1);
                    }
                }
            }

            var msg = new ChatMessageInfoType() {
                AuthorUserInfo = new UserProfileInfoType() {
                    IsActivated = chatUser.Info.Activated,
                    IsOnline = true,
                    Login = chatUser.Info.Login
                },
                Stamp = DateTime.UtcNow,
                Text = text
            };

            this.DeliverPacket(msg);
        }
        
        public void PostVoiceMessage(IChatUser chatUser, PostVoiceMessageSpecType node)
        {
            var msg = new ChatVoiceMessageDataType() {
                AuthorUserInfo = new UserProfileInfoType() {
                    IsActivated = chatUser.Info.Activated,
                    IsOnline = true,
                    Login = chatUser.Info.Login
                },
                Stamp = DateTime.UtcNow,
                OggData = node.OggData
            };
         
            this.DeliverPacket(msg);
        }

        private void DeliverPacket(ServerEnvelopContentType packet)
        {
            ChatUser[] users;
            lock (_userContextsLock)
                users = _usersByLogin.Values.ToArray();

            users.ForEach(u => u.DeliverPacket(packet));
        }

        void IDisposable.Dispose()
        {
            _disposables.SafeDispose();
        }

        public IBasicOperationContext OpenLocalContext()
        {
            return new BasicOperationContext(this);
        }

        private void Initialize()
        {
            using (var ctx = this.OpenLocalContext())
            {
                ctx.Db.Raw.CreateTables();

                //if (!ctx.Db.Raw.DatabaseExists())
                //    ctx.Db.Raw.CreateDatabase();
            }
        }

        private void OnConnectionProc(IServerToClientConnection cnn)
        {
            var session = new ChatClientSession(cnn, this);
            lock (_sessions)
                session.Node = _sessions.AddLast(session);

            cnn.OnClosed += () => {
                lock (_sessions)
                    _sessions.Remove(session.Node);

                session.Node = null;
                session.SafeDispose();
            };

            cnn.Start();
        }

        public void Dispose()
        {
            _disposables.SafeDispose();
        }

        IChatUserContext IChatService.PerformLogin(IChatClientSession session, LoginSpecType spec)
        {
            var userInfo = this.ProfileManager.ValidateCredentials(spec);

            lock (_userContextsLock)
            {
                if (!_usersByLogin.TryGetValue(userInfo.LoginKey, out var ctx))
                {
                    _usersByLogin.Add(userInfo.LoginKey, ctx = new ChatUser(userInfo, this));
                }

                ctx.RegisterSession(session);
                session.OnClosed += () => {
                    ctx.UnregisterSession(session);
                    this.DeliverPacket(new UserProfileInfoType() {
                        Login = userInfo.Login,
                        IsActivated = userInfo.Activated,
                        IsOnline = false,
                    });
                    if (ctx.SessionsCount == 0)
                    {
                        lock (_userContextsLock)
                        {
                            _usersByLogin.Remove(ctx.Info.LoginKey);
                        }
                    }
                };

                this.DeliverPacket(new UserProfileInfoType() {
                    Login = userInfo.Login,
                    IsActivated = userInfo.Activated,
                    IsOnline = true,
                });

                return ctx;
            }
        }

        void IChatService.PerformRegistration(RegisterSpecType spec)
        {
            this.ProfileManager.Register(spec);
        }

        void IChatService.ResetPassword(ResetPasswordSpecType spec)
        {
            this.ProfileManager.RequestAccess(spec);
        }

        //void IChatService.StartChat(ChatUser chatUser)
        //{
        //    lock (_userContextsLock)
        //    {

        //            _usersByLogin.Values.Select(u => u.Info.Login)

        //        return ctx;
        //    }

        //    chatui
        //}
    }

    public class DebugTextWriter : StreamWriter
    {
        public DebugTextWriter()
            : base(new DebugOutStream(), Encoding.Unicode, 1024)
        {
            this.AutoFlush = true;
        }

        private sealed class DebugOutStream : Stream
        {
            public override void Write(byte[] buffer, int offset, int count)
            {
                System.Diagnostics.Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
                this.Flush();
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override void Flush() => System.Diagnostics.Debug.Flush();

            public override long Length => throw bad_op;
            public override int Read(byte[] buffer, int offset, int count) => throw bad_op;
            public override long Seek(long offset, SeekOrigin origin) => throw bad_op;
            public override void SetLength(long value) => throw bad_op;
            public override long Position
            {
                get => throw bad_op;
                set => throw bad_op;
            }

            private static InvalidOperationException bad_op => new InvalidOperationException();
        };
    }
}