using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyDependencies;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Server.Grpc;
using MyServiceBus.Persistence.Server.Middlewares;
using ProtoBuf.Grpc.Server;

namespace MyServiceBus.Persistence.Server
{
    public class Startup
    {

        public static SettingsModel Settings { get; private set; }

        public static MyIoc MyIoc = new MyIoc();
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            Settings = MySettingsReader.SettingsReader.GetSettings<SettingsModel>(".myservicebus-persistence");

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            services.AddCodeFirstGrpc();
            services.AddApplicationInsightsTelemetry();

            services.AddMvc(o => { o.EnableEndpointRouting = false; });

            MyIoc.BindAzureServices(Settings);
            MyIoc.BindMyServiceBusPersistenceServices();

            services.AddSwaggerDocument(o => o.Title = "MyServiceBus-Persistence");

            ServiceLocator.Init(MyIoc, Settings);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {

            applicationLifetime.ApplicationStopping.Register(() =>
            {
                ServiceLocator.StopAsync().Wait();
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
            });

            ServiceLocator.Start();
        }
    }
}