using MessageQueue.Messaging.Configuration;
using MessageQueue.Messaging.Specification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MessageQueue.Messaging
{
    public static class MessageQueueFactory
    {
        private static Dictionary<string, IMessageQueue> _Queues = new Dictionary<string, IMessageQueue>();

        public static IMessageQueue CreateInbound(
            string addressName,
            MessagePattern pattern, 
            bool isTemporary = false,
            IMessageQueue originator = null )
        {
            var key = $"{Direction.Inbound}:{addressName}:{pattern}";
            if (_Queues.ContainsKey(key))
                return _Queues[key];
            
            var queue = Create( addressName, originator );
            queue.InitializeInbound(addressName, pattern, isTemporary );
            _Queues[key] = queue;

            return _Queues[key];
        }

        public static IMessageQueue CreateOutbound(
            string addressName,
            MessagePattern pattern,
            bool isTemporary = false,
            IMessageQueue originator = null )
        {
            var key = $"{Direction.Outbound}:{addressName}:{pattern}";
            if (_Queues.ContainsKey(key))
                return _Queues[key];

            var queue    = Create( addressName, originator );
            queue.InitializeOutbound(addressName, pattern, isTemporary );
            _Queues[key] = queue;

            return _Queues[key];
        }

        private static IMessageQueue Create( string name, IMessageQueue originator = null )
        {            
            if ( originator != null )
            {
                return Activator.CreateInstance( originator.GetType() ) as IMessageQueue;
            }

            var config    = MessagingConfig.CurrentMessages.SingleOrDefault( message => message.Name.Equals(name, StringComparison.OrdinalIgnoreCase) );
            var queueName = config != null
                                ? config.MessageQueueName
                                : MessagingConfig.Current.DefaultMessageQueueName;

            var queueType = MessagingConfig.CurrentMessageQueues.Type;                                                    

            var type      = Type.GetType( queueType );

            return Activator.CreateInstance( type ) as IMessageQueue;
        }

        public static void Delete(IMessageQueue queue)
        {
            queue.DeleteQueue();
            
            _Queues.Where( q => q.Value.MessageAddress == queue.MessageAddress )
                   .ToList()
                   .ForEach( q =>
                   {
                       q.Value.Dispose();
                       _Queues.Remove( q.Key );
                   }
            );
        }
    }

}