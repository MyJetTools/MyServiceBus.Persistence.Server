using Microsoft.WindowsAzure.Storage;
using MyDependencies;
using MyServiceBus.Persistence.AzureStorage;

namespace MyServiceBus.Persistence.Server
{
    public static class ServicesBinder
    {

        public static void BindAzureServices(this IServiceRegistrator sr, SettingsModel settingsModel)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(settingsModel.QueuesConnectionString);
            sr.BindTopicsPersistentStorage(cloudStorageAccount);
            
            cloudStorageAccount = CloudStorageAccount.Parse(settingsModel.MessagesConnectionString);
            sr.BindMessagesPersistentStorage(cloudStorageAccount);
        }
        
    }
}