using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Umbraco.Core.Scoping;
using Umbraco.Web.Composing;

namespace Umbraco.Web.WebApi.Filters
{
    /// <summary>
    /// Ensures that an action is wrapped in a scope
    /// </summary>
    internal sealed class ImplicitlyScopedAttribute : FilterAttribute, IActionFilter
    {
        private readonly IScopeProvider _scopeProvider;

        public ImplicitlyScopedAttribute() : this(Current.ScopeProvider)
        { }

        public ImplicitlyScopedAttribute(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                var res = await continuation();
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    scope.Complete();
                }
            }

            return actionContext.Response;
        }
    }
}
