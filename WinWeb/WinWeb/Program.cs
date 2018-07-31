using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinWeb
{
    class Program
    {
        static void Main(string[] args)
        { 
            WinServer server = new WinServer();
            server.RunServer(8080, callback);
        }
        static void callback(HttpRequest req, HttpResponse res)
        {
            Console.WriteLine(req.ToString());
        }
    }
}