using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.DependencyInjection;
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


        private static IServiceProvider _serviceProvider;

        public static IAppLogger AppLogger { get; private set; }

        public static CompressedMessagesUtils CompressedMessagesUtils { get; private set; }
        
        public static IMessagesContentPersistentStorage MessagesContentPersistentStorage { get; private set; }


        public static AppGlobalFlags AppGlobalFlags { get; private set; }
        
        public static ILegacyCompressedMessagesStorage LegacyCompressedMessagesStorage { get; private set; }
        
        public static ICompressedMessagesStorage CompressedMessagesStorage { get; private set; }

        public static IndexByMinuteWriter IndexByMinuteWriter { get; private set; }
        
        public static PagesToCompressDetector PagesToCompressDetector { get; private set; }
        
        public static ILastCompressedPageStorage LastCompressedPageStorage { get; private set; }
        

        private static async Task InitTopicsAsync(IServiceProvider sp)
        {
            await sp.GetRequiredService<TopicAndQueueInitializer>().InitAsync();

        }

        public static void Init(IServiceProvider sp, SettingsModel settingsModel)
        {

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

            _activePagesWarmerAndGc = sp.GetRequiredService<ActivePagesWarmerAndGc>();

            PersistentOperationsScheduler = sp.GetRequiredService<PersistentOperationsScheduler>();
            PersistentOperationsScheduler.RegisterServiceResolver(sp);

            _queueSnapshotWriter = sp.GetRequiredService<QueueSnapshotWriter>();

            CompressedMessagesStorage = sp.GetRequiredService<ICompressedMessagesStorage>(); 

            CompressedMessagesUtils = sp.GetRequiredService<CompressedMessagesUtils>();

            LegacyCompressedMessagesStorage = sp.GetRequiredService<ILegacyCompressedMessagesStorage>();

            MessagesContentPersistentStorage = sp.GetRequiredService<IMessagesContentPersistentStorage>();

            PagesToCompressDetector = sp.GetRequiredService<PagesToCompressDetector>();

            LastCompressedPageStorage = sp.GetRequiredService<ILastCompressedPageStorage>();

            IndexByMinuteWriter = sp.GetRequiredService<IndexByMinuteWriter>();

            AppLogger = sp.GetRequiredService<IAppLogger>();

            Task.Run(() => InitTopicsAsync(sp));

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

            _taskTimerSyncQueues.RegisterExceptionHandler((timer, e) =>
            {
                AppLogger.AddLog(LogProcess.System, timer, e.Message, e.StackTrace);
                return new ValueTask();
            });
            
            _taskTimerSyncMessages.Register("PersistentOperationsScheduler", PersistentOperationsScheduler.ExecuteOperationAsync);
            
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