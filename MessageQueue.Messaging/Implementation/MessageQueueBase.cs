using MessageQueue.Messaging.Configuration;
using MessageQueue.Messaging.Extensions;
using MessageQueue.Messaging.Specification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageQueue.Messaging.Implementation
{    
    public abstract class MessageQueueBase : IMessageQueue
    {
        protected int  _pollingInterval = 100;   
        protected bool _isListening;                    

        public abstract string Name                   { get; }
        public MessageAddress MessageAddress          { get; protected set; }

        public MessagePattern Pattern                 { get; protected set; }

        public IDictionary<string, object> Properties { get; protected set; }

        protected Direction Direction                 { get; set; }

        public bool IsTemporary                       { get; set; }

        public abstract void InitializeOutbound(string addressName, MessagePattern pattern, bool isTemporary = false);

        public abstract void InitializeInbound(string addressName, MessagePattern pattern, bool isTemporary = false);

        public abstract void Send(Message message);

        public virtual async Task Listen( Action<Message> onMessageReceived, CancellationToken cancellationToken )
        {
            if ( _isListening )
                return;

             await Task.Factory.StartNew( () => ListenInteral( onMessageReceived, cancellationToken ),
                                     cancellationToken,
                                     TaskCreationOptions.LongRunning,
                                     TaskScheduler.Default
                                  );
        }

        protected virtual async Task ListenInteral( Action<Message> onMessageReceived, CancellationToken cancellationToken )
        {
            _isListening = true;
            while ( _isListening )
            {
                Trace.WriteLine( $" Parent thread Id: {Thread.CurrentThread.ManagedThreadId}" );
                try
                {
                    if ( cancellationToken.IsCancellationRequested )
                    {
                        _isListening = false;
                        break;
                    }

                    await Receive( onMessageReceived, cancellationToken );
                    
                    cancellationToken.WaitHandle.WaitOne( _pollingInterval );
                }
                catch(Exception ex )
                {
                    Console.WriteLine( "Exception: {0}", ex );
                }
            }
        }

        public abstract Task Receive( Action<Message> onMessageReceived, 
                                            CancellationToken cancellationToken,
                                             int maximumWaitMilliseconds = 0 );

        public virtual void Receive( Action<Message> onMessageReceived, int maximumWaitMilliseconds = 0 )
        {
            Receive( onMessageReceived, CancellationToken.None, maximumWaitMilliseconds );
        }
    
        public virtual MessageAddress GetAddress(string addressName )
        {
            var config         = MessagingConfig.CurrentMessageQueues;
            var queueConfig    = config.Queues.SingleOrDefault( x => x.Name == addressName );

            var queueAddress   = queueConfig?.Address ?? addressName;
            var messageAddress = new MessageAddress( queueAddress );

            return messageAddress;
        }

        public virtual IMessageQueue GetResponseQueue()
        {
            if ( !( Pattern == MessagePattern.RequestResponse && Direction == Direction.Outbound ) )
                throw new InvalidOperationException( "Cannot get a response queue except for outbound request-response" );

            var address = CreateResponseQueueAddress();
            return MessageQueueFactory.CreateInbound( address, MessagePattern.RequestResponse, true, this );

        }

        public virtual IMessageQueue GetReplyQueue(Message message )
        {
            if(!(Pattern == MessagePattern.RequestResponse && Direction == Direction.Inbound))
                throw new InvalidOperationException( "Cannot get a reply queue except for inbound request-response" );

            return MessageQueueFactory.CreateOutbound( message.ResponseAddressName, MessagePattern.RequestResponse, true, this );
        }

        public abstract void DeleteQueue();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        protected abstract string CreateResponseQueueAddress();

        protected void InitializeProperties(Direction direction,
                                            string addressName,
                                            MessagePattern pattern,
                                            bool isTemporary=false)
        {
            Pattern        = pattern;
            Direction      = direction;
            MessageAddress = GetAddress(addressName);       
            IsTemporary    = isTemporary;
            Properties     = new Dictionary<string, object>();

            var config     = MessagingConfig.CurrentMessageQueues;

            Properties = config?.Properties?.ToDictionary(kv => kv.Key, kv => kv.Value as object);

        }

        protected T RequirePropertyValue<T>(string name)
        {
            var value = GetPropertyValue<T>(name);
            if (value.Equals(default(T)))
            {
                throw new InvalidOperationException($"Property named: {name} of type: {typeof(T).Name} is required for: {Pattern}");
            }
            return value;
        }

        protected T GetPropertyValue<T>(string name)
        {
            T value = default(T);
            var r = Properties.Count( x => x.Key == name ) == 1;
            if (Properties != null 
                && Properties.Count(x => x.Key == name) == 1
                && Properties[name].GetType() == typeof(T))
            {
                value = (T)Properties[name];
            }
            return value;
        }
    }
}