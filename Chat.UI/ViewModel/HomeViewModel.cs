using Chat.Common;
using Chat.Interaction.Xml;
using Chat.UI.Common;
using Chat.UI.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Chat.UI.ViewModel
{
    public enum HomeScreenMode
    {
        Login,
        Register,
        Restore
    }

    public class HomeViewModel : DependencyObject
    {
        public ReadOnlyCollection<HomeScreenMode> Modes { get; } = new ReadOnlyCollection<HomeScreenMode>(Enum.GetValues(typeof(HomeScreenMode))
                                                                                                              .OfType<HomeScreenMode>().ToArray());

        public IAppEnv Env { get; set; }

        #region HomeScreenMode Mode 

        public HomeScreenMode Mode
        {
            get { return (HomeScreenMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Mode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(HomeScreenMode), typeof(HomeViewModel), new UIPropertyMetadata(default(HomeScreenMode)));

        #endregion
        
        #region bool IsLoggedIn 

        public bool IsLoggedIn
        {
            get { return (bool)GetValue(IsLoggedInProperty); }
            set { SetValue(IsLoggedInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLoggedIn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoggedInProperty =
            DependencyProperty.Register("IsLoggedIn", typeof(bool), typeof(HomeViewModel), new UIPropertyMetadata(false));

        #endregion

        #region SessionViewModel CurrentSession 

        public SessionViewModel CurrentSession
        {
            get { return (SessionViewModel)GetValue(CurrentSessionProperty); }
            set { SetValue(CurrentSessionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentSession.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentSessionProperty =
            DependencyProperty.Register("CurrentSession", typeof(SessionViewModel), typeof(HomeViewModel), new UIPropertyMetadata(default(SessionViewModel)));

        #endregion

        public string Email { get; set; }
        public string Email2 { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }

        public string InfoMessage { get; set; }
        public bool IsMessageVisible { get; set; }

        public DelegatedCommand LoginCommand { get; }
        public DelegatedCommand RegisterCommand { get; }
        public DelegatedCommand RestoreCommand { get; }

        public HomeViewModel()
        {
            this.IsLoggedIn = false;

            this.LoginCommand = new DelegatedCommand(async o => {
                IChatClientSession session = null;
                try
                {
                    session = this.Env.SessionFabric.OpenSession("localhost", 12345);
                    session.Start();
                    var profile  = await session.Login(this.Login, this.Password);

                    this.CurrentSession = new SessionViewModel(session, profile);
                    this.IsLoggedIn = true;

                    session.SetContext(this.CurrentSession);
                    // session.StartChat();
                }
                catch (Exception ex)
                {
                    if (session != null)
                        session.SafeDispose();

                    this.CurrentSession = null;
                    this.IsLoggedIn = false;
                    throw new ApplicationException("Failed to login", ex);
                }
            });

            this.RegisterCommand = new DelegatedCommand(async o => {
                try
                {
                    if(this.Password != this.Password2)
                    {
                        throw new ArgumentException("Passwords are not matched");
                    }

                    using (var session = this.Env.SessionFabric.OpenSession("localhost", 12345))
                    {
                        session.Start();
                        await session.Register(this.Email, this.Login, this.Password);

                        //this.CurrentSession = new SessionViewModel(session);
                        this.IsLoggedIn = false;
                        this.InfoMessage = "Activation link was sent to your email!";
                        this.IsMessageVisible = true;
                    }

                    this.Mode = HomeScreenMode.Login;
                }
                catch (Exception ex)
                {
                    this.CurrentSession = null;
                    this.IsLoggedIn = false;
                    throw new ApplicationException("Failed to register", ex);
                }
            });

            this.RestoreCommand = new DelegatedCommand(async o => {
                try
                {
                    if (this.Email != this.Email2)
                    {
                        throw new ArgumentException("Emails are not matched");
                    }

                    var session = this.Env.SessionFabric.OpenSession("localhost", 12345);
                    session.Start();
                    await session.Restore(this.Login, this.Email);

                    this.CurrentSession = new SessionViewModel(session);
                    this.IsLoggedIn = false;
                    this.Mode = HomeScreenMode.Login;
                }
                catch (Exception ex)
                {
                    this.CurrentSession = null;
                    this.IsLoggedIn = false;
                    throw new ApplicationException("Failed to restore", ex);
                }
            });
        }

    }
}
