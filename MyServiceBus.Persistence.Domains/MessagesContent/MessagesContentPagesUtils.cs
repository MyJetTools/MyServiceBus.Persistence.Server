namespace MyServiceBus.Persistence.Domains.MessagesContent
{
    
    public struct MessagePageId
    {
        public MessagePageId(long value)
        {
            Value = value;
        }
        
        public long Value { get; }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj != null && (long)obj == Value;
        }

        public  bool EqualsWith(MessagePageId messagePageId)
        {
            return Value == messagePageId.Value;
        }
        
        public  bool EqualsWith(long value)
        {
            return Value == value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }


        
    }
    
    public static class MessagesContentPagesUtils
    {

        public const int MessagesPerPage = 100_000;
        public static MessagePageId GetPageId(long messageId)
        {
            return new MessagePageId(messageId  / MessagesPerPage);
        }
        
        public static MessagePageId NextPage(this MessagePageId pageId)
        {
            return new MessagePageId(pageId.Value + 1);
        }
        
        public static  MessagePageId PrevPage(this MessagePageId pageId)
        {
            return new MessagePageId(pageId.Value + 1);
        }
        
    }
}