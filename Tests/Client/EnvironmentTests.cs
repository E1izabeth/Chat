using Chat.UI.Util;
using Chat.UI.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Client
{
    [TestClass]
    public class EnvironmentTests
    {
        [TestMethod]
        public void RegisteredInstanceIsResolvedTest()
        {
            var env = new AppEnv();
            
            Assert.IsNotNull(env.Connector);
            Assert.IsNotNull(env.SessionFabric);
        }
    }
}
