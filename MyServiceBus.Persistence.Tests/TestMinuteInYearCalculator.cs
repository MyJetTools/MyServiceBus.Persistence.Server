using MyServiceBus.Persistence.Domains.IndexByMinute;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class TestMinuteInYearCalculator
    {
        
        [Test]
        public void TestMonths()
        {
            var minuteNo = 0;

            foreach (var dt in 2020.GoThroughEveryDay())       
            {
                var calculatedMinute = dt.GetMinuteWithinTHeYear();
                Assert.AreEqual(minuteNo, calculatedMinute);

                minuteNo++;
            }

        }
        
    }
}