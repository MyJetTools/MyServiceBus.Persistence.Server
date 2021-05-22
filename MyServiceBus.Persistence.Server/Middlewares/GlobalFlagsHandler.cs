using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MyServiceBus.Persistence.Domains;

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

            app.Use(async (ctx, next) =>
            {
                try
                {
                    if (IgnorePaths.ContainsKey(ctx.Request.Path.Value ?? ""))
                    {
                        await next.Invoke();
                        return;
                    }
                        

                    if (!ServiceLocator.AppGlobalFlags.Initialized)
                    {
                        await ctx.Response.WriteAsync("Application is not initialized yet");
                        return;
                    }

                    if (ServiceLocator.AppGlobalFlags.IsShuttingDown)
                    {
                        await ctx.Response.WriteAsync("Application is about to be shut down");
                        return;
                    }

                    await next.Invoke();
                }
                catch (Exception e)
                {
                    ServiceLocator.AppLogger.AddLog(LogProcess.System, null, ctx.Request.Path, e.Message, e.StackTrace);
                    throw;
                }

            });

        }
        
    }
}