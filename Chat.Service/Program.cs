using Chat.Service.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Chat.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            var cfg = new ChatServiceConfiguration() {
                DbConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=chatdb1;Integrated Security=True;MultipleActiveResultSets=True;TransparentNetworkIPResolution=False",
                // SessionTimeout = TimeSpan.FromHours(1),
                TokenTimeout = TimeSpan.FromDays(7),
                DeliveryTimeout = TimeSpan.FromMinutes(15),
                ServicePort = 12345,

                SmtpLogin = "chat@localhost",
                SmtpServerHost = "127.0.0.1",
                SmtpServerPort = 25,
                SmtpUseSsl = false,
                SmtpUseDefaultCredentials = true,
                SmtpDeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                SmtpPickupDirectoryLocation = @"D:\Temp\"
            };

            using (var svc = new ChatService(cfg))
            {
                Console.WriteLine("Char service is running.");
                Console.WriteLine();
                Console.WriteLine("Press Q to stop");   
                while (Console.ReadKey().Key != ConsoleKey.Q) ;
            }
        }
    }
}
