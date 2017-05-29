using MessageQueue.Messaging.Extensions;
using MessageQueue.Messaging.Specification;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using msmq = System.Messaging;

namespace MessageQueue.Messaging.Implementation.Msmq
{
    public sealed class MsmqMessageQueue : MessageQueueBase
    {        
        private const string      _multicastAddress = "MulticastAddress";
        private msmq.MessageQueue _queue;

        public override string Name => "MSMQ";

        public override void InitializeInbound(string addressName,
                                               MessagePattern pattern,
                                               bool isTemporary = false)
        {
            base.InitializeProperties(Direction.Inbound, addressName, pattern, isTemporary);

            _queue = new msmq.MessageQueue( MessageAddress.Address );

            if(pattern == MessagePattern.PublishSubscribe )
            {
                _queue.MulticastAddress = RequirePropertyValue<String>( _multicastAddress );
            }
        }

        public override void InitializeOutbound(string addressName,
                                                MessagePattern pattern,
                                                bool isTemporary = false)
        {
            InitializeProperties( Direction.Outbound, addressName, pattern, isTemporary );
            _queue = new msmq.MessageQueue( MessageAddress.Address );
        }
        
        protected override string CreateResponseQueueAddress()
        {
            var uniqueId = Guid.NewGuid().ToString().Substring( 0, 6 );
            var address  = $".\\private$\\{uniqueId}";      
            msmq.MessageQueue.Create( address );
            return address;
        }

        public override void Send(Message message)
        {
            var outbound = new msmq.Message()
            {
                BodyStream = message.ToJsonStream()
            };
            if ( !string.IsNullOrEmpty(message.ResponseAddressName))
            {
                outbound.ResponseQueue = new msmq.MessageQueue(message.ResponseAddressName);
            }
            _queue.Send(outbound);
        }
 
        public override async Task Receive(Action<Message> onMessageReceived,
                                    CancellationToken cancellationToken,
                                    int maximumWaitMilliseconds = 0 )
        {
            msmq.Message inboundMessage = 
                            maximumWaitMilliseconds > 0 
                            ? _queue.Receive( TimeSpan.FromMilliseconds(maximumWaitMilliseconds) )     
                            : _queue.Receive();               

            var message = Message.FromJson( inboundMessage.BodyStream );

            if ( cancellationToken != CancellationToken.None)
            {
                await Task.Factory.StartNew( () => onMessageReceived( message ), cancellationToken, 
                                                TaskCreationOptions.AttachedToParent,
                                                TaskScheduler.Default );
            }
            else
            {
                onMessageReceived( message );
            }
        }

        public override void DeleteQueue()
        {
            if ( !IsTemporary )
            {
                throw new InvalidOperationException( "Only temporary queues can be deleted" );
            }

            if ( msmq.MessageQueue.Exists( MessageAddress.Address ) )
            {
                msmq.MessageQueue.Delete( MessageAddress.Address );
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _queue != null)
            {
                _queue.Dispose();               
            }
        }
    }
}