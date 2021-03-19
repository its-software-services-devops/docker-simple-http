using System;
using System.IO;
using System.Net.Sockets;
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
            string redisIP = (param as ConnectParam).DestinationIP;
            string redisPort = (param as ConnectParam).Port.ToString();
            string hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            string workerIP = Environment.GetEnvironmentVariable("INSTANCE_IP");

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
                    string tmpFile = String.Format("{0}.log", redisIP);
                    Utils.SendStatsToBigQuery(arr, tmpFile);
                    arr.Clear();
                }

                Thread.Sleep(1000 * 5);
            }
        }

        private static void StartThreads()
        {
            string cfgFile = Environment.GetEnvironmentVariable("CONFIG_FILE");

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
            }  
            
            file.Close();              
        }

        private static void StartTCPCheckThread(string[] words)
        {
            string redisIP = words[1];
            string redisPort = words[2];

            ConnectParam param1 = new ConnectParam(redisIP, Int32.Parse(redisPort));
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
