using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace WinWeb
{
    /// <summary>
    /// Simple class to parse the request into a path, an array of headers, and url parameters
    /// </summary>
    class HttpRequest
    {
        private string http_request;
        /// <summary>
        /// The url path, default is /
        /// </summary>
        public string Path { get; private set; }
        /// <summary>
        /// A list of key values representing the headers
        /// </summary>
        public Dictionary<string, string> Headers;
        /// <summary>
        /// a list of key values representing the URL parameters
        /// </summary>
        public Dictionary<string, string> Parameters;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="http_request">Raw http request text</param>
        public HttpRequest(string http_request)
        {

            this.http_request = http_request;

            Headers = new Dictionary<string, string>();

            Parameters = new Dictionary<string, string>();

            this.Path = get_path();

            get_headers();

               get_parameters();

            StringBuilder build = new StringBuilder();

            for (int i = 0; i < Path.Length; i++)
            {
                if (Path[i].Equals('?'))
                    break;
                build.Append(Path[i]);
            }

            if (build.Length > 1 && build[build.Length - 1].Equals('/'))
                build.Remove(build.Length - 1, 1);

            Path = build.ToString();

        }
        private string get_path()
        {
            StringBuilder path = new StringBuilder();

            bool flag = false;

            for (int i = 0; i < this.http_request.Length; i++)
            {
                if (this.http_request[i].Equals('/'))
                {
                    flag = true;
                }
                if (flag && this.http_request[i].Equals(' '))
                {
                    break;
                }
                if (flag)
                {
                    path.Append(this.http_request[i]);
                }
            }
            return path.ToString();
        }

        private void get_headers()
        {
            string copy = http_request.Replace('\n', '\0');

            string[] split = copy.Split('\r');

            for (int i = 1; i < split.Length - 2; i++)
            {
                string[] keyval = split[i].Split(':');

                Headers.Add(keyval[0].Substring(1), keyval[1].Substring(1));
            }
        }

        private void get_parameters()
        {
            int index = Path.IndexOf("?");
            
            if (index != -1)
            {
                string pstring = Path.Substring(index + 1);

                string[] queries = pstring.Split('&');

                foreach (string str in queries)
                {
                    string[] keyval = str.Split('=');

                    Parameters.Add(keyval[0], keyval[1]);
                }
            }
        }

        public override string ToString()
        {
            return this.http_request;
        }
    }
    /// <summary>
    /// Buils an HTTP request header and body 
    /// </summary>
    class HttpResponse
    {
        /// <summary>
        /// HTTP Status Code, default is 200 OK
        /// </summary>
        public HttpStatusCode Code;
        /// <summary>
        /// key value list of headers to send to the server
        /// </summary>
        public Dictionary<string, object> Headers;  
        /// <summary>
        /// The data to be sent to the client, can be text, html, etc...
        /// </summary>
        public string Body;

        /// <summary>
        /// Constructor
        /// </summary>
        public  HttpResponse()
        {
            Code = HttpStatusCode.OK;

            Headers = new Dictionary<string, object>();

            Body = "";
        }
        /// <summary>
        /// Returns an HTTP response in plain text
        /// </summary>
        /// <returns>Raw HTTP response string</returns>
        public override string ToString()
        {
            StringBuilder response = new StringBuilder();

            response.AppendFormat("HTTP {0} {1}\r\n", ((int)Code).ToString(), Code.ToString());

            if (Headers.Keys.Contains("Content-Length"))
            {
                Headers["Content-Length"] = Body.Length;
            }
            else
            {
                Headers.Add("Content-Length", Body.Length);
            }

            if (!Headers.Keys.Contains("Content-Type"))
            {
                Headers.Add("Content-Type", "text/plain");
            }

            foreach (KeyValuePair<string, object> head in Headers)
            {
                response.AppendFormat("{0}: {1}\r\n", head.Key, head.Value);
            }

            response.AppendFormat("\r\n{0}", Body);

            return response.ToString();
        }
    }
    /// <summary>
    /// Simple asyncronous webserver based off the Node.js http library
    /// </summary>
    class WinServer
    {
        private ManualResetEvent mre;
        private TcpListener server;

        //default buffer size to hold http request from client
        const int BUFFER_SIZE = 2048;

        /// <summary>
        /// Callback function
        /// </summary>
        /// <param name="req">The request sent from the client, already parsed and can be used to route, read parameters, etc...</param>
        /// <param name="res">The response to be sent to the client after the request, modify the Code, Headers, and Body for a custom response</param>
        public delegate void OnRecieveRequest(HttpRequest req, HttpResponse res);

        /// <summary>
        /// Constructor
        /// </summary>
        public WinServer()
        {
            mre = new ManualResetEvent(false);
        }

        /// <summary>
        /// Runs the web server
        /// </summary>
        /// <param name="port">The port number to be used, the default port for HTTP is 80</param>
        /// <param name="callback">The callback function to read the request and modify the response</param>
        public void RunServer(int port, OnRecieveRequest callback)
        {
            //calls the main 'RunServer' method
            this.RunServer(port, IPAddress.Any, callback);
        }
        /// <summary>
        /// Runs the web server
        /// </summary>
        /// <param name="port">The port number to be used, the default port for HTTP is 80</param>
        /// <param name="address">A specified ip address</param>
        /// <param name="callback">The callback function to read the request and modify the response</param>
        public void RunServer(int port, IPAddress address, OnRecieveRequest callback)
        {
            //begin listening
            server = new TcpListener(address, port);

            server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

            server.Start();

            //begin accepting
            while (true)
            {
                mre.Reset();

                //on accept, begin reading response
                server.BeginAcceptTcpClient((ac1) =>
                    {
                        //once a client connects, unblock to recieve other responses
                        mre.Set();
                        TcpClient client = ((TcpListener)(ac1.AsyncState)).EndAcceptTcpClient(ac1);

                        NetworkStream stream = client.GetStream();

                        //to hold our request
                        byte[] buffer = new byte[WinServer.BUFFER_SIZE];

                        stream.BeginRead(buffer, 0, WinServer.BUFFER_SIZE, (ac2) =>
                        {
                            int bytes_read = stream.EndRead(ac2);

                            string req_str = UnicodeEncoding.ASCII.GetString(buffer, 0, bytes_read);

                            HttpResponse res = new HttpResponse();

                            //invoke the callback method in order to read the request and build the response
                            callback(new HttpRequest(req_str), res);

                        //begin sending response;

                        byte[] resbytes = UnicodeEncoding.ASCII.GetBytes(res.ToString());

                            //send the built response to the client
                            stream.BeginWrite(resbytes, 0, resbytes.Length, (ac3) =>
                            {
                                stream.EndWrite(ac3);

                                //close everything once the response is sent
                                stream.Close();

                                client.Close();

                                //looks like callback hell
                            }, stream);
                        }, stream);

                    }, server);

                //block and wait for the next response
                mre.WaitOne();
            }
        }
    }
}
