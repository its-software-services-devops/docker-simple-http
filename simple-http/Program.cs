using System;
using System.Linq;
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

            return Host.Create()
                       .Console()
                       .Defaults()
                       .Handler(new CustomHandlerBuilder())
                       .Run();                    
        }
    }
}
