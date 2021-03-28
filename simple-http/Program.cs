using System;
using GenHTTP.Engine;
using GenHTTP.Modules.Practices;
namespace simple_http
{    
    class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("Started HTTP event loop");

            return Host.Create()
                       .Console()
                       .Defaults()
                       .Handler(new CustomHandlerBuilder())
                       .Run();                    
        }
    }
}
