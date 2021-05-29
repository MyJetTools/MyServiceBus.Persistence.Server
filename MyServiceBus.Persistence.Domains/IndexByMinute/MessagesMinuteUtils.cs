using System;

namespace MyServiceBus.Persistence.Domains.IndexByMinute
{
    public static class MessagesMinuteUtils
    {
        public const int IndexStep = sizeof(long);


        public static int GetIndexOffset(int minute)
        {
            return minute * IndexStep;
        }

        public static long GetMessageIdFromMinuteIndexRawData(this byte[] bytes, int minute)
        {
            var startIndex = GetIndexOffset(minute);
            return BitConverter.ToInt64(bytes, startIndex);
        }



        private static void SetMessageIdToMinuteIndexRawData(this byte[] bytes, int minute, long messageId)
        {
            var startIndex = GetIndexOffset(minute);
            BitConverter.TryWriteBytes(bytes.AsSpan(startIndex), messageId);
        }

        public static bool Update(this byte[] bytes, int minute, long messageId)
        {

            var currentMessageId = bytes.GetMessageIdFromMinuteIndexRawData(minute);
            if (currentMessageId <= messageId && currentMessageId != 0)
                return false;

            bytes.SetMessageIdToMinuteIndexRawData(minute, messageId);
            return true;
        }
    }
}