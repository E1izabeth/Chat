using Chat.Common;
using Chat.Interaction.Xml;
using Chat.Service.Db;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Service.Impl
{
    public interface IProfileManager
    {
        IChatService Service { get; }

        void Activate(DbUserInfo userInfo, string key);
        void RestoreAccess(string key);
        void Register(RegisterSpecType registerSpec);
        void RequestAccess(ResetPasswordSpecType spec);
        DbUserInfo ValidateCredentials(LoginSpecType loginSpec);

        void RequestActivation(DbUserInfo userInfo, RequestActivationSpecType spec);
        void SetEmail(DbUserInfo userInfo, ChangeEmailSpecType spec);
        void SetPassword(DbUserInfo userInfo, ChangePasswordSpecType spec);
    }

    class ProfileManager : IProfileManager
    {
        public IChatService Service { get; }

        public ProfileManager(IChatService service)
        {
            this.Service = service;
        }


        #region profile management

        public void Register(RegisterSpecType registerSpec)
        {
            if (registerSpec.Login.IsEmpty())
                throw new ApplicationException("Login cannot be empty");

            if (registerSpec.Password.IsEmpty() || registerSpec.Password.Length < 10)
                throw new ApplicationException("Password should be of length >10 characters");

            using (var ctx = this.Service.OpenLocalContext())
            {
                var loginKey = registerSpec.Login.ToLower();
                if (ctx.Db.Users.FindUserByLoginKey(loginKey) != null)
                {
                    throw new ApplicationException("User " + registerSpec.Login + " already exists");
                }
                else
                {
                    var salt = Convert.ToBase64String(this.Service.SecureRandom.GenerateRandomBytes(64));

                    var user = new DbUserInfo() {
                        Activated = false,
                        HashSalt = salt,
                        Email = registerSpec.Email,
                        IsDeleted = false,
                        RegistrationStamp = DateTime.UtcNow,
                        Login = registerSpec.Login,
                        LoginKey = registerSpec.Login.ToLower(),
                        PasswordHash = registerSpec.Password.ComputeSha256Hash(salt),
                        LastLoginStamp = SqlDateTime.MinValue.Value,
                        LastTokenStamp = SqlDateTime.MinValue.Value
                    };
                    ctx.Db.Users.AddUser(user);
                    ctx.Db.SubmitChanges();

                    this.RequestActivationImpl(user, registerSpec.Email);
                    ctx.Db.SubmitChanges();
                }
            }
        }

        public void RequestActivation(DbUserInfo userInfo, RequestActivationSpecType spec)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                ctx.Db.Raw.Users.Attach(userInfo);
                // ctx.ValidateAuthorized(false);
                this.RequestActivationImpl(userInfo, userInfo.Email);
                ctx.Db.SubmitChanges();
            }
        }

        private string MakeToken()
        {
            return new[] { "=", "/", "+" }.Aggregate(Convert.ToBase64String(this.Service.SecureRandom.GenerateRandomBytes(64)), (s, c) => s.Replace(c, string.Empty));
        }

        private void RequestActivationImpl(DbUserInfo user, string email)
        {
            if (user.Activated)
                throw new ApplicationException("Already activated");

            var activationToken = this.MakeToken();

            user.LastToken = activationToken;
            user.LastTokenStamp = DateTime.UtcNow;
            user.LastTokenKind = DbUserTokenKind.Activation;

            this.Service.SendMail(
                email, "Registration activation",
                $"To confirm your registration use activation token: " + activationToken
            // $"To confirm your registration follow this link: " + this.MakeSeriveLink(ctx, "/profile?action=activate&key=" + activationToken)
            );
        }

        //string MakeSeriveLink(ICarblamRequestContext ctx, string relLink)
        //{
        //    // /profile?action=activate&key={key}

        //    var urlBuilder = _ctx.Configuration.GetServiceUrl();
        //    var pair = ctx.RequestHostName.Split(new[] { ':' }, 2);
        //    urlBuilder.Host = pair[0];
        //    if (pair.Length > 1 && ushort.TryParse(pair[1], out var port))
        //        urlBuilder.Port = port;

        //    var linkUri = new Uri(urlBuilder.Uri, relLink.TrimStart('/'));
        //    return @"<a href=""" + linkUri + @""">" + linkUri + "</a>";
        //}

        public void RequestAccess(ResetPasswordSpecType spec)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                var loginKey = spec.Login.ToLower();

                var user = ctx.Db.Users.FindUserByLoginKey(loginKey);
                if (user != null && user.Email == spec.Email && !user.IsDeleted)
                {
                    var accessRestoreToken = this.MakeToken();

                    user.LastToken = accessRestoreToken;
                    user.LastTokenStamp = DateTime.UtcNow;
                    user.LastTokenKind = DbUserTokenKind.AccessRestore;
                    ctx.Db.SubmitChanges();

                    this.Service.SendMail(
                        spec.Email, "Access restore",
                        $"To regain access to your profile use restore token: " + accessRestoreToken
                    //$"To regain access to your profile follow this link: " + this.MakeSeriveLink(ctx, "/profile?action=restore&key=" + accessRestoreToken)
                    );
                }
                else
                {
                    throw new ApplicationException("User not found or incorrect email");
                }
            }
        }

        public void Activate(DbUserInfo userInfo, string key)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                //var user = ctx.Db.Users.FindUserByTokenKey(key);
                if (userInfo != null && userInfo.LastToken == key && userInfo.LastTokenKind == DbUserTokenKind.Activation)
                {
                    if (userInfo.Activated)
                        throw new ApplicationException("Already activated");

                    if (userInfo.LastTokenStamp + this.Service.Configuration.TokenTimeout >= DateTime.UtcNow)
                    {
                        ctx.Db.Raw.Users.Attach(userInfo);
                        userInfo.LastLoginStamp = DateTime.UtcNow;
                        userInfo.LastToken = null;
                        userInfo.Activated = true;
                        ctx.Db.SubmitChanges();
                    }
                    else
                    {
                        throw new ApplicationException("Acivation token expired");
                    }
                }
                else
                {
                    throw new ApplicationException("Invalid activation token");
                }
            }
        }

        public void RestoreAccess(string key)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                var user = ctx.Db.Users.FindUserByTokenKey(key);
                if (user != null && user.LastToken != null && user.LastTokenKind == DbUserTokenKind.AccessRestore)
                {
                    if (user.LastTokenStamp + this.Service.Configuration.TokenTimeout >= DateTime.UtcNow)
                    {
                        user.LastLoginStamp = DateTime.UtcNow;
                        user.LastToken = null;
                        ctx.Db.SubmitChanges();
                        // ctx.Session.SetUserContext(user);
                    }
                    else
                    {
                        throw new ApplicationException("Acivation token expired");
                    }
                }
                else
                {
                    throw new ApplicationException("Invalid activation token");
                }
            }
        }

        public void SetEmail(DbUserInfo userInfo, ChangeEmailSpecType spec)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                // ctx.ValidateAuthorized(false);

                if (userInfo.Email == spec.OldEmail &&
                    userInfo.PasswordHash == spec.Password.ComputeSha256Hash(userInfo.HashSalt))
                {
                    ctx.Db.Raw.Users.Attach(userInfo);
                    userInfo.Email = spec.NewEmail;
                    ctx.Db.SubmitChanges();
                }
                else
                {
                    throw new ApplicationException("Invalid old email or password");
                }
            }
        }

        public void SetPassword(DbUserInfo userInfo, ChangePasswordSpecType spec)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                if (userInfo.Email == spec.Email)
                // user.PasswordHash == spec.OldPassword.ComputeSha256Hash(user.HashSalt))
                {
                    ctx.Db.Raw.Users.Attach(userInfo);
                    userInfo.PasswordHash = spec.NewPassword.ComputeSha256Hash(userInfo.HashSalt);
                    ctx.Db.SubmitChanges();

                    this.Service.SendMail(spec.Email, "Password was changed", "Dear " + userInfo.Login + ", your password was changed!");
                }
                else
                {
                    throw new ApplicationException("Invalid old email");
                }
            }
        }

        public DbUserInfo ValidateCredentials(LoginSpecType loginSpec)
        {
            using (var ctx = this.Service.OpenLocalContext())
            {
                var loginKey = loginSpec.Login;
                var user = ctx.Db.Users.FindUserByLoginKey(loginKey);
                if (user != null && user.PasswordHash == loginSpec.Password.ComputeSha256Hash(user.HashSalt) && !user.IsDeleted)
                {
                    user.LastLoginStamp = DateTime.UtcNow;
                    ctx.Db.SubmitChanges();

                    // ctx.Session.SetUserContext(user);
                    return user;
                }
                else
                {
                    throw new ApplicationException("Invalid credentials");
                }
            }
        }


        //public void DeleteProfile()
        //{
        //    using (var ctx = _ctx.OpenWebRequestContext())
        //    {
        //        ctx.ValidateAuthorized();

        //        var user = ctx.Db.Users.GetUserById(ctx.Session.UserId);
        //        user.IsDeleted = true;
        //        user.LastToken = null;

        //        _ctx.SessionsManager.DropUserSessions(user.Id);
        //        ctx.Db.SubmitChanges();
        //    }
        //}

        #endregion
    }
}
