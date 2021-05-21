using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MyServiceBus.Persistence.Server.Middlewares
{
    public static class GlobalFlagsHandler
    {

        private static readonly Dictionary<string, string> IgnorePaths = new Dictionary<string, string>
        {
            ["/"] = "/",
            ["/api/status"] = "/api/status",
            ["/logs"] = "/logs",
        };

        public static void UseGlobalFlagsHandler(this IApplicationBuilder app)
        {

            app.Use((ctx, next) =>
            {
                
                if (IgnorePaths.ContainsKey(ctx.Request.Path.Value ?? ""))
                    return next.Invoke();

                if (!ServiceLocator.AppGlobalFlags.Initialized)
                {
                    return ctx.Response.WriteAsync("Application is not initialized yet");
                }

                if (ServiceLocator.AppGlobalFlags.IsShuttingDown)
                {
                    return ctx.Response.WriteAsync("Application is about to be shut down");
                }

                return next.Invoke();
            });

        }
        
    }
}