using Prometheus;

namespace MyServiceBus.Persistence.Server.Services
{
    public static class MetricsCollector
    {
        
        private static readonly Gauge LoadedPagesAmount = Metrics.CreateGauge("service_persistence_loaded_pages",
            "Cached Pages amount per Topic", new GaugeConfiguration
            {
                LabelNames = new[] {"topicId"}
            });
        
        private static readonly Gauge LoadedPagesContentSize = Metrics.CreateGauge("service_persistence_loaded_content_size",
            "Cached Pages content size per Topic", new GaugeConfiguration
            {
                LabelNames = new[] {"topicId"}
            });

        private static void UpdateLoadedPages()
        {
            foreach (var (topicId, metrics) in ServiceLocator.MessagesContentCache.GetMetrics())
            {
                LoadedPagesAmount.WithLabels(topicId).Set(metrics.loadedPages);
                LoadedPagesContentSize.WithLabels(topicId).Set(metrics.contentSize);
            }
        }


        public static void UpdatePrometheus()
        {
            UpdateLoadedPages();
        }
        
    }
}