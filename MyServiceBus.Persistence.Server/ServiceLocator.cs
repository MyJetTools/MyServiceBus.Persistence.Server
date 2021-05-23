using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.DependencyInjection;
using MyServiceBus.Persistence.AzureStorage;
using MyServiceBus.Persistence.AzureStorage.TopicMessages;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Domains.Metrics;
using MyServiceBus.Persistence.Domains.PersistenceOperations;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Server.Services;

namespace MyServiceBus.Persistence.Server
{
    public static class ServiceLocator
    {
        public static QueueSnapshotCache QueueSnapshotCache { get; private set; }

        private static TaskTimer _taskTimerSyncQueues;
        private static TaskTimer _taskTimerSyncMessages;

        public static MessagesContentCache MessagesContentCache { get; private set; }

        private static ActivePagesWarmerAndGc _activePagesWarmerAndGc;

        public static TaskSchedulerByTopic TaskSchedulerByTopic { get; private set; }
        
        
        public static MetricsByTopic MetricsByTopic { get; private set; }
        
        
        public static MessagesContentReader MessagesContentReader { get; private set; }

        private static QueueSnapshotWriter _queueSnapshotWriter;


        private static IServiceProvider _serviceProvider;

        public static IAppLogger AppLogger { get; private set; }
        
        public static LogsSnapshotRepository LogsSnapshotRepository { get; private set; }
        
        
        public static SyncAndGcBlobOperations SyncAndGcBlobOperations { get; private set; }

        public static CompressedMessagesUtils CompressedMessagesUtils { get; private set; }
        
        public static IMessagesContentPersistentStorage MessagesContentPersistentStorage { get; private set; }


        public static AppGlobalFlags AppGlobalFlags { get; private set; }
        
        public static ILegacyCompressedMessagesStorage LegacyCompressedMessagesStorage { get; private set; }
        
        public static ICompressedMessagesStorage CompressedMessagesStorage { get; private set; }

        public static IndexByMinuteWriter IndexByMinuteWriter { get; private set; }
        
        
        public static ILastCompressedPageStorage LastCompressedPageStorage { get; private set; }
        
        public static CompressPageBlobOperation CompressPageBlobOperation { get; private set; }
        

        private static async Task InitTopicsAsync(IServiceProvider sp)
        {
            await sp.GetRequiredService<TopicAndQueueInitializer>().InitAsync();

        }

        public static void Init(IServiceProvider sp, SettingsModel settingsModel)
        {
            AppLogger = sp.GetRequiredService<IAppLogger>();
            LogsSnapshotRepository = sp.GetRequiredService<LogsSnapshotRepository>();

            var items = LogsSnapshotRepository.LoadAsync().AsTask().Result;
            
            ((AppLogger)AppLogger).Init(items);

            var queuesTimeSpan = TimeSpan.Parse(settingsModel.FlushQueuesSnapshotFreq);
            _taskTimerSyncQueues = new TaskTimer(queuesTimeSpan);  
            
            var messagesTimeSpan = TimeSpan.Parse(settingsModel.FlushMessagesFreq);
            _taskTimerSyncMessages = new TaskTimer(messagesTimeSpan);  
            
            AppGlobalFlags = sp.GetRequiredService<AppGlobalFlags>();
            AppGlobalFlags.LoadBlobPagesSize = settingsModel.LoadBlobPagesSize;

            _serviceProvider = sp;
            QueueSnapshotCache = sp.GetRequiredService<QueueSnapshotCache>();
            MessagesContentCache = sp.GetRequiredService<MessagesContentCache>();
            MessagesContentReader = sp.GetRequiredService<MessagesContentReader>();
            MetricsByTopic = sp.GetRequiredService<MetricsByTopic>();

            _activePagesWarmerAndGc = sp.GetRequiredService<ActivePagesWarmerAndGc>();

            TaskSchedulerByTopic = sp.GetRequiredService<TaskSchedulerByTopic>();

            _queueSnapshotWriter = sp.GetRequiredService<QueueSnapshotWriter>();

            CompressPageBlobOperation = sp.GetRequiredService<CompressPageBlobOperation>();

            CompressedMessagesStorage = sp.GetRequiredService<ICompressedMessagesStorage>(); 

            CompressedMessagesUtils = sp.GetRequiredService<CompressedMessagesUtils>();

            LegacyCompressedMessagesStorage = sp.GetRequiredService<ILegacyCompressedMessagesStorage>();

            MessagesContentPersistentStorage = sp.GetRequiredService<IMessagesContentPersistentStorage>();
            ((MessagesPersistentStorage)MessagesContentPersistentStorage).Inject(sp);

            LastCompressedPageStorage = sp.GetRequiredService<ILastCompressedPageStorage>();

            IndexByMinuteWriter = sp.GetRequiredService<IndexByMinuteWriter>();

            SyncAndGcBlobOperations = sp.GetRequiredService<SyncAndGcBlobOperations>();


            Task.Run(() => InitTopicsAsync(sp));

            _taskTimerSyncQueues.Register("SyncQueuesSnapshotToStorage", _queueSnapshotWriter.ExecuteAsync);
            _taskTimerSyncQueues.Register("ActiveMessagesWarmerAndGc", _activePagesWarmerAndGc.CheckAndWarmItUpOrGcAsync);
            _taskTimerSyncQueues.Register("IndexByMinuteWriter", IndexByMinuteWriter.SaveMessagesToStorage);
            _taskTimerSyncQueues.Register("FlushLastCompressedPagesState", LastCompressedPageStorage.FlushAsync);
            _taskTimerSyncQueues.Register("Update prometheus", () =>
            {
                MetricsCollector.UpdatePrometheus();
                return new ValueTask();
            });

            _taskTimerSyncQueues.RegisterExceptionHandler((timer, e) =>
            {
                AppLogger.AddLog(LogProcess.System, timer, e.Message, e.StackTrace);
                return new ValueTask();
            });
                        
            _taskTimerSyncMessages.Register("PersistentOperationsScheduler", SyncAndGcBlobOperations.Sync);
            
            _taskTimerSyncMessages.RegisterExceptionHandler((timer, e) =>
            {
                AppLogger.AddLog(LogProcess.System, timer, e.Message, e.StackTrace);
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
            AppLogger.AddLog(LogProcess.System, null, "SYSTEM", "Start shutting down application");
            AppGlobalFlags.IsShuttingDown = true;

            await _serviceProvider.GetRequiredService<ServicesDisposer>().Shutdown();

            AppLogger.AddLog(LogProcess.System, null,"SYSTEM", "Everything stopped properly");

        }
    }
}