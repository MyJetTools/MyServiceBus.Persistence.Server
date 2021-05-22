using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Server.Grpc;
using MyServiceBus.Persistence.Server.Middlewares;
using Prometheus;
using ProtoBuf.Grpc.Server;

namespace MyServiceBus.Persistence.Server
{
    public class Startup
    {
        public static SettingsModel Settings { get; private set; }

        private static IServiceCollection _services;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            _services = services;
            Settings = MySettingsReader.SettingsReader.GetSettings<SettingsModel>(".myservicebus-persistence");

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            services.AddCodeFirstGrpc();
            services.AddApplicationInsightsTelemetry();

            services.AddMvc(o => { o.EnableEndpointRouting = false; });

            _services.BindAzureServices(Settings);
            _services.BindMyServiceBusPersistenceServices();

            services.AddSwaggerDocument(o => o.Title = "MyServiceBus-Persistence");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {

            applicationLifetime.ApplicationStopping.Register(() =>
            {
                ServiceLocator.StopAsync().Wait();


                foreach (var (topic, metrics) in ServiceLocator.MetricsByTopic.Get())
                {
                   ServiceLocator.AppLogger.AddLog(LogProcess.System, topic, "Last Save message before shutdown", metrics.MaxSavedMessageId.ToString());


                   var pageId = MessagesContentPagesUtils.GetPageId(metrics.MaxSavedMessageId);

                   var page = ServiceLocator.MessagesContentCache.TryGetPage(topic, pageId);

                   if (page != null)
                   {

                       var messages = page.GetMessages();
                       if (messages.Count > 0)
                       {
                           var minId = messages.Min(itm => itm.MessageId);
                           var maxId = messages.Max(itm => itm.MessageId);
                           ServiceLocator.AppLogger.AddLog(LogProcess.System, topic, "Last messages in Cache before shutdown", $"Min={minId};Max={maxId}");
                       }
                       
                   }

                }
                
                var snapshot = ((AppLogger)ServiceLocator.AppLogger).GetSnapshot();
                ServiceLocator.LogsSnapshotRepository.SaveAsync(snapshot).AsTask().Wait();
                
            });


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseGlobalFlagsHandler();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<MyServiceBusMessagesPersistenceGrpcService>();
                endpoints.MapGrpcService<MyServiceBusQueuePersistenceGrpcService>();
                endpoints.MapGrpcService<MyServiceBusHistoryReaderGrpcService>();
                endpoints.MapMetrics();
            });

            var sp = _services.BuildServiceProvider();
            ServiceLocator.Init(sp, Settings);
            ServiceLocator.Start();
        }
    }
}