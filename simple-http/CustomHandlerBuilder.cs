using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using GenHTTP.Modules.IO;
using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

namespace simple_http
{
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
}