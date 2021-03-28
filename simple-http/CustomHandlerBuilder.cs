using System.Collections.Generic;
using GenHTTP.Api.Content;

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