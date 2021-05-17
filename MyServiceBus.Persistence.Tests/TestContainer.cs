using System;
using Microsoft.Extensions.DependencyInjection;
using MyServiceBus.Persistence.Domains;

namespace MyServiceBus.Persistence.Tests
{
    public static class TestContainer
    {

        public static IServiceProvider GetServiceProvider()
        {
            var sc = new ServiceCollection();

            sc.BindMyServiceBusPersistenceServices();


            return sc.BuildServiceProvider();
        }
        
    }
}