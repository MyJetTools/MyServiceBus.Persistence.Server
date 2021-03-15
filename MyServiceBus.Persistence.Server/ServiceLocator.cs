using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.DependencyInjection;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.ExecutionProgress;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Server.Services;

namespace MyServiceBus.Persistence.Server
{
    public static class ServiceLocator
    {

        public static Lazy<string> AppVersion => new (()=>typeof(ServiceLocator).Assembly.GetName().Version+" MyServiceBus-Persistence");
        public static QueueSnapshotCache QueueSnapshotCache { get; private set; }

        private static TaskTimer _taskTimerSyncQueues;
        private static TaskTimer _taskTimerSyncMessages;

        private static LoadedPagesGcBackgroundProcessor _loadedPagesGcBackgroundProcessor;

        public static CurrentRequests CurrentRequests { get; private set; }
        
        public static TopicsList TopicsList { get; private set; }
        
        public static ContentCompressorProcessor ContentCompressorProcessor { get; private set; }
        
        public static MessagesContentReader MessagesContentReader { get; private set; }

        private static QueueSnapshotWriter _queueSnapshotWriter;

        private static IServiceProvider _serviceProvider;
        public static IAppLogger AppLogger { get; private set; }
        public static IMessagesContentPersistentStorage MessagesContentPersistentStorage { get; private set; }
        public static AppGlobalFlags AppGlobalFlags { get; private set; }
        public static ILegacyCompressedMessagesStorage LegacyCompressedMessagesStorage { get; private set; }
        public static ICompressedMessagesStorage CompressedMessagesStorage { get; private set; }
        public static IndexByMinuteWriter IndexByMinuteWriter { get; private set; }
        public static ILastCompressedPageStorage LastCompressedPageStorage { get; private set; }
        private static async Task InitTopicsAsync(IServiceProvider sp)
        {
            await sp.GetRequiredService<TopicAndQueueInitializer>().InitAsync();
        }
        public static void Init(IServiceProvider sp, SettingsModel settingsModel)
        {
            
            TopicsList = sp.GetRequiredService<TopicsList>();
            CurrentRequests = sp.GetRequiredService<CurrentRequests>();

            var queuesTimeSpan = TimeSpan.Parse(settingsModel.FlushQueuesSnapshotFreq);
            _taskTimerSyncQueues = new TaskTimer(queuesTimeSpan);  
            
            var messagesTimeSpan = TimeSpan.Parse(settingsModel.FlushMessagesFreq);
            _taskTimerSyncMessages = new TaskTimer(messagesTimeSpan);  

            
            AppGlobalFlags = sp.GetRequiredService<AppGlobalFlags>();
            AppGlobalFlags.LoadBlobPagesSize = settingsModel.LoadBlobPagesSize;

            _serviceProvider = sp;
            QueueSnapshotCache = sp.GetRequiredService<QueueSnapshotCache>();
            MessagesContentReader = sp.GetRequiredService<MessagesContentReader>();

            _loadedPagesGcBackgroundProcessor = sp.GetRequiredService<LoadedPagesGcBackgroundProcessor>();

            _queueSnapshotWriter = sp.GetRequiredService<QueueSnapshotWriter>();

            CompressedMessagesStorage = sp.GetRequiredService<ICompressedMessagesStorage>(); 

            ContentCompressorProcessor = sp.GetRequiredService<ContentCompressorProcessor>();

            LegacyCompressedMessagesStorage = sp.GetRequiredService<ILegacyCompressedMessagesStorage>();

            MessagesContentPersistentStorage = sp.GetRequiredService<IMessagesContentPersistentStorage>();

            LastCompressedPageStorage = sp.GetRequiredService<ILastCompressedPageStorage>();

            IndexByMinuteWriter = sp.GetRequiredService<IndexByMinuteWriter>();

            AppLogger = sp.GetRequiredService<IAppLogger>();

            Task.Run(() => InitTopicsAsync(sp));

            _taskTimerSyncQueues.Register("SyncQueuesSnapshotToStorage", _queueSnapshotWriter.ExecuteAsync);
            _taskTimerSyncQueues.Register("GcPagesProcessor", _loadedPagesGcBackgroundProcessor.CheckAndGcIfNeededAsync);
            _taskTimerSyncQueues.Register("IndexByMinuteWriter", IndexByMinuteWriter.SaveMessagesToStorage);
            _taskTimerSyncQueues.Register("FlushLastCompressedPagesState", LastCompressedPageStorage.FlushAsync);
            _taskTimerSyncQueues.Register("Update prometheus", () =>
            {
                MetricsCollector.UpdatePrometheus();
                return new ValueTask();
            });
        }


        public static void Start()
        {
            _taskTimerSyncQueues.Start();
            _taskTimerSyncMessages.Start();
        }

        public static async Task StopAsync()
        {
            AppLogger.AddLog("SYSTEM", "Start shutting down application");
            AppGlobalFlags.IsShuttingDown = true;

            await _serviceProvider.GetRequiredService<ServicesDisposer>().Shutdown();

            AppLogger.AddLog("SYSTEM", "Everything stopped properly");

        }
    }
}