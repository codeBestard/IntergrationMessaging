
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MessageQueue.Messaging.Configuration
{   

    public sealed class MessageQueueMessagingRoot
    {
        [JsonProperty( "messageQueueMessaging" )]
        public MessageQueueMessaging MessageQueueMessaging { get; set; }
    }

    public sealed class MessageQueueMessaging
    {
        [JsonProperty( "defaultMessageQueueName" )]
        public string DefaultMessageQueueName { get; set; }
        [JsonProperty( "messages")]
        public IList<Message> Messages { get; set; }
        public IList<MessageQueue> MessageQueues { get; set; }
    }

    public sealed class Message
    {
        public string Name { get; set; }
        public string MessageQueueName { get; set; }
    }

    public sealed class MessageQueue
    {
        public string Name                            { get; set; }
        public string Type                            { get; set; }                
        public IList<Queue> Queues                    { get; set; }
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public sealed class Queue
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public sealed class MessagingConfig
    {
        private static Lazy<MessageQueueMessaging> messaging = new Lazy<MessageQueueMessaging>( () =>
         {
             var path = AppDomain.CurrentDomain.BaseDirectory + @"\Messaging.config.json";
             var root = JsonConvert.DeserializeObject<MessageQueueMessagingRoot>( File.ReadAllText( path ) );
             return root.MessageQueueMessaging;
         } );

        public static MessageQueueMessaging Current => messaging.Value;


        public static string DefaultMessageQueueName => Current.DefaultMessageQueueName;


        public static MessageQueue CurrentMessageQueues => Current.MessageQueues
                                                        .First( q => q.Name.Equals( DefaultMessageQueueName, StringComparison.OrdinalIgnoreCase ) );
                                                        


        public static IEnumerable<Message> CurrentMessages => Current.Messages ?? Enumerable.Empty<Message>();

    }
}
