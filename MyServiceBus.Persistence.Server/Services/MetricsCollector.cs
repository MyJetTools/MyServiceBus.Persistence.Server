using System.Linq;
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

            foreach (var topicDataLocator in ServiceLocator.TopicsList.AllDataLocators)
            {
                var loadedPages = topicDataLocator.GetLoadedPages();
                LoadedPagesAmount.WithLabels(topicDataLocator.TopicId).Set(loadedPages.Count);
                LoadedPagesContentSize.WithLabels(topicDataLocator.TopicId)
                    .Set(loadedPages.Sum(itm => itm.TotalContentSize));
            }
        }

        public static void UpdatePrometheus()
        {
            UpdateLoadedPages();
        }
        
    }
}