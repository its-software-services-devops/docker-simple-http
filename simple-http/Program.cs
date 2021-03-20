using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using GenHTTP.Engine;
using GenHTTP.Modules.Practices;

namespace simple_http
{    
    class Program
    {
        private static Thread t1 = new Thread(new ThreadStart(Thread1));        

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

        private static void Thread2(object param) 
        {
            int size = Int32.Parse(Environment.GetEnvironmentVariable("TCP_CHECK_BQ_SIZE"));
            string hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            string workerIP = Environment.GetEnvironmentVariable("INSTANCE_IP");

            string redisIP = (param as ConnectParam).DestinationIP;
            string redisPort = (param as ConnectParam).Port.ToString();
            string keyword = (param as ConnectParam).Keyword;
            string table = (param as ConnectParam).BigQueryTable;

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
                bool taskSuccess = result.AsyncWaitHandle.WaitOne(2 * 1000, true);

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

                if ((cnt % size) == 0)
                {
                    string tmpFile = String.Format("{0}.log", keyword);
                    Utils.SendStatsToBigQuery(arr, tmpFile, table);
                    arr.Clear();
                }

                Thread.Sleep(1000 * 5);
            }
        }

        private static async void Thread3(object param) 
        {
            string hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            string workerIP = Environment.GetEnvironmentVariable("INSTANCE_IP");

            int size = Int32.Parse(Environment.GetEnvironmentVariable("HTTP_CHECK_BQ_SIZE"));
            if (size == 0)
            {
                size = 100;
            }

            string url = (param as ConnectParam).Url;
            string keyword = (param as ConnectParam).Keyword;
            string table = (param as ConnectParam).BigQueryTable;

            if (workerIP == null)
            {
                workerIP = "N/A";
            }

            string jsonTemplate = "'dtm':'{0}Z', 'host':'{1}', 'success':'{2}', 'url':'{3}', 'status':'{4}', 'worker':'{5}', 'output':'{6}'";
            var arr = new List<string>();

            var client = new HttpClient();

            int cnt = 0;
            while (true)
            {
                HttpResponseMessage result = await client.GetAsync(url);
                var status = result.StatusCode;

                int success = 0;
                if (status.ToString().Equals("OK"))
                {
                    success = 1;
                }
                
                
                string output = "";
                using (HttpContent content = result.Content)
                {
                    string txtContent = await content.ReadAsStringAsync();
                    output = txtContent;
                    if (txtContent != null && txtContent.Length >= 50)
                    {
                        output = txtContent.Substring(0, 50);
                    }
                }

                string dtm = DateTime.UtcNow.ToString("s");
                string json = String.Format(jsonTemplate,
                    dtm,
                    hostName,
                    success,
                    url,
                    status,
                    workerIP,
                    output);                
                json = "{" + json + "}";

                Console.WriteLine(json);
                arr.Add(json);
                cnt++;

                if ((cnt % size) == 0)
                {
                    string tmpFile = String.Format("{0}.log", keyword);
                    Utils.SendStatsToBigQuery(arr, tmpFile, table);
                    arr.Clear();
                }

                Thread.Sleep(1000 * 5);
            }
        }

        private static void StartThreads()
        {
            string cfgFile = Environment.GetEnvironmentVariable("CONFIG_FILE");
            if (cfgFile == null)
            {
                Console.WriteLine("Env CONFIG_FILE is null so no config file to pass");
                return;
            }

            StreamReader file = new StreamReader(cfgFile);
            string line = "";
            while ((line = file.ReadLine()) != null)  
            {
                string[] words = line.Split('|');
                string recType = words[0];

                if (recType.Equals("TCP"))
                {
                    StartTCPCheckThread(words);
                }
                else if (recType.Equals("HTTP"))
                {
                    StartHttpCheckThread(words);
                }                
            }  
            
            file.Close();              
        }

        private static void StartHttpCheckThread(string[] words)
        {
            string url = words[1];
            string keyword = words[2];
            string table = words[3];

            ConnectParam param1 = new ConnectParam(url, keyword, table);
            Thread t = new Thread(new ParameterizedThreadStart(Thread3));
            
            t.Start(param1);
        }

        private static void StartTCPCheckThread(string[] words)
        {
            string redisIP = words[1];
            string redisPort = words[2];
            string keyword = words[3];
            string table = words[4];

            ConnectParam param1 = new ConnectParam(redisIP, Int32.Parse(redisPort), keyword, table);
            Thread t2 = new Thread(new ParameterizedThreadStart(Thread2));
            
            t2.Start(param1);
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

            Utils.AuthenToGCP();
            Console.WriteLine("Started HTTP event loop", msecStr);

            t1.Start();
            StartThreads();

            return Host.Create()
                       .Console()
                       .Defaults()
                       .Handler(new CustomHandlerBuilder())
                       .Run();                    
        }
    }
}
