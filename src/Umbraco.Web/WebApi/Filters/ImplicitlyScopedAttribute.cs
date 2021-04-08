using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Web.Composing;

namespace Umbraco.Web.WebApi.Filters
{
    /// <summary>
    /// Ensures that an action is wrapped in a scope
    /// </summary>
    internal sealed class ImplicitlyScopedAttribute : ActionFilterAttribute
    {
        private readonly IScopeProvider _scopeProvider;

        public ImplicitlyScopedAttribute() : this(Current.ScopeProvider)
        { }

        public ImplicitlyScopedAttribute(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        /// <summary>
        /// Creates a scope and saves it
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            // Create a scope that will be stored in the HttpContext as the AmbientScope,
            var scope = _scopeProvider.CreateScope();
            Current.Logger.Debug(typeof(ImplicitlyScopedAttribute), $"Created scope {scope.InstanceId} for {filterContext.Request.RequestUri}");
        }

        /// <summary>
        /// Completes and disposes of the scope created in OnActionExecuting
        /// </summary>
        /// <param name="actionExecutedContext"></param>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            // Get the scope we created earlier through IScopeAccessor,
            // this is viable because the AmbientScope is stored in the HttpContext object,
            // essentially meaning that it's scoped for each request,
            // so we don't have to worry about accidentally completing/disposing of another requests scope.
            var scopeAccessor = _scopeProvider as IScopeAccessor;
            var scope = scopeAccessor.AmbientScope;

            // Since we're using AmbientScope to get our created scope, it will be null if it has been disposed elsewhere.
            // There is no limit to who can dispose a scope where, so in theory you could use the AmbientScope in a using statement,
            // which would dispose the scope, and then we can't dispose of the scope again, this shouldn't really happen though,
            // because you really shouldn't be disposing the AmbientScope you get through the IScopeAccessor normally.
            if (scope is null)
            {
                Current.Logger.Error<ImplicitlyScopedAttribute>("The implicit scope has already been disposed, indicating that AmbientScope is used in a using statement.");
                return;
            }

            // Complete and dispose the scope.
            Current.Logger.Debug(typeof(ImplicitlyScopedAttribute), $"Completing and disposing scope {scope.InstanceId} for {actionExecutedContext.Request.RequestUri}");
            // Only complete the scope if no exception occured and the response is ok
            if (actionExecutedContext.Exception is null &&
                actionExecutedContext.Response.StatusCode == HttpStatusCode.OK)
            {
                scope.Complete();
            }
            scope.Dispose();

            // This scope should always be the outermost scope,
            // if AmbientScope is not null it either means that this is not the outermost scope, or that a child scope has been left without being disposed.
            if (scopeAccessor.AmbientScope is not null)
            {
                var exception = new InvalidOperationException($"Every scope has not been disposed for request {actionExecutedContext.Request.RequestUri}, see log for more detail.");
                Current.Logger.Error<ImplicitlyScopedAttribute>(exception, "Every scope has not been disposed for request {RequestURI}, " +
                                                                           "this indicates that a child scope has not been disposed, or that the implicit scope is not the outermost scope." +
                                                                           "The ID of the remaining scope is {ScopeID}",
                    actionExecutedContext.Request.RequestUri, scopeAccessor.AmbientScope.InstanceId);
                // Only throw the exception if there isn't one already, we don't want to overwrite the exception, potentially obfuscating a root cause.
                if (actionExecutedContext.Exception is null)
                {
                    throw exception;
                }
            }
        }

    }
}
