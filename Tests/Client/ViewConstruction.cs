using Chat.UI;
using Chat.UI.View;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Client
{
    [TestClass]
    class ViewConstruction
    {
        [TestMethod]
        public void HomeViewConstructionTest()
        {
            new HomeView();
        }

        [TestMethod]
        public void SessionViewConstructionTest()
        {
            new SessionView();
        }

        [TestMethod]
        public void MainWindowConstructionTest()
        {
            new MainWindow();
        }
    }
}
