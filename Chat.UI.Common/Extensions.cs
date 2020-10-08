using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Chat.UI.Common
{
    public static class Extensions
    {
        public static void InvokeAction(this DispatcherObject obj, Action act)
        {
            if (obj.Dispatcher.CheckAccess())
            {
                act();
            }
            else
            {
                obj.Dispatcher.Invoke(act);
            }
        }
    }
}
