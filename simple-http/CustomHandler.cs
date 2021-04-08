using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

using GenHTTP.Modules.IO;
using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

namespace simple_http
{
    public class CustomHandler : IHandler
    {
        private string message = "Hello World #2.1 !!!";

        public IHandler Parent { get; }
        public CustomHandler(IHandler parent)
        {
            Parent = parent;
        }

        public ValueTask<IResponse> HandleAsync(IRequest request)
        {
            var req = request.Respond().Type(new FlexibleContentType(ContentType.TextPlain));

            string referer = request.Referer;
            if ((referer != null) && (referer.Contains("/restart")))
            {
                req = req.Status(GenHTTP.Api.Protocol.ResponseStatus.ExpectationFailed);
                
                Console.WriteLine("Program ended by /restart path");
                Environment.Exit(-1);
            }
            else
            {
                req = req.Content(message);
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
}