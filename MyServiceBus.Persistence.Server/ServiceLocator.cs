using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyDependencies;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
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

        public static PersistentOperationsScheduler PersistentOperationsScheduler { get; private set; }


        public static MessagesContentReader MessagesContentReader { get; private set; }

        private static QueueSnapshotWriter _queueSnapshotWriter;


        private static IServiceResolver _serviceResolver;

        public static IAppLogger AppLogger { get; private set; }

        public static CompressedMessagesUtils CompressedMessagesUtils { get; private set; }
        
        public static IMessagesContentPersistentStorage MessagesContentPersistentStorage { get; private set; }


        public static AppGlobalFlags AppGlobalFlags { get; private set; }
        
        public static ILegacyCompressedMessagesStorage LegacyCompressedMessagesStorage { get; private set; }
        
        public static ICompressedMessagesStorage CompressedMessagesStorage { get; private set; }

        public static IndexByMinuteWriter IndexByMinuteWriter { get; private set; }
        
        public static PagesToCompressDetector PagesToCompressDetector { get; private set; }
        
        public static ILastCompressedPageStorage LastCompressedPageStorage { get; private set; }
        

        private static async Task InitTopicsAsync(IServiceResolver sr)
        {
            await sr.GetService<TopicAndQueueInitializer>().InitAsync();

        }

        public static void Init(IServiceResolver sr, SettingsModel settingsModel)
        {

            var queuesTimeSpan = TimeSpan.Parse(settingsModel.FlushQueuesSnapshotFreq);
            _taskTimerSyncQueues = new TaskTimer(queuesTimeSpan);  
            
            var messagesTimeSpan = TimeSpan.Parse(settingsModel.FlushMessagesFreq);
            _taskTimerSyncMessages = new TaskTimer(messagesTimeSpan);  

            
            AppGlobalFlags = sr.GetService<AppGlobalFlags>();
            AppGlobalFlags.LoadBlobPagesSize = settingsModel.LoadBlobPagesSize;

            _serviceResolver = sr;
            QueueSnapshotCache = sr.GetService<QueueSnapshotCache>();
            MessagesContentCache = sr.GetService<MessagesContentCache>();
            MessagesContentReader = sr.GetService<MessagesContentReader>();

            _activePagesWarmerAndGc = sr.GetService<ActivePagesWarmerAndGc>();

            PersistentOperationsScheduler = sr.GetService<PersistentOperationsScheduler>();
            PersistentOperationsScheduler.RegisterServiceResolver(_serviceResolver);

            _queueSnapshotWriter = sr.GetService<QueueSnapshotWriter>();

            CompressedMessagesStorage = sr.GetService<ICompressedMessagesStorage>(); 

            CompressedMessagesUtils = sr.GetService<CompressedMessagesUtils>();

            LegacyCompressedMessagesStorage = sr.GetService<ILegacyCompressedMessagesStorage>();

            MessagesContentPersistentStorage = sr.GetService<IMessagesContentPersistentStorage>();

            PagesToCompressDetector = sr.GetService<PagesToCompressDetector>();

            LastCompressedPageStorage = sr.GetService<ILastCompressedPageStorage>();
            
            

            IndexByMinuteWriter = sr.GetService<IndexByMinuteWriter>();

            AppLogger = sr.GetService<IAppLogger>();

            Task.Run(() => InitTopicsAsync(sr));

            _taskTimerSyncQueues.Register("SyncQueuesSnapshotToStorage", _queueSnapshotWriter.ExecuteAsync);
            _taskTimerSyncQueues.Register("ActiveMessagesWarmerAndGc", _activePagesWarmerAndGc.CheckAndWarmItUpAsync);
            _taskTimerSyncQueues.Register("IndexByMinuteWriter", IndexByMinuteWriter.SaveMessagesToStorage);
            _taskTimerSyncQueues.Register("PagesToCompressDetector", PagesToCompressDetector.TimerAsync);
            _taskTimerSyncQueues.Register("FlushLastCompressedPagesState", LastCompressedPageStorage.FlushAsync);
            _taskTimerSyncQueues.Register("Update prometheus", () =>
            {
                MetricsCollector.UpdatePrometheus();
                return new ValueTask();
            });

            _taskTimerSyncMessages.Register("PersistentOperationsScheduler", PersistentOperationsScheduler.ExecuteOperationAsync);
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

            await _serviceResolver.GetService<ServicesDisposer>().Shutdown();

            AppLogger.AddLog("SYSTEM", "Everything stopped properly");

        }
    }
}