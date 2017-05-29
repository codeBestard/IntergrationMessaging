namespace MessageQueue.Messaging.Specification
{
    public sealed class MessagePattern
    {
        public static MessagePattern FireAndForget    = new MessagePattern("FireAndForget");
        public static MessagePattern RequestResponse  = new MessagePattern("RequestResponse");
        public static MessagePattern PublishSubscribe = new MessagePattern("PublishSubscribe");
        
        private string Name { get; }

        private MessagePattern() {  }
        private MessagePattern(string name) 
        {            
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}