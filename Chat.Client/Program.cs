using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace Chat.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new Chat.UI.App();
            app.Run();
        }
    }
}
