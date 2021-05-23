namespace MyServiceBus.Persistence.Domains
{
    public class AppGlobalFlags
    {
        public bool Initialized { get; set; }
        public bool IsShuttingDown { get; set; }

        public int LoadBlobPagesSize { get; set; } = 4096 * 2;
        
        public string DebugTopic { get; set; }

    }
}