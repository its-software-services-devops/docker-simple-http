using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using GenHTTP.Engine;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Practices;
using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

namespace simple_http
{    
    class Program
    {
        private static bool isGcpAuthen = false;

        public class CustomHandler : IHandler
        {
            public IHandler Parent { get; }

            public CustomHandler(IHandler parent)
            {
                Parent = parent;
            }

            public ValueTask<IResponse> HandleAsync(IRequest request)
            {
                var req = request.Respond()
                    .Type(new FlexibleContentType(ContentType.TextPlain));

                string referer = request.Referer;
                if ((referer != null) && (referer.Contains("/restart")))
                {
                    req = req.Status(GenHTTP.Api.Protocol.ResponseStatus.ExpectationFailed);
                    
                    Console.WriteLine("Program ended by /restart path");
                    Environment.Exit(-1);
                }
                else
                {
                    req = req.Content("Hello World!");
                }
                var response = req.Build();

                return new ValueTask<IResponse>(response);
            }

            public ValueTask PrepareAsync()
            {
                // perform CPU or I/O heavy work to initialize this
                // handler and it's children
                return new ValueTask();
            }

            public IEnumerable<GenHTTP.Api.Content.ContentElement> GetContent(IRequest request) => Enumerable.Empty<GenHTTP.Api.Content.ContentElement>();
        }

        public class CustomHandlerBuilder : IHandlerBuilder<CustomHandlerBuilder>
        {
            private readonly List<IConcernBuilder> _Concerns = new List<IConcernBuilder>();

            public CustomHandlerBuilder Add(IConcernBuilder concern)
            {
                _Concerns.Add(concern);
                return this;
            }

            public IHandler Build(IHandler parent)
            {
                return Concerns.Chain(parent, _Concerns, (p) => new CustomHandler(p));
            }

        }        

        private static Thread t1 = new Thread(new ThreadStart(Thread1));
        private static Thread t2 = new Thread(new ThreadStart(Thread2));

        private static void Thread1() 
        {
            string msecStr = Environment.GetEnvironmentVariable("DELAY_MSEC");
            if (msecStr == null)
            {
                msecStr = "10";
            }

            Console.WriteLine("Program started with DELAY_MSEC=[{0}] second(s)", msecStr);
            int msec = Int32.Parse(msecStr) * 1000;
            if (msec > 0)
            {
                Thread.Sleep(msec);

                Console.WriteLine("Program ended");
                Environment.Exit(-1);
            }
        }

        private static void Thread2() 
        {
            string redisIP = Environment.GetEnvironmentVariable("REDIS_IP");
            string redisPort = Environment.GetEnvironmentVariable("REDIS_PORT");
            string hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            string workerIP = Environment.GetEnvironmentVariable("INSTANCE_IP");            

            if ((redisIP == null) || (redisPort == null))
            {
                Console.WriteLine("Env REDIS_IP and REDIS_PORT are required!!!");
                return;
            }

            if (workerIP == null)
            {
                workerIP = "N/A";
            }

            string jsonTemplate = "'dtm':'{0}Z', 'host':'{1}', 'success':'{2}', 'ip':'{3}', 'port':'{4}', 'worker':'{5}', 'errorMsg':'{6}'";
            var arr = new List<string>();

            int cnt = 0;
            while (true)
            {
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IAsyncResult result = clientSocket.BeginConnect(redisIP, Int32.Parse(redisPort), null, null);
                bool taskSuccess = result.AsyncWaitHandle.WaitOne(5 * 1000, true);

                int success = 0;
                string errMsg = "SUCCESS";
                if (taskSuccess && clientSocket.Connected)
                {
                    success = 1;
                    clientSocket.EndConnect(result);
                }
                else
                {
                    errMsg = "TIMEOUT";
                }
                
                clientSocket.Close();
                clientSocket = null;

                string dtm = DateTime.UtcNow.ToString("s");
                string json = String.Format(jsonTemplate,
                    dtm,
                    hostName,
                    success,
                    redisIP,
                    redisPort,
                    workerIP,
                    errMsg);                
                json = "{" + json + "}";

                Console.WriteLine(json);
                arr.Add(json);
                cnt++;

                if ((cnt % 12) == 0)
                {
                    SendStatsToBigQuery(arr);
                    arr.Clear();
                }

                Thread.Sleep(1000 * 5);
            }
        }

        private static void SendStatsToBigQuery(List<string> arr)
        {
            string os = Environment.GetEnvironmentVariable("OS");

            string fname = "connection-stat-to-bq.log";
            if (os == null)
            {
                //Linux
                fname = "/tmp/connection-stat-to-bq.log";
            }
            
            using (FileStream fs = File.Open(fname, FileMode.Create))
            {
                StreamWriter sw = new StreamWriter(fs);
                foreach (string line in arr)
                {                 
                    sw.WriteLine(line);
                }
                
                sw.Flush();
                sw.Close();
            }

            string keyFile = Environment.GetEnvironmentVariable("GCP_KEY_FILE_PATH");
            if (keyFile != null)
            {
                if (!isGcpAuthen)
                {
                    string gcloudArg = String.Format("auth activate-service-account --key-file={0}", keyFile);
                    using(System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
                    {
                        pProcess.StartInfo.FileName = "gcloud";
                        pProcess.StartInfo.Arguments = gcloudArg;
                        pProcess.StartInfo.UseShellExecute = true;
                        pProcess.StartInfo.RedirectStandardOutput = false;
                        pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                        pProcess.Start();
                        pProcess.WaitForExit();
                    }

                    Console.WriteLine("Authenticated to GCloud using key file [{0}]", keyFile);
                    isGcpAuthen = true;
                }
            }
            
            string cmd = "bq.cmd";
            if (os == null)
            {
                //Unix
                cmd = "bq";
            }
            string arg = String.Format("load --headless=true --project_id=gcp-dmp-devops --autodetect --source_format=NEWLINE_DELIMITED_JSON {0} {1}", "istio_upstream_error_stat.tcp_connection_stat", fname);
            using(System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
            {
                pProcess.StartInfo.FileName = cmd;
                pProcess.StartInfo.Arguments = arg;
                pProcess.StartInfo.UseShellExecute = true;
                pProcess.StartInfo.RedirectStandardOutput = false;
                pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                pProcess.Start();
                pProcess.WaitForExit();
            }

            Console.WriteLine("Sent to BigQuery");
        }

        private static int Main(string[] args)
        {
            string msecStr = Environment.GetEnvironmentVariable("DELAY_TO_START_SEC");
            if (msecStr == null)
            {
                msecStr = "0";
            }

            Console.WriteLine("Program started with DELAY_TO_START_SEC=[{0}] second(s)", msecStr);
            int msec = Int32.Parse(msecStr) * 1000;
            if (msec > 0)
            {
                Thread.Sleep(msec);
            }
            Console.WriteLine("Started HTTP event loop", msecStr);

            t1.Start();
            t2.Start();

            return Host.Create()
                       .Console()
                       .Defaults()
                       .Handler(new CustomHandlerBuilder())
                       .Run();                    
        }
    }
}
