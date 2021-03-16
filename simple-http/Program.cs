using System;
using System.Threading;

using GenHTTP.Engine;           
using GenHTTP.Modules.IO;    
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

        private static int Main(string[] args)
        {
            var content = Content.From(Resource.FromString("Hello World!"));

            t1.Start();

            return Host.Create()
                       .Console()
                       .Defaults()
                       .Handler(content)
                       .Run();                    
        }
    }
}
