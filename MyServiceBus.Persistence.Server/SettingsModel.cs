using MyYamlSettingsParser;

namespace MyServiceBus.Persistence.Server
{
    public class SettingsModel
    {
        [YamlProperty]
        public string QueuesConnectionString { get; set; }
        
        [YamlProperty]
        public string MessagesConnectionString { get; set; }
        
        [YamlProperty]
        public int LoadBlobPagesSize { get; set; }
        
        
        [YamlProperty]
        public string FlushQueuesSnapshotFreq { get; set; }
        
        [YamlProperty]
        public string FlushMessagesFreq { get; set; }
        
        [YamlProperty]
        public int MaxResponseRecordsAmount { get; set; }
    }
}