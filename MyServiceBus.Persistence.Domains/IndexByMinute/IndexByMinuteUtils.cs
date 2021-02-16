using System;
using System.Collections.Generic;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.IndexByMinute
{

    
    
    public static class IndexByMinuteUtils
    {
        private static readonly List<int> DayNoInYear = new List<int>();

        private const int MinutesPerDay = 60 * 24;
        
        public static int LastDayOfTheYear { get; }

        private static void InitDaysInYearIndex()
        {
            
            DayNoInYear.Add(0);
            var minute = 1;

            //January
            DayNoInYear.Add(minute);
            minute += 31*MinutesPerDay; 

            //February
            DayNoInYear.Add(minute);
            minute += 29*MinutesPerDay; 
            
            //March
            DayNoInYear.Add(minute);
            minute += 31*MinutesPerDay;
            
            //April
            DayNoInYear.Add(minute);
            minute += 30*MinutesPerDay; 
            
            //May
            DayNoInYear.Add(minute);
            minute += 31*MinutesPerDay;
            
            //June
            DayNoInYear.Add(minute);
            minute += 30*MinutesPerDay; 

            //July
            DayNoInYear.Add(minute);
            minute += 31*MinutesPerDay;
            
            //August
            DayNoInYear.Add(minute);
            minute += 31*MinutesPerDay; 
            
            //September
            DayNoInYear.Add(minute);
            minute += 30*MinutesPerDay;             
            
            //October
            DayNoInYear.Add(minute);
            minute += 31*MinutesPerDay;             
            
            //November
            DayNoInYear.Add(minute);
            minute += 30*MinutesPerDay;  

            //December
            DayNoInYear.Add(minute);
        }
        
        
        static IndexByMinuteUtils()
        {
            InitDaysInYearIndex();
            LastDayOfTheYear = GetMinuteWithinTHeYear(new DateTime(2020,12,31, 23,59,59));
        }

        public static int GetMinuteWithinTHeYear(this DateTime dateTime)
        {
            return DayNoInYear[dateTime.Month] + (dateTime.Day-1)*MinutesPerDay + dateTime.Hour * 60 + dateTime.Minute-1;
        }

        
        public static IReadOnlyDictionary<int, long> GroupByMinutes(this IEnumerable<MessageContentGrpcModel> grpcModels)
        {
            var result = new Dictionary<int, long>();

            foreach (var messageContent in grpcModels)
            {
                var minuteNp = messageContent.Created.GetMinuteWithinTHeYear();
                var messageId = messageContent.MessageId;
                
                if (result.ContainsKey(minuteNp))
                {
                    if (result[minuteNp] > messageId)
                        result[minuteNp] = messageId;
                }
                else
                {
                    result.Add(minuteNp, messageId);
                }
            }

            return result;
        }
    }
}