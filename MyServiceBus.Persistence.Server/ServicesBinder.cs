using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using MyServiceBus.Persistence.AzureStorage;
using MyServiceBus.Persistence.Domains;

namespace MyServiceBus.Persistence.Server
{
    public static class ServicesBinder
    {

        public static void BindAzureServices(this IServiceCollection sc, SettingsModel settingsModel)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(settingsModel.QueuesConnectionString);
            sc.BindTopicsPersistentStorage(cloudStorageAccount);
            
            cloudStorageAccount = CloudStorageAccount.Parse(settingsModel.MessagesConnectionString);
            sc.BindMessagesPersistentStorage(cloudStorageAccount);
        }
        
    }
}