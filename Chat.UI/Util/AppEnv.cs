using Chat.Interaction;
using Chat.Interaction.Network;
using Chat.UI.Common;
using Chat.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IocContainer = Funq.Container;

namespace Chat.UI.Util
{
    public interface IAppEnv
    {
        IClientConnector Connector { get; }
        ISessionFabric SessionFabric { get; }
        IDnsService Dns { get; }
    }

    public class AppEnv : IAppEnv
    {
        public IClientConnector Connector { get; private set; }
        public ISessionFabric SessionFabric { get; private set; }
        public IDnsService Dns { get; private set; }

        public AppEnv()
            : this(true)
        {
        }

        public AppEnv(bool setup)
        {
            if (setup)
                this.Setup();
        }

        private void Setup()
        {
            this.Connector = ClientConnector.GetInstance();
            this.SessionFabric = new SessionFabric(this);
            this.Dns = new DnsService();


            DelegatedCommand.OnPropogateError += ex => {
                var processExecutableName = Process.GetCurrentProcess().MainModule.ModuleName.ToLower();
                var myAssemblyMainModuleName = Assembly.GetExecutingAssembly().ManifestModule?.Name?.ToLower();

                return myAssemblyMainModuleName == processExecutableName;
            };
        }


        //public IocContainer Deps { get; }

        //public AppEnv(bool setup = true)
        //{
        //    this.Deps = new IocContainer();

        //    if (setup)
        //        this.Setup(this.Deps);
        //}

        //private void Setup(IocContainer deps)
        //{
        //    deps.Register<ISessionFabric>(new SessionFabric());
        //    deps.Register<IClientConnector>(ClientConnector.GetInstance());
        //}
    }
}
